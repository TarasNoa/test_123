using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Libr4.IDE.Application.AutonomousAppGeneration.Fleet;

public sealed class FleetRetentionHostedService : BackgroundService
{
    private readonly IFleetRetentionService _retention;
    private readonly FleetRetentionOptions _options;
    private readonly ILogger<FleetRetentionHostedService> _logger;

    public FleetRetentionHostedService(
        IFleetRetentionService retention,
        IOptions<FleetRetentionOptions> options,
        ILogger<FleetRetentionHostedService> logger)
    {
        _retention = retention;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.EnableHostedSweep)
            return;

        await Task.Delay(TimeSpan.FromMinutes(2), stoppingToken).ConfigureAwait(false);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await _retention.ApplyRetentionAsync(stoppingToken).ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogWarning(ex, "Fleet retention sweep failed");
            }

            var hours = Math.Clamp(_options.SweepIntervalHours, 1, 168);
            await Task.Delay(TimeSpan.FromHours(hours), stoppingToken).ConfigureAwait(false);
        }
    }
}
