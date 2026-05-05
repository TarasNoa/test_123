using Libr4.Auth.Application.Abstractions;
using Libr4.Auth.Domain.Users;
using Libr4.Auth.Domain.Users.Events;
using Libr4.Shared.Contracts.IntegrationEvents.Auth;
using Libr4.Shared.Kernel.Results;
using Libr4.Shared.Kernel.Time;
using MassTransit;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Libr4.Auth.Application.Users.Commands;

public sealed record RequestEmailConfirmationCommand(Guid UserId) : IRequest<Result>;

public sealed class RequestEmailConfirmationHandler : IRequestHandler<RequestEmailConfirmationCommand, Result>
{
    private readonly IAuthDbContext _db;
    private readonly ITokenGenerator _tokens;
    private readonly IClock _clock;
    private readonly IPublishEndpoint _bus;
    private static readonly TimeSpan Lifetime = TimeSpan.FromHours(24);

    public RequestEmailConfirmationHandler(IAuthDbContext db, ITokenGenerator tokens, IClock clock, IPublishEndpoint bus)
    {
        _db = db;
        _tokens = tokens;
        _clock = clock;
        _bus = bus;
    }

    public async Task<Result> Handle(RequestEmailConfirmationCommand request, CancellationToken ct)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == request.UserId, ct);
        if (user is null) return Result.Failure(AuthErrors.UserNotFound);
        if (user.EmailConfirmed) return Result.Failure(AuthErrors.EmailAlreadyConfirmed);

        var (plain, hash) = _tokens.Create();
        user.IssueToken(UserTokenKind.EmailConfirmation, hash, _clock.UtcNow, Lifetime);
        await _db.SaveChangesAsync(ct);

        await _bus.Publish(new EmailConfirmationRequestedIntegrationEvent(user.Id, user.Email, plain, _clock.UtcNow), ct);
        return Result.Success();
    }
}

public sealed record ConfirmEmailCommand(string Token) : IRequest<Result>;

public sealed class ConfirmEmailHandler : IRequestHandler<ConfirmEmailCommand, Result>
{
    private readonly IAuthDbContext _db;
    private readonly ITokenGenerator _tokens;
    private readonly IClock _clock;

    public ConfirmEmailHandler(IAuthDbContext db, ITokenGenerator tokens, IClock clock)
    {
        _db = db;
        _tokens = tokens;
        _clock = clock;
    }

    public async Task<Result> Handle(ConfirmEmailCommand request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Token))
            return Result.Failure(AuthErrors.InvalidToken);

        var hash = _tokens.Hash(request.Token);
        var token = await _db.UserTokens.FirstOrDefaultAsync(t => t.TokenHash == hash && t.Kind == UserTokenKind.EmailConfirmation, ct);
        if (token is null) return Result.Failure(AuthErrors.InvalidToken);

        var user = await _db.Users.Include(u => u.Tokens).FirstOrDefaultAsync(u => u.Id == token.UserId, ct);
        if (user is null) return Result.Failure(AuthErrors.InvalidToken);

        if (!user.ConfirmEmail(hash, _clock.UtcNow))
            return Result.Failure(AuthErrors.InvalidToken);

        await _db.SaveChangesAsync(ct);
        return Result.Success();
    }
}
