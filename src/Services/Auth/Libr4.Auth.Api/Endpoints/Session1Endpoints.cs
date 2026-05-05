using Libr4.Auth.Application.ApiKeys;
using Libr4.Auth.Application.Gdpr;
using Libr4.Auth.Application.Kyc;
using Libr4.Auth.Application.Levels;
using Libr4.Auth.Application.Onboarding;
using Libr4.Auth.Application.Organizations;
using Libr4.Auth.Application.Profiles;
using Libr4.Auth.Application.Sso;
using Libr4.Auth.Domain.ApiKeys;
using Libr4.Auth.Domain.Gdpr;
using Libr4.Auth.Domain.Kyc;
using Libr4.Auth.Domain.Onboarding;
using Libr4.Auth.Domain.Organizations;
using Libr4.Auth.Domain.Profiles;
using Libr4.Auth.Domain.Sso;
using Libr4.Shared.Web.Auth;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Libr4.Auth.Api.Endpoints;

public static class Session1Endpoints
{
    public static IEndpointRouteBuilder MapSession1Endpoints(this IEndpointRouteBuilder app)
    {
        MapProfileEndpoints(app);
        MapKycEndpoints(app);
        MapOnboardingEndpoints(app);
        MapLevelsEndpoints(app);
        MapApiKeysEndpoints(app);
        MapGdprEndpoints(app);
        MapSsoEndpoints(app);
        MapOrganizationEndpoints(app);
        return app;
    }

    private static void MapProfileEndpoints(IEndpointRouteBuilder app)
    {
        var g = app.MapGroup("/api/v1/profiles").WithTags("Profile").RequireAuthorization();

        g.MapGet("/me", async (CurrentUser user, ISender s, CancellationToken ct) =>
        {
            var r = await s.Send(new GetProfileQuery(user.Id), ct);
            return r.IsSuccess ? Results.Ok(r.Value) : Results.NotFound(r.Error);
        });

        g.MapGet("/{userId:guid}", async (Guid userId, ISender s, CancellationToken ct) =>
        {
            var r = await s.Send(new GetProfileQuery(userId), ct);
            return r.IsSuccess ? Results.Ok(r.Value) : Results.NotFound(r.Error);
        });

        g.MapPut("/me/basics", async (UpdateProfileBasicsRequest req, CurrentUser user, ISender s, CancellationToken ct) =>
        {
            var r = await s.Send(new UpdateProfileBasicsCommand(user.Id, req.Headline, req.Bio, req.Location, req.TimeZone), ct);
            return r.IsSuccess ? Results.Ok(r.Value) : Results.BadRequest(r.Error);
        });

        g.MapPut("/me/availability", async (SetAvailabilityRequest req, CurrentUser user, ISender s, CancellationToken ct) =>
        {
            var r = await s.Send(new SetAvailabilityCommand(user.Id, req.Status, req.HourlyRate, req.Currency), ct);
            return r.IsSuccess ? Results.NoContent() : Results.BadRequest(r.Error);
        });

        g.MapPost("/me/skills", async (AddSkillRequest req, CurrentUser user, ISender s, CancellationToken ct) =>
        {
            var r = await s.Send(new AddSkillCommand(user.Id, req.Name, req.Level, req.YearsOfExperience), ct);
            return r.IsSuccess ? Results.Created() : Results.BadRequest(r.Error);
        });

        g.MapDelete("/me/skills/{name}", async (string name, CurrentUser user, ISender s, CancellationToken ct) =>
        {
            var r = await s.Send(new RemoveSkillCommand(user.Id, name), ct);
            return r.IsSuccess ? Results.NoContent() : Results.NotFound(r.Error);
        });

        g.MapPost("/me/languages", async (AddLanguageRequest req, CurrentUser user, ISender s, CancellationToken ct) =>
        {
            var r = await s.Send(new AddLanguageCommand(user.Id, req.Code, req.Proficiency), ct);
            return r.IsSuccess ? Results.Created() : Results.BadRequest(r.Error);
        });

        g.MapPost("/me/socials", async (SetSocialRequest req, CurrentUser user, ISender s, CancellationToken ct) =>
        {
            var r = await s.Send(new SetSocialLinkCommand(user.Id, req.Platform, req.Url), ct);
            return r.IsSuccess ? Results.NoContent() : Results.BadRequest(r.Error);
        });
    }

