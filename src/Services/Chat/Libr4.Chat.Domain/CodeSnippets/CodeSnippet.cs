using System;
using Libr4.Shared.Kernel.Domain;

namespace Libr4.Chat.Domain.CodeSnippets;

public class CodeSnippet : AggregateRoot<Guid>
{
    public Guid ChannelId { get; private set; }
    public Guid CreatorId { get; private set; }
    public string Language { get; private set; } = string.Empty;
    public string Code { get; private set; } = string.Empty;
    public string Title { get; private set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; private set; }

    private CodeSnippet() { }

    public static CodeSnippet Create(Guid id, Guid channelId, Guid creatorId, string language, string code, string title)
    {
        return new CodeSnippet
        {
            Id = id,
            ChannelId = channelId,
            CreatorId = creatorId,
            Language = language,
            Code = code,
            Title = title,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }
}
