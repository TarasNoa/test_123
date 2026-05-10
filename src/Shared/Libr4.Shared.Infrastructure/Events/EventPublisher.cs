using System;
using System.Threading;
using System.Threading.Tasks;
using Libr4.Shared.Kernel.Domain;
using Libr4.Shared.Infrastructure.Messaging;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Libr4.Shared.Infrastructure.Events;

public interface IEventPublisher
{
    Task PublishAsync(DomainEvent @event);
}

public class EventPublisher : IEventPublisher
{
    private readonly IEventBus _eventBus;
    private readonly IOutboxService _outboxService;
    private readonly ILogger<EventPublisher> _logger;

    public EventPublisher(IEventBus eventBus, IOutboxService outboxService, ILogger<EventPublisher> logger)
    {
        _eventBus = eventBus;
        _outboxService = outboxService;
        _logger = logger;
    }

    public async Task PublishAsync(DomainEvent @event)
    {
        try
        {
            await _outboxService.AddAsync(@event);
            await _eventBus.PublishAsync(@event);
            _logger.LogInformation($"Event published: {@event.GetType().Name}");
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error publishing event: {ex.Message}");
            throw;
        }
    }
}

public class EventProcessingBackgroundService : BackgroundService
{
    private readonly IOutboxService _outboxService;
    private readonly IEventBus _eventBus;
    private readonly ILogger<EventProcessingBackgroundService> _logger;

    public EventProcessingBackgroundService(
        IOutboxService outboxService,
        IEventBus eventBus,
        ILogger<EventProcessingBackgroundService> logger)
    {
        _outboxService = outboxService;
        _eventBus = eventBus;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var pendingEvents = await _outboxService.GetPendingEventsAsync();

                foreach (var @event in pendingEvents)
                {
                    await _eventBus.PublishAsync(@event);
                    await _outboxService.MarkAsProcessedAsync(@event.EventId);
                }

                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in event processing: {ex.Message}");
            }
        }
    }
}