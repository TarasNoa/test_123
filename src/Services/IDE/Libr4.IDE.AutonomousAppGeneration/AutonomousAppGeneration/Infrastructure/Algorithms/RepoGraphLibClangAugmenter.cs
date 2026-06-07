using System.Collections.ObjectModel;
using Libr4.IDE.Application.AutonomousAppGeneration.Infrastructure.Cpp;
using Libr4.IDE.AutonomousAppGeneration.Algorithms.RepoGraph;
using Microsoft.Extensions.Logging.Abstractions;

namespace Libr4.IDE.Application.AutonomousAppGeneration.Infrastructure.Algorithms;

/// <summary>Wave 6.3: augment F# RepoGraph with libclang #include edges for C/C++ files.</summary>
internal static class RepoGraphLibClangAugmenter
{
    private static readonly HashSet<string> CppExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".c", ".cc", ".cpp", ".cxx", ".h", ".hh", ".hpp", ".hxx", ".m", ".mm"
    };

    public static RepoGraphEngine.RepoGraphDto Augment(
        RepoGraphEngine.RepoGraphDto graph,
        IReadOnlyList<string> relativePaths,
        IReadOnlyDictionary<string, string>? contentsByPath)
    {
        if (!CppLibClangBridge.IsAvailable || contentsByPath is null || contentsByPath.Count == 0)
            return graph;

        var pathSet = new HashSet<string>(relativePaths, StringComparer.OrdinalIgnoreCase);
        var edges = graph.Edges.ToList();
        var edgeKeys = new HashSet<string>(
            edges.Select(e => EdgeKey(e.FromPath, e.ToPath)),
            StringComparer.OrdinalIgnoreCase);

        foreach (var path in relativePaths)
        {
            if (!IsCppPath(path) || !contentsByPath.TryGetValue(path, out var content))
                continue;

            if (!CppLibClangBridge.TryParseIncludes(
                    path,
                    content,
                    NullLogger.Instance,
                    out var includes,
                    out _,
                    out _))
            {
                continue;
            }

            foreach (var include in includes)
            {
                var target = ResolveInclude(path, include, pathSet);
                if (target is null)
                    continue;

                var key = EdgeKey(path, target);
                if (!edgeKeys.Add(key))
                    continue;

                edges.Add(new RepoGraphEngine.RepoDependencyEdgeDto(path, target, "include"));
            }
        }

        return new RepoGraphEngine.RepoGraphDto(graph.Files, edges.ToArray());
    }

    private static bool IsCppPath(string path)
    {
        var ext = Path.GetExtension(path);
        return CppExtensions.Contains(ext);
    }

    private static string? ResolveInclude(string fromPath, string includePath, HashSet<string> knownPaths)
    {
        var normalized = includePath.Replace('\\', '/').Trim();
        if (normalized.Length == 0)
            return null;

        var dir = (Path.GetDirectoryName(fromPath) ?? string.Empty).Replace('\\', '/');
        var fromExt = Path.GetExtension(fromPath);

        var candidates = new List<string>();
        if (normalized.StartsWith('/'))
        {
            candidates.Add(normalized.TrimStart('/'));
        }
        else
        {
            candidates.Add(string.IsNullOrEmpty(dir) ? normalized : $"{dir}/{normalized}");
        }

        if (Path.GetExtension(normalized).Length == 0)
        {
            candidates.Add(normalized + fromExt);
            if (!string.IsNullOrEmpty(dir))
                candidates.Add($"{dir}/{normalized}{fromExt}");
        }

        foreach (var candidate in candidates.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            if (knownPaths.Contains(candidate))
                return candidate;

            var fileName = Path.GetFileName(candidate);
            var match = knownPaths.FirstOrDefault(p =>
                p.EndsWith("/" + fileName, StringComparison.OrdinalIgnoreCase)
                || p.Equals(fileName, StringComparison.OrdinalIgnoreCase));
            if (match is not null)
                return match;
        }

        return null;
    }

    private static string EdgeKey(string from, string to) => $"{from}|{to}";
}
