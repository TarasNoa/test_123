using Libr4.Collaboration.Domain;
using Libr4.Collaboration.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Libr4.Collaboration.Infrastructure.Repositories;

public sealed class EfCollaborationRoomRepository : ICollaborationRoomRepository
{
    private readonly CollaborationDbContext _db;

    public EfCollaborationRoomRepository(CollaborationDbContext db)
    {
        _db = db;
    }

    public Task<CollaborationRoom?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => _db.Rooms.FirstOrDefaultAsync(r => r.Id == id, ct);

    public Task<CollaborationRoom?> GetByIdWithDetailsAsync(Guid id, CancellationToken ct = default)
        => _db.Rooms.FirstOrDefaultAsync(r => r.Id == id, ct);

    public Task<List<CollaborationRoom>> GetByUserIdAsync(Guid userId, CancellationToken ct = default)
        => _db.Rooms
            .AsNoTracking()
            .Where(r => r.CreatorId == userId || r.Participants.Any(p => p.UserId == userId))
            .ToListAsync(ct);

    public Task<List<CollaborationRoom>> GetByTaskIdAsync(Guid taskId, CancellationToken ct = default)
        => _db.Rooms.Where(r => r.TaskId == taskId).ToListAsync(ct);

    public Task<List<CollaborationRoom>> GetActiveRoomsAsync(CancellationToken ct = default)
        => _db.Rooms.Where(r => r.Status == CollaborationRoomStatus.Active).ToListAsync(ct);

    public Task<List<CollaborationRoom>> GetPublicRoomsAsync(CancellationToken ct = default)
        => _db.Rooms
            .Where(r => r.IsPublic && r.Status == CollaborationRoomStatus.Active)
            .ToListAsync(ct);

    public Task<bool> ExistsAsync(Guid id, CancellationToken ct = default)
        => _db.Rooms.AnyAsync(r => r.Id == id, ct);

    public async Task AddAsync(CollaborationRoom room, CancellationToken ct = default)
    {
        await _db.Rooms.AddAsync(room, ct);
        await _db.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(CollaborationRoom room, CancellationToken ct = default)
    {
        _db.Rooms.Update(room);
        await _db.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(CollaborationRoom room, CancellationToken ct = default)
    {
        _db.Rooms.Remove(room);
        await _db.SaveChangesAsync(ct);
    }
}
