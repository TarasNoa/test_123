using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Rollout;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Libr4.IDE.Application.AutonomousAppGeneration.Fleet;

/// <summary>
/// Detects runs stuck in Repairing with no recent tool activity and emits fleet alerts.
/// </summary>
public sealed class AgentFleetStuckRunMonitor : BackgroundService
{
    private readonly IAgentFleetIndexStore _index;
    private readonly IRunUsageRollupService _usage;
    private readonly IAgentFleetEventHub _eventHub;
    private readonly AgentFleetOptions _options;
    private readonly ILogger<AgentFleetStuckRunMonitor> _logger;
    private readonly HashSet<Guid> _alerted = new();

    public AgentFleetStuckRunMonitor(
        IAgentFleetIndexStore index,
        IRunUsageRollupService usage,
        IAgentFleetEventHub eventHub,
        IOptions<AgentFleetOptions> options,
        ILogger<AgentFleetStuckRunMonitor> logger)
    {
        _index = index;
        _usage = usage;
        _eventHub = eventHub;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Delay(TimeSpan.FromSeconds(45), stoppingToken).ConfigureAwait(false);
        var threshold = TimeSpan.FromMinutes(Math.Max(5, _options.StuckRepairingMinutes));

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var repairing = await _index.ListAsync(
                    new AgentFleetListQuery(Status: AgentFleetStatus.Repairing, Limit: 200),
                    stoppingToken).ConfigureAwait(false);

                var activeRepairing = repairing.Select(e => e.RunId).ToHashSet();
                _alerted.RemoveWhere(id => !activeRepairing.Contains(id));

                foreach (var entry in repairing)
                {
                    var rollup = _usage.Rollup(entry.RunId);
                    var lastTool = rollup.LastToolActivityAtUtc ?? entry.LastActivityAtUtc;
                    var idle = DateTime.UtcNow - lastTool;
                    if (idle < threshold || _alerted.Contains(entry.RunId))
                        continue;

                    _alerted.Add(entry.RunId);
                    AgentFleetTelemetry.RecordStuckRepairing(entry.RunId);
                    _logger.LogWarning(
                        "[Fleet] Run {RunId} stuck in Repairing for {Minutes:F0}m without tool activity",
                        entry.RunId,
                        idle.TotalMinutes);

                    await _eventHub.PublishAsync(
                        new AgentFleetStatusEvent(entry.RunId, entry.Status, "stuck_repairing", DateTime.UtcNow),
                        stoppingToken).ConfigureAwait(false);
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogDebug(ex, "Agent fleet stuck-run monitor tick failed");
            }

            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken).ConfigureAwait(false);
        }
    }
}
