using Libr4.Chat.Application.Abstractions;
using Libr4.Chat.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Libr4.Chat.Infrastructure.Repositories;

public class ServerRepository : IServerRepository
{
    private readonly ChatDbContext _dbContext;

    public ServerRepository(ChatDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public System.Threading.Tasks.Task<Libr4.Chat.Domain.Servers.Server?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return _dbContext.Servers
            .Include(s => s.Channels)
            .Include(s => s.Members)
            .Include(s => s.ScheduledCalls)
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
    }

    public System.Threading.Tasks.Task<List<Libr4.Chat.Domain.Servers.Server>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return _dbContext.Servers
            .Include(s => s.Channels)
            .Include(s => s.Members)
            .Include(s => s.ScheduledCalls)
            .Where(s => s.Members.Any(m => m.UserId == userId) || s.OwnerId == userId)
            .ToListAsync(cancellationToken);
    }

    public async System.Threading.Tasks.Task AddAsync(Libr4.Chat.Domain.Servers.Server server, CancellationToken cancellationToken = default)
    {
        await _dbContext.Servers.AddAsync(server, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async System.Threading.Tasks.Task UpdateAsync(Libr4.Chat.Domain.Servers.Server server, CancellationToken cancellationToken = default)
    {
        _dbContext.Servers.Update(server);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
