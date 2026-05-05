namespace Libr4.Payments.Domain.Invoices;

/// <summary>
/// Records a fraud incident for a user.
/// </summary>
public class FraudHistory
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public string Reason { get; private set; } = string.Empty;
    public DateTime RecordedAt { get; private set; }
    public string? InvoiceId { get; private set; }

    private FraudHistory() { } // EF Core requires parameterless constructor

    public static FraudHistory Create(Guid userId, string reason, string? invoiceId = null)
    {
        return new FraudHistory
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Reason = reason,
            RecordedAt = DateTime.UtcNow,
            InvoiceId = invoiceId
        };
    }
}
