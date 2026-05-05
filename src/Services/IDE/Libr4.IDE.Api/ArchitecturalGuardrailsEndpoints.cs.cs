using Libr4.IDE.Application.ArchitecturalGuardrails.Commands;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Libr4.IDE.Api;

/// <summary>
/// Minimal API endpoints for Architectural Guardrails
/// </summary>
public static class ArchitecturalGuardrailsEndpoints
{
    public static void MapArchitecturalGuardrailsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/ide/architectural-guardrails")
            .WithTags("Architectural Guardrails")
            .RequireAuthorization();

        group.MapPost("/validate", async (
            [FromBody] RunValidationCommand command,
            IMediator mediator,
            CancellationToken ct) =>
        {
            var result = await mediator.Send(command, ct);
            return Results.Ok(result);
        })
        .WithName("RunArchitecturalValidation")
        .WithSummary("Validate code against architectural rules using AST-based rule checking");
    }
}
