using Libr4.Shared.Infrastructure.Messaging;
using Libr4.Social.Domain.Events;
using Microsoft.Extensions.Logging;

namespace Libr4.Social.Application.EventHandlers;

public class ProfileUpdatedEventHandler : IEventHandler<ProfileUpdatedEvent>
{
    private readonly ILogger<ProfileUpdatedEventHandler> _logger;

    public ProfileUpdatedEventHandler(ILogger<ProfileUpdatedEventHandler> logger)
    {
        _logger = logger;
    }

    public async Task Handle(ProfileUpdatedEvent @event)
    {
        _logger.LogInformation("Profile updated event handled for network {NetworkId}", @event.NetworkId);
        await Task.CompletedTask;
    }
}
