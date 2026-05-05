using Libr4.Payments.Application.Abstractions;
using Libr4.Payments.Domain.Escrow;
using Libr4.Payments.Domain.Invoices;
using Libr4.Payments.Domain.PaymentMethods;
using Libr4.Payments.Domain.Transactions;
using Libr4.Payments.Domain.Wallets;
using Libr4.Shared.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Libr4.Payments.Infrastructure.Persistence;

public class PaymentsDbContext : DbContextBase, IPaymentsDbContext
{
    public PaymentsDbContext(DbContextOptions<PaymentsDbContext> options, IPublisher publisher) : base(options, publisher) { }

    public DbSet<Transaction> Transactions => Set<Transaction>();
    public DbSet<Escrow> Escrows => Set<Escrow>();
    public DbSet<PaymentMethod> PaymentMethods => Set<PaymentMethod>();
    public DbSet<Wallet> Wallets => Set<Wallet>();
    public DbSet<WalletEntry> WalletEntries => Set<WalletEntry>();
    public DbSet<FraudHistory> FraudHistories => Set<FraudHistory>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(PaymentsDbContext).Assembly);
    }
}
