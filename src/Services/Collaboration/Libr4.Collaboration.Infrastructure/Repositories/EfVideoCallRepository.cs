using Libr4.Collaboration.Application.Abstractions;
using Libr4.Collaboration.Domain;
using Libr4.Collaboration.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Libr4.Collaboration.Infrastructure.Repositories;

public sealed class EfVideoCallRepository : IVideoCallRepository
{
    private readonly CollaborationDbContext _db;

    public EfVideoCallRepository(CollaborationDbContext db)
    {
        _db = db;
    }

    public Task<VideoCall?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => _db.VideoCalls.FirstOrDefaultAsync(v => v.Id == id, ct);

    public async Task AddAsync(VideoCall call, CancellationToken ct = default)
    {
        await _db.VideoCalls.AddAsync(call, ct);
        await _db.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(VideoCall call, CancellationToken ct = default)
    {
        _db.VideoCalls.Update(call);
        await _db.SaveChangesAsync(ct);
    }
}
