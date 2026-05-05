using Libr4.AI.Infrastructure.Orchestration;
using Microsoft.AspNetCore.Mvc;

namespace Libr4.AI.Api.Endpoints;

/// <summary>
/// API endpoints for background agent orchestration
/// </summary>
public static class OrchestrationEndpoints
{
    public static IEndpointRouteBuilder MapOrchestrationEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/orchestration")
            .WithTags("Orchestration")
            .WithOpenApi();

        // Dispatch task to background agent
        group.MapPost("/dispatch", async (
            [FromBody] AgentTask task,
            [FromServices] IBackgroundAgentOrchestrator orchestrator,
            CancellationToken ct) =>
        {
            var taskId = await orchestrator.DispatchAsync(task);
            return Results.Ok(new { taskId });
        })
        .WithName("DispatchTask");

        // Get task status
        group.MapGet("/tasks/{taskId:guid}", async (
            Guid taskId,
            [FromServices] IBackgroundAgentOrchestrator orchestrator,
            CancellationToken ct) =>
        {
            var status = await orchestrator.GetTaskStatusAsync(taskId);
            return status is not null ? Results.Ok(status) : Results.NotFound();
        })
        .WithName("GetTaskStatus");

        // Wait for task completion
        group.MapGet("/tasks/{taskId:guid}/wait", async (
            Guid taskId,
            [FromServices] IBackgroundAgentOrchestrator orchestrator,
            CancellationToken ct) =>
        {
            var result = await orchestrator.WaitForCompletionAsync(taskId, ct);
            return result is not null ? Results.Ok(result) : Results.NotFound();
        })
        .WithName("WaitForTaskCompletion");

        // Cancel task
        group.MapDelete("/tasks/{taskId:guid}", async (
            Guid taskId,
            [FromServices] IBackgroundAgentOrchestrator orchestrator,
            CancellationToken ct) =>
        {
            await orchestrator.CancelTaskAsync(taskId);
            return Results.NoContent();
        })
        .WithName("CancelTask");

        // List active tasks
        group.MapGet("/tasks", async (
            [FromServices] IBackgroundAgentOrchestrator orchestrator,
            CancellationToken ct) =>
        {
            var tasks = await orchestrator.ListActiveTasksAsync();
            return Results.Ok(tasks);
        })
        .WithName("ListActiveTasks");

        // Run parallel agents
        group.MapPost("/parallel", async (
            [FromServices] IBackgroundAgentOrchestrator orchestrator,
            [FromBody] AgentTask task,
            CancellationToken ct,
            [FromQuery] int agentCount = 3) =>
        {
            var results = await orchestrator.RunParallelAgentsAsync(task, agentCount, ct);
            return Results.Ok(results);
        })
        .WithName("RunParallelAgents");

        return app;
    }
}
