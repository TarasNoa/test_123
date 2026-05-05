using Libr4.IDE.Application.SemanticCodeGraph.Commands;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Libr4.IDE.Api;

/// <summary>
/// Minimal API endpoints for Semantic Code Graph Service
/// </summary>
public static class SemanticCodeGraphEndpoints
{
    public static void MapSemanticCodeGraphEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/ide/semantic-graph")
            .WithTags("Semantic Code Graph")
            .RequireAuthorization();

        group.MapPost("/build", async (
            [FromBody] BuildGraphCommand command,
            IMediator mediator,
            CancellationToken ct) =>
        {
            var result = await mediator.Send(command, ct);
            return Results.Ok(result);
        })
        .WithName("BuildSemanticGraph")
        .WithSummary("Build semantic code graph with embeddings for entities and relationships");
    }
}
