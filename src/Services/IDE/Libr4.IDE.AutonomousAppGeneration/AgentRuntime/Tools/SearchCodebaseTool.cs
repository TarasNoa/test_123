using System.Text.Json;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Abstractions;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Models;
using Libr4.IDE.Application.AutonomousAppGeneration.FastContext;
using Microsoft.Extensions.Options;

namespace Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Tools;

public sealed class SearchCodebaseTool : IAgentTool
{
    private const int DefaultLimit = 12;
    private const int MaxCallsPerSession = 8;

    private readonly ICodebaseIndex _index;
    private readonly FastContextOptions _options;

    public SearchCodebaseTool(ICodebaseIndex index, IOptions<FastContextOptions> options)
    {
        _index = index;
        _options = options.Value;
    }

    public string Name => "search_codebase";
    public string Description =>
        "Search the workspace codebase with fusion ranking. Input: { \"query\": \"...\", \"limit\": 12, \"include_tests\": false, \"languages\": [\"py\",\"ts\"] }";
    public bool IsReadOnly => true;

    public bool IsConcurrencySafe(JsonElement input) => true;

    public async Task<ToolExecutionResult> ExecuteAsync(JsonElement input, ToolContext context, CancellationToken ct)
    {
        var query = input.TryGetProperty("query", out var qEl) && qEl.ValueKind == JsonValueKind.String
            ? qEl.GetString()
            : null;
        if (string.IsNullOrWhiteSpace(query))
            return Fail("query is required");

        var callCount = context.Session.ReasoningLog.Count(s => s.StartsWith("search_codebase:", StringComparison.Ordinal));
        if (callCount >= MaxCallsPerSession)
            return Fail($"search_codebase budget exhausted ({MaxCallsPerSession} calls per session)");

        context.Session.ReasoningLog.Add($"search_codebase:{query}");

        var limit = input.TryGetProperty("limit", out var limitEl) && limitEl.TryGetInt32(out var l) && l > 0
            ? Math.Min(l, 24)
            : DefaultLimit;
        var includeTests = input.TryGetProperty("include_tests", out var testsEl)
                           && testsEl.ValueKind == JsonValueKind.True;
        var languages = ParseLanguages(input);

        var options = new CodebaseSearchOptions(limit, includeTests, languages);
        var root = context.Workspace.HostPath;
        var runId = context.Session.RunId;

        if (runId is not null)
            await _index.IndexAsync(root, runId, ct).ConfigureAwait(false);

        var hits = await _index.SearchAsync(root, query!, options, ct).ConfigureAwait(false);
        if (hits.Count == 0)
            return Ok("no hits");

        var lines = hits.Select((hit, i) =>
        {
            var snippet = Truncate(hit.Snippet, _options.MaxSnippetChars);
            return $"{i + 1}. {hit.Path}:{hit.StartLine}-{hit.EndLine} score={hit.Score:F3} kind={hit.MatchKind}\n{snippet}";
        });

        return Ok(string.Join("\n\n", lines));
    }

    private static IReadOnlyList<string>? ParseLanguages(JsonElement input)
    {
        if (!input.TryGetProperty("languages", out var langEl) || langEl.ValueKind != JsonValueKind.Array)
            return null;

        var langs = new List<string>();
        foreach (var item in langEl.EnumerateArray())
        {
            if (item.ValueKind == JsonValueKind.String)
            {
                var v = item.GetString();
                if (!string.IsNullOrWhiteSpace(v))
                    langs.Add(v!);
            }
        }

        return langs.Count == 0 ? null : langs;
    }

    private static string Truncate(string text, int maxChars) =>
        text.Length <= maxChars ? text : text[..maxChars] + "\n...[truncated]";

    private static ToolExecutionResult Ok(string output) =>
        new("search_codebase", true, output, Array.Empty<Domain.AutonomousAppGeneration.GeneratedFile>());

    private static ToolExecutionResult Fail(string message) =>
        new("search_codebase", false, message, Array.Empty<Domain.AutonomousAppGeneration.GeneratedFile>());
}
