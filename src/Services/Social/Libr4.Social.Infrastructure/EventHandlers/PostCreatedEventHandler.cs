using Libr4.Social.Domain.Network;
using Libr4.Shared.Infrastructure.Messaging;
using Microsoft.Extensions.Logging;

namespace Libr4.Social.Infrastructure.EventHandlers;

public class PostCreatedEventHandler : IEventHandler<PostCreatedEvent>
{
    private readonly ILogger<PostCreatedEventHandler> _logger;

    public PostCreatedEventHandler(ILogger<PostCreatedEventHandler> logger)
    {
        _logger = logger;
    }

    public async Task Handle(PostCreatedEvent @event)
    {
        _logger.LogInformation($"Post created event handled: {@event.PostId} by user {@event.UserId}");
        // Notify followers, update analytics, etc.
        await Task.CompletedTask;
    }
}