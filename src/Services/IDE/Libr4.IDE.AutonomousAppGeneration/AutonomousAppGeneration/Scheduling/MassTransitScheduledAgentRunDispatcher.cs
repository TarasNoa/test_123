using Libr4.IDE.Application.AutonomousAppGeneration.Scheduling.Messaging;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Libr4.IDE.Application.AutonomousAppGeneration.Scheduling;

public sealed class MassTransitScheduledAgentRunDispatcher : IScheduledAgentRunDispatcher
{
    private readonly IPublishEndpoint _bus;
    private readonly ILogger<MassTransitScheduledAgentRunDispatcher> _logger;

    public MassTransitScheduledAgentRunDispatcher(
        IPublishEndpoint bus,
        ILogger<MassTransitScheduledAgentRunDispatcher> logger)
    {
        _bus = bus;
        _logger = logger;
    }

    public async Task DispatchAsync(ScheduledAgentRunDefinition schedule, CancellationToken ct = default)
    {
        await _bus.Publish(
                new ExecuteScheduledAgentRunMessage(schedule.ScheduleId, DateTime.UtcNow),
                ct)
            .ConfigureAwait(false);

        _logger.LogInformation(
            "Published scheduled agent run message for {ScheduleId} flow={Flow}",
            schedule.ScheduleId,
            schedule.FlowName);
    }
}

public sealed class ScheduledAgentRunConsumer : IConsumer<ExecuteScheduledAgentRunMessage>
{
    private readonly IScheduledAgentRunService _service;
    private readonly ILogger<ScheduledAgentRunConsumer> _logger;

    public ScheduledAgentRunConsumer(
        IScheduledAgentRunService service,
        ILogger<ScheduledAgentRunConsumer> logger)
    {
        _service = service;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<ExecuteScheduledAgentRunMessage> context)
    {
        _logger.LogInformation(
            "Consuming scheduled agent run {ScheduleId}",
            context.Message.ScheduleId);

        await _service.ExecuteAsync(context.Message.ScheduleId, context.CancellationToken)
            .ConfigureAwait(false);
    }
}
