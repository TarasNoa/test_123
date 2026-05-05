using Microsoft.EntityFrameworkCore;

namespace Libr4.IDE.Infrastructure.Persistence;

/// <summary>
/// EF Core DbContext for IDE services.
/// Contains DbSets for agent events, orchestrations, and app generations.
/// </summary>
public class IdeDbContext : DbContext
{
    public IdeDbContext(DbContextOptions<IdeDbContext> options)
        : base(options)
    {
    }

    public DbSet<AgentEventEntity> AgentEvents { get; set; }
    public DbSet<AgentOrchestrationEntity> AgentOrchestrations { get; set; }
    public DbSet<AppGenerationEntity> AppGenerations { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // AgentEvents configuration
        modelBuilder.Entity<AgentEventEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.RunId);
            entity.Property(e => e.Type).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Command);
            entity.Property(e => e.Output);
            entity.Property(e => e.ExitCode);
            entity.Property(e => e.DurationMs);
        });

        // AgentOrchestrations configuration
        modelBuilder.Entity<AgentOrchestrationEntity>(entity =>
        {
            entity.HasKey(e => e.RunId);
            entity.Property(e => e.JsonData).IsRequired();
        });

        // AppGenerations configuration
        modelBuilder.Entity<AppGenerationEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.UserId);
            entity.Property(e => e.Status).IsRequired().HasMaxLength(50);
            entity.Property(e => e.ConfigurationJson).IsRequired();
        });
    }
}
