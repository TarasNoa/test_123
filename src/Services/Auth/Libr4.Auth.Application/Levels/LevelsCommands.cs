using Libr4.Auth.Application.Abstractions;
using Libr4.Auth.Domain.Levels;
using Libr4.Shared.Kernel.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Libr4.Auth.Application.Levels;

public record UserLevelDto(Guid UserId, int Level, int Xp, int XpToNext, decimal Reputation, int TasksCompleted, int FiveStarReviews);

public record GetMyLevelQuery(Guid UserId) : IRequest<Result<UserLevelDto>>;

public sealed class GetMyLevelHandler(IAuthDbContext db) : IRequestHandler<GetMyLevelQuery, Result<UserLevelDto>>
{
    public async Task<Result<UserLevelDto>> Handle(GetMyLevelQuery req, CancellationToken ct)
    {
        var l = await db.UserLevels.AsNoTracking().FirstOrDefaultAsync(x => x.UserId == req.UserId, ct);
        if (l is null)
        {
            // Auto-create on first access
            var fresh = UserLevel.Create(req.UserId, DateTimeOffset.UtcNow);
            await db.UserLevels.AddAsync(fresh, ct);
            await db.SaveChangesAsync(ct);
            l = fresh;
        }
        return Result.Success(new UserLevelDto(l.UserId, l.Level, l.Xp, l.XpToNextLevel(), l.ReputationScore, l.TasksCompleted, l.FiveStarReviews));
    }
}

public record GrantXpCommand(Guid UserId, int Amount, XpReason Reason, string? ReferenceId) : IRequest<Result>;

public sealed class GrantXpHandler(IAuthDbContext db) : IRequestHandler<GrantXpCommand, Result>
{
    public async Task<Result> Handle(GrantXpCommand req, CancellationToken ct)
    {
        var l = await db.UserLevels.FirstOrDefaultAsync(x => x.UserId == req.UserId, ct);
        if (l is null)
        {
            l = UserLevel.Create(req.UserId, DateTimeOffset.UtcNow);
            await db.UserLevels.AddAsync(l, ct);
        }
        l.GrantXp(req.Amount, req.Reason, req.ReferenceId, DateTimeOffset.UtcNow);
        await db.SaveChangesAsync(ct);
        return Result.Success();
    }
}

public record GetLeaderboardQuery(int Top = 50) : IRequest<Result<IReadOnlyList<LeaderboardEntryDto>>>;
public record LeaderboardEntryDto(Guid UserId, int Level, int Xp, decimal Reputation);

public sealed class GetLeaderboardHandler(IAuthDbContext db) : IRequestHandler<GetLeaderboardQuery, Result<IReadOnlyList<LeaderboardEntryDto>>>
{
    public async Task<Result<IReadOnlyList<LeaderboardEntryDto>>> Handle(GetLeaderboardQuery req, CancellationToken ct)
    {
        var top = await db.UserLevels.AsNoTracking()
            .OrderByDescending(x => x.Xp)
            .Take(Math.Clamp(req.Top, 1, 200))
            .Select(x => new LeaderboardEntryDto(x.UserId, x.Level, x.Xp, x.ReputationScore))
            .ToListAsync(ct);
        return Result.Success<IReadOnlyList<LeaderboardEntryDto>>(top);
    }
}
