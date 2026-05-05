using Libr4.Tasks.Domain.BlindApplications;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Libr4.Tasks.Infrastructure.Persistence.Configurations;

public sealed class BlindApplicationConfig : IEntityTypeConfiguration<BlindApplication>
{
    public void Configure(EntityTypeBuilder<BlindApplication> b)
    {
        b.ToTable("blind_applications");
        b.HasKey(x => x.Id);
        b.HasIndex(x => x.TaskId);
        b.HasIndex(x => x.ApplicantId);
        b.HasIndex(x => x.AnonymousId).IsUnique();
        b.HasIndex(x => x.Status);
        b.HasIndex(x => x.SubmittedAt);
        b.HasIndex(x => x.RevealedAt);

        b.Property(x => x.ProposalText).HasMaxLength(5000).IsRequired();
        b.Property(x => x.CoverLetter).HasMaxLength(2000);
        b.Property(x => x.Availability).HasMaxLength(500);
        b.Property(x => x.ExperienceLevel).HasMaxLength(50).IsRequired();
        b.Property(x => x.ClientNotes).HasMaxLength(2000);
        b.Property(x => x.Status).HasConversion<string>();
        b.Property(x => x.PortfolioLinks).HasConversion(
            v => string.Join(",", v),
            v => v.Split(new[] { ',' }, System.StringSplitOptions.RemoveEmptyEntries).ToList());
        b.Property(x => x.SkillTags).HasConversion(
            v => string.Join(",", v),
            v => v.Split(new[] { ',' }, System.StringSplitOptions.RemoveEmptyEntries).ToList());
        b.Property(x => x.AnonymizedProfile).HasColumnType("jsonb");
    }
}
