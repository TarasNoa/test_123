using Libr4.Payments.Domain.Escrow;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Libr4.Payments.Infrastructure.Persistence.Configurations;

public class EscrowConfiguration : IEntityTypeConfiguration<Escrow>
{
    public void Configure(EntityTypeBuilder<Escrow> builder)
    {
        builder.ToTable("escrows", "payments");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.TaskId).IsRequired();
        builder.Property(e => e.ClientId).IsRequired();
        builder.Property(e => e.FreelancerId).IsRequired();
        builder.Property(e => e.Amount).IsRequired().HasPrecision(18, 2);
        builder.Property(e => e.Currency).IsRequired().HasMaxLength(3);
        builder.Property(e => e.Status).IsRequired().HasConversion<string>();
        builder.Property(e => e.StripePaymentIntentId).HasMaxLength(100);
        builder.Property(e => e.CreatedAt).IsRequired();
        builder.Property(e => e.ReleasedAt);
        builder.Property(e => e.RefundedAt);

        builder.HasIndex(e => e.TaskId).IsUnique();
        builder.HasIndex(e => e.ClientId);
        builder.HasIndex(e => e.FreelancerId);
        builder.HasIndex(e => e.Status);
    }
}
