using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Libr4.IDE.Application.AutonomousAppGeneration.Fleet;

/// <summary>
/// Keeps fleet index in sync with active runs and emits status events without polling the UI.
/// </summary>
public sealed class AgentFleetSyncHostedService : BackgroundService
{
    private readonly IAgentFleetRegistry _fleet;
    private readonly Services.IAutonomousRunControlService _runControl;
    private readonly ILogger<AgentFleetSyncHostedService> _logger;
    private int _lastActiveCount;

    public AgentFleetSyncHostedService(
        IAgentFleetRegistry fleet,
        Services.IAutonomousRunControlService runControl,
        ILogger<AgentFleetSyncHostedService> logger)
    {
        _fleet = fleet;
        _runControl = runControl;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken).ConfigureAwait(false);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var active = _runControl.GetHealthSnapshot().Active;
                var delta = active.Count - _lastActiveCount;
                if (delta != 0)
                {
                    AgentFleetTelemetry.RunsActive.Add(delta);
                    _lastActiveCount = active.Count;
                }

                foreach (var item in active)
                    await _fleet.UpsertFromRunAsync(item.RunId, stoppingToken).ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogDebug(ex, "Agent fleet sync tick failed");
            }

            await Task.Delay(TimeSpan.FromSeconds(3), stoppingToken).ConfigureAwait(false);
        }
    }
}
