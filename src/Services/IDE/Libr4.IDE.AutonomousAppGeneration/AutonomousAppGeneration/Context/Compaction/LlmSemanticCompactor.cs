using System.Text.Json;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Libr4.IDE.Application.AutonomousAppGeneration.Context.Compaction;

public sealed class LlmSemanticCompactor : ISemanticCompactor
{
    private readonly IServiceScopeFactory _scopes;
    private readonly HeuristicSemanticCompactor _fallback;
    private readonly ILogger<LlmSemanticCompactor> _logger;

    public LlmSemanticCompactor(
        IServiceScopeFactory scopes,
        ILogger<LlmSemanticCompactor> logger,
        HeuristicSemanticCompactor? fallback = null)
    {
        _scopes = scopes;
        _logger = logger;
        _fallback = fallback ?? new HeuristicSemanticCompactor();
    }

    public async Task<SemanticCompactionSummary> SummarizeAsync(
        IReadOnlyList<AgentConversationTurn> turnsToSummarize,
        IReadOnlyList<string> manifestPaths,
        CancellationToken ct = default)
    {
        if (turnsToSummarize.Count == 0)
            return new SemanticCompactionSummary([], manifestPaths.ToArray(), [], [], []);

        var transcript = string.Join(
            "\n",
            turnsToSummarize.Select(t => $"[{t.Role}] {Truncate(t.Content, 1200)}"));

        var prompt =
            "Summarize this agent repair transcript into JSON only.\n" +
            "Schema: {\"decisions\":[],\"filesTouched\":[],\"openIssues\":[],\"nextActions\":[],\"errorsResolved\":[]}\n\n" +
            $"Preserve manifest paths: {string.Join(", ", manifestPaths.Take(24))}\n\n" +
            "TRANSCRIPT:\n" +
            transcript;

        try
        {
            using var scope = _scopes.CreateScope();
            var ai = scope.ServiceProvider.GetRequiredService<Libr4.AI.Application.Abstractions.IAIService>();
            var raw = await ai.GenerateCompletionAsync(prompt, "Return JSON only.").ConfigureAwait(false);
            var json = ExtractJson(raw);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            return new SemanticCompactionSummary(
                ReadArray(root, "decisions"),
                MergePaths(ReadArray(root, "filesTouched"), manifestPaths),
                ReadArray(root, "openIssues"),
                ReadArray(root, "nextActions"),
                ReadArray(root, "errorsResolved"));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "LLM semantic compaction failed; using heuristic fallback");
            return await _fallback.SummarizeAsync(turnsToSummarize, manifestPaths, ct).ConfigureAwait(false);
        }
    }

    private static IReadOnlyList<string> ReadArray(JsonElement root, string name) =>
        root.TryGetProperty(name, out var arr) && arr.ValueKind == JsonValueKind.Array
            ? arr.EnumerateArray()
                .Where(e => e.ValueKind == JsonValueKind.String)
                .Select(e => e.GetString() ?? string.Empty)
                .Where(s => s.Length > 0)
                .ToArray()
            : Array.Empty<string>();

    private static IReadOnlyList<string> MergePaths(IReadOnlyList<string> fromModel, IReadOnlyList<string> manifest)
    {
        var set = new HashSet<string>(manifest, StringComparer.OrdinalIgnoreCase);
        foreach (var path in fromModel)
            set.Add(path);
        return set.Take(32).ToArray();
    }

    private static string ExtractJson(string raw)
    {
        var start = raw.IndexOf('{');
        var end = raw.LastIndexOf('}');
        if (start >= 0 && end > start)
            return raw[start..(end + 1)];
        return raw;
    }

    private static string Truncate(string text, int max) =>
        text.Length <= max ? text : text[..max] + "…";
}
