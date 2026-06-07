namespace Libr4.IDE.Application.AutonomousAppGeneration.Context.RepoGraph;

public sealed record RepoFileNode(string RelativePath, string Language);

public sealed record RepoDependencyEdge(string FromPath, string ToPath, string Kind);

public sealed class RepoGraph
{
    public IReadOnlyList<RepoFileNode> Files { get; init; } = Array.Empty<RepoFileNode>();
    public IReadOnlyList<RepoDependencyEdge> Edges { get; init; } = Array.Empty<RepoDependencyEdge>();
}

public sealed class RepoGraphOptions
{
    public bool UseRepoGraphOrdering { get; set; } = true;
}
