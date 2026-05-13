using Libr4.IDE.Application.ShadowWorkspace.Commands;
using Libr4.IDE.Application.ShadowWorkspace.Validators;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Libr4.IDE.Api;

public static class ShadowWorkspaceEndpoints
{
    public static void MapShadowWorkspaceEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/ide/shadow-workspace")
            .RequireAuthorization();

        group.MapPost("/create", async (
            CreateShadowWorkspaceCommand command,
            IMediator mediator,
            CancellationToken ct) =>
        {
            var result = await mediator.Send(command, ct);
            return Results.Ok(result);
        });
    }
}
