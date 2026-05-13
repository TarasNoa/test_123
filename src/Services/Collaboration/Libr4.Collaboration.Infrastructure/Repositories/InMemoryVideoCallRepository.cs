using Libr4.Collaboration.Application.Abstractions;
using Libr4.Collaboration.Domain;

namespace Libr4.Collaboration.Infrastructure.Repositories;

public sealed class InMemoryVideoCallRepository : IVideoCallRepository
{
    private readonly List<VideoCall> _calls = new();

    public Task<VideoCall?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return Task.FromResult(_calls.FirstOrDefault(c => c.Id == id));
    }

    public Task AddAsync(VideoCall call, CancellationToken ct = default)
    {
        _calls.Add(call);
        return Task.CompletedTask;
    }

    public Task UpdateAsync(VideoCall call, CancellationToken ct = default)
    {
        var index = _calls.FindIndex(c => c.Id == call.Id);
        if (index >= 0) _calls[index] = call;
        return Task.CompletedTask;
    }
}
