namespace Libr4.IDE.Application.AutonomousAppGeneration.Runtime;

/// <summary>
/// Factory for isolated runtimes. Each implementation provides a different
/// isolation mechanism:
///   - <c>DockerIsolatedRuntime</c>: container with bind-mount (default).
///   - <c>WslIsolatedRuntime</c>: a dedicated WSL2 distro per session.
///   - <c>HyperVIsolatedRuntime</c>: a real VM (heaviest, strongest isolation).
///   - <c>ProcessIsolatedRuntime</c>: local process fallback (no isolation;
///     only for developer machines where Docker is unavailable).
/// </summary>
public interface IIsolatedRuntime
{
    /// <summary>Human readable provider name, e.g. "docker", "wsl", "hyperv".</summary>
    string ProviderName { get; }

    /// <summary>
    /// Spins up a new isolated session. <paramref name="hostMountPath"/> is
    /// mounted into the guest at a stable path; the directory must already
    /// exist on the host.
    /// </summary>
    Task<IRuntimeSession> StartSessionAsync(
        string image,
        string hostMountPath,
        CancellationToken ct = default);
}
