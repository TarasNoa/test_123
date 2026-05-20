using Libr4.Shared.Kernel.Results;
using MediatR;

namespace Libr4.Tasks.Application.Dashboard.Queries;

public sealed record GetUserStatsQuery(Guid UserId) : IRequest<Result<UserStatsDto>>;

public sealed record UserStatsDto(int ActiveTasks, int CompletedTasks, decimal Rating);

public sealed class GetUserStatsHandler : IRequestHandler<GetUserStatsQuery, Result<UserStatsDto>>
{
    public async Task<Result<UserStatsDto>> Handle(GetUserStatsQuery request, CancellationToken ct)
    {
        return Result.Success(new UserStatsDto(0, 0, 0m));
    }
}
