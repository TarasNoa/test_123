/*
using Libr4.IDE.Domain.AI;
using Microsoft.AspNetCore.Mvc;

namespace Libr4.IDE.Api;

/// <summary>
/// Minimal API endpoints for AI Workflow
/// Enhanced with concepts from context-engineering-kit, Archon
/// </summary>
public static class AIWorkflowEndpoints
{
    public static void MapAIWorkflowEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/ide/workflows")
            .WithTags("AI Workflows")
            .RequireAuthorization();

        // Workflow operations
        group.MapPost("/create", (
            Guid userId,
            string name,
            List<WorkflowStep> steps,
            string? description = null,
            bool reflexionEnabled = true,
            int maxReflectionCycles = 3,
            CancellationToken ct) =>
        {
            return Results.Ok(new { name, stepCount = steps.Count });
        })
        .WithName("CreateAIWorkflow")
        .WithSummary("Create AI workflow")
        .WithDescription("Create new AI workflow with steps and reflection support");

        group.MapPost("/{workflowId}/start", (
            Guid workflowId,
            CancellationToken ct) =>
        {
            return Results.Ok(new { workflowId, status = "running" });
        })
        .WithName("StartAIWorkflow")
        .WithSummary("Start AI workflow")
        .WithDescription("Start execution of AI workflow");

        group.MapPost("/{workflowId}/step/{stepIndex}/complete", (
            Guid workflowId,
            int stepIndex,
            Dictionary<string, object> stepResult,
            CancellationToken ct) =>
        {
            return Results.Ok(new { workflowId, stepIndex });
        })
        .WithName("CompleteWorkflowStep")
        .WithSummary("Complete workflow step")
        .WithDescription("Mark workflow step as completed with results");

        // Reflection operations (from context-engineering-kit)
        group.MapPost("/{workflowId}/step/{stepIndex}/reflection/start", (
            Guid workflowId,
            int stepIndex,
            CancellationToken ct) =>
        {
            return Results.Ok(new { workflowId, stepIndex, status = "reflecting" });
        })
        .WithName("StartReflection")
        .WithSummary("Start reflection cycle")
        .WithDescription("Start reflection cycle for workflow step");

        group.MapPost("/{workflowId}/reflection/{reflectionId}/complete", (
            Guid workflowId,
            Guid reflectionId,
            ReflexionStatus status,
            List<string>? findings = null,
            List<string>? suggestions = null,
            CancellationToken ct) =>
        {
            return Results.Ok(new { workflowId, reflectionId, status });
        })
        .WithName("CompleteReflection")
        .WithSummary("Complete reflection cycle")
        .WithDescription("Complete reflection cycle with status and insights");

        group.MapPost("/{workflowId}/reflection/{reflectionId}/insights", (
            Guid workflowId,
            Guid reflectionId,
            List<string> insights,
            CancellationToken ct) =>
        {
            return Results.Ok(new { workflowId, reflectionId, insightCount = insights.Count });
        })
        .WithName("MemorizeInsights")
        .WithSummary("Memorize insights")
        .WithDescription("Memorize insights from reflection cycle");

        // Isolation operations (from Archon)
        group.MapPost("/{workflowId}/isolation/create", (
            Guid workflowId,
            string worktreePath,
            string branchName,
            CancellationToken ct) =>
        {
            return Results.Ok(new { workflowId, worktreePath, branchName });
        })
        .WithName("CreateIsolationContext")
        .WithSummary("Create isolation context")
        .WithDescription("Create git worktree isolation context for workflow");

        // Workflow status
        group.MapGet("/{workflowId}", (
            Guid workflowId,
            CancellationToken ct) =>
        {
            return Results.Ok(new { workflowId });
        })
        .WithName("GetWorkflowStatus")
        .WithSummary("Get workflow status")
        .WithDescription("Get current status and progress of workflow");

        group.MapPost("/{workflowId}/retry", (
            Guid workflowId,
            CancellationToken ct) =>
        {
            return Results.Ok(new { workflowId, status = "pending" });
        })
        .WithName("RetryWorkflow")
        .WithSummary("Retry failed workflow")
        .WithDescription("Retry failed workflow with retry count tracking");
    }
}
*/
