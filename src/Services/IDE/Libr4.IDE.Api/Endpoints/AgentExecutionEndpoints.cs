using Libr4.IDE.Application.AgentExecution;
using Libr4.Shared.Infrastructure.Messaging;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Libr4.IDE.Api.Endpoints;

public static class AgentExecutionEndpoints
{
    public static void MapAgentExecutionEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/agent/execution")
            .WithTags("Agent Execution")
            .RequireAuthorization();

        group.MapPost("/execute", ExecuteCode)
            .WithName("ExecuteAgentCode")
            .WithSummary("Execute AI-generated code with auto-fix on errors");

        group.MapGet("/context/{contextId}", GetExecutionContext)
            .WithName("GetExecutionContext")
            .WithSummary("Get execution context details");

        group.MapGet("/results/{contextId}", GetExecutionResults)
            .WithName("GetExecutionResults")
            .WithSummary("Get all execution results for a context");
    }

    private static async Task<IResult> ExecuteCode(
        [FromBody] ExecuteCodeRequest request,
        HttpContext context,
        ICommandBus commandBus)
    {
        var userId = Guid.Parse(context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? throw new UnauthorizedAccessException());

        var command = new ExecuteAgentCodeCommand
        {
            AgentId = request.AgentId ?? Guid.NewGuid(),
            WorkspaceId = request.WorkspaceId ?? Guid.NewGuid(),
            Code = request.Code,
            Language = request.Language,
            Task = request.Task
        };

        var executionContext = await commandBus.SendAsync<ExecuteAgentCodeCommand, AgentExecutionContext>(command);

        return Results.Accepted($"/api/agent/execution/context/{executionContext.Id}", new
        {
            executionContext.Id,
            executionContext.CurrentStatus,
            executionContext.CurrentAttempt,
            codeGenerations = executionContext.CodeGenerations.Count,
            lastError = executionContext.LastErrorMessage
        });
    }

    private static async Task<IResult> GetExecutionContext(
        Guid contextId,
        IAgentExecutionRepository repository)
    {
        var context = await repository.GetByIdAsync(contextId);
        if (context == null)
            return Results.NotFound();

        return Results.Ok(new
        {
            context.Id,
            context.CurrentStatus,
            context.Task,
            context.CurrentAttempt,
            context.MaxRetryAttempts,
            context.StartedAt,
            context.CompletedAt,
            codeGenerations = context.CodeGenerations.Select(cg => new
            {
                cg.Language,
                cg.Code,
                cg.Description,
                cg.GeneratedAt
            }),
            executionResults = context.ExecutionResults.Select(r => new
            {
                r.Status,
                r.Output,
                r.ErrorMessage,
                r.ExecutionTime,
                r.AttemptNumber
            })
        });
    }

    private static async Task<IResult> GetExecutionResults(
        Guid contextId,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 10,
        IAgentExecutionRepository repository)
    {
        var context = await repository.GetByIdAsync(contextId);
        if (context == null)
            return Results.NotFound();

        var results = context.ExecutionResults
            .OrderByDescending(r => r.CreatedAt)
            .Skip(skip)
            .Take(take)
            .Select(r => new
            {
                r.Id,
                r.Status,
                r.Output,
                r.ErrorMessage,
                r.ExecutionTime,
                r.AttemptNumber,
                r.CreatedAt
            });

        return Results.Ok(results);
    }
}

public record ExecuteCodeRequest(
    string Code,
    string Language,
    string Task,
    Guid? AgentId = null,
    Guid? WorkspaceId = null);