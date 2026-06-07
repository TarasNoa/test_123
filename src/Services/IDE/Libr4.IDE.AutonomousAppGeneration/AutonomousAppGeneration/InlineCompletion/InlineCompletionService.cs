using Libr4.AI.Application.Abstractions;
using Libr4.IDE.Application.AutonomousAppGeneration.Context.RepoGraph;
using Libr4.IDE.Application.AutonomousAppGeneration.Infrastructure.Fim;
using Libr4.IDE.Application.AutonomousAppGeneration.ModelRouting;
using Libr4.IDE.Application.AutonomousAppGeneration.WorkspaceTrust;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Libr4.IDE.Application.AutonomousAppGeneration.InlineCompletion;

public sealed class InlineCompletionService : IInlineCompletionService
{
    private readonly IAIService _ai;
    private readonly IAgentModelRouter _modelRouter;
    private readonly IRepoGraphBuilder? _repoGraph;
    private readonly IWorkspaceTrustRunGate? _workspaceTrust;
    private readonly InlineCompletionOptions _options;
    private readonly ILogger<InlineCompletionService> _logger;

    public InlineCompletionService(
        IAIService ai,
        IAgentModelRouter modelRouter,
        IOptions<InlineCompletionOptions> options,
        ILogger<InlineCompletionService> logger,
        IRepoGraphBuilder? repoGraph = null,
        IWorkspaceTrustRunGate? workspaceTrust = null)
    {
        _ai = ai;
        _modelRouter = modelRouter;
        _repoGraph = repoGraph;
        _workspaceTrust = workspaceTrust;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<InlineCompletionResult> CompleteAsync(
        InlineCompletionRequest request,
        CancellationToken ct = default)
    {
        var started = Environment.TickCount64;

        if (!_options.Enabled)
            return Suppressed(started, "disabled");

        if (request.SuppressWhileAgentRunning)
            return Suppressed(started, "agent_running");

        if (request.RunId is Guid runId && _workspaceTrust?.DenyCloudInference(runId) == true)
            return Suppressed(started, "deny_cloud_inference");

        if (string.IsNullOrWhiteSpace(request.FileContent))
            return Empty(started);

        if (!TrySplitAtCursor(request, out var prefix, out var suffix))
            return Empty(started);

        var related = BuildRelatedImportsHint(request);
        var intent = string.IsNullOrWhiteSpace(request.SessionIntent)
            ? string.Empty
            : $"\nAgent intent: {request.SessionIntent.Trim()}";

        var fimBody = string.IsNullOrEmpty(suffix)
            ? $"{prefix}\n{FimPromptBuilder.HoleMarker}"
            : $"{prefix}\n{FimPromptBuilder.HoleMarker}\n{suffix}";

        var systemPrompt =
            "You are a code completion engine. Return ONLY the text that belongs at the cursor hole marker. " +
            "No markdown fences, no explanation, no repetition of existing code.";

        var userPrompt = $"""
            File: {request.FilePath}
            Language: {request.Language}
            {related}{intent}

            Complete at <|fim_hole|>:
            {fimBody}
            """;

        var route = _modelRouter.Route(_options.ModelRole);
        var model = route.PrimaryModel;

        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeoutCts.CancelAfter(TimeSpan.FromMilliseconds(Math.Clamp(_options.MaxLatencyMs, 100, 10000)));

        try
        {
            var raw = await _ai.GenerateCompletionAsync(userPrompt, systemPrompt, model)
                .WaitAsync(timeoutCts.Token)
                .ConfigureAwait(false);

            var text = NormalizeCompletion(raw, prefix, suffix);
            if (string.IsNullOrWhiteSpace(text))
                return Empty(started);

            var max = Math.Clamp(_options.MaxCompletionChars, 32, 4096);
            if (text.Length > max)
                text = text[..max];

            var latency = (int)(Environment.TickCount64 - started);
            return new InlineCompletionResult(text, false, null, latency, model);
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            _logger.LogDebug("[InlineCompletion] Timed out after {Ms}ms file={File}", _options.MaxLatencyMs, request.FilePath);
            return Suppressed(started, "timeout");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[InlineCompletion] Failed file={File}", request.FilePath);
            return Suppressed(started, "error");
        }
    }

    private string BuildRelatedImportsHint(InlineCompletionRequest request)
    {
        if (_repoGraph is null || _options.MaxRelatedImportLines <= 0)
            return string.Empty;

        try
        {
            var contents = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                [request.FilePath] = request.FileContent
            };
            var graph = _repoGraph.Build(new[] { request.FilePath }, contents);
            if (graph.Edges.Count == 0)
                return string.Empty;

            var related = graph.Edges
                .Where(e => e.FromPath.Equals(request.FilePath, StringComparison.OrdinalIgnoreCase)
                            || e.ToPath.Equals(request.FilePath, StringComparison.OrdinalIgnoreCase))
                .Select(e => e.FromPath.Equals(request.FilePath, StringComparison.OrdinalIgnoreCase) ? e.ToPath : e.FromPath)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Take(4)
                .ToList();

            if (related.Count == 0)
                return string.Empty;

            return $"\nRelated files: {string.Join(", ", related)}";
        }
        catch
        {
            return string.Empty;
        }
    }

    private static bool TrySplitAtCursor(
        InlineCompletionRequest request,
        out string prefix,
        out string suffix)
    {
        prefix = string.Empty;
        suffix = string.Empty;

        var normalized = request.FileContent.Replace("\r\n", "\n");
        var lines = normalized.Split('\n');
        if (request.Line < 1 || request.Line > lines.Length)
            return false;

        var lineIndex = request.Line - 1;
        var colIndex = Math.Clamp(request.Column - 1, 0, lines[lineIndex].Length);
        var beforeLines = lines.Take(lineIndex).ToList();
        var currentLine = lines[lineIndex];
        var afterLines = lines.Skip(lineIndex + 1).ToList();

        beforeLines.Add(currentLine[..colIndex]);
        afterLines.Insert(0, currentLine[colIndex..]);

        prefix = string.Join('\n', beforeLines);
        suffix = string.Join('\n', afterLines);
        return true;
    }

    private static string NormalizeCompletion(string raw, string prefix, string suffix)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return string.Empty;

        var text = raw.Trim();
        if (text.StartsWith("```", StringComparison.Ordinal))
        {
            var end = text.LastIndexOf("```", StringComparison.Ordinal);
            if (end > 3)
                text = text[3..end].TrimStart('\n');
        }

        // Drop if model repeated entire prefix/suffix.
        if (!string.IsNullOrEmpty(prefix) && text.StartsWith(prefix, StringComparison.Ordinal))
            text = text[prefix.Length..];
        if (!string.IsNullOrEmpty(suffix) && text.EndsWith(suffix, StringComparison.Ordinal))
            text = text[..^suffix.Length];

        return text.TrimEnd('\r', '\n');
    }

    private static InlineCompletionResult Empty(long started) =>
        new(null, false, null, (int)(Environment.TickCount64 - started), null);

    private static InlineCompletionResult Suppressed(long started, string reason) =>
        new(null, true, reason, (int)(Environment.TickCount64 - started), null);
}
