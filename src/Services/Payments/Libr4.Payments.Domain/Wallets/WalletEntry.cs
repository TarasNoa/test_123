namespace Libr4.Payments.Domain.Wallets;

public class WalletEntry
{
    public Guid Id { get; private set; }
    public Guid WalletId { get; private set; }
    public Guid TransactionId { get; private set; }
    public decimal Credit { get; private set; }
    public decimal Debit { get; private set; }
    public decimal BalanceAfter { get; private set; }
    public string Description { get; private set; } = string.Empty;
    public DateTime CreatedAt { get; private set; }

    private WalletEntry() { } // EF Core

    public static WalletEntry Create(
        Guid id,
        Guid walletId,
        Guid transactionId,
        decimal credit,
        decimal debit,
        decimal balanceAfter,
        string description)
    {
        return new WalletEntry
        {
            Id = id,
            WalletId = walletId,
            TransactionId = transactionId,
            Credit = credit,
            Debit = debit,
            BalanceAfter = balanceAfter,
            Description = description,
            CreatedAt = DateTime.UtcNow
        };
    }
}
