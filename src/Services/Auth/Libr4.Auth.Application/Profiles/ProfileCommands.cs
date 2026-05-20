using FluentValidation;
using Libr4.Auth.Application.Abstractions;
using Libr4.Auth.Domain.Profiles;
using Libr4.Shared.Kernel.Errors;
using Libr4.Shared.Kernel.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Libr4.Auth.Application.Profiles;

public record ProfileDto(
    Guid Id,
    Guid UserId,
    string? Headline,
    string? Bio,
    string? Location,
    string? TimeZone,
    string? AvatarUrl,
    string? CoverUrl,
    string? WebsiteUrl,
    AvailabilityStatus Availability,
    decimal? HourlyRate,
    string? HourlyRateCurrency,
    int CompletenessPct,
    bool IsPublic,
    IReadOnlyList<SkillDto> Skills,
    IReadOnlyList<LanguageDto> Languages,
    IReadOnlyList<SocialDto> Socials);

public record SkillDto(string Name, SkillLevel Level, int YearsOfExperience, bool Verified);
public record LanguageDto(string Code, LanguageProficiency Proficiency);
public record SocialDto(SocialPlatform Platform, string Url);

// === Get profile by user id ===
public record GetProfileQuery(Guid UserId) : IRequest<Result<ProfileDto>>;

public sealed class GetProfileHandler(IAuthDbContext db) : IRequestHandler<GetProfileQuery, Result<ProfileDto>>
{
    public async Task<Result<ProfileDto>> Handle(GetProfileQuery req, CancellationToken ct)
    {
        var p = await db.Profiles.AsNoTracking()
            .Include(x => x.Skills)
            .Include(x => x.Languages)
            .Include(x => x.Socials)
            .FirstOrDefaultAsync(x => x.UserId == req.UserId, ct);
        if (p is null) return Result.Failure<ProfileDto>(Error.NotFound("profile.not_found", "Profile not found"));
        return Result.Success(MapToDto(p));
    }

    internal static ProfileDto MapToDto(UserProfile p) => new(
        p.Id, p.UserId, p.Headline, p.Bio, p.Location, p.TimeZone,
        p.AvatarUrl, p.CoverUrl, p.WebsiteUrl, p.Availability,
        p.HourlyRate, p.HourlyRateCurrency, p.ProfileCompletenessPct, p.IsPublic,
        p.Skills.Select(s => new SkillDto(s.Name, s.Level, s.YearsOfExperience, s.Verified)).ToList(),
        p.Languages.Select(l => new LanguageDto(l.Code, l.Proficiency)).ToList(),
        p.Socials.Select(s => new SocialDto(s.Platform, s.Url)).ToList());
}

// === Update basics ===
public record UpdateProfileBasicsCommand(
    Guid UserId, string? Headline, string? Bio, string? Location, string? TimeZone) : IRequest<Result<ProfileDto>>;

public sealed class UpdateProfileBasicsValidator : AbstractValidator<UpdateProfileBasicsCommand>
{
    public UpdateProfileBasicsValidator()
    {
        RuleFor(x => x.Headline).MaximumLength(200);
        RuleFor(x => x.Bio).MaximumLength(4000);
    }
}

public sealed class UpdateProfileBasicsHandler(IAuthDbContext db) : IRequestHandler<UpdateProfileBasicsCommand, Result<ProfileDto>>
{
    public async Task<Result<ProfileDto>> Handle(UpdateProfileBasicsCommand req, CancellationToken ct)
    {
        var now = DateTimeOffset.UtcNow;
        var p = await db.Profiles
            .Include(x => x.Skills).Include(x => x.Languages).Include(x => x.Socials)
            .FirstOrDefaultAsync(x => x.UserId == req.UserId, ct);
        if (p is null)
        {
            p = UserProfile.Create(req.UserId, now);
            await db.Profiles.AddAsync(p, ct);
        }
        p.UpdateBasics(req.Headline, req.Bio, req.Location, req.TimeZone, now);

        // Sync User table so getMe() returns up-to-date data
        var user = await db.Users.FirstOrDefaultAsync(x => x.Id == req.UserId, ct);
        if (user is not null)
        {
            if (!string.IsNullOrWhiteSpace(req.Headline))
                user.UpdateDisplayName(req.Headline);
            user.UpdateBio(req.Bio);
        }

        await db.SaveChangesAsync(ct);
        return Result.Success(GetProfileHandler.MapToDto(p));
    }
}

