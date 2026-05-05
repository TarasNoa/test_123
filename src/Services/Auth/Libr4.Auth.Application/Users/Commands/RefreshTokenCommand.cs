using Libr4.Auth.Application.Abstractions;
using Libr4.Auth.Application.Dtos;
using Libr4.Auth.Domain.Users;
using Libr4.Shared.Kernel.Results;
using Libr4.Shared.Kernel.Time;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Libr4.Auth.Application.Users.Commands;

public sealed record RefreshTokenCommand(string RefreshTokenPlain, string? IpAddress) : IRequest<Result<AuthTokens>>;

public sealed class RefreshTokenHandler : IRequestHandler<RefreshTokenCommand, Result<AuthTokens>>
{
    private readonly IAuthDbContext _db;
    private readonly IJwtTokenService _jwt;
    private readonly IClock _clock;
    private static readonly TimeSpan RefreshLifetime = TimeSpan.FromDays(30);

    public RefreshTokenHandler(IAuthDbContext db, IJwtTokenService jwt, IClock clock)
    {
        _db = db;
        _jwt = jwt;
        _clock = clock;
    }

    public async Task<Result<AuthTokens>> Handle(RefreshTokenCommand request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.RefreshTokenPlain))
            return Result.Failure<AuthTokens>(AuthErrors.InvalidRefreshToken);

        var tokenHash = _jwt.HashRefreshToken(request.RefreshTokenPlain);

        var stored = await _db.RefreshTokens.FirstOrDefaultAsync(t => t.TokenHash == tokenHash, ct);
        if (stored is null || !stored.IsActive(_clock.UtcNow))
            return Result.Failure<AuthTokens>(AuthErrors.InvalidRefreshToken);

        var user = await _db.Users
            .Include(u => u.Roles)
            .FirstOrDefaultAsync(u => u.Id == stored.UserId, ct);
        if (user is null || !user.IsActive)
            return Result.Failure<AuthTokens>(AuthErrors.InvalidRefreshToken);

        // Rotate
        var access = _jwt.CreateAccessToken(user);
        var (newPlain, newHash) = _jwt.CreateRefreshToken();
        var newRefresh = new RefreshToken(user.Id, newHash, _clock.UtcNow, RefreshLifetime, request.IpAddress);
        stored.Revoke(_clock.UtcNow, request.IpAddress, newHash);
        user.AddRefreshToken(newRefresh);

        await _db.SaveChangesAsync(ct);

        return new AuthTokens(access.Token, access.ExpiresAt, newPlain, newRefresh.ExpiresAt);
    }
}
