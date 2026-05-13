using Libr4.IDE.Domain.AI;
using Microsoft.AspNetCore.Mvc;

namespace Libr4.IDE.Api;

public static class AIWorkflowEndpoints
{
    public static void MapAIWorkflowEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/ide/workflows")
            .RequireAuthorization();

        group.MapPost("/create", (
            Guid userId,
            string name,
            List<WorkflowStep> steps,
            CancellationToken ct,
            string? description = null,
            bool reflexionEnabled = true,
            int maxReflectionCycles = 3) =>
        {
            return Results.Ok(new { name, stepCount = steps.Count });
        });

        group.MapPost("/{workflowId}/start", (
            Guid workflowId,
            CancellationToken ct) =>
        {
            return Results.Ok(new { workflowId, status = "running" });
        });

        group.MapPost("/{workflowId}/step/{stepIndex}/complete", (
            Guid workflowId,
            int stepIndex,
            Dictionary<string, object> stepResult,
            CancellationToken ct) =>
        {
            return Results.Ok(new { workflowId, stepIndex });
        });

        group.MapPost("/{workflowId}/step/{stepIndex}/reflection/start", (
            Guid workflowId,
            int stepIndex,
            CancellationToken ct) =>
        {
            return Results.Ok(new { workflowId, stepIndex, status = "reflecting" });
        });

        group.MapPost("/{workflowId}/reflection/{reflectionId}/complete", (
            Guid workflowId,
            Guid reflectionId,
            [FromQuery] ReflexionStatus status,
            [FromBody] ReflectionCompletionRequest request,
            CancellationToken ct) =>
        {
            return Results.Ok(new { workflowId, reflectionId, status });
        });

        group.MapPost("/{workflowId}/reflection/{reflectionId}/insights", (
            Guid workflowId,
            Guid reflectionId,
            [FromBody] List<string> insights,
            CancellationToken ct) =>
        {
            return Results.Ok(new { workflowId, reflectionId, insightCount = insights.Count });
        });

        group.MapPost("/{workflowId}/isolation/create", (
            Guid workflowId,
            string worktreePath,
            string branchName,
            CancellationToken ct) =>
        {
            return Results.Ok(new { workflowId, worktreePath, branchName });
        });

        group.MapGet("/{workflowId}", (
            Guid workflowId,
            CancellationToken ct) =>
        {
            return Results.Ok(new { workflowId });
        });

        group.MapPost("/{workflowId}/retry", (
            Guid workflowId,
            CancellationToken ct) =>
        {
            return Results.Ok(new { workflowId, status = "pending" });
        });
    }
}

public record ReflectionCompletionRequest(List<string>? Findings, List<string>? Suggestions);
