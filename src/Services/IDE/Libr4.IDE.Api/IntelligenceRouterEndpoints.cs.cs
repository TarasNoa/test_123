using Libr4.IDE.Application.IntelligenceRouter.Commands;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Libr4.IDE.Api;

/// <summary>
/// Minimal API endpoints for Intelligence Router Service
/// </summary>
public static class IntelligenceRouterEndpoints
{
    public static void MapIntelligenceRouterEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/ide/intelligence-router")
            .WithTags("Intelligence Router")
            .RequireAuthorization();

        group.MapPost("/build-routing-plan", async (
            [FromBody] BuildRoutingPlanCommand command,
            IMediator mediator,
            CancellationToken ct) =>
        {
            var result = await mediator.Send(command, ct);
            return Results.Ok(result);
        })
        .WithName("BuildRoutingPlan")
        .WithSummary("Generate smart routing plan selecting AI models and tools for each dev phase");
    }
}
