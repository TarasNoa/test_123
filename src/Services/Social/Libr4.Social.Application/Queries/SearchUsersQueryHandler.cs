using Libr4.Shared.Kernel.Application;
using Libr4.Shared.Infrastructure.Messaging;
using Libr4.Social.Application.Abstractions;

namespace Libr4.Social.Application.Queries;

public class SearchUsersQuery : IQuery<List<UserSearchResultDto>>
{
    public Guid UserId { get; set; }
    public string SearchTerm { get; set; } = string.Empty;
    public int Skip { get; set; } = 0;
    public int Take { get; set; } = 20;
}

public record UserSearchResultDto(Guid UserId, string Name, string? ProfileImageUrl, int FollowerCount);

public class SearchUsersQueryHandler : IQueryHandler<SearchUsersQuery, List<UserSearchResultDto>>
{
    private readonly ISocialNetworkRepository _repository;

    public SearchUsersQueryHandler(ISocialNetworkRepository repository)
    {
        _repository = repository;
    }

    public async Task<List<UserSearchResultDto>> HandleAsync(SearchUsersQuery query, CancellationToken cancellationToken)
    {
        var allNetworks = await _repository.GetAllAsync(cancellationToken);
        var results = allNetworks
            .Where(n => n.UserId != query.UserId &&
                (n.Profile.Name.Contains(query.SearchTerm, StringComparison.OrdinalIgnoreCase) ||
                 (n.Profile.Bio != null && n.Profile.Bio.Contains(query.SearchTerm, StringComparison.OrdinalIgnoreCase))))
            .Skip(query.Skip)
            .Take(query.Take)
            .Select(n => new UserSearchResultDto(n.UserId, n.Profile.Name, n.Profile.ProfileImageUrl, n.Followers.Count))
            .ToList();

        return results;
    }
}
