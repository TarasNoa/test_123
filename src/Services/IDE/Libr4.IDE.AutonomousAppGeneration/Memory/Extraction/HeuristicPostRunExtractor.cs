using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Playbook;
using Libr4.IDE.Domain.AutonomousAppGeneration;

namespace Libr4.IDE.Application.AutonomousAppGeneration.Memory.Extraction;

/// <summary>Deterministic lesson extraction — safe for tests and LLM fallback.</summary>
public sealed class HeuristicPostRunExtractor
{
    public PostRunExtractionResult Extract(PostRunExtractionRequest request)
    {
        var lessons = new List<PostRunLesson>();
        var succeeded = request.Status == GenerationStatus.Completed;

        if (!string.IsNullOrWhiteSpace(request.StackPattern))
        {
            lessons.Add(new PostRunLesson(
                $"stack:{request.ApplicationName ?? "app"}",
                $"Stack pattern: {request.StackPattern}",
                MemoryKind.Semantic,
                0.8));
        }

        if (succeeded)
        {
            lessons.Add(new PostRunLesson(
                $"success:{request.RunId:N}",
                $"Run completed after {request.IterationCount} iteration(s) for {request.ApplicationName ?? "app"}.",
                MemoryKind.Strategic,
                0.9));
        }
        else
        {
            var reason = request.FailureReason ?? "unknown_failure";
            lessons.Add(new PostRunLesson(
                $"failure:{request.RunId:N}",
                $"Run failed: {Truncate(reason, 480)}",
                MemoryKind.Meta,
                0.85));

            foreach (var error in request.Errors.Take(6))
            {
                lessons.Add(new PostRunLesson(
                    $"error:{RepairPlaybookSignature.FromError(error)[..12]}",
                    $"{error.ErrorType}: {Truncate(error.Message, 220)} @ {error.FilePath}",
                    MemoryKind.Episodic,
                    0.7));
            }
        }

        foreach (var line in request.RolloutLines.Where(l => l.Contains("tool=", StringComparison.OrdinalIgnoreCase)))
        {
            if (!line.Contains("success=True", StringComparison.OrdinalIgnoreCase))
                continue;

            lessons.Add(new PostRunLesson(
                $"tool:{ExtractToolName(line)}",
                Truncate(line, 300),
                MemoryKind.Procedural,
                0.75));
        }

        return new PostRunExtractionResult(
            request.RunId,
            request.Status.ToString(),
            lessons
                .DistinctBy(l => l.Key, StringComparer.OrdinalIgnoreCase)
                .Take(12)
                .ToArray(),
            "heuristic");
    }

    private static string ExtractToolName(string line)
    {
        const string marker = "tool=";
        var idx = line.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
        if (idx < 0)
            return "unknown";
        var rest = line[(idx + marker.Length)..];
        var end = rest.IndexOf(' ');
        return end < 0 ? rest : rest[..end];
    }

    private static string Truncate(string value, int max) =>
        value.Length <= max ? value : value[..max] + "...";
}
