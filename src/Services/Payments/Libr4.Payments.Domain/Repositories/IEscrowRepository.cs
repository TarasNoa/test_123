using EscrowEntity = Libr4.Payments.Domain.Escrow.Escrow;

namespace Libr4.Payments.Domain.Repositories;

public interface IEscrowRepository
{
    Task<EscrowEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task AddAsync(EscrowEntity escrow, CancellationToken cancellationToken = default);
    Task UpdateAsync(EscrowEntity escrow, CancellationToken cancellationToken = default);
}
