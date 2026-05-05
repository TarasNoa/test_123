using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Libr4.IDE.Application.ShadowWorkspace;

/// <summary>
/// Background service for warming up container pool on startup
/// </summary>
public class ContainerPoolWarmupService : BackgroundService
{
    private readonly IPreWarmedContainerPool _pool;
    private readonly ILogger<ContainerPoolWarmupService> _logger;

    public ContainerPoolWarmupService(
        IPreWarmedContainerPool pool,
        ILogger<ContainerPoolWarmupService> logger)
    {
        _pool = pool;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Container pool warmup service starting...");
        
        await _pool.WarmupAsync(3, stoppingToken);
        
        _logger.LogInformation("Container pool warmup completed");
        
        // Keep running to handle shutdown
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            
            var available = await _pool.GetAvailableCountAsync();
            if (available < 2)
            {
                _logger.LogInformation("Low container pool ({Available}), warming up more...", available);
                await _pool.WarmupAsync(2, stoppingToken);
            }
        }
    }
}
