using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Libr4.IDE.Application.AutonomousAppGeneration.Memory.Consolidation;

public sealed class DreamConsolidationNightlyHostedService : BackgroundService
{
    private readonly IDreamConsolidationService _consolidation;
    private readonly DreamConsolidationOptions _options;
    private readonly ILogger<DreamConsolidationNightlyHostedService> _logger;
    private DateTime? _lastRunDateUtc;

    public DreamConsolidationNightlyHostedService(
        IDreamConsolidationService consolidation,
        IOptions<DreamConsolidationOptions> options,
        ILogger<DreamConsolidationNightlyHostedService> logger)
    {
        _consolidation = consolidation;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Enabled)
        {
            _logger.LogInformation("Dream consolidation nightly job is disabled");
            return;
        }

        _logger.LogInformation("Dream consolidation nightly job started (hour UTC={Hour})", _options.NightlyHourUtc);

        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(15));
        while (await timer.WaitForNextTickAsync(stoppingToken).ConfigureAwait(false))
        {
            if (!ShouldRunNow())
                continue;

            _lastRunDateUtc = DateTime.UtcNow.Date;
            await _consolidation.RunAsync(stoppingToken).ConfigureAwait(false);
        }
    }

    public bool ShouldRunNow()
    {
        var now = DateTime.UtcNow;
        if (now.Hour != Math.Clamp(_options.NightlyHourUtc, 0, 23))
            return false;

        return _lastRunDateUtc != now.Date;
    }
}
