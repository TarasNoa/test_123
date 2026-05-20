using System.Security.Claims;
using Libr4.Matching.Application.Abstractions;
using Libr4.Matching.Domain.Matches;
using Libr4.Matching.Domain.Profiles;

namespace Libr4.Matching.Api.Endpoints;

public static class MatchingEndpoints
{
    public static IEndpointRouteBuilder MapMatchingEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/matching").WithTags("Matching").RequireAuthorization();
        var v1 = app.MapGroup("/api/v1/matching").WithTags("Matching V1").RequireAuthorization();

        // ─── GET /recommendations/tasks — рекомендованные задачи для текущего юзера ──
        v1.MapGet("/recommendations/tasks", async (
            ClaimsPrincipal principal,
            IMatchingService matching,
            int topK = 10,
            CancellationToken ct = default) =>
        {
            var userIdStr = principal.FindFirstValue(ClaimTypes.NameIdentifier)
                         ?? principal.FindFirstValue("sub");

            if (!Guid.TryParse(userIdStr, out var userId))
                return Results.Unauthorized();

            var results = await matching.FindMatchesForFreelancerAsync(userId, topK, ct);

            return Results.Ok(new RecommendedTasksResponse(
                FreelancerId: userId,
                Tasks: results.Select(r => new RecommendedTaskDto(
                    TaskId: r.TaskId,
                    TotalScore: r.TotalScore,
                    SemanticScore: r.SemanticScore,
                    MatchingSkills: r.MatchingSkills.ToList(),
                    Explanation: r.Explanation)).ToList()));
        });

        // ─── GET /recommendations/freelancers/{taskId} — фрилансеры для задачи ──
        v1.MapGet("/recommendations/freelancers/{taskId:guid}", async (
            Guid taskId,
            int topK = 20,
            IMatchingService matching = default!,
            CancellationToken ct = default) =>
        {
            var results = await matching.FindMatchesForTaskAsync(taskId, topK, ct);
            return Results.Ok(results);
        });

        // ─── POST /feedback ────────────────────────────────────────────────────
        v1.MapPost("/feedback", async (
            FeedbackRequest req,
            IMatchingService matching = default!,
            CancellationToken ct = default) =>
        {
            await matching.RecordFeedbackAsync(req.MatchId, req.Feedback, ct);
            return Results.NoContent();
        });

        // ─── Legacy endpoints ──────────────────────────────────────────────────
        group.MapPost("/tasks/{taskId:guid}/matches", async (
            Guid taskId, int topK = 20, IMatchingService matching = default!, CancellationToken ct = default) =>
        {
            var results = await matching.FindMatchesForTaskAsync(taskId, topK, ct);
            return Results.Ok(results);
        });

        group.MapPost("/freelancers/{freelancerId:guid}/matches", async (
            Guid freelancerId, int topK = 20, IMatchingService matching = default!, CancellationToken ct = default) =>
        {
            var results = await matching.FindMatchesForFreelancerAsync(freelancerId, topK, ct);
            return Results.Ok(results);
        });

        group.MapPost("/freelancers/{freelancerId:guid}/index", async (
            Guid freelancerId, FreelancerMatchProfile profile,
            IMatchingService matching = default!, CancellationToken ct = default) =>
        {
            profile.FreelancerId = freelancerId;
            await matching.IndexFreelancerAsync(profile, ct);
            return Results.NoContent();
        });

        group.MapPost("/tasks/{taskId:guid}/index", async (
            Guid taskId, TaskMatchProfile profile,
            IMatchingService matching = default!, CancellationToken ct = default) =>
        {
            profile.TaskId = taskId;
            await matching.IndexTaskAsync(profile, ct);
            return Results.NoContent();
        });

        group.MapPost("/feedback", async (
            FeedbackRequest req, IMatchingService matching = default!, CancellationToken ct = default) =>
        {
            await matching.RecordFeedbackAsync(req.MatchId, req.Feedback, ct);
            return Results.NoContent();
        });

        return app;
    }
}

public record FeedbackRequest(Guid MatchId, MatchFeedback Feedback);

public record RecommendedTasksResponse(
    Guid FreelancerId,
    List<RecommendedTaskDto> Tasks);

public record RecommendedTaskDto(
    Guid TaskId,
    float TotalScore,
    float SemanticScore,
    List<string> MatchingSkills,
    string Explanation);
