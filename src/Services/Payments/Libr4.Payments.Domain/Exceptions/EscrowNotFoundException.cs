namespace Libr4.Payments.Domain.Exceptions;

public class EscrowNotFoundException : Exception
{
    public Guid EscrowId { get; }
    public EscrowNotFoundException(Guid escrowId) : base($"Escrow {escrowId} not found")
    {
        EscrowId = escrowId;
    }
}
