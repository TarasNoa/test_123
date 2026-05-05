using FluentValidation;
using Libr4.Auth.Application.Abstractions;
using Libr4.Auth.Application.Dtos;
using Libr4.Auth.Domain.Users;
using Libr4.Shared.Contracts.IntegrationEvents.Auth;
using Libr4.Shared.Kernel.Results;
using Libr4.Shared.Kernel.Time;
using MassTransit;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Libr4.Auth.Application.Users.Commands;

public sealed record LoginCommand(LoginRequest Payload, string? IpAddress) : IRequest<Result<AuthTokens>>;

public sealed class LoginValidator : AbstractValidator<LoginCommand>
{
    public LoginValidator()
    {
        RuleFor(x => x.Payload.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Payload.Password).NotEmpty();
    }
}

public sealed class LoginHandler : IRequestHandler<LoginCommand, Result<AuthTokens>>
{
    private readonly IAuthDbContext _db;
    private readonly IPasswordHasher _hasher;
    private readonly IJwtTokenService _jwt;
    private readonly ITotpService _totp;
    private readonly IClock _clock;
    private readonly IPublishEndpoint _bus;
    private static readonly TimeSpan RefreshLifetime = TimeSpan.FromDays(30);

    public LoginHandler(
        IAuthDbContext db, IPasswordHasher hasher, IJwtTokenService jwt,
        ITotpService totp, IClock clock, IPublishEndpoint bus)
    {
        _db = db;
        _hasher = hasher;
        _jwt = jwt;
        _totp = totp;
        _clock = clock;
        _bus = bus;
    }

    public async Task<Result<AuthTokens>> Handle(LoginCommand request, CancellationToken ct)
    {
        var email = request.Payload.Email.Trim().ToLowerInvariant();
        var user = await _db.Users
            .Include(u => u.Roles)
            .FirstOrDefaultAsync(u => u.Email == email, ct);

        if (user is null)
            return Result.Failure<AuthTokens>(AuthErrors.InvalidCredentials);

        if (!user.IsActive)
            return Result.Failure<AuthTokens>(AuthErrors.AccountInactive);

        if (user.IsLockedOut(_clock.UtcNow))
            return Result.Failure<AuthTokens>(AuthErrors.AccountLocked);

        if (!_hasher.Verify(request.Payload.Password, user.PasswordHash))
        {
            user.RecordFailedLogin(_clock.UtcNow);
            await _db.SaveChangesAsync(ct);
            return Result.Failure<AuthTokens>(AuthErrors.InvalidCredentials);
        }

        if (user.TwoFactorEnabled)
        {
            if (string.IsNullOrWhiteSpace(request.Payload.TwoFactorCode))
                return Result.Failure<AuthTokens>(AuthErrors.TwoFactorRequired);
            if (!_totp.VerifyCode(user.TwoFactorSecretEncrypted!, request.Payload.TwoFactorCode))
                return Result.Failure<AuthTokens>(AuthErrors.TwoFactorInvalid);
        }

        user.RecordSuccessfulLogin(_clock.UtcNow);

        var access = _jwt.CreateAccessToken(user);
        var (refreshPlain, refreshHash) = _jwt.CreateRefreshToken();
        // var refresh = new RefreshToken(user.Id, refreshHash, _clock.UtcNow, RefreshLifetime, request.IpAddress);
        // user.AddRefreshToken(refresh);

        await _db.SaveChangesAsync(ct);

        await _bus.Publish(new UserLoggedInIntegrationEvent(user.Id, user.Email, request.IpAddress, _clock.UtcNow), ct);

        return new AuthTokens(access.Token, access.ExpiresAt, refreshPlain, _clock.UtcNow.Add(RefreshLifetime));
    }
}
