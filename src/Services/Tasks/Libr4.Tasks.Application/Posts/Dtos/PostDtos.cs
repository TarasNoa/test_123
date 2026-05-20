namespace Libr4.Tasks.Application.Posts.Dtos;

public sealed record PostDto(Guid Id, Guid AuthorId, string Title, string Content, DateTimeOffset CreatedAt);

public sealed record CreatePostRequest(string Title, string Content, List<string>? Tags, List<string>? MediaUrls);

public sealed record AddCommentRequest(string Content);
