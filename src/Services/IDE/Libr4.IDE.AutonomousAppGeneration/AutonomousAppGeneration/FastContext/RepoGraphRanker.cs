using Libr4.IDE.Application.AutonomousAppGeneration.Context.RepoGraph;

namespace Libr4.IDE.Application.AutonomousAppGeneration.FastContext;

public sealed class RepoGraphRanker
{
    private readonly IRepoGraphBuilder _graphBuilder;

    public RepoGraphRanker(IRepoGraphBuilder graphBuilder) => _graphBuilder = graphBuilder;

    public IReadOnlyList<(CodebaseSearchHit Hit, double Boost)> BoostNeighbors(
        string workspaceRoot,
        IReadOnlyList<CodebaseSearchHit> hits)
    {
        if (hits.Count == 0)
            return Array.Empty<(CodebaseSearchHit, double)>();

        var paths = Directory.Exists(workspaceRoot)
            ? Directory.EnumerateFiles(workspaceRoot, "*", SearchOption.AllDirectories)
                .Where(p => !ShouldSkip(p))
                .Select(p => Path.GetRelativePath(workspaceRoot, p).Replace('\\', '/'))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList()
            : hits.Select(h => h.Path).Distinct(StringComparer.OrdinalIgnoreCase).ToList();

        var graph = _graphBuilder.Build(paths);
        var neighborMap = BuildNeighborMap(graph);

        return hits.Select(hit =>
        {
            var boost = 0.0;
            foreach (var seed in hits.Take(3))
            {
                if (neighborMap.TryGetValue(Normalize(seed.Path), out var neighbors)
                    && neighbors.Contains(Normalize(hit.Path)))
                {
                    boost += 0.5;
                }
            }

            if (hit.Path.Contains('/', StringComparison.Ordinal))
                boost += 0.1;

            return (hit, boost);
        }).ToList();
    }

    private static Dictionary<string, HashSet<string>> BuildNeighborMap(RepoGraph graph)
    {
        var map = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
        foreach (var edge in graph.Edges)
        {
            AddNeighbor(map, edge.FromPath, edge.ToPath);
            AddNeighbor(map, edge.ToPath, edge.FromPath);
        }

        return map;
    }

    private static void AddNeighbor(Dictionary<string, HashSet<string>> map, string from, string to)
    {
        var key = Normalize(from);
        if (!map.TryGetValue(key, out var set))
        {
            set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            map[key] = set;
        }

        set.Add(Normalize(to));
    }

    private static string Normalize(string path) => path.Replace('\\', '/');

    private static bool ShouldSkip(string path)
    {
        var p = path.Replace('\\', '/');
        return p.Contains("/node_modules/", StringComparison.OrdinalIgnoreCase)
               || p.Contains("/.git/", StringComparison.OrdinalIgnoreCase);
    }
}
