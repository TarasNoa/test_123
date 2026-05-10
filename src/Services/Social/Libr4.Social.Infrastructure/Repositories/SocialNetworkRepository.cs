using Libr4.Social.Domain.Network;
using Libr4.Shared.Infrastructure.Repositories;

namespace Libr4.Social.Infrastructure.Repositories;

public interface ISocialNetworkRepository : IRepository<SocialNetwork>
{
    Task<SocialNetwork?> GetByUserIdAsync(Guid userId);
    Task<List<SocialNetwork>> GetByInfluenceScoreAsync(double minScore);
    Task<List<SocialNetwork>> GetRecommendedConnectionsAsync(Guid userId, int topN);
}

public class SocialNetworkRepository : GenericRepository<SocialNetwork>, ISocialNetworkRepository
{
    public SocialNetworkRepository(SocialDbContext dbContext) : base(dbContext)
    {
    }

    public async Task<SocialNetwork?> GetByUserIdAsync(Guid userId)
    {
        return await _dbContext.SocialNetworks
            .Include(x => x.Connections)
            .Include(x => x.Posts)
            .Include(x => x.Followers)
            .Include(x => x.Following)
            .FirstOrDefaultAsync(x => x.UserId == userId);
    }

    public async Task<List<SocialNetwork>> GetByInfluenceScoreAsync(double minScore)
    {
        return await _dbContext.SocialNetworks
            .Where(x => x.InfluenceScore >= minScore)
            .OrderByDescending(x => x.InfluenceScore)
            .ToListAsync();
    }

    public async Task<List<SocialNetwork>> GetRecommendedConnectionsAsync(Guid userId, int topN)
    {
        var user = await GetByUserIdAsync(userId);
        if (user == null)
            return new List<SocialNetwork>();

        var userConnectionIds = user.Connections.Select(c => c.ConnectedUserId).ToList();

        return await _dbContext.SocialNetworks
            .Where(x => x.UserId != userId && !userConnectionIds.Contains(x.UserId))
            .OrderByDescending(x => x.Followers.Count)
            .Take(topN)
            .ToListAsync();
    }
}