    private static void MapKycEndpoints(IEndpointRouteBuilder app)
    {
        var g = app.MapGroup("/api/v1/kyc").WithTags("KYC").RequireAuthorization();

        g.MapGet("/me", async (CurrentUser user, ISender s, CancellationToken ct) =>
        {
            var r = await s.Send(new GetMyKycQuery(user.Id), ct);
            return r.IsSuccess ? Results.Ok(r.Value) : Results.BadRequest(r.Error);
        });

        g.MapPost("/initiate", async (InitiateKycRequest req, CurrentUser user, ISender s, CancellationToken ct) =>
        {
            var r = await s.Send(new InitiateKycCommand(user.Id, req.Level, req.Provider), ct);
            return r.IsSuccess ? Results.Ok(new { verificationId = r.Value }) : Results.BadRequest(r.Error);
        });

        g.MapPost("/{verificationId:guid}/personal", async (Guid verificationId, SubmitPersonalDataRequest req, ISender s, CancellationToken ct) =>
        {
            var r = await s.Send(new SubmitKycPersonalDataCommand(verificationId, req.FullName, req.DateOfBirth,
                req.Nationality, req.CountryOfResidence, req.AddressLine1, req.AddressLine2, req.City, req.PostalCode), ct);
            return r.IsSuccess ? Results.NoContent() : Results.BadRequest(r.Error);
        });

        g.MapPost("/{verificationId:guid}/documents", async (Guid verificationId, UploadKycDocRequest req, ISender s, CancellationToken ct) =>
        {
            var r = await s.Send(new UploadKycDocumentCommand(verificationId, req.Type, req.FileUrl, req.Country), ct);
            return r.IsSuccess ? Results.Ok(new { documentId = r.Value }) : Results.BadRequest(r.Error);
        });

        // Admin operations
        var admin = app.MapGroup("/api/v1/admin/kyc").WithTags("KYC Admin").RequireAuthorization("Admin");

        admin.MapPost("/{verificationId:guid}/approve", async (Guid verificationId, ApproveKycRequest req, ISender s, CancellationToken ct) =>
        {
            var r = await s.Send(new ApproveKycCommand(verificationId, req.Risk, req.IsPep), ct);
            return r.IsSuccess ? Results.NoContent() : Results.BadRequest(r.Error);
        });

        admin.MapPost("/{verificationId:guid}/reject", async (Guid verificationId, RejectKycRequest req, ISender s, CancellationToken ct) =>
        {
            var r = await s.Send(new RejectKycCommand(verificationId, req.Reason), ct);
            return r.IsSuccess ? Results.NoContent() : Results.BadRequest(r.Error);
        });
    }

    private static void MapOnboardingEndpoints(IEndpointRouteBuilder app)
    {
        var g = app.MapGroup("/api/v1/onboarding").WithTags("Onboarding").RequireAuthorization();

        g.MapPost("/start", async (StartOnboardingRequest req, CurrentUser user, ISender s, CancellationToken ct) =>
        {
            var r = await s.Send(new StartOnboardingCommand(user.Id, req.Flow), ct);
            return r.IsSuccess ? Results.Ok(new { progressId = r.Value }) : Results.BadRequest(r.Error);
        });

        g.MapGet("/{flow}", async (OnboardingFlow flow, CurrentUser user, ISender s, CancellationToken ct) =>
        {
            var r = await s.Send(new GetMyOnboardingQuery(user.Id, flow), ct);
            return r.IsSuccess ? Results.Ok(r.Value) : Results.NotFound();
        });

        g.MapPost("/{flow}/steps/{stepKey}/complete", async (OnboardingFlow flow, string stepKey, CompleteStepRequest req, CurrentUser user, ISender s, CancellationToken ct) =>
        {
            var r = await s.Send(new CompleteOnboardingStepCommand(user.Id, flow, stepKey, req.PayloadJson), ct);
            return r.IsSuccess ? Results.NoContent() : Results.BadRequest(r.Error);
        });
    }

