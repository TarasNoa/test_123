using Libr4.IDE.Application.AutonomousRuntimePolicy.Commands;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Libr4.IDE.Api;

/// <summary>
/// Minimal API endpoints for Autonomous Runtime Policy Service
/// </summary>
public static class AutonomousRuntimePolicyEndpoints
{
    public static void MapAutonomousRuntimePolicyEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/ide/autonomous-policy")
            .WithTags("Autonomous Runtime Policy")
            .RequireAuthorization();

        group.MapPost("/generate", async (
            [FromBody] GeneratePolicyCommand command,
            IMediator mediator,
            CancellationToken ct) =>
        {
            var result = await mediator.Send(command, ct);
            return Results.Ok(result);
        })
        .WithName("GenerateRuntimePolicy")
        .WithSummary("Generate autonomous runtime policy with domain signals and quality contracts");
    }
}
