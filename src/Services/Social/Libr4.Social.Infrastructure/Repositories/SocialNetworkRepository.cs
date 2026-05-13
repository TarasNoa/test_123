using Libr4.Social.Domain.Network;
using Libr4.Social.Application.Abstractions;
using Libr4.Shared.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Libr4.Social.Infrastructure.Repositories;

public class SocialNetworkRepository : GenericRepository<SocialNetwork>, ISocialNetworkRepository
{
    public SocialNetworkRepository(SocialDbContext dbContext) : base(dbContext)
    {
    }

    public async Task<SocialNetwork?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Set<SocialNetwork>()
            .Include(x => x.Connections)
            .Include(x => x.Posts)
            .ThenInclude(p => p.Comments)
            .Include(x => x.Followers)
            .Include(x => x.Following)
            .FirstOrDefaultAsync(x => x.UserId == userId, cancellationToken);
    }

    public async Task<List<SocialNetwork>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.Set<SocialNetwork>()
            .Include(x => x.Connections)
            .Include(x => x.Posts)
            .Include(x => x.Followers)
            .Include(x => x.Following)
            .ToListAsync(cancellationToken);
    }

    public async Task<SocialNetwork?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await base.GetByIdAsync(id);
    }

    public async Task AddAsync(SocialNetwork network, CancellationToken cancellationToken = default)
    {
        await base.AddAsync(network);
    }

    public async Task UpdateAsync(SocialNetwork network, CancellationToken cancellationToken = default)
    {
        await base.UpdateAsync(network);
    }
}