    private static void MapLevelsEndpoints(IEndpointRouteBuilder app)
    {
        var g = app.MapGroup("/api/v1/levels").WithTags("Levels & XP");

        g.MapGet("/me", async (CurrentUser user, ISender s, CancellationToken ct) =>
        {
            var r = await s.Send(new GetMyLevelQuery(user.Id), ct);
            return r.IsSuccess ? Results.Ok(r.Value) : Results.BadRequest(r.Error);
        }).RequireAuthorization();

        g.MapGet("/leaderboard", async (ISender s, CancellationToken ct, [FromQuery] int top = 50) =>
        {
            var r = await s.Send(new GetLeaderboardQuery(top), ct);
            return Results.Ok(r.Value);
        });
    }

    private static void MapApiKeysEndpoints(IEndpointRouteBuilder app)
    {
        var g = app.MapGroup("/api/v1/api-keys").WithTags("API Keys").RequireAuthorization();

        g.MapGet("/", async (CurrentUser user, ISender s, CancellationToken ct) =>
        {
            var r = await s.Send(new ListApiKeysQuery(user.Id), ct);
            return Results.Ok(r.Value);
        });

        g.MapPost("/", async (IssueApiKeyRequest req, CurrentUser user, ISender s, CancellationToken ct) =>
        {
            var lifetime = req.LifetimeDays.HasValue ? TimeSpan.FromDays(req.LifetimeDays.Value) : (TimeSpan?)null;
            var r = await s.Send(new IssueApiKeyCommand(user.Id, req.Name, req.Scopes, lifetime), ct);
            return r.IsSuccess ? Results.Created($"/api/v1/api-keys/{r.Value.Id}", r.Value) : Results.BadRequest(r.Error);
        });

        g.MapDelete("/{keyId:guid}", async (Guid keyId, [FromBody] RevokeKeyRequest? req, CurrentUser user, ISender s, CancellationToken ct) =>
        {
            var r = await s.Send(new RevokeApiKeyCommand(user.Id, keyId, req?.Reason), ct);
            return r.IsSuccess ? Results.NoContent() : Results.NotFound(r.Error);
        });
    }

    private static void MapGdprEndpoints(IEndpointRouteBuilder app)
    {
        var g = app.MapGroup("/api/v1/gdpr").WithTags("GDPR").RequireAuthorization();

        g.MapPost("/requests", async (SubmitGdprRequest req, CurrentUser user, ISender s, CancellationToken ct) =>
        {
            var r = await s.Send(new SubmitGdprRequestCommand(user.Id, req.Type, req.Reason), ct);
            return r.IsSuccess ? Results.Created("/api/v1/gdpr/requests", new { id = r.Value }) : Results.BadRequest(r.Error);
        });

        g.MapDelete("/requests/{requestId:guid}", async (Guid requestId, CurrentUser user, ISender s, CancellationToken ct) =>
        {
            var r = await s.Send(new CancelGdprRequestCommand(user.Id, requestId), ct);
            return r.IsSuccess ? Results.NoContent() : Results.NotFound(r.Error);
        });

        g.MapGet("/requests", async (CurrentUser user, ISender s, CancellationToken ct) =>
        {
            var r = await s.Send(new GetMyGdprRequestsQuery(user.Id), ct);
            return Results.Ok(r.Value);
        });

        g.MapPost("/consent", async (RecordConsentRequest req, CurrentUser user, HttpContext ctx, ISender s, CancellationToken ct) =>
        {
            var ip = ctx.Connection.RemoteIpAddress?.ToString();
            var ua = ctx.Request.Headers["User-Agent"].ToString();
            var r = await s.Send(new RecordConsentCommand(user.Id, req.Type, req.Version, req.Granted, ip, ua), ct);
            return r.IsSuccess ? Results.NoContent() : Results.BadRequest(r.Error);
        });
    }

