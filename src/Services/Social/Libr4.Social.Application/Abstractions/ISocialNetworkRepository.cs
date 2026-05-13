using Libr4.Social.Domain.Network;

namespace Libr4.Social.Application.Abstractions;

public interface ISocialNetworkRepository
{
    Task<SocialNetwork?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<SocialNetwork?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<List<SocialNetwork>> GetAllAsync(CancellationToken cancellationToken = default);
    Task AddAsync(SocialNetwork network, CancellationToken cancellationToken = default);
    Task UpdateAsync(SocialNetwork network, CancellationToken cancellationToken = default);
}
