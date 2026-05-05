namespace Libr4.Shared.Contracts.MultiRepo;

/// <summary>
/// Represents a workspace for a repository.
/// </summary>
public record RepoWorkspace
{
    /// <summary>
    /// Unique identifier for the workspace.
    /// </summary>
    public string Id { get; init; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Repository URL.
    /// </summary>
    public string RepoUrl { get; init; } = string.Empty;

    /// <summary>
    /// Repository name.
    /// </summary>
    public string RepoName { get; init; } = string.Empty;

    /// <summary>
    /// Branch name.
    /// </summary>
    public string Branch { get; init; } = "main";

    /// <summary>
    /// Local path to the workspace.
    /// </summary>
    public string LocalPath { get; init; } = string.Empty;

    /// <summary>
    /// Status of the workspace.
    /// </summary>
    public WorkspaceStatus Status { get; init; }

    /// <summary>
    /// When the workspace was created.
    /// </summary>
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// When the workspace was last updated.
    /// </summary>
    public DateTime? LastUpdatedAt { get; init; }

    /// <summary>
    /// Metadata about the workspace.
    /// </summary>
    public Dictionary<string, string> Metadata { get; init; } = new();
}

/// <summary>
/// Status of a workspace.
/// </summary>
public enum WorkspaceStatus
{
    Initializing,
    Ready,
    Cloning,
    Syncing,
    Error,
    Disconnected
}

/// <summary>
/// Interface for managing multi-repo workspaces.
/// </summary>
public interface IMultiRepoWorkspaceRegistry
{
    /// <summary>
    /// Registers a new workspace for a repository.
    /// </summary>
    /// <param name="repoUrl">Repository URL.</param>
    /// <param name="branch">Branch name (default: main).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The registered workspace.</returns>
    Task<RepoWorkspace> RegisterWorkspaceAsync(
        string repoUrl,
        string branch = "main",
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a workspace by ID.
    /// </summary>
    /// <param name="workspaceId">Workspace ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The workspace, or null if not found.</returns>
    Task<RepoWorkspace?> GetWorkspaceAsync(
        string workspaceId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a workspace by repository URL.
    /// </summary>
    /// <param name="repoUrl">Repository URL.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The workspace, or null if not found.</returns>
    Task<RepoWorkspace?> GetWorkspaceByRepoAsync(
        string repoUrl,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all workspaces.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of all workspaces.</returns>
    Task<IReadOnlyList<RepoWorkspace>> GetAllWorkspacesAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Syncs a workspace with the remote repository.
    /// </summary>
    /// <param name="workspaceId">Workspace ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated workspace.</returns>
    Task<RepoWorkspace> SyncWorkspaceAsync(
        string workspaceId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Unregisters a workspace.
    /// </summary>
    /// <param name="workspaceId">Workspace ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if unregistered, false if not found.</returns>
    Task<bool> UnregisterWorkspaceAsync(
        string workspaceId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the status of all workspaces.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Dictionary mapping workspace IDs to their status.</returns>
    Task<Dictionary<string, WorkspaceStatus>> GetAllStatusesAsync(
        CancellationToken cancellationToken = default);
}

/// <summary>
/// In-memory implementation of multi-repo workspace registry.
/// </summary>
public class InMemoryMultiRepoWorkspaceRegistry : IMultiRepoWorkspaceRegistry
{
    private readonly Dictionary<string, RepoWorkspace> _workspaces = new();

    public Task<RepoWorkspace> RegisterWorkspaceAsync(
        string repoUrl,
        string branch = "main",
        CancellationToken cancellationToken = default)
    {
        var repoName = ExtractRepoName(repoUrl);
        var workspace = new RepoWorkspace
        {
            RepoUrl = repoUrl,
            RepoName = repoName,
            Branch = branch,
            LocalPath = $"/tmp/workspaces/{repoName}",
            Status = WorkspaceStatus.Initializing
        };

        _workspaces[workspace.Id] = workspace;
        return Task.FromResult(workspace);
    }

    public Task<RepoWorkspace?> GetWorkspaceAsync(
        string workspaceId,
        CancellationToken cancellationToken = default)
    {
        _workspaces.TryGetValue(workspaceId, out var workspace);
        return Task.FromResult(workspace);
    }

    public Task<RepoWorkspace?> GetWorkspaceByRepoAsync(
        string repoUrl,
        CancellationToken cancellationToken = default)
    {
        var workspace = _workspaces.Values.FirstOrDefault(w => w.RepoUrl == repoUrl);
        return Task.FromResult(workspace);
    }

    public Task<IReadOnlyList<RepoWorkspace>> GetAllWorkspacesAsync(
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IReadOnlyList<RepoWorkspace>>(_workspaces.Values.ToList().AsReadOnly());
    }

    public Task<RepoWorkspace> SyncWorkspaceAsync(
        string workspaceId,
        CancellationToken cancellationToken = default)
    {
        if (!_workspaces.TryGetValue(workspaceId, out var workspace))
        {
            throw new ArgumentException($"Workspace with ID {workspaceId} not found", nameof(workspaceId));
        }

        var updated = workspace with
        {
            Status = WorkspaceStatus.Syncing,
            LastUpdatedAt = DateTime.UtcNow
        };

        _workspaces[workspaceId] = updated;

        // Simulate sync completion
        updated = updated with
        {
            Status = WorkspaceStatus.Ready
        };

        _workspaces[workspaceId] = updated;
        return Task.FromResult(updated);
    }

    public Task<bool> UnregisterWorkspaceAsync(
        string workspaceId,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_workspaces.Remove(workspaceId));
    }

    public Task<Dictionary<string, WorkspaceStatus>> GetAllStatusesAsync(
        CancellationToken cancellationToken = default)
    {
        var statuses = _workspaces.ToDictionary(
            kvp => kvp.Key,
            kvp => kvp.Value.Status);

        return Task.FromResult(statuses);
    }

    private static string ExtractRepoName(string repoUrl)
    {
        var parts = repoUrl.Split('/');
        return parts[^1].Replace(".git", "", StringComparison.OrdinalIgnoreCase);
    }
}
