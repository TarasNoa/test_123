using Libr4.AI.Application.Abstractions;
using Libr4.AI.Domain.Agents;
using Libr4.AI.Domain.Chats;
using Microsoft.EntityFrameworkCore;

namespace Libr4.AI.Infrastructure.Persistence;

public class AIDbContext : DbContext, IAIDbContext
{
    public DbSet<AIChat> Chats => Set<AIChat>();
    public DbSet<AIMessage> Messages => Set<AIMessage>();
    public DbSet<Agent> Agents => Set<Agent>();
    public DbSet<AgentTool> AgentTools => Set<AgentTool>();

    public AIDbContext(DbContextOptions<AIDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("ai");
        
        // AIChat
        modelBuilder.Entity<AIChat>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Title).HasMaxLength(200);
            entity.Property(e => e.Model).HasMaxLength(100);
            entity.HasMany(e => e.Messages).WithOne().HasForeignKey("ChatId").OnDelete(DeleteBehavior.Cascade);
        });

        // AIMessage
        modelBuilder.Entity<AIMessage>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Content).HasColumnType("text");
            entity.Property(e => e.ToolCallId).HasMaxLength(100);
        });

        // Agent
        modelBuilder.Entity<Agent>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).HasMaxLength(100);
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.Model).HasMaxLength(100);
            entity.Property(e => e.SystemPrompt).HasColumnType("text");
            entity.HasMany(e => e.AllowedTools).WithOne().HasForeignKey("AgentId").OnDelete(DeleteBehavior.Cascade);
        });

        // AgentTool
        modelBuilder.Entity<AgentTool>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).HasMaxLength(100);
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.Parameters).HasColumnType("jsonb");
        });

        base.OnModelCreating(modelBuilder);
    }
}
