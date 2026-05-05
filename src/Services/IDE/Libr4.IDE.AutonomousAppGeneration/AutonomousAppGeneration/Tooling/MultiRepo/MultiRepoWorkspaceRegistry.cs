namespace Libr4.IDE.Application.AutonomousAppGeneration.Tooling.MultiRepo;

public sealed class MultiRepoWorkspaceRegistry : IMultiRepoWorkspaceRegistry
{
    private readonly Dictionary<string, RepoWorkspace> _workspaces = new(StringComparer.OrdinalIgnoreCase);

    public void Register(RepoWorkspace workspace)
    {
        if (string.IsNullOrWhiteSpace(workspace.Id))
            throw new ArgumentException("Workspace id is required.", nameof(workspace));
        _workspaces[workspace.Id] = workspace;
    }

    public RepoWorkspace? Get(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
            return null;
        return _workspaces.TryGetValue(id, out var ws) ? ws : null;
    }

    public IReadOnlyList<RepoWorkspace> GetAll() => _workspaces.Values.OrderBy(x => x.Id, StringComparer.OrdinalIgnoreCase).ToArray();
}
