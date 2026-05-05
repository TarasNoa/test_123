using Libr4.Tasks.Domain.TeamsPortfolio;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Libr4.Tasks.Infrastructure.Persistence.Configurations;

public sealed class FreelancerTeamConfig : IEntityTypeConfiguration<FreelancerTeam>
{
    public void Configure(EntityTypeBuilder<FreelancerTeam> b)
    {
        b.ToTable("teams");
        b.HasKey(x => x.Id);
        b.HasIndex(x => x.CreatedBy);
        b.HasIndex(x => x.IsActive);
        b.HasIndex(x => x.IsVerified);
        b.HasIndex(x => x.CreatedAt);

        b.Property(x => x.Name).HasMaxLength(255).IsRequired();
        b.Property(x => x.Description).HasMaxLength(5000);
        b.Property(x => x.Tagline).HasMaxLength(500);
        b.Property(x => x.Website).HasMaxLength(500);
        b.Property(x => x.Location).HasMaxLength(255);
        b.Property(x => x.Timezone).HasMaxLength(50);
        b.Property(x => x.PreferredRateType).HasMaxLength(20);
        b.Property(x => x.LogoUrl).HasMaxLength(500);
        b.Property(x => x.BannerUrl).HasMaxLength(500);
        b.Property(x => x.BrandColors).HasColumnType("jsonb");
        b.Property(x => x.Languages).HasConversion(
            v => string.Join(",", v),
            v => v.Split(new[] { ',' }, System.StringSplitOptions.RemoveEmptyEntries).ToList());
        b.Property(x => x.Skills).HasConversion(
            v => string.Join(",", v),
            v => v.Split(new[] { ',' }, System.StringSplitOptions.RemoveEmptyEntries).ToList());
        b.Property(x => x.Industries).HasConversion(
            v => string.Join(",", v),
            v => v.Split(new[] { ',' }, System.StringSplitOptions.RemoveEmptyEntries).ToList());
        b.Property(x => x.Categories).HasConversion(
            v => string.Join(",", v),
            v => v.Split(new[] { ',' }, System.StringSplitOptions.RemoveEmptyEntries).ToList());

        b.HasMany(x => x.Members)
            .WithOne()
            .HasForeignKey(m => m.TeamId)
            .OnDelete(DeleteBehavior.Cascade);

        b.HasMany(x => x.PortfolioItems)
            .WithOne()
            .HasForeignKey(p => p.TeamId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class TeamMemberConfig : IEntityTypeConfiguration<TeamMember>
{
    public void Configure(EntityTypeBuilder<TeamMember> b)
    {
        b.ToTable("team_members");
        b.HasKey(x => x.Id);
        b.HasIndex(x => x.TeamId);
        b.HasIndex(x => x.UserId);
        b.HasIndex(x => x.Status);

        b.Property(x => x.Role).HasConversion<string>();
        b.Property(x => x.Title).HasMaxLength(100);
        b.Property(x => x.Bio).HasMaxLength(5000);
        b.Property(x => x.Status).HasConversion<string>();
        b.Property(x => x.Permissions).HasConversion(
            v => string.Join(",", v),
            v => v.Split(new[] { ',' }, System.StringSplitOptions.RemoveEmptyEntries).ToList());
    }
}

public sealed class ReviewConfig : IEntityTypeConfiguration<Review>
{
    public void Configure(EntityTypeBuilder<Review> b)
    {
        b.ToTable("team_reviews");
        b.HasKey(x => x.Id);
        b.HasIndex(x => x.ReviewerId);
        b.HasIndex(x => x.TargetId);
        b.HasIndex(x => x.TaskId);
        b.HasIndex(x => x.IsPublic);
        b.HasIndex(x => x.CreatedAt);

        b.Property(x => x.TargetType).HasConversion<string>();
        b.Property(x => x.ReviewText).HasMaxLength(5000);
        b.Property(x => x.ResponseText).HasMaxLength(5000);
        b.Property(x => x.CriteriaScores).HasColumnType("jsonb");
        b.Property(x => x.Strengths).HasConversion(
            v => string.Join(",", v),
            v => v.Split(new[] { ',' }, System.StringSplitOptions.RemoveEmptyEntries).ToList());
        b.Property(x => x.Improvements).HasConversion(
            v => string.Join(",", v),
            v => v.Split(new[] { ',' }, System.StringSplitOptions.RemoveEmptyEntries).ToList());
    }
}

public sealed class RateHistoryConfig : IEntityTypeConfiguration<RateHistory>
{
    public void Configure(EntityTypeBuilder<RateHistory> b)
    {
        b.ToTable("rate_history");
        b.HasKey(x => x.Id);
        b.HasIndex(x => x.UserId);
        b.HasIndex(x => x.IsCurrent);
        b.HasIndex(x => x.EffectiveDate);

        b.Property(x => x.RateType).HasConversion<string>();
        b.Property(x => x.Currency).HasMaxLength(3).IsRequired();
        b.Property(x => x.ProjectType).HasMaxLength(100);
        b.Property(x => x.ExperienceLevel).HasMaxLength(50);
        b.Property(x => x.ReasonForChange).HasMaxLength(255);
        b.Property(x => x.Skills).HasConversion(
            v => string.Join(",", v),
            v => v.Split(new[] { ',' }, System.StringSplitOptions.RemoveEmptyEntries).ToList());
    }
}

public sealed class SkillTestConfig : IEntityTypeConfiguration<SkillTest>
{
    public void Configure(EntityTypeBuilder<SkillTest> b)
    {
        b.ToTable("skill_tests");
        b.HasKey(x => x.Id);
        b.HasIndex(x => x.Category);
        b.HasIndex(x => x.IsActive);
        b.HasIndex(x => x.CreatedAt);

        b.Property(x => x.Name).HasMaxLength(255).IsRequired();
        b.Property(x => x.Description).HasMaxLength(5000);
        b.Property(x => x.Category).HasMaxLength(100).IsRequired();
        b.Property(x => x.Difficulty).HasConversion<string>();
        b.Property(x => x.Instructions).HasMaxLength(5000);
        b.Property(x => x.Questions).HasColumnType("jsonb");
        b.Property(x => x.Resources).HasColumnType("jsonb");

        b.HasMany(x => x.Results)
            .WithOne()
            .HasForeignKey(r => r.TestId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class SkillTestResultConfig : IEntityTypeConfiguration<SkillTestResult>
{
    public void Configure(EntityTypeBuilder<SkillTestResult> b)
    {
        b.ToTable("skill_test_results");
        b.HasKey(x => x.Id);
        b.HasIndex(x => x.TestId);
        b.HasIndex(x => x.UserId);
        b.HasIndex(x => x.Passed);
        b.HasIndex(x => x.CompletedAt);

        b.Property(x => x.IpAddress).HasMaxLength(45);
        b.Property(x => x.UserAgent).HasMaxLength(500);
        b.Property(x => x.VerificationMethod).HasMaxLength(50);
        b.Property(x => x.Answers).HasColumnType("jsonb");
    }
}

public sealed class ClientVerificationConfig : IEntityTypeConfiguration<ClientVerification>
{
    public void Configure(EntityTypeBuilder<ClientVerification> b)
    {
        b.ToTable("client_verifications");
        b.HasKey(x => x.Id);
        b.HasIndex(x => x.UserId);
        b.HasIndex(x => x.Status);
        b.HasIndex(x => x.SubmittedAt);

        b.Property(x => x.VerificationType).HasMaxLength(50).IsRequired();
        b.Property(x => x.BusinessName).HasMaxLength(255);
        b.Property(x => x.BusinessAddress).HasMaxLength(5000);
        b.Property(x => x.BusinessPhone).HasMaxLength(50);
        b.Property(x => x.BusinessEmail).HasMaxLength(255);
        b.Property(x => x.Website).HasMaxLength(500);
        b.Property(x => x.TaxId).HasMaxLength(100);
        b.Property(x => x.RegistrationNumber).HasMaxLength(100);
        b.Property(x => x.BusinessType).HasMaxLength(100);
        b.Property(x => x.Status).HasConversion<string>();
        b.Property(x => x.RejectionReason).HasMaxLength(5000);
        b.Property(x => x.VerificationNotes).HasMaxLength(5000);
        b.Property(x => x.BadgeLevel).HasMaxLength(20);
        b.Property(x => x.BadgeUrl).HasMaxLength(500);
        b.Property(x => x.Documents).HasColumnType("jsonb");
    }
}

public sealed class TeamPortfolioItemConfig : IEntityTypeConfiguration<PortfolioItem>
{
    public void Configure(EntityTypeBuilder<PortfolioItem> b)
    {
        b.ToTable("team_portfolio_items");
        b.HasKey(x => x.Id);
        b.HasIndex(x => x.UserId);
        b.HasIndex(x => x.TeamId);
        b.HasIndex(x => x.IsPublic);
        b.HasIndex(x => x.CreatedAt);

        b.Property(x => x.Title).HasMaxLength(255).IsRequired();
        b.Property(x => x.Description).HasMaxLength(5000);
        b.Property(x => x.Category).HasMaxLength(100);
        b.Property(x => x.ProjectUrl).HasMaxLength(500);
        b.Property(x => x.ClientName).HasMaxLength(255);
        b.Property(x => x.ClientTestimonial).HasMaxLength(5000);
        b.Property(x => x.ProjectDuration).HasMaxLength(100);
        b.Property(x => x.RoleInProject).HasMaxLength(100);
        b.Property(x => x.BudgetRange).HasColumnType("jsonb");
        b.Property(x => x.Tags).HasConversion(
            v => string.Join(",", v),
            v => v.Split(new[] { ',' }, System.StringSplitOptions.RemoveEmptyEntries).ToList());
        b.Property(x => x.Images).HasConversion(
            v => string.Join(",", v),
            v => v.Split(new[] { ',' }, System.StringSplitOptions.RemoveEmptyEntries).ToList());
        b.Property(x => x.Videos).HasConversion(
            v => string.Join(",", v),
            v => v.Split(new[] { ',' }, System.StringSplitOptions.RemoveEmptyEntries).ToList());
        b.Property(x => x.Files).HasConversion(
            v => string.Join(",", v),
            v => v.Split(new[] { ',' }, System.StringSplitOptions.RemoveEmptyEntries).ToList());
        b.Property(x => x.Technologies).HasConversion(
            v => string.Join(",", v),
            v => v.Split(new[] { ',' }, System.StringSplitOptions.RemoveEmptyEntries).ToList());
        b.Property(x => x.ToolsUsed).HasConversion(
            v => string.Join(",", v),
            v => v.Split(new[] { ',' }, System.StringSplitOptions.RemoveEmptyEntries).ToList());
        b.Property(x => x.Methodologies).HasConversion(
            v => string.Join(",", v),
            v => v.Split(new[] { ',' }, System.StringSplitOptions.RemoveEmptyEntries).ToList());
    }
}

public sealed class PortfolioAnalyticsConfig : IEntityTypeConfiguration<PortfolioAnalytics>
{
    public void Configure(EntityTypeBuilder<PortfolioAnalytics> b)
    {
        b.ToTable("team_portfolio_analytics");
        b.HasKey(x => x.Id);
        b.HasIndex(x => x.PortfolioItemId).IsUnique();
        b.HasIndex(x => x.UpdatedAt);

        b.Property(x => x.ViewsByCountry).HasColumnType("jsonb");
        b.Property(x => x.ViewsBySource).HasColumnType("jsonb");
        b.Property(x => x.DailyViews).HasColumnType("jsonb");
    }
}
