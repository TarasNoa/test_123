using Libr4.Matching.Domain.Matches;
using Microsoft.EntityFrameworkCore;

namespace Libr4.Matching.Infrastructure.Persistence;

public class MatchingDbContext : DbContext
{
    public MatchingDbContext(DbContextOptions<MatchingDbContext> options) : base(options) { }

    public DbSet<Match> Matches => Set<Match>();
    public DbSet<ScoringWeightsEntity> ScoringWeights => Set<ScoringWeightsEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("matching");

        modelBuilder.Entity<Match>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.MatchingSkills)
                .HasConversion(
                    v => string.Join(',', v),
                    v => (IReadOnlyList<string>)v.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList());
        });

        modelBuilder.Entity<ScoringWeightsEntity>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.IsActive);
        });
    }
}

public class ScoringWeightsEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public bool IsActive { get; set; }
    public double KeywordSkillWeight { get; set; }
    public double SemanticWeight { get; set; }
    public double ExperienceWeight { get; set; }
    public double ReputationWeight { get; set; }
    public double RecencyWeight { get; set; }
    public double BudgetFitWeight { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
