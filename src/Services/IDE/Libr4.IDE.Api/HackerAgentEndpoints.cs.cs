/*
using Libr4.IDE.Application.HackerAgent.Commands;
using Libr4.IDE.Application.HackerAgent.Validators;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Libr4.IDE.Api;

/// <summary>
/// Minimal API endpoints for Hacker Agent
/// </summary>
public static class HackerAgentEndpoints
{
    public static void MapHackerAgentEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/ide/hacker-agent")
            .WithTags("Hacker Agent")
            .RequireAuthorization()
            .WithOpenApi();
        
        group.MapPost("/run", async (
            [FromBody] RunHackerAgentCommand command,
            IMediator mediator,
            RunHackerAgentCommandValidator validator,
            CancellationToken ct) =>
        {
            // Validate command
            var validationResult = await validator.ValidateAsync(command, ct);
            if (!validationResult.IsValid)
            {
                return Results.ValidationProblem(validationResult.ToDictionary());
            }
            
            // Execute command
            var result = await mediator.Send(command, ct);
            
            return Results.Ok(result);
        })
        .WithName("RunHackerAgent")
        .WithSummary("Run hacker agent")
        .WithDescription("Generates security testing scripts, fetches tools from GitHub, and executes full security testing workflow")
        .WithOpenApi();
    }
}
*/
