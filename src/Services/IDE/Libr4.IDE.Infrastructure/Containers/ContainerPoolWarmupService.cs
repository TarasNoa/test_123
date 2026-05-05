namespace Libr4.IDE.Infrastructure.Containers;

/// <summary>
/// Background service to warm up container pool on startup
/// Ensures 5-10 containers ready for instant use
/// </summary>
public class ContainerPoolWarmupService : IHostedService, IDisposable
{
    private readonly IPreWarmedContainerPool _pool;
    private readonly ILogger<ContainerPoolWarmupService> _logger;
    private Timer? _warmupTimer;

    public ContainerPoolWarmupService(
        IPreWarmedContainerPool pool,
        ILogger<ContainerPoolWarmupService> logger)
    {
        _pool = pool;
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Container pool warmup service starting...");

        // Initial warm-up after 10 seconds
        _warmupTimer = new Timer(
            async _ => await PerformWarmup(),
            null,
            TimeSpan.FromSeconds(10),
            TimeSpan.FromMinutes(1));  // Check every minute

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Container pool warmup service stopping...");
        _warmupTimer?.Change(Timeout.Infinite, 0);
        return Task.CompletedTask;
    }

    private async Task PerformWarmup()
    {
        try
        {
            var stats = _pool.GetStats();
            _logger.LogDebug(
                "Pool stats: {Warm} warm, {InUse} in-use (target: 5-10)",
                stats.WarmContainersAvailable, stats.InUseContainers);

            // Warm up if below minimum
            if (stats.WarmContainersAvailable < 5)
            {
                var needed = 5 - stats.WarmContainersAvailable;
                _logger.LogInformation("Warming up {Needed} containers...", needed);
                await _pool.WarmUpAsync(needed);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during container pool warmup");
        }
    }

    public void Dispose()
    {
        _warmupTimer?.Dispose();
    }
}
