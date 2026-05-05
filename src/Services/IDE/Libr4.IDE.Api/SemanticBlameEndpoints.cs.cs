using Libr4.IDE.Application.SemanticBlame.Commands;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Libr4.IDE.Api;

/// <summary>
/// Minimal API endpoints for Semantic Blame (AI-powered git blame analysis)
/// </summary>
public static class SemanticBlameEndpoints
{
    public static void MapSemanticBlameEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/ide/semantic-blame")
            .WithTags("Semantic Blame")
            .RequireAuthorization();

        group.MapPost("/run", async (
            [FromBody] RunBlameCommand command,
            IMediator mediator,
            CancellationToken ct) =>
        {
            var result = await mediator.Send(command, ct);
            return Results.Ok(result);
        })
        .WithName("RunSemanticBlame")
        .WithSummary("AI-powered semantic git blame with ownership analysis and contributor insights");
    }
}
