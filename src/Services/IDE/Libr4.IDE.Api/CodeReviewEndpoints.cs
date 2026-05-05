/*
using Libr4.IDE.Application.CodeReview.Commands;
using Libr4.IDE.Application.CodeReview.Validators;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Libr4.IDE.Api;

/// <summary>
/// Minimal API endpoints for Code Review Service
/// </summary>
public static class CodeReviewEndpoints
{
    public static void MapCodeReviewEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/ide/code-review")
            .WithTags("Code Review")
            .RequireAuthorization()
            .WithOpenApi();
        
        group.MapPost("/run-review", async (
            [FromBody] RunCodeReviewCommand command,
            IMediator mediator,
            RunCodeReviewCommandValidator validator,
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
        .WithName("RunCodeReview")
        .WithSummary("Run a code review")
        .WithDescription("Performs automated code reviews with architectural guardrails, risk detection, and recommendations")
        .WithOpenApi();
    }
}
*/
