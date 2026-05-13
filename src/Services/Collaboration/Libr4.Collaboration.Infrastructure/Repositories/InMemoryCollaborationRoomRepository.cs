using Libr4.Collaboration.Domain;

namespace Libr4.Collaboration.Infrastructure.Repositories;

public sealed class InMemoryCollaborationRoomRepository : ICollaborationRoomRepository
{
    private readonly List<CollaborationRoom> _rooms = new();

    public Task<CollaborationRoom?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_rooms.FirstOrDefault(r => r.Id == id));
    }

    public Task<CollaborationRoom?> GetByIdWithDetailsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_rooms.FirstOrDefault(r => r.Id == id));
    }

    public Task<List<CollaborationRoom>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var rooms = _rooms
            .Where(r => r.CreatorId == userId || r.Participants.Any(p => p.UserId == userId))
            .ToList();
        return Task.FromResult(rooms);
    }

    public Task<List<CollaborationRoom>> GetByTaskIdAsync(Guid taskId, CancellationToken cancellationToken = default)
    {
        var rooms = _rooms.Where(r => r.TaskId == taskId).ToList();
        return Task.FromResult(rooms);
    }

    public Task<List<CollaborationRoom>> GetActiveRoomsAsync(CancellationToken cancellationToken = default)
    {
        var rooms = _rooms.Where(r => r.Status == CollaborationRoomStatus.Active).ToList();
        return Task.FromResult(rooms);
    }

    public Task<List<CollaborationRoom>> GetPublicRoomsAsync(CancellationToken cancellationToken = default)
    {
        var rooms = _rooms.Where(r => r.IsPublic && r.Status == CollaborationRoomStatus.Active).ToList();
        return Task.FromResult(rooms);
    }

    public Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_rooms.Any(r => r.Id == id));
    }

    public Task AddAsync(CollaborationRoom room, CancellationToken cancellationToken = default)
    {
        _rooms.Add(room);
        return Task.CompletedTask;
    }

    public Task UpdateAsync(CollaborationRoom room, CancellationToken cancellationToken = default)
    {
        var index = _rooms.FindIndex(r => r.Id == room.Id);
        if (index >= 0)
        {
            _rooms[index] = room;
        }
        return Task.CompletedTask;
    }

    public Task DeleteAsync(CollaborationRoom room, CancellationToken cancellationToken = default)
    {
        _rooms.RemoveAll(r => r.Id == room.Id);
        return Task.CompletedTask;
    }
}
