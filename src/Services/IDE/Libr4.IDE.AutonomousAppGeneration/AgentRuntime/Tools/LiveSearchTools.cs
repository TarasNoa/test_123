using System.Text.Json;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Abstractions;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Models;
using Libr4.IDE.Application.AutonomousAppGeneration.LiveSearch;
using Libr4.IDE.Domain.AutonomousAppGeneration;

namespace Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Tools;

public sealed class SearchWebTool : IAgentTool
{
    private readonly ILiveSearchService _search;

    public SearchWebTool(ILiveSearchService search) => _search = search;

    public string Name => "search_web";
    public string Description =>
        "Search the public web for fresh information. Input: { \"query\", \"max_results\"? }";
    public bool IsReadOnly => true;
    public bool IsConcurrencySafe(JsonElement input) => true;

    public async Task<ToolExecutionResult> ExecuteAsync(JsonElement input, ToolContext context, CancellationToken ct)
    {
        var query = ReadString(input, "query");
        if (string.IsNullOrWhiteSpace(query))
            return Fail("query is required");

        var maxResults = ReadInt(input, "max_results");
        try
        {
            var response = await _search.SearchWebAsync(
                new LiveSearchRequest(query!, context.Session.SessionId, maxResults),
                ct).ConfigureAwait(false);
            return Ok(FormatResponse(response));
        }
        catch (InvalidOperationException ex)
        {
            return Fail(ex.Message);
        }
    }

    internal static string FormatResponse(LiveSearchResponse response)
    {
        if (response.Hits.Count == 0)
            return $"(no results for `{response.Query}` via {response.Provider})";

        var lines = new List<string>
        {
            $"query: {response.Query}",
            $"provider: {response.Provider}",
            response.FromCache ? "cache: hit" : "cache: miss"
        };
        if (!string.IsNullOrWhiteSpace(response.TruncationNotice))
            lines.Add(response.TruncationNotice);

        var index = 1;
        foreach (var hit in response.Hits)
        {
            lines.Add($"{index}. {hit.Title}");
            if (!string.IsNullOrWhiteSpace(hit.Url))
                lines.Add($"   url: {hit.Url}");
            lines.Add($"   snippet: {hit.Snippet}");
            index++;
        }

        return string.Join('\n', lines);
    }

    private static string? ReadString(JsonElement input, string name) =>
        input.TryGetProperty(name, out var el) && el.ValueKind == JsonValueKind.String ? el.GetString() : null;

    private static int? ReadInt(JsonElement input, string name) =>
        input.TryGetProperty(name, out var el) && el.ValueKind == JsonValueKind.Number ? el.GetInt32() : null;

    private ToolExecutionResult Ok(string output) => new(Name, true, output, Array.Empty<GeneratedFile>());
    private ToolExecutionResult Fail(string reason) => new(Name, false, reason, Array.Empty<GeneratedFile>());
}

public sealed class SearchXTool : IAgentTool
{
    private readonly ILiveSearchService _search;

    public SearchXTool(ILiveSearchService search) => _search = search;

    public string Name => "search_x";
    public string Description =>
        "Search recent posts on X (Twitter). Requires API key. Input: { \"query\", \"max_results\"? }";
    public bool IsReadOnly => true;
    public bool IsConcurrencySafe(JsonElement input) => true;

    public async Task<ToolExecutionResult> ExecuteAsync(JsonElement input, ToolContext context, CancellationToken ct)
    {
        var query = ReadString(input, "query");
        if (string.IsNullOrWhiteSpace(query))
            return Fail("query is required");

        var maxResults = ReadInt(input, "max_results");
        try
        {
            var response = await _search.SearchXAsync(
                new LiveSearchRequest(query!, context.Session.SessionId, maxResults),
                ct).ConfigureAwait(false);
            return new ToolExecutionResult(Name, true, SearchWebTool.FormatResponse(response), Array.Empty<GeneratedFile>());
        }
        catch (InvalidOperationException ex)
        {
            return Fail(ex.Message);
        }
    }

    private static string? ReadString(JsonElement input, string name) =>
        input.TryGetProperty(name, out var el) && el.ValueKind == JsonValueKind.String ? el.GetString() : null;

    private static int? ReadInt(JsonElement input, string name) =>
        input.TryGetProperty(name, out var el) && el.ValueKind == JsonValueKind.Number ? el.GetInt32() : null;

    private ToolExecutionResult Fail(string reason) => new(Name, false, reason, Array.Empty<GeneratedFile>());
}
