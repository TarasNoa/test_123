using System.Text.Json;
using System.Text.RegularExpressions;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Abstractions;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Models;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Rollout;
using Libr4.IDE.Application.AutonomousAppGeneration.Memory.Hermes;
using Libr4.IDE.Domain.AutonomousAppGeneration;
using Microsoft.Extensions.Options;

namespace Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Tools;

/// <summary>Hermes memory_write — agent explicitly saves a lesson or pattern.</summary>
public sealed class MemoryWriteTool : IAgentTool
{
    private static readonly Regex KeyPattern = new(@"^[a-zA-Z0-9._:/-]+$", RegexOptions.Compiled);

    private readonly IHermesMemoryStore _store;
    private readonly IHermesMemoryManager _manager;
    private readonly IRolloutRecorder? _rollout;
    private readonly MemoryToolOptions _options;

    public MemoryWriteTool(
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

    public string Name => "memory_write";
    public string Description =>
        "Save a lesson or repair pattern to Hermes memory. Input: { \"key\", \"summary\", \"scope\": \"project|run|user\", \"kind\": \"procedural|semantic|...\", \"payload\"? }";
    public bool IsReadOnly => false;
    public bool IsConcurrencySafe(JsonElement input) => false;

    public async Task<ToolExecutionResult> ExecuteAsync(JsonElement input, ToolContext context, CancellationToken ct)
    {
        if (context.Session.RunId is not Guid runId)
            return Fail("runId required for memory_write");

        if (context.Session.MemoryWriteCount >= _options.MaxWritesPerSession)
            return Fail($"session memory_write limit reached ({_options.MaxWritesPerSession})");

        var key = ReadString(input, "key");
        var summary = ReadString(input, "summary");
        var scope = ReadString(input, "scope") ?? HermesMemoryScopeResolver.Project;
        var payload = ReadString(input, "payload");
        var userId = ReadString(input, "user_id");

        if (string.IsNullOrWhiteSpace(key))
            return Fail("key required");
        if (string.IsNullOrWhiteSpace(summary))
            return Fail("summary required");
        if (!HermesMemoryScopeResolver.IsValidScope(scope))
            return Fail("scope must be project, run, or user");
        if (key.Length > _options.MaxKeyChars)
            return Fail($"key exceeds {_options.MaxKeyChars} chars");
        if (!KeyPattern.IsMatch(key))
            return Fail("key must match [a-zA-Z0-9._:/-]+");
        if (summary.Length > _options.MaxSummaryChars)
            return Fail($"summary exceeds {_options.MaxSummaryChars} chars");
        if (payload is { Length: > 0 } && payload.Length > _options.MaxPayloadChars)
            return Fail($"payload exceeds {_options.MaxPayloadChars} chars");

        var kind = HermesMemoryScopeResolver.ParseKind(ReadString(input, "kind")) ?? MemoryKind.Procedural;

        string fingerprint;
        try
        {
            fingerprint = HermesMemoryScopeResolver.ResolveFingerprint(scope, context, _manager, userId);
        }
        catch (InvalidOperationException ex)
        {
            return Fail(ex.Message);
        }

        var stage = context.Mode == AgentSessionMode.Generation ? "generation" : "repair";
        var tokens = Math.Max(1, (summary.Length + (payload?.Length ?? 0)) / 4);

        await _store.UpsertAsync(
            new HermesMemoryEntry(
                Guid.NewGuid(),
                runId,
                userId,
                fingerprint,
                kind,
                stage,
                key,
                summary,
                payload,
                tokens,
                Score: 1.0,
                DateTime.UtcNow),
            ct).ConfigureAwait(false);

        context.Session.MemoryWriteCount++;

        if (_rollout is not null)
        {
            await _rollout.RecordMemoryOperationAsync(
                runId,
                context.Session.SessionId,
                operation: "write",
                scope,
                key,
                kind.ToString(),
                resultCount: 1,
                ct).ConfigureAwait(false);
        }

        return Ok($"""
            memory_write ok
            scope={scope}
            fingerprint={fingerprint}
            kind={HermesMemoryScoring.KindLabel(kind)}
            key={key}
            """);
    }

    private static string? ReadString(JsonElement input, string property) =>
        input.TryGetProperty(property, out var el) && el.ValueKind == JsonValueKind.String
            ? el.GetString()
            : null;

    private static ToolExecutionResult Ok(string output) =>
        new("memory_write", true, output, Array.Empty<GeneratedFile>());

    private static ToolExecutionResult Fail(string msg) =>
        new("memory_write", false, msg, Array.Empty<GeneratedFile>());
}
