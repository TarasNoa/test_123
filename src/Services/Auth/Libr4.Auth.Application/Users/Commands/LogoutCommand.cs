using Libr4.Auth.Application.Abstractions;
using Libr4.Shared.Kernel.Results;
using Libr4.Shared.Kernel.Time;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Libr4.Auth.Application.Users.Commands;

public sealed record LogoutCommand(string RefreshTokenPlain, string? IpAddress) : IRequest<Result>;

public sealed class LogoutHandler : IRequestHandler<LogoutCommand, Result>
{
    private readonly IAuthDbContext _db;
    private readonly IJwtTokenService _jwt;
    private readonly IClock _clock;

    public LogoutHandler(IAuthDbContext db, IJwtTokenService jwt, IClock clock)
    {
        _db = db;
        _jwt = jwt;
        _clock = clock;
    }

    public async Task<Result> Handle(LogoutCommand request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.RefreshTokenPlain))
            return Result.Success();

        var hash = _jwt.HashRefreshToken(request.RefreshTokenPlain);
        var token = await _db.RefreshTokens.FirstOrDefaultAsync(t => t.TokenHash == hash, ct);
        if (token is not null && token.RevokedAt is null)
        {
            token.Revoke(_clock.UtcNow, request.IpAddress);
            await _db.SaveChangesAsync(ct);
        }
        return Result.Success();
    }
}
