using System.Text.Json;
using Libr4.IDE.Domain.AutonomousAppGeneration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Libr4.IDE.Application.AutonomousAppGeneration.Memory.Extraction;

public sealed class LlmPostRunExtractor
{
    private readonly IServiceScopeFactory _scopes;
    private readonly HeuristicPostRunExtractor _fallback;
    private readonly ILogger<LlmPostRunExtractor> _logger;

    public LlmPostRunExtractor(
        IServiceScopeFactory scopes,
        ILogger<LlmPostRunExtractor> logger,
        HeuristicPostRunExtractor? fallback = null)
    {
        _scopes = scopes;
        _logger = logger;
        _fallback = fallback ?? new HeuristicPostRunExtractor();
    }

    public async Task<PostRunExtractionResult> ExtractAsync(
        PostRunExtractionRequest request,
        CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        var errors = string.Join(
            "\n",
            request.Errors.Select(e => $"- {e.ErrorType}: {e.Message} @ {e.FilePath}"));
        var rollout = string.Join("\n", request.RolloutLines.Take(32));

        var prompt =
            "Extract post-run lessons for an autonomous app generation agent.\n" +
            "Return JSON only: {\"lessons\":[{\"key\":\"...\",\"summary\":\"...\",\"kind\":\"episodic|procedural|semantic|strategic|meta\",\"confidence\":0.0}]}\n\n" +
            $"status={request.Status}\n" +
            $"failure_reason={request.FailureReason}\n" +
            $"stack={request.StackPattern}\n" +
            $"iterations={request.IterationCount}\n\n" +
            "ERRORS:\n" + (string.IsNullOrWhiteSpace(errors) ? "(none)" : errors) + "\n\n" +
            "ROLLOUT:\n" + (string.IsNullOrWhiteSpace(rollout) ? "(none)" : rollout);

        try
        {
            using var scope = _scopes.CreateScope();
            var ai = scope.ServiceProvider.GetRequiredService<Libr4.AI.Application.Abstractions.IAIService>();
            var raw = await ai.GenerateCompletionAsync(prompt, "Return JSON only.").ConfigureAwait(false);
            var lessons = ParseLessons(raw);
            if (lessons.Count == 0)
                return _fallback.Extract(request);

            return new PostRunExtractionResult(
                request.RunId,
                request.Status.ToString(),
                lessons,
                "llm");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "LLM post-run extraction failed for run {RunId}; using heuristic fallback", request.RunId);
            return _fallback.Extract(request);
        }
    }

    private static IReadOnlyList<PostRunLesson> ParseLessons(string raw)
    {
        var json = ExtractJson(raw);
        using var doc = JsonDocument.Parse(json);
        if (!doc.RootElement.TryGetProperty("lessons", out var arr) || arr.ValueKind != JsonValueKind.Array)
            return Array.Empty<PostRunLesson>();

        var lessons = new List<PostRunLesson>();
        foreach (var item in arr.EnumerateArray())
        {
            var key = item.TryGetProperty("key", out var k) && k.ValueKind == JsonValueKind.String ? k.GetString() : null;
            var summary = item.TryGetProperty("summary", out var s) && s.ValueKind == JsonValueKind.String ? s.GetString() : null;
            if (string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(summary))
                continue;

            var kind = ParseKind(item.TryGetProperty("kind", out var kindEl) ? kindEl.GetString() : null);
            var confidence = item.TryGetProperty("confidence", out var c) && c.TryGetDouble(out var score) ? score : 0.8;
            lessons.Add(new PostRunLesson(key, summary, kind, confidence));
        }

        return lessons;
    }

    private static MemoryKind ParseKind(string? raw) => (raw ?? string.Empty).Trim().ToLowerInvariant() switch
    {
        "procedural" or "l1" => MemoryKind.Procedural,
        "semantic" or "l2" => MemoryKind.Semantic,
        "strategic" or "l3" => MemoryKind.Strategic,
        "meta" or "l4" => MemoryKind.Meta,
        _ => MemoryKind.Episodic
    };

    private static string ExtractJson(string raw)
    {
        var start = raw.IndexOf('{');
        var end = raw.LastIndexOf('}');
        if (start < 0 || end <= start)
            throw new InvalidOperationException("No JSON object in LLM response");
        return raw[start..(end + 1)];
    }
}
