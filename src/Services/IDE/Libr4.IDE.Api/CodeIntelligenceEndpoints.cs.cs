/*
using Libr4.IDE.Application.CodeIntelligence.Commands;
using Libr4.IDE.Application.CodeIntelligence.Validators;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Libr4.IDE.Api;

/// <summary>
/// Minimal API endpoints for Code Intelligence
/// </summary>
public static class CodeIntelligenceEndpoints
{
    public static void MapCodeIntelligenceEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/ide/code-intelligence")
            .WithTags("Code Intelligence")
            .RequireAuthorization()
            .WithOpenApi();
        
        group.MapPost("/completions", async (
            [FromBody] GetCompletionsCommand command,
            IMediator mediator,
            GetCompletionsCommandValidator validator,
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
        .WithName("GetCompletions")
        .WithSummary("Get code completions")
        .WithDescription("Provides LSP-based code completions with smart suggestions and context-aware ranking")
        .WithOpenApi();
    }
}
*/
