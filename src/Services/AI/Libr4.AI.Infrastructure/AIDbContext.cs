using Microsoft.EntityFrameworkCore;
using Libr4.AI.Application.Abstractions;
using Libr4.AI.Domain.Agents;
using Libr4.AI.Domain.Chats;

namespace Libr4.AI.Infrastructure;

public class AIDbContext : DbContext, IAIDbContext
{
    public AIDbContext(DbContextOptions<AIDbContext> options) : base(options) { }

    public DbSet<AIChat> Chats { get; set; }
    public DbSet<AIMessage> Messages { get; set; }
    public DbSet<Agent> Agents { get; set; }
    public DbSet<AgentTool> AgentTools { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Agent>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.SystemPrompt).HasMaxLength(2000);
            entity.Property(e => e.Model).HasMaxLength(100);
            entity.Property(e => e.Status).HasConversion<string>();
            entity.Property(e => e.Type).HasConversion<string>();
            entity.HasMany(e => e.AllowedTools).WithOne().HasForeignKey("AgentId");
        });

        modelBuilder.Entity<AgentTool>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Description).HasMaxLength(500);
        });

        modelBuilder.Entity<AIChat>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Title).HasMaxLength(200);
            entity.Property(e => e.Model).HasMaxLength(100);
            entity.HasMany(e => e.Messages).WithOne().HasForeignKey("ChatId");
        });

        modelBuilder.Entity<AIMessage>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Content).IsRequired();
            entity.Property(e => e.Role).HasConversion<string>();
        });
    }
}