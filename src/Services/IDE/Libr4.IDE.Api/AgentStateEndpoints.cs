using Libr4.IDE.Application.AutonomousAppGeneration.AgentEvents;
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
            .WithOpenApi();

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
        group.MapPost("/run", async (
            [FromBody] RunCodeRequest request,
            ApplicationDbContext context,
            CancellationToken ct) =>
        {
            // Create new run ID
            var runId = Guid.NewGuid();
            
            // Create TaskAssigned event
            var taskEvent = new AgentEventEntity
            {
                Id = Guid.NewGuid(),
                RunId = runId,
                Type = "TaskAssigned",
                Timestamp = DateTime.UtcNow,
                Command = request.Code,
                Output = null,
                ExitCode = null,
                DurationMs = null
            };

            context.AgentEvents.Add(taskEvent);
            await context.SaveChangesAsync(ct);

            // TODO: Call F# state machine to process task
            // TODO: Call Rust sandbox via gRPC to execute code
            // TODO: Update event with result

            return Results.Ok(new { runId, status = "TaskAssigned" });
        })
        .WithName("RunCode")
        .WithSummary("Run code through full chain: C# → F# → Rust");
    }
}

public record RunCodeRequest
{
    public string Code { get; init; } = string.Empty;
    public string Language { get; init; } = "python";
}
