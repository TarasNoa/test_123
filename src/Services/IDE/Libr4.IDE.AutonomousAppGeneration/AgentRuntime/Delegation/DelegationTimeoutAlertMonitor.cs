using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Delegation;

/// <summary>
/// Emits alert when delegation timeout rate exceeds threshold within rolling hour window.
/// </summary>
public sealed class DelegationTimeoutAlertMonitor : BackgroundService
{
    private readonly DelegationRuntimeOptions _options;
    private readonly ILogger<DelegationTimeoutAlertMonitor> _logger;
    private DateTime _lastAlertUtc = DateTime.MinValue;

    public DelegationTimeoutAlertMonitor(
        IOptions<DelegationRuntimeOptions> options,
        ILogger<DelegationTimeoutAlertMonitor> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken).ConfigureAwait(false);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                EvaluateHourlyTimeoutRate();
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogDebug(ex, "Delegation timeout alert monitor tick failed");
            }

            await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken).ConfigureAwait(false);
        }
    }

    public void EvaluateHourlyTimeoutRate()
    {
        if (!_options.EnableTimeoutRateAlerts)
            return;

        var stats = DelegationTelemetry.GetHourlyStats();
        if (stats.SampleCount < Math.Max(1, _options.TimeoutAlertMinSamples))
            return;

        if (stats.TimeoutRate <= _options.TimeoutAlertRateThreshold)
            return;

        var cooldown = TimeSpan.FromMinutes(Math.Max(5, _options.TimeoutAlertCooldownMinutes));
        if (DateTime.UtcNow - _lastAlertUtc < cooldown)
            return;

        _lastAlertUtc = DateTime.UtcNow;
        DelegationTelemetry.RecordTimeoutRateAlert(stats.TimeoutRate, stats.SampleCount);
        _logger.LogWarning(
            "[Delegation] Hourly timeout rate {Rate:P1} exceeds threshold {Threshold:P0} ({Timeouts}/{Total} samples)",
            stats.TimeoutRate,
            _options.TimeoutAlertRateThreshold,
            stats.TimeoutCount,
            stats.SampleCount);
    }
}
