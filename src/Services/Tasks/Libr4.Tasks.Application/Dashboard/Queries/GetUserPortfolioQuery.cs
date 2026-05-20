using Libr4.Shared.Kernel.Results;
using MediatR;

namespace Libr4.Tasks.Application.Dashboard.Queries;

public sealed record GetUserPortfolioQuery(Guid UserId) : IRequest<Result<UserPortfolioDto>>;

public sealed record UserPortfolioDto(int TotalTasks, int CompletedTasks, decimal TotalEarnings);

public sealed class GetUserPortfolioHandler : IRequestHandler<GetUserPortfolioQuery, Result<UserPortfolioDto>>
{
    public async Task<Result<UserPortfolioDto>> Handle(GetUserPortfolioQuery request, CancellationToken ct)
    {
        return Result.Success(new UserPortfolioDto(0, 0, 0m));
    }
}
