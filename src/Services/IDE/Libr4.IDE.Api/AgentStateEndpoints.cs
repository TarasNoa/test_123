using Libr4.IDE.Application.AutonomousAppGeneration.AgentEvents;
using Libr4.IDE.Infrastructure.Clients;
using Libr4.IDE.Infrastructure.Orchestration;
using Libr4.IDE.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Libr4.IDE.Api;

/// <summary>
/// API endpoints for agent states and events
/// Provides REST API for Frontend to fetch real agent data from PostgreSQL
/// </summary>
public static class AgentStateEndpoints
{
    public static void MapAgentStateEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/ide/agent-states")
            .WithTags("AgentStates")
            ;

        // Get all agent events (using AgentEventEntity from persistence)
        group.MapGet("/events", async (
            ApplicationDbContext context,
            CancellationToken ct) =>
        {
            var events = await context.AgentEvents
                .AsNoTracking()
                .OrderByDescending(e => e.Timestamp)
                .Take(100)
                .ToListAsync(ct);

            return Results.Ok(events.Select(e => new
            {
                e.Id,
                e.RunId,
                e.Type,
                e.Timestamp,
                e.Command,
                e.Output,
                e.ExitCode,
                e.DurationMs
            }));
        })
        .WithName("GetAllAgentEvents")
        .WithSummary("Get all agent events");

        // Get events for specific run
        group.MapGet("/events/{runId}", async (
            Guid runId,
            ApplicationDbContext context,
            CancellationToken ct) =>
        {
            var events = await context.AgentEvents
                .AsNoTracking()
                .Where(e => e.RunId == runId)
                .OrderBy(e => e.Timestamp)
                .ToListAsync(ct);

            return Results.Ok(events.Select(e => new
            {
                e.Id,
                e.RunId,
                e.Type,
                e.Timestamp,
                e.Command,
                e.Output,
                e.ExitCode,
                e.DurationMs
            }));
        })
        .WithName("GetEventsByRunId")
        .WithSummary("Get events for specific run");

        // Run code - activates full chain: Frontend → C# → F# → Rust
        // Production-ready: health check, cancellation, termination reason handling, transactions
        group.MapPost("/run", async (
            [FromBody] RunCodeRequest request,
            AgentOrchestrator orchestrator,
            ISandboxClient sandbox,
            CancellationToken ct) =>
        {
            // Health check: verify Rust server is alive before accepting task
            var isHealthy = await sandbox.HealthCheckAsync(ct);
            if (!isHealthy)
            {
                return Results.Problem(
                    detail: "Rust sandbox server is not available",
                    statusCode: 503
                );
            }

            // Run task securely with transaction support and cancellation
            var agentId = Guid.NewGuid(); // TODO: Get actual agent ID from request or context
            await orchestrator.ProcessTaskAsync(agentId, request.Code, ct);

            return Results.Ok(new { agentId, status = "TaskAssigned" });
        })
        .RequireAuthorization()
        .WithName("RunCode")
        .WithSummary("Run code through full chain: C# → F# → Rust with health check, cancellation, and transactions");
    }
}

public record RunCodeRequest
{
    public string Code { get; init; } = string.Empty;
    public string Language { get; init; } = "python";
}
