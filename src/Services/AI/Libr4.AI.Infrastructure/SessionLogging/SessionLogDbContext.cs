/*
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Libr4.AI.Infrastructure.SessionLogging;

/// <summary>
/// EF Core database context for session logging.
/// Supports PostgreSQL with JSONB columns for flexibility.
/// </summary>
public class SessionLogDbContext : DbContext
{
    public SessionLogDbContext(DbContextOptions<SessionLogDbContext> options) : base(options)
    {
    }

    public DbSet<SessionLog> Sessions { get; set; } = null!;
    public DbSet<SessionMessageLog> Messages { get; set; } = null!;
    public DbSet<SessionToolLog> ToolExecutions { get; set; } = null!;
    public DbSet<SessionErrorLog> Errors { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // SessionLog configuration
        modelBuilder.Entity<SessionLog>(entity =>
        {
            entity.ToTable("session_logs", "logging");
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.AgentId);
            entity.HasIndex(e => e.ProjectId);
            entity.HasIndex(e => e.StartedAt);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => new { e.UserId, e.StartedAt }); // Common query pattern

            entity.Property(e => e.Id).HasMaxLength(50);
            entity.Property(e => e.UserId).HasMaxLength(100).IsRequired();
            entity.Property(e => e.AgentId).HasMaxLength(100);
            entity.Property(e => e.ProjectId).HasMaxLength(100);
            entity.Property(e => e.Status).HasConversion<string>().HasMaxLength(20);
            entity.Property(e => e.EndReason).HasMaxLength(50);
            entity.Property(e => e.Metadata)
                .HasColumnType("jsonb")
                .HasDefaultValueSql("'{}'");
            entity.Property(e => e.EstimatedCost).HasPrecision(18, 6);

            entity.HasMany(e => e.Messages)
                .WithOne()
                .HasForeignKey(e => e.SessionId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(e => e.ToolExecutions)
                .WithOne()
                .HasForeignKey(e => e.SessionId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(e => e.Errors)
                .WithOne()
                .HasForeignKey(e => e.SessionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // SessionMessageLog configuration
        modelBuilder.Entity<SessionMessageLog>(entity =>
        {
            entity.ToTable("session_messages", "logging");
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.SessionId);
            entity.HasIndex(e => e.Timestamp);

            entity.Property(e => e.Id).HasMaxLength(50);
            entity.Property(e => e.SessionId).HasMaxLength(50).IsRequired();
            entity.Property(e => e.Role).HasConversion<string>().HasMaxLength(20);
            entity.Property(e => e.Model).HasMaxLength(100);
            entity.Property(e => e.Content)
                .HasColumnType("text"); // Full text for message content
            entity.Property(e => e.Metadata)
                .HasColumnType("jsonb")
                .HasDefaultValueSql("'{}'");
        });

        // SessionToolLog configuration
        modelBuilder.Entity<SessionToolLog>(entity =>
        {
            entity.ToTable("session_tools", "logging");
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.SessionId);
            entity.HasIndex(e => e.ToolName);
            entity.HasIndex(e => e.Timestamp);

            entity.Property(e => e.Id).HasMaxLength(50);
            entity.Property(e => e.SessionId).HasMaxLength(50).IsRequired();
            entity.Property(e => e.ToolName).HasMaxLength(100).IsRequired();
            entity.Property(e => e.ToolInput).HasColumnType("text");
            entity.Property(e => e.ToolOutput).HasColumnType("text");
        });

        // SessionErrorLog configuration
        modelBuilder.Entity<SessionErrorLog>(entity =>
        {
            entity.ToTable("session_errors", "logging");
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.SessionId);
            entity.HasIndex(e => e.ErrorType);
            entity.HasIndex(e => e.Timestamp);

            entity.Property(e => e.Id).HasMaxLength(50);
            entity.Property(e => e.SessionId).HasMaxLength(50).IsRequired();
            entity.Property(e => e.ErrorType).HasMaxLength(100).IsRequired();
            entity.Property(e => e.ErrorMessage).HasMaxLength(2000);
            entity.Property(e => e.StackTrace).HasColumnType("text");
            entity.Property(e => e.Context).HasMaxLength(500);
        });
    }
}

/// <summary>
/// Session log entity - top level session container.
/// </summary>
public class SessionLog
{
    public string Id { get; set; } = null!;
    public string UserId { get; set; } = null!;
    public string? AgentId { get; set; }
    public string? ProjectId { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? EndedAt { get; set; }
    public DateTime? LastActivityAt { get; set; }
    public long? DurationSeconds { get; set; }
    public SessionStatus Status { get; set; }
    public string? EndReason { get; set; }
    public int MessageCount { get; set; }
    public int TokenCount { get; set; }
    public int ErrorCount { get; set; } = 0;
    public decimal EstimatedCost { get; set; }
    public Dictionary<string, string> Metadata { get; set; } = new();

    // Navigation properties
    public List<SessionMessageLog> Messages { get; set; } = new();
    public List<SessionToolLog> ToolExecutions { get; set; } = new();
    public List<SessionErrorLog> Errors { get; set; } = new();
}

/// <summary>
/// Individual message within a session.
/// </summary>
public class SessionMessageLog
{
    public string Id { get; set; } = null!;
    public string SessionId { get; set; } = null!;
    public MessageRole Role { get; set; }
    public string Content { get; set; } = null!;
    public bool ContentEncrypted { get; set; } = false;
    public string Model { get; set; } = "unknown";
    public int PromptTokens { get; set; }
    public int CompletionTokens { get; set; }
    public int TotalTokens { get; set; }
    public DateTime Timestamp { get; set; }
    public Dictionary<string, string> Metadata { get; set; } = new();
}

/// <summary>
/// Tool execution log within a session.
/// </summary>
public class SessionToolLog
{
    public string Id { get; set; } = null!;
    public string SessionId { get; set; } = null!;
    public string ToolName { get; set; } = null!;
    public string? ToolInput { get; set; }
    public string? ToolOutput { get; set; }
    public bool Success { get; set; } = true;
    public long? DurationMs { get; set; }
    public DateTime Timestamp { get; set; }
}

/// <summary>
/// Error log within a session.
/// </summary>
public class SessionErrorLog
{
    public string Id { get; set; } = null!;
    public string SessionId { get; set; } = null!;
    public string ErrorType { get; set; } = null!;
    public string ErrorMessage { get; set; } = null!;
    public string? StackTrace { get; set; }
    public string? Context { get; set; }
    public DateTime Timestamp { get; set; }
}
*/
