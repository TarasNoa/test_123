namespace Libr4.IDE.Domain;

using Libr4.Shared.Kernel;

public interface ICodeSessionRepository
{
    Task<CodeSession?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task AddAsync(CodeSession session, CancellationToken cancellationToken = default);
    Task UpdateAsync(CodeSession session, CancellationToken cancellationToken = default);
    Task DeleteAsync(CodeSession session, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
    Task<List<CodeSession>> GetByCreatorIdAsync(Guid creatorId, CancellationToken cancellationToken = default);
    Task<List<CodeSession>> GetActiveSessionsAsync(CancellationToken cancellationToken = default);
}
