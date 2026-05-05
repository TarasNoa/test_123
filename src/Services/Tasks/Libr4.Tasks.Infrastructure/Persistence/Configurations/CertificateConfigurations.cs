using Libr4.Tasks.Domain.Certificates;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Libr4.Tasks.Infrastructure.Persistence.Configurations;

public sealed class CertificateConfig : IEntityTypeConfiguration<Certificate>
{
    public void Configure(EntityTypeBuilder<Certificate> b)
    {
        b.ToTable("certificates");
        b.HasKey(x => x.Id);
        b.HasIndex(x => x.UserId);
        b.HasIndex(x => x.Status);
        b.HasIndex(x => x.CertificateType);
        b.HasIndex(x => x.IssuedDate);
        b.HasIndex(x => x.ExpiryDate);
        b.HasIndex(x => x.CreatedAt);

        b.Property(x => x.Title).HasMaxLength(200).IsRequired();
        b.Property(x => x.Description).HasMaxLength(5000).IsRequired();
        b.Property(x => x.CertificateType).HasConversion<string>();
        b.Property(x => x.Status).HasConversion<string>();
        b.Property(x => x.IssuingOrganization).HasMaxLength(200).IsRequired();
        b.Property(x => x.CertificateUrl).HasMaxLength(500);
        b.Property(x => x.CredentialId).HasMaxLength(200);
        b.Property(x => x.VerificationNotes).HasMaxLength(1000);
        b.Property(x => x.Skills).HasConversion(
            v => string.Join(",", v),
            v => v.Split(new[] { ',' }, System.StringSplitOptions.RemoveEmptyEntries).ToList());
        b.Property(x => x.Tags).HasConversion(
            v => string.Join(",", v),
            v => v.Split(new[] { ',' }, System.StringSplitOptions.RemoveEmptyEntries).ToList());
        b.Property(x => x.Metadata).HasColumnType("jsonb");

        b.HasMany(x => x.Verifications)
            .WithOne()
            .HasForeignKey(v => v.CertificateId)
            .OnDelete(DeleteBehavior.Cascade);

        b.HasMany(x => x.Endorsements)
            .WithOne()
            .HasForeignKey(e => e.CertificateId)
            .OnDelete(DeleteBehavior.Cascade);

        b.HasMany(x => x.Attachments)
            .WithOne()
            .HasForeignKey(a => a.CertificateId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class CertificateVerificationConfig : IEntityTypeConfiguration<CertificateVerification>
{
    public void Configure(EntityTypeBuilder<CertificateVerification> b)
    {
        b.ToTable("certificate_verifications");
        b.HasKey(x => x.Id);
        b.HasIndex(x => x.CertificateId);
        b.HasIndex(x => x.VerifierId);
        b.HasIndex(x => x.CreatedAt);

        b.Property(x => x.Status).HasMaxLength(50).IsRequired();
        b.Property(x => x.VerificationNotes).HasMaxLength(1000);
    }
}

public sealed class CertificateEndorsementConfig : IEntityTypeConfiguration<CertificateEndorsement>
{
    public void Configure(EntityTypeBuilder<CertificateEndorsement> b)
    {
        b.ToTable("certificate_endorsements");
        b.HasKey(x => x.Id);
        b.HasIndex(x => x.CertificateId);
        b.HasIndex(x => x.EndorserId);
        b.HasIndex(x => x.CreatedAt);

        b.Property(x => x.EndorsementText).HasMaxLength(1000).IsRequired();
    }
}

public sealed class CertificateAttachmentConfig : IEntityTypeConfiguration<CertificateAttachment>
{
    public void Configure(EntityTypeBuilder<CertificateAttachment> b)
    {
        b.ToTable("certificate_attachments");
        b.HasKey(x => x.Id);
        b.HasIndex(x => x.CertificateId);
        b.HasIndex(x => x.UploadedAt);

        b.Property(x => x.Filename).HasMaxLength(255).IsRequired();
        b.Property(x => x.FilePath).HasMaxLength(500).IsRequired();
        b.Property(x => x.MimeType).HasMaxLength(100).IsRequired();
        b.Property(x => x.Description).HasMaxLength(500);
    }
}
