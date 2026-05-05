namespace Libr4.IDE.Application.AutonomousAppGeneration.Runtime;

/// <summary>
/// A live isolated session (a container, a VM, a WSL distro...).
/// The host directory <see cref="HostMountPath"/> is mirrored inside the
/// guest at <see cref="GuestMountPath"/>: writes on either side are visible
/// on the other one via the underlying mount mechanism (bind-mount for
/// Docker, virtiofs / SMB for VMs). This gives us bidirectional sync for
/// free on the file-system side — file-system watchers on the host are then
/// enough to push change notifications back into the IDE.
/// </summary>
public interface IRuntimeSession : IAsyncDisposable
{
    string ProviderName { get; }
    string SessionId { get; }

    /// <summary>Path on the host machine that is shared with the guest.</summary>
    string HostMountPath { get; }

    /// <summary>Path inside the guest where <see cref="HostMountPath"/> is mounted.</summary>
    string GuestMountPath { get; }

    /// <summary>Runtime image / profile that was used to create the session.</summary>
    string Image { get; }

    /// <summary>
    /// Executes a shell command inside the session, relative to
    /// <paramref name="workingSubDirectory"/> under the guest mount root.
    /// </summary>
    Task<ExecResult> ExecAsync(
        string command,
        string workingSubDirectory,
        IDictionary<string, string>? environmentVariables = null,
        TimeSpan? timeout = null,
        CancellationToken ct = default);
}
