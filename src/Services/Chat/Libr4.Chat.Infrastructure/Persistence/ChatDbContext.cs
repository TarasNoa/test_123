using Libr4.Chat.Application.Abstractions;
using Libr4.Chat.Domain.Calls;
using Libr4.Chat.Domain.Chats;
using ChatEntity = Libr4.Chat.Domain.Chats.Chat;
using Libr4.Chat.Domain.Messages;
using Libr4.Chat.Domain.Notifications;
using Libr4.Chat.Domain.Servers;
using Microsoft.EntityFrameworkCore;

namespace Libr4.Chat.Infrastructure.Persistence;

public class ChatDbContext : DbContext, IChatDbContext
{
    public DbSet<ChatEntity> Chats => Set<ChatEntity>();
    public DbSet<Message> Messages => Set<Message>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<ChatMember> ChatMembers => Set<ChatMember>();

    public ChatDbContext(DbContextOptions<ChatDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("chat");
        modelBuilder.Ignore<ChatParticipant>();
        modelBuilder.Ignore<MessageAttachment>();
        modelBuilder.Ignore<CallParticipant>();
        modelBuilder.Ignore<CallRecording>();
        modelBuilder.Ignore<Channel>();
        modelBuilder.Ignore<Role>();
        modelBuilder.Ignore<ServerMember>();
        modelBuilder.Ignore<Domain.Servers.Task>();
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ChatDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
