using Microsoft.EntityFrameworkCore;
using Pgvector.EntityFrameworkCore;
using Libr4.AI.Infrastructure.Persistence;

namespace Libr4.AI.Infrastructure.VectorStore;

public class VectorDbContext : DbContext
{
    public DbSet<MemoryVector> MemoryVectors { get; set; }

    public VectorDbContext(DbContextOptions<VectorDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Ensure pgvector extension is enabled
        modelBuilder.HasPostgresExtension("vector");

        modelBuilder.Entity<MemoryVector>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Embedding)
                .HasColumnType("vector(1536)")  // OpenAI embedding dimension
                .IsRequired();
            
            // Index for similarity search
            entity.HasIndex(e => e.Embedding)
                .HasMethod("ivfflat")
                .HasOperators("vector_cosine_ops");
            
            // Indexes for filtering
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.AgentId);
            entity.HasIndex(e => e.SessionId);
        });
    }
}

public class MemoryVector
{
    public Guid Id { get; set; }
    public string MemoryId { get; set; } = string.Empty;
    public Vector Embedding { get; set; } = Vector.Zero;
    public string UserId { get; set; } = string.Empty;
    public string AgentId { get; set; } = string.Empty;
    public string SessionId { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
    public Dictionary<string, string> Metadata { get; set; } = new();
}
