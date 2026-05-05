using System.Security.Cryptography;
using FluentValidation;
using Libr4.Auth.Application.Abstractions;
using Libr4.Auth.Domain.Organizations;
using Libr4.Shared.Kernel.Errors;
using Libr4.Shared.Kernel.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Libr4.Auth.Application.Organizations;

public record OrgDto(Guid Id, string Name, string Slug, OrganizationPlan Plan, int SeatLimit, int MemberCount,
    Guid OwnerId, DateTimeOffset CreatedAt, string? LogoUrl, string? WebsiteUrl);
public record OrgMemberDto(Guid UserId, OrgRole Role, DateTimeOffset JoinedAt);
public record OrgInviteDto(Guid Id, string Email, OrgRole Role, DateTimeOffset CreatedAt, DateTimeOffset ExpiresAt, bool IsAccepted);

public record CreateOrganizationCommand(Guid OwnerId, string Name, string Slug, OrganizationPlan Plan)
    : IRequest<Result<Guid>>;

public sealed class CreateOrganizationValidator : AbstractValidator<CreateOrganizationCommand>
{
    public CreateOrganizationValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Slug).NotEmpty().Matches("^[a-z0-9][a-z0-9-]{1,79}$")
            .WithMessage("Slug must be lowercase alphanumeric with dashes");
    }
}

public sealed class CreateOrganizationHandler(IAuthDbContext db) : IRequestHandler<CreateOrganizationCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(CreateOrganizationCommand req, CancellationToken ct)
    {
        var slug = req.Slug.ToLowerInvariant();
        if (await db.Organizations.AnyAsync(x => x.Slug == slug, ct))
            return Result.Failure<Guid>(Error.Conflict("org.slug_taken", "Slug already in use"));

        var org = Organization.Create(req.Name, slug, req.OwnerId, req.Plan, DateTimeOffset.UtcNow);
        await db.Organizations.AddAsync(org, ct);
        await db.SaveChangesAsync(ct);
        return Result.Success(org.Id);
    }
}

public record InviteMemberCommand(Guid OrgId, Guid InvitedBy, string Email, OrgRole Role)
    : IRequest<Result<InviteResponse>>;

public record InviteResponse(Guid InviteId, string Token, DateTimeOffset ExpiresAt);

public sealed class InviteMemberHandler(IAuthDbContext db) : IRequestHandler<InviteMemberCommand, Result<InviteResponse>>
{
    public async Task<Result<InviteResponse>> Handle(InviteMemberCommand req, CancellationToken ct)
    {
        var org = await db.Organizations.Include(x => x.Members).Include(x => x.Invites)
            .FirstOrDefaultAsync(x => x.Id == req.OrgId, ct);
        if (org is null) return Result.Failure<InviteResponse>(Error.NotFound("org.not_found", "Organization not found"));

        var bytes = RandomNumberGenerator.GetBytes(24);
        var token = Convert.ToBase64String(bytes).Replace('+', '-').Replace('/', '_').TrimEnd('=');
        var hash = Convert.ToHexString(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(token)));

        try
        {
            var invite = org.InviteUser(req.Email, req.Role, req.InvitedBy, hash, DateTimeOffset.UtcNow);
            await db.SaveChangesAsync(ct);
            return Result.Success(new InviteResponse(invite.Id, token, invite.ExpiresAt));
        }
        catch (Libr4.Shared.Kernel.Domain.DomainException ex)
        {
            return Result.Failure<InviteResponse>(Error.Conflict(ex.Code ?? "org.invite_failed", ex.Message));
        }
    }
}

public record AcceptInviteCommand(Guid OrgId, string Token, Guid AcceptingUserId) : IRequest<Result>;

public sealed class AcceptInviteHandler(IAuthDbContext db) : IRequestHandler<AcceptInviteCommand, Result>
{
    public async Task<Result> Handle(AcceptInviteCommand req, CancellationToken ct)
    {
        var hash = Convert.ToHexString(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(req.Token)));
        var org = await db.Organizations.Include(x => x.Members).Include(x => x.Invites)
            .FirstOrDefaultAsync(x => x.Id == req.OrgId, ct);
        if (org is null) return Result.Failure(Error.NotFound("org.not_found", "Organization not found"));
        if (!org.AcceptInvite(hash, req.AcceptingUserId, DateTimeOffset.UtcNow))
            return Result.Failure(Error.Validation("org.invite_invalid", "Invite invalid or expired"));
        await db.SaveChangesAsync(ct);
        return Result.Success();
    }
}

public record GetMyOrgsQuery(Guid UserId) : IRequest<Result<IReadOnlyList<OrgDto>>>;

public sealed class GetMyOrgsHandler(IAuthDbContext db) : IRequestHandler<GetMyOrgsQuery, Result<IReadOnlyList<OrgDto>>>
{
    public async Task<Result<IReadOnlyList<OrgDto>>> Handle(GetMyOrgsQuery req, CancellationToken ct)
    {
        var items = await db.Organizations.AsNoTracking().Include(x => x.Members)
            .Where(x => x.IsActive && x.Members.Any(m => m.UserId == req.UserId))
            .Select(x => new OrgDto(x.Id, x.Name, x.Slug, x.Plan, x.SeatLimit, x.Members.Count,
                x.OwnerId, x.CreatedAt, x.LogoUrl, x.WebsiteUrl))
            .ToListAsync(ct);
        return Result.Success<IReadOnlyList<OrgDto>>(items);
    }
}
