using Libr4.IDE.Application.ShadowWorkspace.Commands;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Libr4.IDE.Api;

/// <summary>
/// Minimal API endpoints for Shadow Workspace Service
/// </summary>
public static class ShadowWorkspaceEndpoints
{
    public static void MapShadowWorkspaceEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/ide/shadow-workspace")
            .WithTags("Shadow Workspace")
            .RequireAuthorization();

        group.MapPost("/create", async (
            [FromBody] CreateShadowWorkspaceCommand command,
            IMediator mediator,
            CancellationToken ct) =>
        {
            var result = await mediator.Send(command, ct);
            return Results.Ok(result);
        })
        .WithName("CreateShadowWorkspace")
        .WithSummary("Create a shadow workspace for safe agent operations");
    }
}
