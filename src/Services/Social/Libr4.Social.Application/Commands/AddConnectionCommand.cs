using Libr4.Shared.Infrastructure.Messaging;
using Libr4.Social.Application.Abstractions;
using Libr4.Social.Domain.Network;
using Microsoft.Extensions.Logging;

namespace Libr4.Social.Application.Commands;

public class AddConnectionCommand
{
    public Guid UserId { get; set; }
    public Guid ConnectedUserId { get; set; }
    public ConnectionType Type { get; set; }
    public string? Note { get; set; }
}

public class AddConnectionCommandHandler : ICommandHandler<AddConnectionCommand>
{
    private readonly ISocialNetworkRepository _repository;
    private readonly ILogger<AddConnectionCommandHandler> _logger;

    public AddConnectionCommandHandler(ISocialNetworkRepository repository, ILogger<AddConnectionCommandHandler> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task Handle(AddConnectionCommand command)
    {
        var network = await _repository.GetByUserIdAsync(command.UserId);
        if (network == null)
            throw new InvalidOperationException("User network not found");

        network.AddConnection(command.ConnectedUserId, command.Type, command.Note);
        await _repository.UpdateAsync(network);

        _logger.LogInformation("Connection added for user {UserId} to {ConnectedUserId}", command.UserId, command.ConnectedUserId);
    }
}
