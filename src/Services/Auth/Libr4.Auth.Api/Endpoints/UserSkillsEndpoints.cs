using System.Security.Claims;
using Libr4.Shared.Contracts.IntegrationEvents.Auth;
using Libr4.Shared.Web.Auth;
using MassTransit;
using Microsoft.AspNetCore.Mvc;

namespace Libr4.Auth.Api.Endpoints;

public static class UserSkillsEndpoints
{
    public static IEndpointRouteBuilder MapUserSkillsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/users/skills")
            .WithTags("User Skills")
            .RequireAuthorization();

        // Get current user's skills
        group.MapGet("/my", async (
            CurrentUser user,
            Application.Abstractions.IAuthDbContext db,
            CancellationToken ct) =>
        {
            var skills = db.UserSkills
                .Where(s => s.UserId == user.Id)
                .OrderByDescending(s => s.Score)
                .Select(s => new UserSkillDto(
                    s.Id,
                    s.Name,
                    s.Score,
                    s.Level,
                    s.Source,
                    s.ExperienceYears,
                    s.Contexts,
                    s.AssessmentReason,  // Why AI gave this score
                    s.AssessedAt))
                .ToList();

            var assessment = db.SkillAssessments
                .Where(a => a.UserId == user.Id)
                .OrderByDescending(a => a.AssessedAt)
                .FirstOrDefault();

            return Results.Ok(new UserSkillsSummaryDto(
                user.Id,
                skills,
                assessment?.OverallLevel ?? "Not assessed",
                assessment?.OverallScore ?? 0,
                assessment?.PrimaryExpertise ?? "Unknown",
                assessment?.SecondaryExpertise ?? new List<string>(),
                assessment?.Recommendations ?? new List<string>(),
                assessment?.AssessedAt));
        });

        // Get skills by user ID (public profile view)
        group.MapGet("/{userId:guid}", async (
            Guid userId,
            Application.Abstractions.IAuthDbContext db,
            CancellationToken ct) =>
        {
            var skills = db.UserSkills
                .Where(s => s.UserId == userId)
                .OrderByDescending(s => s.Score)
                .Select(s => new UserSkillDto(
                    s.Id,
                    s.Name,
                    s.Score,
                    s.Level,
                    s.Source,
                    s.ExperienceYears,
                    s.Contexts,
                    null,  // Hide assessment reason for public view
                    s.AssessedAt))
                .ToList();

            var assessment = db.SkillAssessments
                .Where(a => a.UserId == userId)
                .OrderByDescending(a => a.AssessedAt)
                .FirstOrDefault();

            return Results.Ok(new UserSkillsSummaryDto(
                userId,
                skills,
                assessment?.OverallLevel ?? "Not assessed",
                assessment?.OverallScore ?? 0,
                assessment?.PrimaryExpertise ?? "Unknown",
                assessment?.SecondaryExpertise ?? new List<string>(),
                assessment?.Recommendations ?? new List<string>(),
                assessment?.AssessedAt));
        }).AllowAnonymous();

        // Save skills from CV analysis (internal use by verification service)
        group.MapPost("/save-assessment", async (
            [FromBody] SaveSkillsAssessmentRequest request,
            CurrentUser user,
            Application.Abstractions.IAuthDbContext db,
            IPublishEndpoint bus,
            CancellationToken ct) =>
        {
            // Clear old skills
            var oldSkills = db.UserSkills.Where(s => s.UserId == user.Id).ToList();
            foreach (var old in oldSkills) db.UserSkills.Remove(old);

            // Save new skills
            foreach (var skill in request.Skills)
            {
                db.UserSkills.Add(new Domain.Skills.UserSkill
                {
                    Id = Guid.NewGuid(),
                    UserId = user.Id,
                    Name = skill.Name,
                    Score = skill.Score,
                    Level = skill.Level,
                    Source = skill.Source,
                    ExperienceYears = skill.ExperienceYears,
                    Contexts = skill.Contexts,
                    AssessmentReason = skill.AssessmentReason,
                    AssessedAt = DateTimeOffset.UtcNow
                });
            }

            // Save assessment summary
            var oldAssessment = db.SkillAssessments.FirstOrDefault(a => a.UserId == user.Id);
            if (oldAssessment != null) db.SkillAssessments.Remove(oldAssessment);

            db.SkillAssessments.Add(new Domain.Skills.SkillAssessment
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                OverallLevel = request.OverallLevel,
                OverallScore = request.OverallScore,
                PrimaryExpertise = request.PrimaryExpertise,
                SecondaryExpertise = request.SecondaryExpertise,
                Recommendations = request.Recommendations,
                AssessedAt = DateTimeOffset.UtcNow
            });

            await db.SaveChangesAsync(ct);

            // Publish event so Matching service reindexes the freelancer
            await bus.Publish(new SkillAssessmentCompletedIntegrationEvent(
                UserId: user.Id,
                OverallLevel: request.OverallLevel,
                OverallScore: request.OverallScore,
                PrimaryExpertise: request.PrimaryExpertise,
                SecondaryExpertise: request.SecondaryExpertise,
                Skills: request.Skills.Select(s => new AssessedSkillDto(
                    s.Name, s.Score, s.Level, s.ExperienceYears, s.Contexts)).ToList(),
                Recommendations: request.Recommendations,
                OccurredOn: DateTimeOffset.UtcNow), ct);

            return Results.Ok(new { savedSkills = request.Skills.Count });
        });

        return app;
    }
}

// DTOs
public sealed record UserSkillDto(
    Guid Id,
    string Name,
    float Score,  // 0-100
    string Level,
    string Source,
    int ExperienceYears,
    List<string> Contexts,
    string? AssessmentReason,  // Explanation from AI
    DateTimeOffset AssessedAt);

public sealed record UserSkillsSummaryDto(
    Guid UserId,
    List<UserSkillDto> Skills,
    string OverallLevel,
    float OverallScore,
    string PrimaryExpertise,
    List<string> SecondaryExpertise,
    List<string> Recommendations,
    DateTimeOffset? LastAssessedAt);

public sealed record SaveSkillsAssessmentRequest(
    List<SkillInputDto> Skills,
    string OverallLevel,
    float OverallScore,
    string PrimaryExpertise,
    List<string> SecondaryExpertise,
    List<string> Recommendations);
