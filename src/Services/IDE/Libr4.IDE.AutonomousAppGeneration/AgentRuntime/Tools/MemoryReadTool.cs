using System.Text;
using System.Text.Json;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Abstractions;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Models;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Rollout;
using Libr4.IDE.Application.AutonomousAppGeneration.Memory.Hermes;
using Microsoft.Extensions.Options;

namespace Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Tools;

/// <summary>Hermes memory_read — agent queries memory by keyword and kind.</summary>
public sealed class MemoryReadTool : IAgentTool
{
    private readonly IHermesMemoryStore _store;
    private readonly IHermesMemoryManager _manager;
    private readonly IRolloutRecorder? _rollout;
    private readonly MemoryToolOptions _options;

    public MemoryReadTool(
        IHermesMemoryStore store,
        IHermesMemoryManager manager,
        IOptions<MemoryToolOptions> options,
        IRolloutRecorder? rollout = null)
    {
        _store = store;
        _manager = manager;
        _rollout = rollout;
        _options = options.Value;
    }

    public string Name => "memory_read";
    public string Description =>
        "Query Hermes memory. Input: { \"keyword\"?, \"kind\"?, \"scope\": \"project|run|user\", \"top_k\"? }";
    public bool IsReadOnly => true;
    public bool IsConcurrencySafe(JsonElement input) => true;

    public async Task<ToolExecutionResult> ExecuteAsync(JsonElement input, ToolContext context, CancellationToken ct)
    {
        if (context.Session.RunId is not Guid runId)
            return Fail("runId required for memory_read");

        var keyword = ReadString(input, "keyword");
        var scope = ReadString(input, "scope") ?? HermesMemoryScopeResolver.Project;
        var userId = ReadString(input, "user_id");
        var topK = ReadInt(input, "top_k") ?? 8;
        topK = Math.Clamp(topK, 1, _options.MaxReadTopK);

        if (!HermesMemoryScopeResolver.IsValidScope(scope))
            return Fail("scope must be project, run, or user");

        var kindFilter = HermesMemoryScopeResolver.ParseKind(ReadString(input, "kind"));
        if (string.IsNullOrWhiteSpace(keyword) && kindFilter is null)
            return Fail("keyword or kind required");

        string fingerprint;
        try
        {
            fingerprint = HermesMemoryScopeResolver.ResolveFingerprint(scope, context, _manager, userId);
        }
        catch (InvalidOperationException ex)
        {
            return Fail(ex.Message);
        }

        var kinds = kindFilter is { } k ? new[] { k } : null;
        var results = await _store.RetrieveAsync(
            new HermesMemoryQuery(fingerprint, keyword, topK, kinds, userId),
            ct).ConfigureAwait(false);

        if (_rollout is not null)
        {
            await _rollout.RecordMemoryOperationAsync(
                runId,
                context.Session.SessionId,
                operation: "read",
                scope,
                key: keyword,
                kind: kindFilter?.ToString(),
                resultCount: results.Count,
                ct).ConfigureAwait(false);
        }

        if (results.Count == 0)
            return Ok($"memory_read: no matches (scope={scope}, fingerprint={fingerprint})");

        var sb = new StringBuilder();
        sb.AppendLine($"memory_read: {results.Count} match(es) scope={scope}");
        foreach (var result in results)
        {
            var label = HermesMemoryScoring.KindLabel(result.Entry.Kind);
            sb.AppendLine(
                $"- [{label}] {result.Entry.Key}: {Truncate(result.Entry.Summary, 240)} " +
                $"(score: {result.RelevanceScore:F2}, reason: {result.RetrievalReason})");
        }

        return Ok(sb.ToString().TrimEnd());
    }

    private static string? ReadString(JsonElement input, string property) =>
        input.TryGetProperty(property, out var el) && el.ValueKind == JsonValueKind.String
            ? el.GetString()
            : null;

    private static int? ReadInt(JsonElement input, string property) =>
        input.TryGetProperty(property, out var el) && el.ValueKind == JsonValueKind.Number && el.TryGetInt32(out var value)
            ? value
            : null;

    private static string Truncate(string value, int max) =>
        value.Length <= max ? value : value[..max] + "...";

    private static ToolExecutionResult Ok(string output) =>
        new("memory_read", true, output, Array.Empty<Domain.AutonomousAppGeneration.GeneratedFile>());

    private static ToolExecutionResult Fail(string msg) =>
        new("memory_read", false, msg, Array.Empty<Domain.AutonomousAppGeneration.GeneratedFile>());
}
