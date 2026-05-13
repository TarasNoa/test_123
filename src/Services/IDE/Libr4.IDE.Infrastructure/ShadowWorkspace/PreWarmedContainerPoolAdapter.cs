using Libr4.IDE.Infrastructure.Containers;

namespace Libr4.IDE.Infrastructure.ShadowWorkspace;

/// <summary>
/// Adapter bridging Application.IPreWarmedContainerPool to Infrastructure.IPreWarmedContainerPool.
/// </summary>
public sealed class PreWarmedContainerPoolAdapter : Application.ShadowWorkspace.IPreWarmedContainerPool
{
    private readonly Containers.IPreWarmedContainerPool _inner;

    public PreWarmedContainerPoolAdapter(Containers.IPreWarmedContainerPool inner)
    {
        _inner = inner;
    }

    public async Task<string> AcquireContainerAsync(CancellationToken ct = default)
    {
        var warm = await _inner.AcquireAsync("default", ct);
        return warm?.ContainerId ?? throw new InvalidOperationException("Pool exhausted");
    }

    public Task ReleaseContainerAsync(string containerId, CancellationToken ct = default)
        => _inner.ReleaseAsync(containerId, ct);

    public Task<int> GetAvailableCountAsync()
        => Task.FromResult(_inner.GetStats().WarmContainersAvailable);

    public Task WarmupAsync(int count = 3, CancellationToken ct = default)
        => _inner.WarmUpAsync(count, ct);
}
