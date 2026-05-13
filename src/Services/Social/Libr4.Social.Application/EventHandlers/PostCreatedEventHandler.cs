using Libr4.Shared.Infrastructure.Messaging;
using Libr4.Social.Domain.Events;
using Microsoft.Extensions.Logging;

namespace Libr4.Social.Application.EventHandlers;

public class PostCreatedEventHandler : IEventHandler<PostCreatedEvent>
{
    private readonly ILogger<PostCreatedEventHandler> _logger;

    public PostCreatedEventHandler(ILogger<PostCreatedEventHandler> logger)
    {
        _logger = logger;
    }

    public async Task Handle(PostCreatedEvent @event)
    {
        _logger.LogInformation("Post created event handled: {PostId} in network {NetworkId}", @event.PostId, @event.NetworkId);
        await Task.CompletedTask;
    }
}
