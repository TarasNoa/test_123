using Libr4.Shared.Infrastructure.Messaging;
using Libr4.Social.Domain.Events;
using Microsoft.Extensions.Logging;

namespace Libr4.Social.Application.EventHandlers;

public class FollowerAddedEventHandler : IEventHandler<FollowerAddedEvent>
{
    private readonly ILogger<FollowerAddedEventHandler> _logger;

    public FollowerAddedEventHandler(ILogger<FollowerAddedEventHandler> logger)
    {
        _logger = logger;
    }

    public async Task Handle(FollowerAddedEvent @event)
    {
        _logger.LogInformation("Follower added event handled: {FollowerId} to network {NetworkId}", @event.FollowerId, @event.NetworkId);
        await Task.CompletedTask;
    }
}
