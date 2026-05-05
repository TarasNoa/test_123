using Libr4.Collaboration.Domain;

namespace Libr4.Collaboration.Domain;

public interface ICollaborationRoomRepository
{
    Task<CollaborationRoom?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<CollaborationRoom?> GetByIdWithDetailsAsync(Guid id, CancellationToken cancellationToken = default);
    Task<List<CollaborationRoom>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<List<CollaborationRoom>> GetByTaskIdAsync(Guid taskId, CancellationToken cancellationToken = default);
    Task<List<CollaborationRoom>> GetActiveRoomsAsync(CancellationToken cancellationToken = default);
    Task<List<CollaborationRoom>> GetPublicRoomsAsync(CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default);
    Task AddAsync(CollaborationRoom room, CancellationToken cancellationToken = default);
    Task UpdateAsync(CollaborationRoom room, CancellationToken cancellationToken = default);
    Task DeleteAsync(CollaborationRoom room, CancellationToken cancellationToken = default);
}
