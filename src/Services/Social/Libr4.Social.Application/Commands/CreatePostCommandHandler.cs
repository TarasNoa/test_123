using Libr4.Shared.Infrastructure.Messaging;
using Libr4.Social.Domain.Network;
using Microsoft.Extensions.Logging;

namespace Libr4.Social.Application.Commands;

public class CreatePostCommand
{
    public Guid UserId { get; set; }
    public string Content { get; set; } = string.Empty;
    public List<string> Tags { get; set; } = new();
    public List<string> AttachmentUrls { get; set; } = new();
}

public class CreatePostCommandHandler : ICommandHandler<CreatePostCommand, Guid>
{
    private readonly ISocialNetworkRepository _repository;
    private readonly ILogger<CreatePostCommandHandler> _logger;

    public CreatePostCommandHandler(ISocialNetworkRepository repository, ILogger<CreatePostCommandHandler> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<Guid> Handle(CreatePostCommand command)
    {
        var network = await _repository.GetByUserIdAsync(command.UserId);
        if (network == null)
            throw new InvalidOperationException("User network not found");

        network.CreatePost(command.Content, command.Tags, command.AttachmentUrls);
        await _repository.UpdateAsync(network);

        var post = network.Posts.Last();
        _logger.LogInformation($"Post created: {post.Id} by user {command.UserId}");

        return post.Id;
    }
}