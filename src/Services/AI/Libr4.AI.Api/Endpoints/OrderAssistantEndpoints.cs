using Libr4.AI.Application.Abstractions;
using Microsoft.AspNetCore.Mvc;

namespace Libr4.AI.Api.Endpoints;

public static class OrderAssistantEndpoints
{
    public static void MapOrderAssistantEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/ai/order-assistant")
            .WithTags("Order Assistant")
            .RequireAuthorization();

        group.MapPost("/suggest", async (
            [FromBody] OrderAssistantRequest request,
            IOrderAssistantService service) =>
        {
            if (request == null)
            {
                return Results.BadRequest(new { error = "Request cannot be null" });
            }

            if (string.IsNullOrWhiteSpace(request.TaskTitle))
            {
                return Results.BadRequest(new { error = "Task title is required" });
            }

            if (request.BudgetMin < 0 || request.BudgetMax < request.BudgetMin)
            {
                return Results.BadRequest(new { error = "Invalid budget range" });
            }

            if (request.DurationDays < 1)
            {
                return Results.BadRequest(new { error = "Duration must be at least 1 day" });
            }

            try
            {
                var result = await service.SuggestOrderAsync(request);
                return Results.Ok(new { suggestion = result });
            }
            catch (Exception ex)
            {
                return Results.Problem(
                    detail: $"Failed to suggest order: {ex.Message}",
                    statusCode: 500,
                    title: "Order Suggestion Error");
            }
        })
        .WithName("SuggestOrder")
        .WithSummary("AI-powered order suggestion for marketplace tasks")
        .WithDescription("Analyzes task requirements and candidate freelancers to suggest optimal budget, duration, and recommendations using Rust/F# algorithms.");

        group.MapPost("/suggest/{userId}", async (
            Guid userId,
            [FromBody] OrderAssistantRequest request,
            IOrderAssistantService service) =>
        {
            if (request == null)
            {
                return Results.BadRequest(new { error = "Request cannot be null" });
            }

            if (request.UserId != userId)
            {
                return Results.BadRequest(new { error = "User ID in path must match request user ID" });
            }

            try
            {
                var result = await service.SuggestOrderAsync(request);
                return Results.Ok(new { userId, suggestion = result });
            }
            catch (Exception ex)
            {
                return Results.Problem(
                    detail: $"Failed to suggest order for user {userId}: {ex.Message}",
                    statusCode: 500,
                    title: "Order Suggestion Error");
            }
        })
        .WithName("SuggestOrderForUser")
        .WithSummary("AI-powered order suggestion for a specific user")
        .WithDescription("Suggests order parameters for a specific user based on task and freelancer data.");

        group.MapGet("/health", () => Results.Ok(new { status = "Order Assistant is healthy", timestamp = DateTimeOffset.UtcNow }))
            .WithName("OrderAssistantHealth")
            .WithSummary("Health check for Order Assistant service");

        group.MapGet("/stats", async (IOrderAssistantService service) =>
        {
            // Placeholder for stats - in real implementation, track usage metrics
            return Results.Ok(new
            {
                totalRequests = 0,
                averageResponseTime = 0.0,
                lastRequestAt = (DateTimeOffset?)null
            });
        })
        .WithName("OrderAssistantStats")
        .WithSummary("Get usage statistics for Order Assistant service");
    }
}