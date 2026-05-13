using Libr4.Shared.Infrastructure.Messaging;
using Libr4.Social.Application.Abstractions;
using Microsoft.Extensions.Logging;

namespace Libr4.Social.Application.Commands;

public class RemoveConnectionCommand
{
    public Guid UserId { get; set; }
    public Guid ConnectedUserId { get; set; }
}

public class RemoveConnectionCommandHandler : ICommandHandler<RemoveConnectionCommand>
{
    private readonly ISocialNetworkRepository _repository;
    private readonly ILogger<RemoveConnectionCommandHandler> _logger;

    public RemoveConnectionCommandHandler(ISocialNetworkRepository repository, ILogger<RemoveConnectionCommandHandler> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task Handle(RemoveConnectionCommand command)
    {
        var network = await _repository.GetByUserIdAsync(command.UserId);
        if (network == null)
            throw new InvalidOperationException("User network not found");

        network.RemoveConnection(command.ConnectedUserId);
        await _repository.UpdateAsync(network);

        _logger.LogInformation("Connection removed for user {UserId} from {ConnectedUserId}", command.UserId, command.ConnectedUserId);
    }
}
