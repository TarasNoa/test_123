using Libr4.Shared.Kernel.Results;
using MediatR;

namespace Libr4.Tasks.Application.Dashboard.Queries;

public sealed record GetUserProjectsQuery(Guid UserId) : IRequest<Result<List<UserProjectDto>>>;

public sealed record UserProjectDto(Guid Id, string Title, string Status, DateTimeOffset CreatedAt);

public sealed class GetUserProjectsHandler : IRequestHandler<GetUserProjectsQuery, Result<List<UserProjectDto>>>
{
    public async Task<Result<List<UserProjectDto>>> Handle(GetUserProjectsQuery request, CancellationToken ct)
    {
        return Result.Success(new List<UserProjectDto>());
    }
}
