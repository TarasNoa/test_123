using Microsoft.EntityFrameworkCore;
using Libr4.IDE.Domain.Entities;
using Libr4.IDE.Infrastructure.Persistence.Entities;

namespace Libr4.IDE.Infrastructure.Persistence;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    // Нужен ResilientOrchestrator (4 обращения к _db.Agents) и AgentHub
    public DbSet<AgentEntity> Agents { get; set; }

    // Нужен AgentStateEndpoints (/events, /events/{runId})
    public DbSet<AgentEventEntity> AgentEvents { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<AgentEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(256);
            entity.Property(e => e.OwnerId).IsRequired().HasMaxLength(256);
            entity.Property(e => e.State).IsRequired().HasDefaultValue("Idle");
            // Optimistic concurrency — маппится на [Timestamp] атрибут
            entity.Property(e => e.RowVersion).IsRowVersion();
            // Быстрый поиск агента по владельцу (нужен для /run endpoint)
            entity.HasIndex(e => e.OwnerId);
        });

        modelBuilder.Entity<AgentEventEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Type).IsRequired();
            entity.HasIndex(e => e.RunId);
            entity.HasIndex(e => e.Timestamp);
        });
    }
}
