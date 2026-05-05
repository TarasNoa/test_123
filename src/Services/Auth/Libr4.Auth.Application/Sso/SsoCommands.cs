using Libr4.Auth.Application.Abstractions;
using Libr4.Auth.Domain.Sso;
using Libr4.Shared.Kernel.Errors;
using Libr4.Shared.Kernel.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Libr4.Auth.Application.Sso;

public record ExternalLoginDto(SsoProvider Provider, string ProviderUserId, string? Email,
    string? DisplayName, DateTimeOffset LinkedAt, DateTimeOffset? LastUsedAt);

public record LinkExternalLoginCommand(Guid UserId, SsoProvider Provider, string ProviderUserId,
    string? Email, string? DisplayName, string? AvatarUrl) : IRequest<Result>;

public sealed class LinkExternalLoginHandler(IAuthDbContext db) : IRequestHandler<LinkExternalLoginCommand, Result>
{
    public async Task<Result> Handle(LinkExternalLoginCommand req, CancellationToken ct)
    {
        var existing = await db.ExternalLogins.FirstOrDefaultAsync(
            x => x.Provider == req.Provider && x.ProviderUserId == req.ProviderUserId, ct);
        if (existing is not null)
        {
            if (existing.UserId != req.UserId)
                return Result.Failure(Error.Conflict("sso.already_linked", "External account already linked to another user"));
            existing.RecordLogin(DateTimeOffset.UtcNow);
        }
        else
        {
            var link = ExternalLogin.Link(req.UserId, req.Provider, req.ProviderUserId,
                req.Email, req.DisplayName, req.AvatarUrl, DateTimeOffset.UtcNow);
            await db.ExternalLogins.AddAsync(link, ct);
        }
        await db.SaveChangesAsync(ct);
        return Result.Success();
    }
}

public record UnlinkExternalLoginCommand(Guid UserId, SsoProvider Provider) : IRequest<Result>;

public sealed class UnlinkExternalLoginHandler(IAuthDbContext db) : IRequestHandler<UnlinkExternalLoginCommand, Result>
{
    public async Task<Result> Handle(UnlinkExternalLoginCommand req, CancellationToken ct)
    {
        var l = await db.ExternalLogins.FirstOrDefaultAsync(
            x => x.UserId == req.UserId && x.Provider == req.Provider, ct);
        if (l is null) return Result.Failure(Error.NotFound("sso.not_linked", "Provider not linked"));
        db.ExternalLogins.Remove(l);
        await db.SaveChangesAsync(ct);
        return Result.Success();
    }
}

public record GetMyExternalLoginsQuery(Guid UserId) : IRequest<Result<IReadOnlyList<ExternalLoginDto>>>;

public sealed class GetMyExternalLoginsHandler(IAuthDbContext db) : IRequestHandler<GetMyExternalLoginsQuery, Result<IReadOnlyList<ExternalLoginDto>>>
{
    public async Task<Result<IReadOnlyList<ExternalLoginDto>>> Handle(GetMyExternalLoginsQuery req, CancellationToken ct)
    {
        var items = await db.ExternalLogins.AsNoTracking()
            .Where(x => x.UserId == req.UserId)
            .Select(x => new ExternalLoginDto(x.Provider, x.ProviderUserId, x.Email, x.DisplayName, x.LinkedAt, x.LastUsedAt))
            .ToListAsync(ct);
        return Result.Success<IReadOnlyList<ExternalLoginDto>>(items);
    }
}
