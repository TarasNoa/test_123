using Microsoft.EntityFrameworkCore;
using Libr4.IDE.Domain.Entities;
using Libr4.IDE.Infrastructure.Persistence.Converters;

namespace Libr4.IDE.Infrastructure.Persistence;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    public DbSet<AgentEntity> Agents { get; set; }
    public DbSet<AgentEvent> AgentEvents { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // CRITICAL MOMENT: Bind F# logic to DB column
        modelBuilder.Entity<AgentEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            
            // Magic here: Tell EF Core to use our converter
            entity.Property(e => e.State)
                .HasConversion(new AgentStateConverter())
                .HasColumnType("text") // Save as text in Postgres
                .IsRequired();
        });

        modelBuilder.Entity<AgentEvent>(entity =>
        {
            entity.HasKey(e => e.Id);
            
            // Optimistic Concurrency: Configure RowVersion
            entity.Property(e => e.RowVersion)
                .IsRowVersion();
            
            // Index on RunId for performance with 10,000+ events
            entity.HasIndex(e => e.RunId);
        });
    }
}
