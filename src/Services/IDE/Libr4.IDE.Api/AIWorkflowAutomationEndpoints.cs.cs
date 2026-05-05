using Libr4.IDE.Application.AIWorkflowAutomation.Commands;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Libr4.IDE.Api;

/// <summary>
/// Minimal API endpoints for AI Workflow Automation
/// </summary>
public static class AIWorkflowAutomationEndpoints
{
    public static void MapAIWorkflowAutomationEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/ide/workflow-automation")
            .WithTags("AI Workflow Automation")
            .RequireAuthorization();

        group.MapPost("/distill", async (
            [FromBody] DistillWorkflowCommand command,
            IMediator mediator,
            CancellationToken ct) =>
        {
            var result = await mediator.Send(command, ct);
            return Results.Ok(result);
        })
        .WithName("DistillWorkflow")
        .WithSummary("Distill workflow into reusable skill");
    }
}
