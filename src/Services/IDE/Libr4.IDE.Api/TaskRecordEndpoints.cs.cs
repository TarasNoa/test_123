/*
using Libr4.IDE.Application.TaskRecord.Commands;
using Libr4.IDE.Application.TaskRecord.Validators;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Libr4.IDE.Api;

/// <summary>
/// Minimal API endpoints for Task Record
/// </summary>
public static class TaskRecordEndpoints
{
    public static void MapTaskRecordEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/ide/task-record")
            .WithTags("Task Record")
            .RequireAuthorization()
            .WithOpenApi();
        
        group.MapPost("/create", async (
            [FromBody] CreateTaskRecordCommand command,
            IMediator mediator,
            CreateTaskRecordCommandValidator validator,
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
        .WithName("CreateTaskRecord")
        .WithSummary("Create task record")
        .WithDescription("Creates a task record with persistence and checkpoint capabilities for task override and resume")
        .WithOpenApi();
    }
}
*/
