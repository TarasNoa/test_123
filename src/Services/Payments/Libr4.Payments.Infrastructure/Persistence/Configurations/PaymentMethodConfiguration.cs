using Libr4.Payments.Domain.PaymentMethods;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Libr4.Payments.Infrastructure.Persistence.Configurations;

public class PaymentMethodConfiguration : IEntityTypeConfiguration<PaymentMethod>
{
    public void Configure(EntityTypeBuilder<PaymentMethod> builder)
    {
        builder.ToTable("payment_methods", "payments");

        builder.HasKey(pm => pm.Id);

        builder.Property(pm => pm.UserId).IsRequired();
        builder.Property(pm => pm.Type).IsRequired().HasConversion<string>();
        builder.Property(pm => pm.StripePaymentMethodId).HasMaxLength(100);
        builder.Property(pm => pm.Last4).HasMaxLength(4);
        builder.Property(pm => pm.Brand).HasMaxLength(50);
        builder.Property(pm => pm.ExpMonth);
        builder.Property(pm => pm.ExpYear);
        builder.Property(pm => pm.IsDefault).IsRequired();
        builder.Property(pm => pm.CreatedAt).IsRequired();

        builder.HasIndex(pm => pm.UserId);
        builder.HasIndex(pm => pm.StripePaymentMethodId).IsUnique();
        builder.HasIndex(pm => new { pm.UserId, pm.IsDefault }).IsUnique(false);
    }
}
