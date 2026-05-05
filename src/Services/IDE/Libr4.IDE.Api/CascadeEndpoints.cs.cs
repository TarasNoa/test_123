using Libr4.IDE.Application.Cascade.Commands;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Libr4.IDE.Api;

/// <summary>
/// Minimal API endpoints for Cascade Service
/// </summary>
public static class CascadeEndpoints
{
    public static void MapCascadeEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/ide/cascade")
            .WithTags("Cascade")
            .RequireAuthorization();

        group.MapPost("/plan", async (
            [FromBody] RunCascadePlanningCommand command,
            IMediator mediator,
            CancellationToken ct) =>
        {
            var result = await mediator.Send(command, ct);
            return Results.Ok(result);
        })
        .WithName("RunCascadePlanning")
        .WithSummary("Run cascade planning")
        .WithDescription("Decomposes a task prompt into an LLM-generated multi-phase orchestration plan with heuristic fallback");
    }
}
