using Libr4.IDE.Application.SeniorRolePrompts.Commands;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Libr4.IDE.Api;

/// <summary>
/// Minimal API endpoints for Senior Role Prompts Service
/// </summary>
public static class SeniorRolePromptsEndpoints
{
    public static void MapSeniorRolePromptsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/ide/senior-prompts")
            .WithTags("Senior Role Prompts")
            .RequireAuthorization();

        group.MapPost("/generate", async (
            [FromBody] GenerateRolePromptCommand command,
            IMediator mediator,
            CancellationToken ct) =>
        {
            var result = await mediator.Send(command, ct);
            return Results.Ok(result);
        })
        .WithName("GenerateRolePrompt")
        .WithSummary("Generate phase-specific senior role prompt for IDE agents");

        group.MapGet("/phases/{phaseType}", async (
            string phaseType,
            string domainClass,
            bool richMode,
            IMediator mediator,
            CancellationToken ct) =>
        {
            if (!Enum.TryParse<Libr4.IDE.Domain.SeniorRolePrompts.PhaseType>(phaseType, true, out var phase))
                return Results.BadRequest($"Unknown phase type: {phaseType}");

            var command = new GenerateRolePromptCommand
            {
                PhaseType = phase,
                PhaseName = phaseType,
                DomainClass = domainClass ?? "Standard",
                RichMode = richMode
            };
            var result = await mediator.Send(command, ct);
            return Results.Ok(result);
        })
        .WithName("GetRolePromptByPhase")
        .WithSummary("Get senior role prompt for a specific execution phase");
    }
}
