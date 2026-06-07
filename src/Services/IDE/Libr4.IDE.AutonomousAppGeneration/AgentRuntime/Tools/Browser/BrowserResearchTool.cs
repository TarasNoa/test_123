using System.Text;
using System.Text.Json;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Abstractions;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Models;
using Libr4.IDE.Application.Obscura;

namespace Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Tools.Browser;

/// <summary>
/// Multi-URL web research via <see cref="IAgentObscuraTool.ResearchAsync"/>.
/// Enables stealth mode automatically for external URLs.
/// </summary>
public sealed class BrowserResearchTool : IAgentTool
{
    private readonly IAgentObscuraTool _obscura;

    public BrowserResearchTool(IAgentObscuraTool obscura) => _obscura = obscura;

    public string Name => BrowserToolNames.Research;
    public string Description =>
        "Research multiple URLs for a query. Input: { \"query\": \"...\", \"sources\": [\"https://...\"], \"max_sources\": 3, \"stealth_mode\": true }";
    public bool IsReadOnly => true;
    public bool IsConcurrencySafe(JsonElement input) => false;

    public async Task<ToolExecutionResult> ExecuteAsync(JsonElement input, ToolContext context, CancellationToken ct)
    {
        var query = ObscuraBrowserToolFacade.GetString(input, "query");
        if (string.IsNullOrWhiteSpace(query))
            return ObscuraBrowserToolFacade.Fail(Name, "query is required");

        var sources = ParseSources(input);
        if (sources.Count == 0)
            return ObscuraBrowserToolFacade.Fail(Name, "sources array with at least one URL is required");

        var maxSources = ObscuraBrowserToolFacade.GetInt(input, "max_sources", 5);
        var stealth = BrowserUrlClassifier.ResolveStealthMode(input, sources);

        var result = await _obscura.ResearchAsync(
            query,
            sources.ToArray(),
            new WebResearchOptions
            {
                StealthMode = stealth,
                MaxSources = Math.Clamp(maxSources, 1, 10),
                RunId = context.Session.RunId?.ToString("D")
            },
            ct).ConfigureAwait(false);

        return ObscuraBrowserToolFacade.Ok(Name, FormatSummary(result, stealth));
    }

    private static List<string> ParseSources(JsonElement input)
    {
        if (!input.TryGetProperty("sources", out var sourcesEl) || sourcesEl.ValueKind != JsonValueKind.Array)
            return [];

        return sourcesEl.EnumerateArray()
            .Where(e => e.ValueKind == JsonValueKind.String)
            .Select(e => e.GetString() ?? string.Empty)
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    internal static string FormatSummary(WebResearchResult result, bool stealthMode)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"query={result.Query}");
        sb.AppendLine($"stealth_mode={stealthMode}");
        sb.AppendLine($"successful={result.SuccessfulSources}/{result.TotalSourcesChecked}");
        foreach (var source in result.Sources)
        {
            sb.AppendLine($"--- {source.Url}");
            if (!string.IsNullOrWhiteSpace(source.Title))
                sb.AppendLine($"title={source.Title}");
            sb.AppendLine($"relevance={source.RelevanceScore:F2}");
            var snippet = source.Content.Length > 800 ? source.Content[..800] + "…" : source.Content;
            if (!string.IsNullOrWhiteSpace(snippet))
                sb.AppendLine(snippet);
        }

        return sb.ToString().TrimEnd();
    }
}
