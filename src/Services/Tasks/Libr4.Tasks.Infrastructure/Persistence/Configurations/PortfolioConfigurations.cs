using Libr4.Tasks.Domain.Portfolio;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Libr4.Tasks.Infrastructure.Persistence.Configurations;

public sealed class PortfolioItemConfig : IEntityTypeConfiguration<PortfolioItem>
{
    public void Configure(EntityTypeBuilder<PortfolioItem> b)
    {
        b.ToTable("portfolio_items");
        b.HasKey(x => x.Id);
        b.HasIndex(x => x.UserId);
        b.HasIndex(x => x.Status);
        b.HasIndex(x => x.ItemType);
        b.HasIndex(x => x.Featured);
        b.HasIndex(x => x.CreatedAt);

        b.Property(x => x.Title).HasMaxLength(200).IsRequired();
        b.Property(x => x.Description).HasMaxLength(5000).IsRequired();
        b.Property(x => x.ItemType).HasConversion<string>();
        b.Property(x => x.Status).HasConversion<string>();
        b.Property(x => x.Client).HasMaxLength(200);
        b.Property(x => x.ProjectUrl).HasMaxLength(500);
        b.Property(x => x.GithubUrl).HasMaxLength(500);
        b.Property(x => x.LiveUrl).HasMaxLength(500);
        b.Property(x => x.Tags).HasConversion(
            v => string.Join(",", v),
            v => v.Split(new[] { ',' }, System.StringSplitOptions.RemoveEmptyEntries).ToList());
        b.Property(x => x.SkillsUsed).HasConversion(
            v => string.Join(",", v),
            v => v.Split(new[] { ',' }, System.StringSplitOptions.RemoveEmptyEntries).ToList());
        b.Property(x => x.Metadata).HasColumnType("jsonb");
    }
}
