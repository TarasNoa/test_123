using Libr4.Matching.Application.Abstractions;
using Libr4.Matching.Domain.Matches;
using Libr4.Matching.Domain.Profiles;

namespace Libr4.Matching.Api.Endpoints;

public static class MatchingEndpoints
{
    public static IEndpointRouteBuilder MapMatchingEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/matching").WithTags("Matching");

        group.MapPost("/tasks/{taskId:guid}/matches", async (
            Guid taskId,
            int topK = 20,
            IMatchingService matching = default!,
            CancellationToken ct = default) =>
        {
            var results = await matching.FindMatchesForTaskAsync(taskId, topK, ct);
            return Results.Ok(results);
        });

        group.MapPost("/freelancers/{freelancerId:guid}/matches", async (
            Guid freelancerId,
            int topK = 20,
            IMatchingService matching = default!,
            CancellationToken ct = default) =>
        {
            var results = await matching.FindMatchesForFreelancerAsync(freelancerId, topK, ct);
            return Results.Ok(results);
        });

        group.MapPost("/freelancers/{freelancerId:guid}/index", async (
            Guid freelancerId,
            FreelancerMatchProfile profile,
            IMatchingService matching = default!,
            CancellationToken ct = default) =>
        {
            profile.FreelancerId = freelancerId;
            await matching.IndexFreelancerAsync(profile, ct);
            return Results.NoContent();
        });

        group.MapPost("/tasks/{taskId:guid}/index", async (
            Guid taskId,
            TaskMatchProfile profile,
            IMatchingService matching = default!,
            CancellationToken ct = default) =>
        {
            profile.TaskId = taskId;
            await matching.IndexTaskAsync(profile, ct);
            return Results.NoContent();
        });

        group.MapPost("/feedback", async (
            FeedbackRequest req,
            IMatchingService matching = default!,
            CancellationToken ct = default) =>
        {
            await matching.RecordFeedbackAsync(req.MatchId, req.Feedback, ct);
            return Results.NoContent();
        });

        return app;
    }
}

public record FeedbackRequest(Guid MatchId, MatchFeedback Feedback);
