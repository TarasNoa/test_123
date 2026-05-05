namespace Libr4.IDE.Application.AutonomousAppGeneration.Runtime;

/// <summary>
/// Fired whenever a file inside the workspace changes, regardless of whether
/// the change originated from the IDE user or from an agent running inside
/// the isolated runtime. The IDE can subscribe to refresh its editors.
/// </summary>
public sealed record WorkspaceFileChange(
    Guid WorkspaceId,
    string RelativePath,
    WorkspaceFileChangeKind Kind,
    DateTime TimestampUtc);

public enum WorkspaceFileChangeKind
{
    Created,
    Modified,
    Deleted,
    Renamed
}

/// <summary>
/// Bidirectional sync between the IDE and the guest runtime.
///
/// With bind-mount based runtimes (Docker, WSL bind, virtiofs in Hyper-V)
/// both sides already see the exact same bytes; the service only needs to
/// observe the host directory and emit <see cref="WorkspaceFileChange"/>
/// events, so IDE clients know when the code was modified from inside the
/// runtime (e.g. by the fixer agent).
/// </summary>
public interface IWorkspaceSyncService : IAsyncDisposable
{
    event Action<WorkspaceFileChange>? OnFileChanged;

    void StartWatching(WorkspaceHandle handle);

    void StopWatching(Guid workspaceId);
}
