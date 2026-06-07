using Libr4.IDE.Domain.AutonomousAppGeneration;

namespace Libr4.IDE.Application.AutonomousAppGeneration.Context.RepoGraph;

public interface IRepoContextFormatter
{
    string FormatFile(string relativePath, string content);

    string BuildRelatedContext(
        IReadOnlyList<GeneratedFile> workspaceFiles,
        IRepoGraphBuilder graphBuilder,
        int maxChars,
        IReadOnlyList<string>? focusPaths = null,
        int maxFiles = 24);
}

public sealed class RepoContextFormatter : IRepoContextFormatter
{
    public string FormatFile(string relativePath, string content) =>
        $"#{relativePath}\n{content}\n\n";

    public string BuildRelatedContext(
        IReadOnlyList<GeneratedFile> workspaceFiles,
        IRepoGraphBuilder graphBuilder,
        int maxChars,
        IReadOnlyList<string>? focusPaths = null,
        int maxFiles = 24)
    {
        if (workspaceFiles.Count == 0 || maxChars <= 0)
            return string.Empty;

        var byPath = workspaceFiles.ToDictionary(
            f => f.RelativePath.Replace('\\', '/'),
            f => f,
            StringComparer.OrdinalIgnoreCase);

        var candidatePaths = ResolveCandidatePaths(byPath.Keys.ToList(), graphBuilder, focusPaths);
        var contents = candidatePaths.ToDictionary(
            p => p,
            p => byPath[p].Content,
            StringComparer.OrdinalIgnoreCase);
        var ordered = graphBuilder.OrderForGeneration(candidatePaths, contents);

        var sb = new System.Text.StringBuilder();
        var added = 0;
        foreach (var path in ordered)
        {
            if (added >= maxFiles)
                break;
            if (!byPath.TryGetValue(path, out var file))
                continue;

            var chunk = FormatFile(file.RelativePath, file.Content);
            if (sb.Length > 0 && sb.Length + chunk.Length > maxChars)
                break;

            sb.Append(chunk);
            added++;
        }

        return sb.ToString();
    }

    private static IReadOnlyList<string> ResolveCandidatePaths(
        IReadOnlyList<string> workspacePaths,
        IRepoGraphBuilder graphBuilder,
        IReadOnlyList<string>? focusPaths)
    {
        if (focusPaths is not { Count: > 0 })
            return workspacePaths;

        var pathSet = workspacePaths.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var graph = graphBuilder.Build(workspacePaths);
        var needed = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var focus in focusPaths)
        {
            var normalized = focus.Replace('\\', '/');
            if (pathSet.Contains(normalized))
                needed.Add(normalized);

            foreach (var edge in graph.Edges)
            {
                if (!edge.FromPath.Equals(normalized, StringComparison.OrdinalIgnoreCase))
                    continue;
                if (pathSet.Contains(edge.ToPath))
                    needed.Add(edge.ToPath);
            }
        }

        return needed.Count == 0 ? workspacePaths : needed.ToList();
    }
}
