using System.Security.Claims;
using Libr4.IDE.Application.Orchestration;
using Libr4.IDE.Infrastructure.Clients;
using Libr4.IDE.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Libr4.IDE.Api;

/// <summary>
/// REST API для управления агентами и их событиями.
/// Все защищённые эндпоинты требуют JWT.
/// </summary>
public static class AgentStateEndpoints
{
    public static void MapAgentStateEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/ide/agent-states")
            .WithTags("AgentStates")
            .WithOpenApi();

        // ── GET /events ───────────────────────────────────────────────────────
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
                e.Id, e.RunId, e.Type, e.Timestamp,
                e.Command, e.Output, e.ExitCode, e.DurationMs
            }));
        })
        .WithName("GetAllAgentEvents")
        .WithSummary("Get latest 100 agent events");

        // ── GET /events/{runId} ───────────────────────────────────────────────
        group.MapGet("/events/{runId:guid}", async (
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
                e.Id, e.RunId, e.Type, e.Timestamp,
                e.Command, e.Output, e.ExitCode, e.DurationMs
            }));
        })
        .WithName("GetEventsByRunId")
        .WithSummary("Get all events for a specific run");

        // ── POST /run ─────────────────────────────────────────────────────────
        group.MapPost("/run", async (
            [FromBody] RunCodeRequest request,
            HttpContext httpContext,
            ResilientOrchestrator orchestrator,
            ISandboxClient sandbox,
            ApplicationDbContext db,
            CancellationToken ct) =>
        {
            // 1. Получаем userId из JWT — RequireAuthorization() уже проверил токен
            var userId = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrWhiteSpace(userId))
                return Results.Unauthorized();

            // 2. Ищем агента текущего пользователя в БД
            var agent = await db.Agents
                .FirstOrDefaultAsync(a => a.OwnerId == userId, ct);

            if (agent == null)
                return Results.NotFound(new
                {
                    error = "Agent not found.",
                    hint = "Create an agent first via POST /api/ide/agents"
                });

            // 3. Проверяем что Rust sandbox жив перед принятием задачи
            var isHealthy = await sandbox.HealthCheckAsync(ct);
            if (!isHealthy)
                return Results.Problem(
                    detail: "Rust sandbox is not available. Try again later.",
                    statusCode: 503);

            // 4. Запускаем через полную цепочку: C# → F# state machine → Rust sandbox
            await orchestrator.RunSecurelyAsync(agent.Id, request.Code, ct);

            return Results.Ok(new
            {
                agentId = agent.Id,
                status  = "TaskAssigned"
            });
        })
        .RequireAuthorization()
        .WithName("RunCode")
        .WithSummary("Run code: C# → F# → Rust sandbox");
    }
}

public record RunCodeRequest
{
    public string Code     { get; init; } = string.Empty;
    public string Language { get; init; } = "python";
}
