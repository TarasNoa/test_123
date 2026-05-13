using Libr4.IDE.Application.MultiAgentOrchestration.Commands;
using Libr4.IDE.Application.MultiAgentOrchestration.Validators;
using Libr4.IDE.Domain.MultiAgentOrchestration;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Libr4.IDE.Api;

public static class MultiAgentOrchestrationEndpoints
{
    public static void MapMultiAgentOrchestrationEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/ide/multi-agent")
            .RequireAuthorization();
        
        group.MapPost("/start-orchestration", async (
            [FromBody] StartAgentOrchestrationCommand command,
            IMediator mediator,
            StartAgentOrchestrationCommandValidator validator,
            CancellationToken ct) =>
        {
            var validationResult = await validator.ValidateAsync(command, ct);
            if (!validationResult.IsValid)
            {
                return Results.ValidationProblem(validationResult.ToDictionary());
            }
            
            var result = await mediator.Send(command, ct);
            return Results.Ok(result);
        });

        group.MapPost("/autonomous-loop/start", (
            [FromBody] AutonomousDevelopmentLoop loop,
            CancellationToken ct) =>
        {
            return Results.Ok(new { loopId = loop.Id, status = "started" });
        });

        group.MapPost("/circuit-breaker/trip", (
            [FromBody] CircuitBreaker breaker,
            CancellationToken ct) =>
        {
            return Results.Ok(new { breakerId = breaker.Id, state = breaker.State });
        });

        group.MapPost("/environment/create", (
            [FromBody] AiEnvironment environment,
            CancellationToken ct) =>
        {
            return Results.Ok(new { environmentId = environment.Id, name = environment.EnvironmentName });
        });

        group.MapPost("/skill-packages/install", (
            [FromBody] SkillPackage package,
            CancellationToken ct) =>
        {
            return Results.Ok(new { packageId = package.Id, name = package.PackageName });
        });

        group.MapPost("/swarm/create", (
            [FromBody] AgentSwarm swarm,
            CancellationToken ct) =>
        {
            return Results.Ok(new { swarmId = swarm.Id, orchestrator = swarm.OrchestratorType });
        });

        group.MapPost("/generative-ui/register", (
            [FromBody] GenerativeUiComponent component,
            CancellationToken ct) =>
        {
            return Results.Ok(new { componentId = component.Id, name = component.ComponentName });
        });

        group.MapPost("/ai-tasks/create", (
            [FromBody] AiTask task,
            CancellationToken ct) =>
        {
            return Results.Ok(new { taskId = task.Id, title = task.TaskTitle });
        });

        group.MapGet("/orchestration/{runId}", async (
            Guid runId,
            IMediator mediator,
            CancellationToken ct) =>
        {
            return Results.Ok(new { runId, status = "running" });
        });
    }
}
