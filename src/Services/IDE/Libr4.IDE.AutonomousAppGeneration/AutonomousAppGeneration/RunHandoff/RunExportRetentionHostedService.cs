using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Libr4.IDE.Application.AutonomousAppGeneration.RunHandoff;

public sealed class RunExportRetentionHostedService : BackgroundService
{
    private readonly IRunExportService _exports;
    private readonly RunExportOptions _options;
    private readonly ILogger<RunExportRetentionHostedService> _logger;

    public RunExportRetentionHostedService(
        IRunExportService exports,
        IOptions<RunExportOptions> options,
        ILogger<RunExportRetentionHostedService> logger)
    {
        _exports = exports;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken).ConfigureAwait(false);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var removed = _exports.PruneExpiredExports();
                if (removed > 0)
                {
                    _logger.LogInformation(
                        "Pruned {Count} expired run export bundles (retention {Days}d)",
                        removed,
                        _options.RetentionDays);
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogWarning(ex, "Run export retention sweep failed");
            }

            await Task.Delay(TimeSpan.FromHours(6), stoppingToken).ConfigureAwait(false);
        }
    }
}
