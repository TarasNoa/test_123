using Libr4.Social.Domain.Network;
using Libr4.Social.Application.Abstractions;
using Libr4.Shared.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Libr4.Social.Infrastructure.Repositories;

public class SocialNetworkRepository : GenericRepository<SocialNetwork>, ISocialNetworkRepository
{
    public SocialNetworkRepository(SocialDbContext dbContext) : base(dbContext)
    {
    }

    public async Task<SocialNetwork?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Set<SocialNetwork>()
            .AsTracking()
            .Include(x => x.Connections)
            .Include(x => x.Posts)
            .ThenInclude(p => p.Comments)
            .FirstOrDefaultAsync(x => x.UserId == userId, cancellationToken);
    }

    public async Task<List<SocialNetwork>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.Set<SocialNetwork>()
            .AsTracking()
            .Include(x => x.Connections)
            .Include(x => x.Posts)
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
        // Entity is already tracked by EF, just save changes
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<Guid> CreatePostAsync(Guid userId, string content, List<string> tags, List<string> attachmentUrls, CancellationToken cancellationToken = default)
    {
        var postId = Guid.NewGuid();
        var networkId = await _dbContext.Set<SocialNetwork>()
            .Where(n => n.UserId == userId)
            .Select(n => (Guid?)n.Id)
            .FirstOrDefaultAsync(cancellationToken);

        // Auto-create social network if not exists
        if (networkId == null)
        {
            var newNetwork = SocialNetwork.Create(userId);
            await _dbContext.Set<SocialNetwork>().AddAsync(newNetwork, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);
            networkId = newNetwork.Id;
        }

        var sql = @"INSERT INTO ""UserPost"" (""Id"", ""Content"", ""Tags"", ""AttachmentUrls"", ""CreatedAt"", ""Likes"", ""SocialNetworkId"") VALUES ({0}, {1}, {2}::text[], {3}::text[], {4}, ARRAY[]::uuid[], {5})";

        await _dbContext.Database.ExecuteSqlRawAsync(
            sql,
            new object[] { postId, content, tags.ToArray(), attachmentUrls.ToArray(), DateTime.UtcNow, networkId });

        return postId;
    }

    public async Task AddCommentAsync(Guid postId, Guid authorId, string text, CancellationToken cancellationToken = default)
    {
        var commentId = Guid.NewGuid();
        var sql = @"INSERT INTO ""PostComment"" (""Id"", ""AuthorId"", ""Text"", ""CreatedAt"", ""UserPostId"") VALUES ({0}, {1}, {2}, {3}, {4})";

        await _dbContext.Database.ExecuteSqlRawAsync(
            sql,
            new object[] { commentId, authorId, text, DateTime.UtcNow, postId });
    }
}