using Libr4.Shared.Infrastructure.Messaging;
using Libr4.Social.Application.Abstractions;
using Microsoft.Extensions.Logging;

namespace Libr4.Social.Application.Commands;

public class DeleteCommentCommand
{
    public Guid UserId { get; set; }
    public Guid PostId { get; set; }
    public Guid CommentId { get; set; }
}

public class DeleteCommentCommandHandler : ICommandHandler<DeleteCommentCommand>
{
    private readonly ISocialNetworkRepository _repository;
    private readonly ILogger<DeleteCommentCommandHandler> _logger;

    public DeleteCommentCommandHandler(ISocialNetworkRepository repository, ILogger<DeleteCommentCommandHandler> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task Handle(DeleteCommentCommand command)
    {
        var network = await _repository.GetByUserIdAsync(command.UserId);
        if (network == null)
            throw new InvalidOperationException("User network not found");

        var post = network.Posts.FirstOrDefault(p => p.Id == command.PostId);
        if (post != null)
        {
            var comment = post.Comments.FirstOrDefault(c => c.Id == command.CommentId);
            if (comment != null)
            {
                post.Comments.Remove(comment);
                await _repository.UpdateAsync(network);
            }
        }

        _logger.LogInformation("Comment {CommentId} deleted from post {PostId} by user {UserId}", command.CommentId, command.PostId, command.UserId);
    }
}
