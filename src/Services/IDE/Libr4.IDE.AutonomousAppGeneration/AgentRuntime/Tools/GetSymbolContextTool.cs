using System.Text.Json;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Abstractions;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Models;
using Libr4.IDE.Application.AutonomousAppGeneration.FastContext;
using Microsoft.Extensions.Options;

namespace Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Tools;

public sealed class GetSymbolContextTool : IAgentTool
{
    private readonly ICodebaseIndex _index;
    private readonly FastContextOptions _options;

    public GetSymbolContextTool(ICodebaseIndex index, IOptions<FastContextOptions> options)
    {
        _index = index;
        _options = options.Value;
    }

    public string Name => "get_symbol_context";
    public string Description =>
        "Resolve a symbol to its defining file and surrounding context. Input: { \"symbol\": \"UserService\", \"path_hint\": \"backend/\" }";
    public bool IsReadOnly => true;

    public bool IsConcurrencySafe(JsonElement input) => true;

    public async Task<ToolExecutionResult> ExecuteAsync(JsonElement input, ToolContext context, CancellationToken ct)
    {
        var symbol = input.TryGetProperty("symbol", out var symEl) && symEl.ValueKind == JsonValueKind.String
            ? symEl.GetString()
            : null;
        if (string.IsNullOrWhiteSpace(symbol))
            return Fail("symbol is required");

        var pathHint = input.TryGetProperty("path_hint", out var hintEl) && hintEl.ValueKind == JsonValueKind.String
            ? hintEl.GetString()
            : null;

        var root = context.Workspace.HostPath;
        var runId = context.Session.RunId;
        if (runId is not null)
            await _index.IndexAsync(root, runId, ct).ConfigureAwait(false);

        var result = await _index.GetSymbolAsync(root, symbol!, pathHint, ct).ConfigureAwait(false);
        if (result is null)
            return Fail($"symbol not found: {symbol}");

        var snippet = Truncate(result.Snippet, _options.MaxSnippetChars);
        var related = result.RelatedPaths.Count == 0
            ? string.Empty
            : $"\nrelated: {string.Join(", ", result.RelatedPaths)}";

        var output =
            $"{result.Path}:{result.StartLine}-{result.EndLine}\n{snippet}{related}";

        return Ok(output);
    }

    private static string Truncate(string text, int maxChars) =>
        text.Length <= maxChars ? text : text[..maxChars] + "\n...[truncated]";

    private static ToolExecutionResult Ok(string output) =>
        new("get_symbol_context", true, output, Array.Empty<Domain.AutonomousAppGeneration.GeneratedFile>());

    private static ToolExecutionResult Fail(string message) =>
        new("get_symbol_context", false, message, Array.Empty<Domain.AutonomousAppGeneration.GeneratedFile>());
}
