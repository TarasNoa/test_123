using Microsoft.EntityFrameworkCore;
using Libr4.IDE.Domain;
using Libr4.IDE.Domain.AI;
using Libr4.IDE.Infrastructure.Persistence.Entities;

namespace Libr4.IDE.Infrastructure.Persistence;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    public DbSet<AgentEventEntity> AgentEvents { get; set; }
    public DbSet<AIConversation> AIConversations { get; set; }
    public DbSet<AIMessage> AIMessages { get; set; }
    public DbSet<CodeSession> CodeSessions { get; set; }
    public DbSet<CodeFile> CodeFiles { get; set; }
    public DbSet<CodeSessionParticipant> CodeSessionParticipants { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<AgentEventEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Type).IsRequired();
            entity.HasIndex(e => e.RunId);
        });

        modelBuilder.Entity<AIConversation>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.UserId);
            entity.HasMany(e => e.Messages).WithOne().HasForeignKey(m => m.ConversationId);
            entity.Ignore(e => e.ContextData);
            entity.Ignore(e => e.Actions);
        });

        modelBuilder.Entity<AIMessage>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.ConversationId);
            entity.Ignore(e => e.Attachments);
            entity.Ignore(e => e.CodeSnippets);
        });

        modelBuilder.Entity<CodeSession>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.CreatorId);
            entity.HasMany(e => e.Files).WithOne().HasForeignKey(f => f.SessionId);
            entity.HasMany(e => e.Participants).WithOne().HasForeignKey(p => p.SessionId);
        });

        modelBuilder.Entity<CodeFile>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.SessionId);
        });

        modelBuilder.Entity<CodeSessionParticipant>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.SessionId);
        });
    }
}
