using Libr4.Collaboration.Application.Abstractions;
using Libr4.Collaboration.Domain;

namespace Libr4.Collaboration.Infrastructure.Repositories;

public sealed class InMemoryWhiteboardRepository : IWhiteboardRepository
{
    private readonly List<Whiteboard> _whiteboards = new();

    public Task<Whiteboard?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return Task.FromResult(_whiteboards.FirstOrDefault(w => w.Id == id));
    }

    public Task AddAsync(Whiteboard whiteboard, CancellationToken ct = default)
    {
        _whiteboards.Add(whiteboard);
        return Task.CompletedTask;
    }

    public Task UpdateAsync(Whiteboard whiteboard, CancellationToken ct = default)
    {
        var index = _whiteboards.FindIndex(w => w.Id == whiteboard.Id);
        if (index >= 0) _whiteboards[index] = whiteboard;
        return Task.CompletedTask;
    }
}
