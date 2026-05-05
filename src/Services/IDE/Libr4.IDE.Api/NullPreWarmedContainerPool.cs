using Libr4.IDE.Application.ShadowWorkspace;

namespace Libr4.IDE.Api;

/// <summary>
/// No-op stub for IPreWarmedContainerPool until real container pool is wired.
/// Tech-debt: replace with PreWarmedContainerPool when gRPC container runtime is available.
/// </summary>
internal sealed class NullPreWarmedContainerPool : IPreWarmedContainerPool
{
    public Task<string> AcquireContainerAsync(CancellationToken ct = default)
        => Task.FromResult(string.Empty);

    public Task ReleaseContainerAsync(string containerId, CancellationToken ct = default)
        => Task.CompletedTask;

    public Task<int> GetAvailableCountAsync()
        => Task.FromResult(0);

    public Task WarmupAsync(int count, CancellationToken ct = default)
        => Task.CompletedTask;
}