// === Set availability ===
public record SetAvailabilityCommand(Guid UserId, AvailabilityStatus Status, decimal? HourlyRate, string? Currency)
    : IRequest<Result>;

public sealed class SetAvailabilityHandler(IAuthDbContext db) : IRequestHandler<SetAvailabilityCommand, Result>
{
    public async Task<Result> Handle(SetAvailabilityCommand req, CancellationToken ct)
    {
        var p = await db.Profiles.FirstOrDefaultAsync(x => x.UserId == req.UserId, ct);
        if (p is null) return Result.Failure(Error.NotFound("profile.not_found", "Profile not found"));
        p.SetAvailability(req.Status, req.HourlyRate, req.Currency, DateTimeOffset.UtcNow);
        await db.SaveChangesAsync(ct);
        return Result.Success();
    }
}

// === Add/Remove skill ===
public record AddSkillCommand(Guid UserId, string Name, SkillLevel Level, int YearsOfExperience) : IRequest<Result>;

public sealed class AddSkillHandler(IAuthDbContext db) : IRequestHandler<AddSkillCommand, Result>
{
    public async Task<Result> Handle(AddSkillCommand req, CancellationToken ct)
    {
        var p = await db.Profiles.Include(x => x.Skills).FirstOrDefaultAsync(x => x.UserId == req.UserId, ct);
        if (p is null) return Result.Failure(Error.NotFound("profile.not_found", "Profile not found"));
        p.AddSkill(req.Name, req.Level, req.YearsOfExperience);
        await db.SaveChangesAsync(ct);
        return Result.Success();
    }
}

public record RemoveSkillCommand(Guid UserId, string Name) : IRequest<Result>;

public sealed class RemoveSkillHandler(IAuthDbContext db) : IRequestHandler<RemoveSkillCommand, Result>
{
    public async Task<Result> Handle(RemoveSkillCommand req, CancellationToken ct)
    {
        var p = await db.Profiles.Include(x => x.Skills).FirstOrDefaultAsync(x => x.UserId == req.UserId, ct);
        if (p is null) return Result.Failure(Error.NotFound("profile.not_found", "Profile not found"));
        p.RemoveSkill(req.Name);
        await db.SaveChangesAsync(ct);
        return Result.Success();
    }
}

// === Add language / social ===
public record AddLanguageCommand(Guid UserId, string Code, LanguageProficiency Proficiency) : IRequest<Result>;

public sealed class AddLanguageHandler(IAuthDbContext db) : IRequestHandler<AddLanguageCommand, Result>
{
    public async Task<Result> Handle(AddLanguageCommand req, CancellationToken ct)
    {
        var p = await db.Profiles.Include(x => x.Languages).FirstOrDefaultAsync(x => x.UserId == req.UserId, ct);
        if (p is null) return Result.Failure(Error.NotFound("profile.not_found", "Profile not found"));
        p.AddLanguage(req.Code, req.Proficiency);
        await db.SaveChangesAsync(ct);
        return Result.Success();
    }
}

public record SetSocialLinkCommand(Guid UserId, SocialPlatform Platform, string Url) : IRequest<Result>;

public sealed class SetSocialLinkHandler(IAuthDbContext db) : IRequestHandler<SetSocialLinkCommand, Result>
{
    public async Task<Result> Handle(SetSocialLinkCommand req, CancellationToken ct)
    {
        var p = await db.Profiles.Include(x => x.Socials).FirstOrDefaultAsync(x => x.UserId == req.UserId, ct);
        if (p is null) return Result.Failure(Error.NotFound("profile.not_found", "Profile not found"));
        p.AddSocial(req.Platform, req.Url);
        await db.SaveChangesAsync(ct);
        return Result.Success();
    }
}
