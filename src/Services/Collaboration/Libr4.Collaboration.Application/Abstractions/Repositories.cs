using Libr4.Collaboration.Domain;

namespace Libr4.Collaboration.Application.Abstractions;

public interface IWhiteboardRepository
{
    Task<Whiteboard?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task AddAsync(Whiteboard whiteboard, CancellationToken ct = default);
    Task UpdateAsync(Whiteboard whiteboard, CancellationToken ct = default);
}

public interface IVideoCallRepository
{
    Task<VideoCall?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task AddAsync(VideoCall call, CancellationToken ct = default);
    Task UpdateAsync(VideoCall call, CancellationToken ct = default);
}
