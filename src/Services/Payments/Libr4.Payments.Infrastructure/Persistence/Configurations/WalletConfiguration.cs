using Libr4.Payments.Domain.Wallets;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Libr4.Payments.Infrastructure.Persistence.Configurations;

public class WalletConfiguration : IEntityTypeConfiguration<Wallet>
{
    public void Configure(EntityTypeBuilder<Wallet> builder)
    {
        builder.ToTable("wallets", "payments");

        builder.HasKey(w => w.Id);

        builder.Property(w => w.UserId).IsRequired();
        builder.Property(w => w.Balance).IsRequired().HasPrecision(18, 2);
        builder.Property(w => w.HeldBalance).IsRequired().HasPrecision(18, 2);
        builder.Property(w => w.Currency).IsRequired().HasMaxLength(3);
        builder.Property(w => w.CreatedAt).IsRequired();
        builder.Property(w => w.UpdatedAt).IsRequired();

        builder.HasIndex(w => new { w.UserId, w.Currency }).IsUnique();

        // Wallet entries — use backing field directly so EF Core tracks collection changes
        builder.Metadata
            .FindNavigation(nameof(Wallet.Entries))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);

        builder.HasMany(typeof(WalletEntry), "_entries")
            .WithOne()
            .HasForeignKey("WalletId")
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class WalletEntryConfiguration : IEntityTypeConfiguration<WalletEntry>
{
    public void Configure(EntityTypeBuilder<WalletEntry> builder)
    {
        builder.ToTable("wallet_entries", "payments");

        builder.HasKey(we => we.Id);

        builder.Property(we => we.WalletId).IsRequired();
        builder.Property(we => we.TransactionId).IsRequired();
        builder.Property(we => we.Credit).IsRequired().HasPrecision(18, 2);
        builder.Property(we => we.Debit).IsRequired().HasPrecision(18, 2);
        builder.Property(we => we.BalanceAfter).IsRequired().HasPrecision(18, 2);
        builder.Property(we => we.Description).IsRequired().HasMaxLength(500);
        builder.Property(we => we.CreatedAt).IsRequired();

        builder.HasIndex(we => we.WalletId);
        builder.HasIndex(we => we.TransactionId);
        builder.HasIndex(we => we.CreatedAt);
    }
}
