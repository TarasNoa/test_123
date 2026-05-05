namespace Libr4.IDE.Application.ShadowWorkspace;

/// <summary>
/// Interface for pre-warmed container pool
/// </summary>
public interface IPreWarmedContainerPool
{
    Task<string> AcquireContainerAsync(CancellationToken ct = default);
    Task ReleaseContainerAsync(string containerId, CancellationToken ct = default);
    Task<int> GetAvailableCountAsync();
    Task WarmupAsync(int count = 3, CancellationToken ct = default);
}