    private static void MapSsoEndpoints(IEndpointRouteBuilder app)
    {
        var g = app.MapGroup("/api/v1/sso").WithTags("SSO").RequireAuthorization();

        g.MapGet("/links", async (CurrentUser user, ISender s, CancellationToken ct) =>
        {
            var r = await s.Send(new GetMyExternalLoginsQuery(user.Id), ct);
            return Results.Ok(r.Value);
        });

        g.MapPost("/link", async (LinkSsoRequest req, CurrentUser user, ISender s, CancellationToken ct) =>
        {
            var r = await s.Send(new LinkExternalLoginCommand(user.Id, req.Provider, req.ProviderUserId,
                req.Email, req.DisplayName, req.AvatarUrl), ct);
            return r.IsSuccess ? Results.NoContent() : Results.BadRequest(r.Error);
        });

        g.MapDelete("/{provider}", async (SsoProvider provider, CurrentUser user, ISender s, CancellationToken ct) =>
        {
            var r = await s.Send(new UnlinkExternalLoginCommand(user.Id, provider), ct);
            return r.IsSuccess ? Results.NoContent() : Results.NotFound(r.Error);
        });
    }

    private static void MapOrganizationEndpoints(IEndpointRouteBuilder app)
    {
        var g = app.MapGroup("/api/v1/organizations").WithTags("Organizations").RequireAuthorization();

        g.MapGet("/my", async (CurrentUser user, ISender s, CancellationToken ct) =>
        {
            var r = await s.Send(new GetMyOrgsQuery(user.Id), ct);
            return Results.Ok(r.Value);
        });

        g.MapPost("/", async (CreateOrgRequest req, CurrentUser user, ISender s, CancellationToken ct) =>
        {
            var r = await s.Send(new CreateOrganizationCommand(user.Id, req.Name, req.Slug, req.Plan), ct);
            return r.IsSuccess ? Results.Created($"/api/v1/organizations/{r.Value}", new { id = r.Value }) : Results.BadRequest(r.Error);
        });

        g.MapPost("/{orgId:guid}/invites", async (Guid orgId, InviteRequest req, CurrentUser user, ISender s, CancellationToken ct) =>
        {
            var r = await s.Send(new InviteMemberCommand(orgId, user.Id, req.Email, req.Role), ct);
            return r.IsSuccess ? Results.Ok(r.Value) : Results.BadRequest(r.Error);
        });

        g.MapPost("/{orgId:guid}/invites/accept", async (Guid orgId, AcceptInviteRequest req, CurrentUser user, ISender s, CancellationToken ct) =>
        {
            var r = await s.Send(new AcceptInviteCommand(orgId, req.Token, user.Id), ct);
            return r.IsSuccess ? Results.NoContent() : Results.BadRequest(r.Error);
        });
    }
}

// Request DTOs
public record UpdateProfileBasicsRequest(string? Headline, string? Bio, string? Location, string? TimeZone);
public record SetAvailabilityRequest(AvailabilityStatus Status, decimal? HourlyRate, string? Currency);
public record AddSkillRequest(string Name, SkillLevel Level, int YearsOfExperience);
public record AddLanguageRequest(string Code, LanguageProficiency Proficiency);
public record SetSocialRequest(SocialPlatform Platform, string Url);

public record InitiateKycRequest(KycLevel Level, string Provider);
public record SubmitPersonalDataRequest(string FullName, DateOnly DateOfBirth, string Nationality,
    string CountryOfResidence, string AddressLine1, string? AddressLine2, string City, string PostalCode);
public record UploadKycDocRequest(KycDocumentType Type, string FileUrl, string? Country);
public record ApproveKycRequest(Libr4.Auth.Domain.Kyc.RiskRating Risk, bool IsPep);
public record RejectKycRequest(string Reason);

public record StartOnboardingRequest(OnboardingFlow Flow);
public record CompleteStepRequest(string? PayloadJson);

public record IssueApiKeyRequest(string Name, ApiKeyScope Scopes, int? LifetimeDays);
public record RevokeKeyRequest(string? Reason);

public record SubmitGdprRequest(GdprRequestType Type, string? Reason);
public record RecordConsentRequest(ConsentType Type, string Version, bool Granted);

public record LinkSsoRequest(SsoProvider Provider, string ProviderUserId, string? Email, string? DisplayName, string? AvatarUrl);

public record CreateOrgRequest(string Name, string Slug, OrganizationPlan Plan);
public record InviteRequest(string Email, OrgRole Role);
public record AcceptInviteRequest(string Token);
