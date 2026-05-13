namespace Libr4.Collaboration.Domain;

public interface IDocumentRepository
{
    Task<SharedDocument?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task AddAsync(SharedDocument document, CancellationToken cancellationToken = default);
    Task UpdateAsync(SharedDocument document, CancellationToken cancellationToken = default);
}
