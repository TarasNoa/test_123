using Libr4.Collaboration.Domain;
using Libr4.Collaboration.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Libr4.Collaboration.Infrastructure.Repositories;

public sealed class EfDocumentRepository : IDocumentRepository
{
    private readonly CollaborationDbContext _db;

    public EfDocumentRepository(CollaborationDbContext db)
    {
        _db = db;
    }

    public Task<SharedDocument?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => _db.Documents.FirstOrDefaultAsync(d => d.Id == id, ct);

    public async Task AddAsync(SharedDocument document, CancellationToken ct = default)
    {
        await _db.Documents.AddAsync(document, ct);
        await _db.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(SharedDocument document, CancellationToken ct = default)
    {
        _db.Documents.Update(document);
        await _db.SaveChangesAsync(ct);
    }
}
