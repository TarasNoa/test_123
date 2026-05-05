namespace Libr4.IDE.Application.AutonomousAppGeneration.Runtime;

/// <summary>
/// Pool of workspaces backed by long-living isolated runtimes.
///
/// Implementation guarantees:
/// * Workspaces that share the same <paramref name="runtimeImage"/> MAY live
///   inside the same runtime session (classic VM hosting several workspaces).
/// * Workspaces with different images get separate sessions.
/// * <see cref="AcquireAsync"/> prepares the host directory, ensures the
///   runtime is running and returns a ready-to-use <see cref="WorkspaceHandle"/>.
/// * <see cref="ReleaseAsync"/> tears down the workspace; when the hosting
///   runtime has no more workspaces, the runtime itself is disposed.
/// </summary>
public interface IWorkspacePool
{
    Task<WorkspaceHandle> AcquireAsync(string runtimeImage, CancellationToken ct = default);

    Task ReleaseAsync(WorkspaceHandle handle, CancellationToken ct = default);

    /// <summary>Returns a snapshot of the currently live workspaces.</summary>
    IReadOnlyList<WorkspaceHandle> ListActive();
}
