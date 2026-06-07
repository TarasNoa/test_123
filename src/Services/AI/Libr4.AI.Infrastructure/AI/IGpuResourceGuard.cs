namespace Libr4.AI.Infrastructure.AI;

public interface IGpuResourceGuard
{
    /// <summary>Blocks until GPU utilization and VRAM are below configured thresholds (or timeout).</summary>
    Task WaitForHeadroomAsync(CancellationToken cancellationToken = default);
}
