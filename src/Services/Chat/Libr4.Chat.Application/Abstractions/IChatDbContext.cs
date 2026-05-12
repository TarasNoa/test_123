using Libr4.Chat.Domain.Chats;
using Libr4.Chat.Domain.CodeSnippets;
using Libr4.Chat.Domain.Messages;
using Libr4.Chat.Domain.Notifications;
using Microsoft.EntityFrameworkCore;

namespace Libr4.Chat.Application.Abstractions;

public interface IChatDbContext
{
    DbSet<Libr4.Chat.Domain.Chats.Chat> Chats { get; }
    DbSet<Message> Messages { get; }
    DbSet<Notification> Notifications { get; }
    DbSet<ChatMember> ChatMembers { get; }
    DbSet<CodeSnippet> CodeSnippets { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
