using Libr4.Auth.Application.Abstractions;
using Libr4.Auth.Application.Dtos;
using Libr4.Auth.Domain.Users;
using Libr4.Shared.Kernel.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Libr4.Auth.Application.Users.Queries;

public sealed record GetCurrentUserQuery(Guid UserId) : IRequest<Result<UserDto>>;

public sealed class GetCurrentUserHandler : IRequestHandler<GetCurrentUserQuery, Result<UserDto>>
{
    private readonly IAuthDbContext _db;

    public GetCurrentUserHandler(IAuthDbContext db) => _db = db;

    public async Task<Result<UserDto>> Handle(GetCurrentUserQuery request, CancellationToken ct)
    {
        var u = await _db.Users
            .Include(u => u.Roles)
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == request.UserId, ct);
        if (u is null)
            return Result.Failure<UserDto>(AuthErrors.UserNotFound);

        return new UserDto(
            u.Id, u.Email, u.DisplayName,
            u.Roles.Select(r => r.Name).ToList(),
            u.EmailConfirmed, u.TwoFactorEnabled, u.CreatedAt,
            u.Role, u.Phone, u.Country, u.City,
            u.CompanyName, u.Industry, u.CompanySize, u.Website,
            u.Skills.AsReadOnly(), u.Experience, u.HourlyRate,
            u.Specialization, u.LinkedInUrl, u.CvUrl,
            u.AvatarUrl, u.CoverUrl, u.Bio, u.Rating, u.TotalEarnings,
            u.TotalSpent, u.CompletedTasks, u.IsFreelancer, u.IsClient);
    }
}
