using Libr4.Shared.Kernel.Application;
using Libr4.Shared.Kernel.Domain;
using MassTransit;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace Libr4.Shared.Infrastructure.Messaging;

public sealed class MassTransitEventBus : IEventBus
{
    private readonly IPublishEndpoint _publish;
    private readonly ConcurrentDictionary<string, List<Delegate>> _handlers = new();
    private readonly ILogger<MassTransitEventBus> _logger;

    public MassTransitEventBus(IPublishEndpoint publish, ILogger<MassTransitEventBus> logger)
    {
        _publish = publish;
        _logger = logger;
    }

    // IEventBus from Libr4.Shared.Kernel.Application
    public Task PublishAsync<T>(T integrationEvent, CancellationToken cancellationToken = default)
        where T : class
        => _publish.Publish(integrationEvent, cancellationToken);

    // IEventBus from Libr4.Shared.Infrastructure.Messaging
    public void Subscribe<TEvent>(IEventHandler<TEvent> handler) where TEvent : DomainEvent
    {
        var eventType = typeof(TEvent).Name;
        var handlerDelegate = new Action<TEvent>(e => handler.Handle(e).Wait());

        _handlers.AddOrUpdate(eventType,
            new List<Delegate> { handlerDelegate },
            (key, list) =>
            {
                list.Add(handlerDelegate);
                return list;
            });

        _logger.LogInformation($"Handler subscribed for event: {eventType}");
    }

    public async Task PublishAsync<TEvent>(TEvent @event) where TEvent : DomainEvent
    {
        var eventType = typeof(TEvent).Name;

        if (_handlers.TryGetValue(eventType, out var handlers))
        {
            foreach (var handler in handlers)
            {
                try
                {
                    if (handler is Delegate del)
                    {
                        var task = del.DynamicInvoke(@event) as Task;
                        if (task != null)
                            await task;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Error publishing event {eventType}: {ex.Message}");
                }
            }
        }
    }

    public async Task PublishAsync(DomainEvent @event)
    {
        var eventType = @event.GetType().Name;
        _logger.LogInformation($"Publishing event: {eventType}");

        if (_handlers.TryGetValue(eventType, out var handlers))
        {
            var tasks = handlers.Select(handler =>
            {
                try
                {
                    return handler.DynamicInvoke(@event) as Task ?? Task.CompletedTask;
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Error in handler: {ex.Message}");
                    return Task.CompletedTask;
                }
            });

            await Task.WhenAll(tasks);
        }
    }
}
