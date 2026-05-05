using Libr4.Tasks.Domain.DisputeResolution;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Libr4.Tasks.Infrastructure.Persistence.Configurations;

public sealed class DisputeConfig : IEntityTypeConfiguration<Dispute>
{
    public void Configure(EntityTypeBuilder<Dispute> b)
    {
        b.ToTable("disputes");
        b.HasKey(x => x.Id);
        b.HasIndex(x => x.TaskId);
        b.HasIndex(x => x.InitiatorId);
        b.HasIndex(x => x.RespondentId);
        b.HasIndex(x => x.Status);
        b.HasIndex(x => x.Severity);
        b.HasIndex(x => x.Priority);
        b.HasIndex(x => x.CreatedAt);
        b.HasIndex(x => x.ResolvedAt);

        b.Property(x => x.Category).HasConversion<string>();
        b.Property(x => x.Title).HasMaxLength(200).IsRequired();
        b.Property(x => x.Description).HasMaxLength(5000).IsRequired();
        b.Property(x => x.ResolutionRequested).HasMaxLength(100).IsRequired();
        b.Property(x => x.Status).HasConversion<string>();
        b.Property(x => x.Severity).HasConversion<string>();
        b.Property(x => x.Priority).HasConversion<string>();
        b.Property(x => x.FinalOutcome).HasMaxLength(100);
        b.Property(x => x.EscalationReason).HasMaxLength(5000);
        b.Property(x => x.DismissalReason).HasMaxLength(5000);
        b.Property(x => x.AiAnalysis).HasColumnType("jsonb");
        b.Property(x => x.EvidenceFiles).HasConversion(
            v => string.Join(",", v),
            v => v.Split(new[] { ',' }, System.StringSplitOptions.RemoveEmptyEntries).ToList());

        b.HasMany(x => x.Messages)
            .WithOne()
            .HasForeignKey(m => m.DisputeId)
            .OnDelete(DeleteBehavior.Cascade);

        b.HasMany(x => x.Evidence)
            .WithOne()
            .HasForeignKey(e => e.DisputeId)
            .OnDelete(DeleteBehavior.Cascade);

        b.HasMany(x => x.Resolutions)
            .WithOne()
            .HasForeignKey(r => r.DisputeId)
            .OnDelete(DeleteBehavior.Cascade);

        b.HasMany(x => x.Arbitrators)
            .WithOne()
            .HasForeignKey(a => a.DisputeId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class DisputeMessageConfig : IEntityTypeConfiguration<DisputeMessage>
{
    public void Configure(EntityTypeBuilder<DisputeMessage> b)
    {
        b.ToTable("dispute_messages");
        b.HasKey(x => x.Id);
        b.HasIndex(x => x.DisputeId);
        b.HasIndex(x => x.SenderId);
        b.HasIndex(x => x.CreatedAt);

        b.Property(x => x.Message).HasMaxLength(5000).IsRequired();
        b.Property(x => x.MessageType).HasMaxLength(50).IsRequired();
        b.Property(x => x.Status).HasMaxLength(20).IsRequired();
        b.Property(x => x.EvidenceFiles).HasConversion(
            v => string.Join(",", v),
            v => v.Split(new[] { ',' }, System.StringSplitOptions.RemoveEmptyEntries).ToList());
        b.Property(x => x.Attachments).HasColumnType("jsonb");
    }
}

public sealed class DisputeEvidenceConfig : IEntityTypeConfiguration<DisputeEvidence>
{
    public void Configure(EntityTypeBuilder<DisputeEvidence> b)
    {
        b.ToTable("dispute_evidence");
        b.HasKey(x => x.Id);
        b.HasIndex(x => x.DisputeId);
        b.HasIndex(x => x.SubmittedBy);
        b.HasIndex(x => x.Status);
        b.HasIndex(x => x.SubmittedAt);

        b.Property(x => x.EvidenceType).HasMaxLength(50).IsRequired();
        b.Property(x => x.EvidenceData).HasMaxLength(5000);
        b.Property(x => x.Description).HasMaxLength(5000);
        b.Property(x => x.FileName).HasMaxLength(255);
        b.Property(x => x.FileType).HasMaxLength(100);
        b.Property(x => x.FileHash).HasMaxLength(64);
        b.Property(x => x.VerificationNotes).HasMaxLength(5000);
        b.Property(x => x.InadmissibilityReason).HasMaxLength(5000);
        b.Property(x => x.Status).HasMaxLength(20).IsRequired();
    }
}

public sealed class DisputeResolutionConfig : IEntityTypeConfiguration<DisputeResolution>
{
    public void Configure(EntityTypeBuilder<DisputeResolution> b)
    {
        b.ToTable("dispute_resolutions");
        b.HasKey(x => x.Id);
        b.HasIndex(x => x.DisputeId);
        b.HasIndex(x => x.ProposerId);
        b.HasIndex(x => x.Status);
        b.HasIndex(x => x.ProposedAt);

        b.Property(x => x.ResolutionType).HasMaxLength(50).IsRequired();
        b.Property(x => x.ResolutionTerms).HasMaxLength(5000).IsRequired();
        b.Property(x => x.Response).HasMaxLength(20);
        b.Property(x => x.CounterTerms).HasMaxLength(5000);
        b.Property(x => x.ResponseReason).HasMaxLength(5000);
        b.Property(x => x.Status).HasMaxLength(20).IsRequired();
        b.Property(x => x.AiAnalysis).HasColumnType("jsonb");
        b.Property(x => x.AdditionalActions).HasConversion(
            v => string.Join(",", v),
            v => v.Split(new[] { ',' }, System.StringSplitOptions.RemoveEmptyEntries).ToList());
        b.Property(x => x.Deadlines).HasColumnType("jsonb");
    }
}

public sealed class DisputeArbitratorConfig : IEntityTypeConfiguration<DisputeArbitrator>
{
    public void Configure(EntityTypeBuilder<DisputeArbitrator> b)
    {
        b.ToTable("dispute_arbitrators");
        b.HasKey(x => x.Id);
        b.HasIndex(x => x.DisputeId);
        b.HasIndex(x => x.ArbitratorId);
        b.HasIndex(x => x.Status);
        b.HasIndex(x => x.AssignedAt);

        b.Property(x => x.AssignmentReason).HasMaxLength(5000);
        b.Property(x => x.Specialization).HasMaxLength(100);
        b.Property(x => x.ExperienceLevel).HasMaxLength(50);
        b.Property(x => x.Status).HasMaxLength(20).IsRequired();
        b.Property(x => x.Decision).HasMaxLength(5000);
        b.Property(x => x.DecisionReasoning).HasMaxLength(5000);
    }
}
