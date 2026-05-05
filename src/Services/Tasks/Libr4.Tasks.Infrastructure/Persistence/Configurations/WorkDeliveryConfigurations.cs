using Libr4.Tasks.Domain.WorkDelivery;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WorkDeliveryDomain = Libr4.Tasks.Domain.WorkDelivery.WorkDelivery;

namespace Libr4.Tasks.Infrastructure.Persistence.Configurations;

public sealed class WorkDeliveryConfig : IEntityTypeConfiguration<WorkDeliveryDomain>
{
    public void Configure(EntityTypeBuilder<WorkDeliveryDomain> b)
    {
        b.ToTable("work_deliveries");
        b.HasKey(x => x.Id);
        b.HasIndex(x => x.TaskId);
        b.HasIndex(x => x.FreelancerId);
        b.HasIndex(x => x.ClientId);
        b.HasIndex(x => x.Status);
        b.HasIndex(x => x.CreatedAt);
        b.HasIndex(x => x.SubmittedAt);

        b.Property(x => x.Title).HasMaxLength(200).IsRequired();
        b.Property(x => x.Description).HasMaxLength(5000);
        b.Property(x => x.Status).HasConversion<string>();
        b.Property(x => x.PreviewType).HasConversion<string>();
        b.Property(x => x.PreviewUrl).HasMaxLength(500);
        b.Property(x => x.PreviewContainerId).HasMaxLength(100);
        b.Property(x => x.PaymentCurrency).HasMaxLength(3).IsRequired();
        b.Property(x => x.PaymentStatus).HasMaxLength(20).IsRequired();
        b.Property(x => x.PaymentTransactionId).HasMaxLength(100);
        b.Property(x => x.ExtraData).HasColumnType("jsonb");

        b.HasMany(x => x.Files)
            .WithOne()
            .HasForeignKey(f => f.DeliveryId)
            .OnDelete(DeleteBehavior.Cascade);

        b.HasMany(x => x.PreviewSessions)
            .WithOne()
            .HasForeignKey(p => p.DeliveryId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class WorkDeliveryFileConfig : IEntityTypeConfiguration<WorkDeliveryFile>
{
    public void Configure(EntityTypeBuilder<WorkDeliveryFile> b)
    {
        b.ToTable("work_delivery_files");
        b.HasKey(x => x.Id);
        b.HasIndex(x => x.DeliveryId);
        b.HasIndex(x => x.UploadedAt);

        b.Property(x => x.Filename).HasMaxLength(255).IsRequired();
        b.Property(x => x.OriginalFilename).HasMaxLength(255).IsRequired();
        b.Property(x => x.FilePath).HasMaxLength(500).IsRequired();
        b.Property(x => x.MimeType).HasMaxLength(100).IsRequired();
        b.Property(x => x.FileCategory).HasMaxLength(50);
        b.Property(x => x.ScanResult).HasMaxLength(50);
        b.Property(x => x.ContentPreview).HasMaxLength(5000);
        b.Property(x => x.ScanDetails).HasColumnType("jsonb");
    }
}

public sealed class PreviewSessionConfig : IEntityTypeConfiguration<PreviewSession>
{
    public void Configure(EntityTypeBuilder<PreviewSession> b)
    {
        b.ToTable("preview_sessions");
        b.HasKey(x => x.Id);
        b.HasIndex(x => x.DeliveryId);
        b.HasIndex(x => x.ClientId);
        b.HasIndex(x => x.SessionToken).IsUnique();
        b.HasIndex(x => x.CreatedAt);

        b.Property(x => x.SessionToken).HasMaxLength(100).IsRequired();
        b.Property(x => x.Status).HasMaxLength(20).IsRequired();
        b.Property(x => x.PreviewUrl).HasMaxLength(500);
        b.Property(x => x.WebsocketUrl).HasMaxLength(500);
        b.Property(x => x.ContainerId).HasMaxLength(100);
        b.Property(x => x.ContainerName).HasMaxLength(100);
        b.Property(x => x.CpuLimit).HasMaxLength(10).IsRequired();
        b.Property(x => x.MemoryLimit).HasMaxLength(10).IsRequired();
        b.Property(x => x.ClientNotes).HasMaxLength(5000);
        b.Property(x => x.ErrorMessage).HasMaxLength(5000);
        b.Property(x => x.InteractionsLog).HasColumnType("jsonb");
        b.Property(x => x.ErrorDetails).HasColumnType("jsonb");
    }
}
