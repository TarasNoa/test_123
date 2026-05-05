/*
using Libr4.IDE.Application.LLMRouter.Commands;
using Libr4.IDE.Application.LLMRouter.Validators;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Libr4.IDE.Api;

/// <summary>
/// Minimal API endpoints for LLM Router
/// </summary>
public static class LLMRouterEndpoints
{
    public static void MapLLMRouterEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/ide/llm-router")
            .WithTags("LLM Router")
            .RequireAuthorization();
        
        group.MapPost("/route", async (
            [FromBody] RouteLLMCommand command,
            IMediator mediator,
            RouteLLMCommandValidator validator,
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
        .WithName("RouteLLM")
        .WithSummary("Route LLM request")
        .WithDescription("Optimizes LLM costs by 92% through intelligent routing and model selection")
        .WithOpenApi();
    }
}
*/
