using Libr4.IDE.Application.OrchestrationRun.Commands;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Libr4.IDE.Api;

/// <summary>
/// Minimal API endpoints for Orchestration Run Service
/// </summary>
public static class OrchestrationRunEndpoints
{
    public static void MapOrchestrationRunEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/ide/orchestration-run")
            .WithTags("Orchestration Run")
            .RequireAuthorization();

        group.MapPost("/start", async (
            [FromBody] StartOrchestrationRunCommand command,
            IMediator mediator,
            CancellationToken ct) =>
        {
            var result = await mediator.Send(command, ct);
            return Results.Ok(result);
        })
        .WithName("StartOrchestrationRun")
        .WithSummary("Start an orchestration run with skill selection and workflow transitions");
    }
}
