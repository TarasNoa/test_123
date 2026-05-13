using Libr4.IDE.Domain;

namespace Libr4.IDE.Infrastructure.Persistence;

public class InMemoryCodeSessionRepository : ICodeSessionRepository
{
    private readonly List<CodeSession> _sessions = new();

    public Task<CodeSession?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_sessions.FirstOrDefault(s => s.Id == id));
    }

    public Task AddAsync(CodeSession session, CancellationToken cancellationToken = default)
    {
        _sessions.Add(session);
        return Task.CompletedTask;
    }

    public Task UpdateAsync(CodeSession session, CancellationToken cancellationToken = default)
    {
        var index = _sessions.FindIndex(s => s.Id == session.Id);
        if (index >= 0) _sessions[index] = session;
        return Task.CompletedTask;
    }

    public Task DeleteAsync(CodeSession session, CancellationToken cancellationToken = default)
    {
        _sessions.RemoveAll(s => s.Id == session.Id);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    public Task<List<CodeSession>> GetByCreatorIdAsync(Guid creatorId, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_sessions.Where(s => s.CreatorId == creatorId).ToList());
    }

    public Task<List<CodeSession>> GetActiveSessionsAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_sessions.Where(s => s.IsActive).ToList());
    }
}
