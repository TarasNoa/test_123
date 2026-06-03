using DomainGeneratedFile = Libr4.IDE.Domain.AutonomousAppGeneration.GeneratedFile;

namespace Libr4.IDE.AutonomousAppGeneration.Agents;

/// <summary>
/// Extracts and merges <see cref="GeneratedFile"/> entries from multi-agent orchestration output.
/// </summary>
public static class MultiAgentArtifactCollector
{
    public static List<DomainGeneratedFile> CollectFiles(OrchestrationResult phaseResult)
    {
        var byPath = new Dictionary<string, DomainGeneratedFile>(StringComparer.OrdinalIgnoreCase);

        foreach (var task in phaseResult.Results)
            MergeTaskResults(task, byPath);

        return byPath.Values.ToList();
    }

    public static string CollectContent(OrchestrationResult phaseResult)
    {
        var chunks = new List<string>();
        foreach (var task in phaseResult.Results)
            CollectContentRecursive(task, chunks);
        return string.Join("\n", chunks);
    }

    private static void MergeTaskResults(TaskResult task, Dictionary<string, DomainGeneratedFile> byPath)
    {
        MergeAgentContent(task.Result?.Content, byPath);

        foreach (var nested in task.NestedResults)
            MergeTaskResults(nested, byPath);
    }

    private static void CollectContentRecursive(TaskResult task, List<string> chunks)
    {
        if (task.Result is not null && !string.IsNullOrWhiteSpace(task.Result.Content))
            chunks.Add(task.Result.Content);

        foreach (var nested in task.NestedResults)
            CollectContentRecursive(nested, chunks);
    }

    private static void MergeAgentContent(string? content, Dictionary<string, DomainGeneratedFile> byPath)
    {
        if (string.IsNullOrWhiteSpace(content))
            return;

        foreach (var file in AgentGeneratedFileParser.TryParse(content))
        {
            var path = Libr4.IDE.Application.AutonomousAppGeneration.Infrastructure.StackArtifactCompleteness
                .SanitizeRelativePath(file.RelativePath);
            if (string.IsNullOrWhiteSpace(path))
                continue;

            var normalized = new DomainGeneratedFile(path, file.Language, file.Content);
            if (byPath.TryGetValue(path, out var existing))
            {
                if ((normalized.Content?.Length ?? 0) > (existing.Content?.Length ?? 0))
                    byPath[path] = normalized;
            }
            else
            {
                byPath[path] = normalized;
            }
        }
    }
}
