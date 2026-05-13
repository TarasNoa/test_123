using Libr4.Shared.Infrastructure.Messaging;
using Libr4.Social.Application.Abstractions;
using Microsoft.Extensions.Logging;

namespace Libr4.Social.Application.Commands;

public class DeletePostCommand
{
    public Guid UserId { get; set; }
    public Guid PostId { get; set; }
}

public class DeletePostCommandHandler : ICommandHandler<DeletePostCommand>
{
    private readonly ISocialNetworkRepository _repository;
    private readonly ILogger<DeletePostCommandHandler> _logger;

    public DeletePostCommandHandler(ISocialNetworkRepository repository, ILogger<DeletePostCommandHandler> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task Handle(DeletePostCommand command)
    {
        var network = await _repository.GetByUserIdAsync(command.UserId);
        if (network == null)
            throw new InvalidOperationException("User network not found");

        network.DeletePost(command.PostId);
        await _repository.UpdateAsync(network);

        _logger.LogInformation("Post {PostId} deleted by user {UserId}", command.PostId, command.UserId);
    }
}
