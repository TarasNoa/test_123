using Libr4.IDE.Application.AutonomousAppGeneration.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace Libr4.IDE.Application.AutonomousAppGeneration.Persistence;

/// <summary>
/// P2-1 of audit roadmap. EF Core context for persistent orchestration metadata.
/// Production deployments register this against PostgreSQL via
/// <see cref="DependencyInjectionExtensions.AddPostgresPersistence"/>.
/// </summary>
public sealed class AutoGenDbContext : DbContext
{
    public AutoGenDbContext(DbContextOptions<AutoGenDbContext> options) : base(options) { }

    public DbSet<RunRegistryEntry> Runs => Set<RunRegistryEntry>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<RunRegistryEntry>(e =>
        {
            e.ToTable("autogen_runs");
            e.HasKey(x => x.Id);

            e.Property(x => x.Fingerprint)
                .IsRequired()
                .HasMaxLength(128);

            e.Property(x => x.Status)
                .IsRequired()
                .HasMaxLength(64);

            e.Property(x => x.FailureReason)
                .HasMaxLength(2048);

            e.Property(x => x.ApplicationName)
                .HasMaxLength(256);

            e.Property(x => x.PayloadJson)
                .HasColumnType("text"); // future: jsonb in postgres-specific overrides

            e.HasIndex(x => x.Fingerprint).HasDatabaseName("ix_autogen_runs_fingerprint");
            e.HasIndex(x => x.Status).HasDatabaseName("ix_autogen_runs_status");
            e.HasIndex(x => x.UpdatedAtUtc).HasDatabaseName("ix_autogen_runs_updated_at_utc");
        });
    }
}
