namespace Libr4.IDE.Application.AutonomousAppGeneration.Context.RepoGraph;

public interface IRepoGraphBuilder
{
    RepoGraph Build(IReadOnlyList<string> relativePaths, IReadOnlyDictionary<string, string>? contentsByPath = null);
    IReadOnlyList<string> OrderForGeneration(IReadOnlyList<string> relativePaths, IReadOnlyDictionary<string, string>? contentsByPath = null);
    IReadOnlyList<string> OrderForRepair(IReadOnlyList<string> relativePaths, IReadOnlyDictionary<string, string>? contentsByPath = null);
}
