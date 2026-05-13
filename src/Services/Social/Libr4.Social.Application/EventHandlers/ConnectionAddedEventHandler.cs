using Libr4.Shared.Infrastructure.Messaging;
using Libr4.Social.Domain.Events;
using Microsoft.Extensions.Logging;

namespace Libr4.Social.Application.EventHandlers;

public class ConnectionAddedEventHandler : IEventHandler<ConnectionAddedEvent>
{
    private readonly ILogger<ConnectionAddedEventHandler> _logger;

    public ConnectionAddedEventHandler(ILogger<ConnectionAddedEventHandler> logger)
    {
        _logger = logger;
    }

    public async Task Handle(ConnectionAddedEvent @event)
    {
        _logger.LogInformation("Connection added event handled: {ConnectedUserId} to network {NetworkId} with type {Type}", @event.ConnectedUserId, @event.NetworkId, @event.Type);
        await Task.CompletedTask;
    }
}
