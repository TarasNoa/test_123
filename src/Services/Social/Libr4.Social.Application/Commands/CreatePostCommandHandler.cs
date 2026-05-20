using Libr4.Shared.Infrastructure.Messaging;
using Libr4.Shared.Infrastructure.Events;
using Libr4.Social.Application.Abstractions;
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
        var postId = await _repository.CreatePostAsync(
            command.UserId,
            command.Content,
            command.Tags,
            command.AttachmentUrls);

        _logger.LogInformation("Post created: {PostId} by user {UserId}", postId, command.UserId);

        return postId;
    }
}