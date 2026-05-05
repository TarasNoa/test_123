namespace Libr4.IDE.Application.AutonomousAppGeneration.Runtime.Stubs;

/// <summary>
/// Placeholder for a full Hyper-V backed runtime (strongest isolation).
/// A real implementation would:
///   1. Spin up (or reuse) a pooled VM from a Windows/Linux golden image.
///   2. Mount the host directory via virtiofs or SMB at e.g. <c>/mnt/libr4</c>.
///   3. Run commands through SSH or <c>hvc.exe</c>.
/// Left as a stub while DockerIsolatedRuntime covers the 95% case; the
/// orchestrator is already decoupled via <see cref="IIsolatedRuntime"/>.
/// </summary>
public sealed class HyperVIsolatedRuntime : IIsolatedRuntime
{
    public string ProviderName => "hyperv";

    public Task<IRuntimeSession> StartSessionAsync(
        string image, string hostMountPath, CancellationToken ct = default)
        => throw new PlatformNotSupportedException(
            "Hyper-V runtime requires VM pool provisioning and is not enabled in this build. Configure runtime provider as docker, wsl, or process.");
}
