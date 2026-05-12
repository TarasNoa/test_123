using Libr4.Payments.Domain.Escrow;
using Libr4.Payments.Domain.PaymentMethods;
using Libr4.Payments.Domain.Transactions;
using Libr4.Payments.Domain.Wallets;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace Libr4.Payments.Application.Abstractions;

public interface IPaymentsDbContext
{
    DbSet<Transaction> Transactions { get; }
    DbSet<Libr4.Payments.Domain.Escrow.Escrow> Escrows { get; }
    DbSet<PaymentMethod> PaymentMethods { get; }
    DbSet<Wallet> Wallets { get; }
    DbSet<WalletEntry> WalletEntries { get; }

    EntityEntry Entry(object entity);
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
