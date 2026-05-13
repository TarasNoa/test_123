using Libr4.IDE.Domain;
using Microsoft.EntityFrameworkCore;

namespace Libr4.IDE.Infrastructure.Persistence;

public class EfCodeSessionRepository : ICodeSessionRepository
{
    private readonly ApplicationDbContext _context;

    public EfCodeSessionRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public Task<CodeSession?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => _context.CodeSessions.AsNoTracking()
            .Include(s => s.Files)
            .Include(s => s.Participants)
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

    public async Task AddAsync(CodeSession session, CancellationToken cancellationToken = default)
    {
        await _context.CodeSessions.AddAsync(session, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public Task UpdateAsync(CodeSession session, CancellationToken cancellationToken = default)
    {
        _context.CodeSessions.Update(session);
        return _context.SaveChangesAsync(cancellationToken);
    }

    public Task DeleteAsync(CodeSession session, CancellationToken cancellationToken = default)
    {
        _context.CodeSessions.Remove(session);
        return _context.SaveChangesAsync(cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        => _context.SaveChangesAsync(cancellationToken);

    public Task<List<CodeSession>> GetByCreatorIdAsync(Guid creatorId, CancellationToken cancellationToken = default)
        => _context.CodeSessions.AsNoTracking()
            .Where(s => s.CreatorId == creatorId)
            .OrderByDescending(s => s.LastActivityAt)
            .ToListAsync(cancellationToken);

    public Task<List<CodeSession>> GetActiveSessionsAsync(CancellationToken cancellationToken = default)
        => _context.CodeSessions.AsNoTracking()
            .Where(s => s.IsActive)
            .OrderByDescending(s => s.LastActivityAt)
            .ToListAsync(cancellationToken);
}
