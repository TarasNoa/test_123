using Libr4.IDE.Application.AutonomousAppGeneration.Infrastructure.Algorithms;

namespace Libr4.IDE.Application.AutonomousAppGeneration.Context.RepoGraph;

public sealed class RepoGraphBuilder : IRepoGraphBuilder
{
    public RepoGraph Build(IReadOnlyList<string> relativePaths, IReadOnlyDictionary<string, string>? contentsByPath = null) =>
        FSharpAlgorithmsBridge.ToRepoGraph(FSharpAlgorithmsBridge.BuildRepoGraph(relativePaths, contentsByPath));

    public IReadOnlyList<string> OrderForGeneration(IReadOnlyList<string> relativePaths, IReadOnlyDictionary<string, string>? contentsByPath = null) =>
        FSharpAlgorithmsBridge.OrderForGeneration(relativePaths, contentsByPath);

    public IReadOnlyList<string> OrderForRepair(IReadOnlyList<string> relativePaths, IReadOnlyDictionary<string, string>? contentsByPath = null) =>
        FSharpAlgorithmsBridge.OrderForRepair(relativePaths, contentsByPath);
}
