using Libr4.AI.Application.Abstractions;
using Microsoft.AspNetCore.Mvc;

namespace Libr4.AI.Api.Endpoints;

public static class TaskRecommendationEndpoints
{
    public static void MapTaskRecommendationEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/ai/task-recommendations")
            .WithTags("Task Recommendations")
            .RequireAuthorization();

        group.MapPost("/recommend", async (
            [FromBody] TaskRecommendationRequest request,
            ITaskRecommendationService service) =>
        {
            if (request == null || request.UserProfile == null || request.AvailableTasks == null)
            {
                return Results.BadRequest(new { error = "Request, user profile, and available tasks cannot be null" });
            }

            if (request.AvailableTasks.Count == 0)
            {
                return Results.BadRequest(new { error = "Available tasks list cannot be empty" });
            }

            try
            {
                var result = await service.RecommendTasksAsync(request);
                return Results.Ok(new { recommendations = result });
            }
            catch (Exception ex)
            {
                return Results.Problem(
                    detail: $"Failed to recommend tasks: {ex.Message}",
                    statusCode: 500,
                    title: "Task Recommendation Error");
            }
        })
        .WithName("RecommendTasks")
        .WithSummary("AI-powered task recommendations for freelancers")
        .WithDescription("Recommends tasks based on freelancer skills, interests, and task requirements using Rust/F# algorithms.");

        group.MapPost("/recommend/{userId}", async (
            Guid userId,
            [FromBody] TaskRecommendationRequest request,
            ITaskRecommendationService service) =>
        {
            if (request == null || request.UserProfile == null || request.AvailableTasks == null)
            {
                return Results.BadRequest(new { error = "Request, user profile, and available tasks cannot be null" });
            }

            if (request.UserProfile.UserId != userId)
            {
                return Results.BadRequest(new { error = "User ID in path must match user profile ID" });
            }

            try
            {
                var result = await service.RecommendTasksAsync(request);
                return Results.Ok(new { userId, recommendations = result });
            }
            catch (Exception ex)
            {
                return Results.Problem(
                    detail: $"Failed to recommend tasks for user {userId}: {ex.Message}",
                    statusCode: 500,
                    title: "Task Recommendation Error");
            }
        })
        .WithName("RecommendTasksForUser")
        .WithSummary("AI-powered task recommendations for a specific freelancer")
        .WithDescription("Recommends tasks for a specific user based on their profile and available tasks.");

        group.MapGet("/health", () => Results.Ok(new { status = "Task Recommendations is healthy", timestamp = DateTimeOffset.UtcNow }))
            .WithName("TaskRecommendationsHealth")
            .WithSummary("Health check for Task Recommendations service");

        group.MapGet("/stats", async (ITaskRecommendationService service) =>
        {
            // Placeholder for stats - in real implementation, track usage metrics
            return Results.Ok(new
            {
                totalRequests = 0, // Would be tracked in infrastructure
                averageResponseTime = 0.0,
                lastRequestAt = (DateTimeOffset?)null
            });
        })
        .WithName("TaskRecommendationsStats")
        .WithSummary("Get usage statistics for Task Recommendations service");
    }
}