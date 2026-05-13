using Libr4.Collaboration.Application.Abstractions;
using Libr4.Collaboration.Domain;
using Libr4.Collaboration.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Libr4.Collaboration.Infrastructure.Repositories;

public sealed class EfWhiteboardRepository : IWhiteboardRepository
{
    private readonly CollaborationDbContext _db;

    public EfWhiteboardRepository(CollaborationDbContext db)
    {
        _db = db;
    }

    public Task<Whiteboard?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => _db.Whiteboards.FirstOrDefaultAsync(w => w.Id == id, ct);

    public async Task AddAsync(Whiteboard whiteboard, CancellationToken ct = default)
    {
        await _db.Whiteboards.AddAsync(whiteboard, ct);
        await _db.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(Whiteboard whiteboard, CancellationToken ct = default)
    {
        _db.Whiteboards.Update(whiteboard);
        await _db.SaveChangesAsync(ct);
    }
}
