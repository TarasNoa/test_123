using Libr4.Shared.Kernel.Domain;
using Libr4.Shared.Kernel.Errors;
using Libr4.Shared.Kernel.Results;

namespace Libr4.Payments.Domain.Wallets;

public class Wallet : AggregateRoot<Guid>
{
    public Guid UserId { get; private set; }
    public decimal Balance { get; private set; }
    public decimal HeldBalance { get; private set; }
    public string Currency { get; private set; } = "USD";
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    private readonly List<WalletEntry> _entries = new();
    public IReadOnlyCollection<WalletEntry> Entries => _entries.AsReadOnly();

    private Wallet() { } // EF Core

    public static Wallet Create(Guid id, Guid userId, string currency = "USD")
    {
        return new Wallet
        {
            Id = id,
            UserId = userId,
            Balance = 0,
            HeldBalance = 0,
            Currency = currency,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    public void Credit(decimal amount, Guid transactionId, string description)
    {
        if (amount <= 0)
            throw new DomainException("Credit amount must be positive");

        var entry = WalletEntry.Create(
            Guid.NewGuid(),
            Id,
            transactionId,
            amount,
            0,
            Balance + amount,
            description);

        _entries.Add(entry);
        Balance += amount;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Debit(decimal amount, Guid transactionId, string description)
    {
        if (amount <= 0)
            throw new DomainException("Debit amount must be positive");

        if (Balance < amount)
            throw new DomainException("Insufficient balance");

        var entry = WalletEntry.Create(
            Guid.NewGuid(),
            Id,
            transactionId,
            0,
            amount,
            Balance - amount,
            description);

        _entries.Add(entry);
        Balance -= amount;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Hold(decimal amount)
    {
        if (amount <= 0)
            throw new DomainException("Hold amount must be positive");

        if (Balance < amount)
            throw new DomainException("Insufficient balance for hold");

        Balance -= amount;
        HeldBalance += amount;
        UpdatedAt = DateTime.UtcNow;
    }

    public void ReleaseHold(decimal amount)
    {
        if (amount <= 0)
            throw new DomainException("Release amount must be positive");

        if (HeldBalance < amount)
            throw new DomainException("Insufficient held balance");

        HeldBalance -= amount;
        Balance += amount;
        UpdatedAt = DateTime.UtcNow;
    }

    public void ReleaseHoldToBeneficiary(decimal amount, Guid beneficiaryWalletId, Guid transactionId, string description)
    {
        if (amount <= 0)
            throw new DomainException("Release amount must be positive");

        if (HeldBalance < amount)
            throw new DomainException("Insufficient held balance");

        HeldBalance -= amount;

        var entry = WalletEntry.Create(
            Guid.NewGuid(),
            Id,
            transactionId,
            0,
            amount,
            Balance,
            $"Transfer to wallet {beneficiaryWalletId}: {description}");

        _entries.Add(entry);
        UpdatedAt = DateTime.UtcNow;
    }
}
