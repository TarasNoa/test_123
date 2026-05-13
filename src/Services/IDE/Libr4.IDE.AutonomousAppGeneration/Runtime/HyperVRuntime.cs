namespace Libr4.IDE.Application.AutonomousAppGeneration.Runtime;

/// <summary>
/// Hyper-V backed runtime (strongest isolation).
/// Full implementation would spin up pooled VMs; currently returns
/// PlatformNotSupportedException until VM pool provisioning is configured.
/// </summary>
public sealed class HyperVRuntime : IIsolatedRuntime
{
    public string ProviderName => "hyperv";

    public Task<IRuntimeSession> StartSessionAsync(
        string image, string hostMountPath, CancellationToken ct = default)
        => throw new PlatformNotSupportedException(
            "Hyper-V runtime requires VM pool provisioning and is not enabled in this build. Configure runtime provider as docker, wsl, or process.");
}
