using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Libr4.IDE.Application.AutonomousAppGeneration.Scheduling;

public sealed class ScheduledAgentRunHostedService : BackgroundService
{
    private readonly IScheduledAgentRunService _service;
    private readonly IScheduledAgentRunDispatcher? _dispatcher;
    private readonly AgentSchedulingOptions _options;
    private readonly ILogger<ScheduledAgentRunHostedService> _logger;

    public ScheduledAgentRunHostedService(
        IScheduledAgentRunService service,
        IOptions<AgentSchedulingOptions> options,
        ILogger<ScheduledAgentRunHostedService> logger,
        IScheduledAgentRunDispatcher? dispatcher = null)
    {
        _service = service;
        _dispatcher = dispatcher;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Enabled)
        {
            _logger.LogInformation("Agent scheduling disabled");
            return;
        }

        await _service.EnsureConfiguredSchedulesAsync(stoppingToken).ConfigureAwait(false);

        var interval = TimeSpan.FromSeconds(Math.Max(15, _options.PollIntervalSeconds));
        using var timer = new PeriodicTimer(interval);
        _logger.LogInformation("Scheduled agent run daemon started (poll={Seconds}s, massTransit={Mt})",
            interval.TotalSeconds,
            _options.UseMassTransit);

        while (await timer.WaitForNextTickAsync(stoppingToken).ConfigureAwait(false))
        {
            try
            {
                await TickAsync(stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Scheduled agent run tick failed");
            }
        }
    }

    private async Task TickAsync(CancellationToken ct)
    {
        var due = await _service.GetDueSchedulesAsync(DateTime.UtcNow, ct).ConfigureAwait(false);
        foreach (var schedule in due)
        {
            if (_options.UseMassTransit)
            {
                if (_dispatcher is null)
                {
                    _logger.LogWarning("UseMassTransit=true but no IScheduledAgentRunDispatcher registered");
                    continue;
                }

                await _dispatcher.DispatchAsync(schedule, ct).ConfigureAwait(false);
            }
            else
            {
                await _service.ExecuteAsync(schedule.ScheduleId, ct).ConfigureAwait(false);
            }
        }
    }
}

public sealed class ScheduledAgentRunSchemaMigrator : IHostedService
{
    private readonly IScheduledAgentRunStore _store;

    public ScheduledAgentRunSchemaMigrator(IScheduledAgentRunStore store) => _store = store;

    public Task StartAsync(CancellationToken cancellationToken) =>
        _store.EnsureSchemaAsync(cancellationToken);

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
