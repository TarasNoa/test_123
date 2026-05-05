/*
using System.Threading.RateLimiting;
using Libr4.IDE.Application.TaskDecomposition.Commands;
using Libr4.IDE.Application.TaskDecomposition.DTOs;
using Libr4.IDE.Application.TaskDecomposition.Validators;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Libr4.IDE.Api;

/// <summary>
/// Minimal API endpoints for Task Decomposition Service
/// </summary>
public static class TaskDecompositionEndpoints
{
    public static void MapTaskDecompositionEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/ide/task-decomposition")
            .WithTags("Task Decomposition")
            .RequireAuthorization()
            .WithOpenApi();
        
        // Rate limiting policy
        var rateLimiter = new RateLimiter();
        
        group.MapPost("/decompose", async (
            [FromBody] DecomposeTaskCommand command,
            IMediator mediator,
            DecomposeTaskCommandValidator validator,
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
        .WithName("DecomposeTask")
        .WithSummary("Decompose a task into an execution plan")
        .WithDescription("Breaks down a complex user request into executable phases for safe AI operations")
        .WithOpenApi();
        
        group.MapPost("/decompose-stream", async (
            [FromBody] DecomposeTaskCommand command,
            IMediator mediator,
            DecomposeTaskCommandValidator validator,
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
        .WithName("DecomposeTaskStream")
        .WithSummary("Decompose a task with streaming response")
        .WithDescription("Breaks down a complex user request into executable phases with streaming")
        .WithOpenApi();
    }
}
*/
