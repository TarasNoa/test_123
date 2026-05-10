using Microsoft.EntityFrameworkCore;
using Libr4.AI.Domain.Agents;
using Libr4.AI.Domain.OrderAssistant;
using Libr4.AI.Domain.TaskRecommendations;

namespace Libr4.AI.Infrastructure;

public class AIDbContext : DbContext, IAIDbContext
{
    public AIDbContext(DbContextOptions<AIDbContext> options) : base(options) { }

    public DbSet<Agent> Agents { get; set; }
    public DbSet<OrderSuggestion> OrderSuggestions { get; set; }
    public DbSet<TaskRecommendation> TaskRecommendations { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Agent configuration
        modelBuilder.Entity<Agent>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Role).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Prompt).HasMaxLength(1000);
            entity.Property(e => e.Status).HasConversion<string>();
        });

        // OrderSuggestion configuration
        modelBuilder.Entity<OrderSuggestion>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.TaskTitle).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Description).HasMaxLength(1000);
            entity.Property(e => e.RecommendedFreelancers).HasConversion(
                v => string.Join(',', v),
                v => v.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList());
            entity.Property(e => e.ConfidenceScore).HasPrecision(3, 2);
        });

        // TaskRecommendation configuration
        modelBuilder.Entity<TaskRecommendation>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.TaskTitle).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Reason).HasMaxLength(500);
            entity.Property(e => e.MatchingSkills).HasConversion(
                v => string.Join(',', v),
                v => v.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList());
            entity.Property(e => e.MatchScore).HasPrecision(3, 2);
        });
    }
}