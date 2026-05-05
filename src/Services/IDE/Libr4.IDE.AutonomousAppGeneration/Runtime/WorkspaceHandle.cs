namespace Libr4.IDE.Application.AutonomousAppGeneration.Runtime;

/// <summary>
/// Reference to a live shadow workspace: a subdirectory on the host that is
/// mirrored into an <see cref="IRuntimeSession"/>. Multiple handles can share
/// the same runtime session (several workspaces living in a single VM).
/// </summary>
public sealed class WorkspaceHandle
{
    public Guid WorkspaceId { get; }
    /// <summary>Host directory, visible from the IDE side.</summary>
    public string HostPath { get; }
    /// <summary>Path inside the guest, under <see cref="IRuntimeSession.GuestMountPath"/>.</summary>
    public string GuestPath { get; }
    public IRuntimeSession Runtime { get; }

    public WorkspaceHandle(Guid workspaceId, string hostPath, string guestPath, IRuntimeSession runtime)
    {
        WorkspaceId = workspaceId;
        HostPath = hostPath ?? throw new ArgumentNullException(nameof(hostPath));
        GuestPath = guestPath ?? throw new ArgumentNullException(nameof(guestPath));
        Runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));
    }
}
