using Libr4.Payments.Domain.Transactions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Libr4.Payments.Infrastructure.Persistence.Configurations;

public class TransactionConfiguration : IEntityTypeConfiguration<Transaction>
{
    public void Configure(EntityTypeBuilder<Transaction> builder)
    {
        builder.ToTable("transactions", "payments");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.UserId).IsRequired();
        builder.Property(t => t.RelatedTaskId);
        builder.Property(t => t.Type).IsRequired().HasConversion<string>();
        builder.Property(t => t.Status).IsRequired().HasConversion<string>();
        builder.Property(t => t.Amount).IsRequired().HasPrecision(18, 2);
        builder.Property(t => t.Currency).IsRequired().HasMaxLength(3);
        builder.Property(t => t.Description).HasMaxLength(500);
        builder.Property(t => t.StripePaymentIntentId).HasMaxLength(100);
        builder.Property(t => t.StripeChargeId).HasMaxLength(100);
        builder.Property(t => t.CreatedAt).IsRequired();
        builder.Property(t => t.CompletedAt);

        builder.HasIndex(t => t.UserId);
        builder.HasIndex(t => t.Status);
        builder.HasIndex(t => t.Type);
        builder.HasIndex(t => t.CreatedAt);
        builder.HasIndex(t => t.StripePaymentIntentId).IsUnique(false);
    }
}
