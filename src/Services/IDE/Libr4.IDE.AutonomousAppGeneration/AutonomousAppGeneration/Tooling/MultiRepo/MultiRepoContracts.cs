namespace Libr4.IDE.Application.AutonomousAppGeneration.Tooling.MultiRepo;

public sealed record RepoWorkspace(string Id, string RootPath, bool IsPrimary);

public interface IMultiRepoWorkspaceRegistry
{
    void Register(RepoWorkspace workspace);
    RepoWorkspace? Get(string id);
    IReadOnlyList<RepoWorkspace> GetAll();
}
