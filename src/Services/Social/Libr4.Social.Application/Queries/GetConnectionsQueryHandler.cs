using Libr4.Shared.Kernel.Application;
using Libr4.Shared.Infrastructure.Messaging;
using Libr4.Social.Application.Abstractions;

namespace Libr4.Social.Application.Queries;

public class GetConnectionsQuery : IQuery<List<SocialConnectionDto>>
{
    public Guid UserId { get; set; }
}

public class GetConnectionsQueryHandler : IQueryHandler<GetConnectionsQuery, List<SocialConnectionDto>>
{
    private readonly ISocialNetworkRepository _repository;

    public GetConnectionsQueryHandler(ISocialNetworkRepository repository)
    {
        _repository = repository;
    }

    public async Task<List<SocialConnectionDto>> HandleAsync(GetConnectionsQuery query, CancellationToken cancellationToken)
    {
        var network = await _repository.GetByUserIdAsync(query.UserId, cancellationToken);
        if (network == null)
            return new List<SocialConnectionDto>();

        return network.Connections
            .Select(c => new SocialConnectionDto(c.Id, c.ConnectedUserId, c.Type, c.Note))
            .ToList();
    }
}
