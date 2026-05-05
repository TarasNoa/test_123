using FluentValidation;
using Libr4.Auth.Application.Abstractions;
using Libr4.Auth.Domain.Users;
using Libr4.Shared.Contracts.IntegrationEvents.Auth;
using Libr4.Shared.Kernel.Results;
using Libr4.Shared.Kernel.Time;
using MassTransit;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Libr4.Auth.Application.Users.Commands;

public sealed record RequestPasswordResetCommand(string Email) : IRequest<Result>;

public sealed class RequestPasswordResetHandler : IRequestHandler<RequestPasswordResetCommand, Result>
{
    private readonly IAuthDbContext _db;
    private readonly ITokenGenerator _tokens;
    private readonly IClock _clock;
    private readonly IPublishEndpoint _bus;
    private static readonly TimeSpan Lifetime = TimeSpan.FromHours(2);

    public RequestPasswordResetHandler(IAuthDbContext db, ITokenGenerator tokens, IClock clock, IPublishEndpoint bus)
    {
        _db = db;
        _tokens = tokens;
        _clock = clock;
        _bus = bus;
    }

    public async Task<Result> Handle(RequestPasswordResetCommand request, CancellationToken ct)
    {
        // Always return success to avoid email enumeration.
        var email = request.Email.Trim().ToLowerInvariant();
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == email, ct);
        if (user is null || !user.IsActive) return Result.Success();

        var (plain, hash) = _tokens.Create();
        user.IssueToken(UserTokenKind.PasswordReset, hash, _clock.UtcNow, Lifetime);
        await _db.SaveChangesAsync(ct);

        await _bus.Publish(new PasswordResetRequestedIntegrationEvent(user.Id, user.Email, plain, _clock.UtcNow), ct);
        return Result.Success();
    }
}

public sealed record ResetPasswordCommand(string Token, string NewPassword) : IRequest<Result>;

public sealed class ResetPasswordValidator : AbstractValidator<ResetPasswordCommand>
{
    public ResetPasswordValidator()
    {
        RuleFor(x => x.Token).NotEmpty();
        RuleFor(x => x.NewPassword)
            .NotEmpty()
            .MinimumLength(8)
            .Matches(@"[A-Za-z]").WithMessage("Password must contain letters")
            .Matches(@"\d").WithMessage("Password must contain digits");
    }
}

public sealed class ResetPasswordHandler : IRequestHandler<ResetPasswordCommand, Result>
{
    private readonly IAuthDbContext _db;
    private readonly ITokenGenerator _tokens;
    private readonly IPasswordHasher _hasher;
    private readonly IClock _clock;

    public ResetPasswordHandler(IAuthDbContext db, ITokenGenerator tokens, IPasswordHasher hasher, IClock clock)
    {
        _db = db;
        _tokens = tokens;
        _hasher = hasher;
        _clock = clock;
    }

    public async Task<Result> Handle(ResetPasswordCommand request, CancellationToken ct)
    {
        var hash = _tokens.Hash(request.Token);
        var token = await _db.UserTokens.FirstOrDefaultAsync(t => t.TokenHash == hash && t.Kind == UserTokenKind.PasswordReset, ct);
        if (token is null) return Result.Failure(AuthErrors.InvalidToken);

        var user = await _db.Users
            .Include(u => u.Tokens)
            .Include(u => u.RefreshTokens)
            .FirstOrDefaultAsync(u => u.Id == token.UserId, ct);
        if (user is null) return Result.Failure(AuthErrors.InvalidToken);

        if (!user.ResetPassword(hash, _hasher.Hash(request.NewPassword), _clock.UtcNow))
            return Result.Failure(AuthErrors.InvalidToken);

        await _db.SaveChangesAsync(ct);
        return Result.Success();
    }
}

public sealed record ChangePasswordCommand(Guid UserId, string CurrentPassword, string NewPassword) : IRequest<Result>;

public sealed class ChangePasswordValidator : AbstractValidator<ChangePasswordCommand>
{
    public ChangePasswordValidator()
    {
        RuleFor(x => x.CurrentPassword).NotEmpty();
        RuleFor(x => x.NewPassword).NotEmpty().MinimumLength(8)
            .Matches(@"[A-Za-z]").Matches(@"\d");
    }
}

public sealed class ChangePasswordHandler : IRequestHandler<ChangePasswordCommand, Result>
{
    private readonly IAuthDbContext _db;
    private readonly IPasswordHasher _hasher;
    private readonly IClock _clock;

    public ChangePasswordHandler(IAuthDbContext db, IPasswordHasher hasher, IClock clock)
    {
        _db = db;
        _hasher = hasher;
        _clock = clock;
    }

    public async Task<Result> Handle(ChangePasswordCommand request, CancellationToken ct)
    {
        var user = await _db.Users
            .Include(u => u.RefreshTokens)
            .FirstOrDefaultAsync(u => u.Id == request.UserId, ct);
        if (user is null) return Result.Failure(AuthErrors.UserNotFound);
        if (!_hasher.Verify(request.CurrentPassword, user.PasswordHash))
            return Result.Failure(AuthErrors.InvalidCredentials);

        // Domain-level "reset": use token path by issuing a throwaway token? simpler: mutate via explicit path
        // Reuse ResetPassword-like semantics via a fresh token
        var (_, hash) = (Guid.NewGuid().ToString("N"), Guid.NewGuid().ToString("N"));
        user.IssueToken(UserTokenKind.PasswordReset, hash, _clock.UtcNow, TimeSpan.FromMinutes(1));
        user.ResetPassword(hash, _hasher.Hash(request.NewPassword), _clock.UtcNow);

        await _db.SaveChangesAsync(ct);
        return Result.Success();
    }
}
