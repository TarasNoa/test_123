using Libr4.Chat.Application.Abstractions;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Libr4.Chat.Api.Endpoints;

public static class ServerEndpoints
{
    public static void MapServerEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/chat/servers")
            .WithTags("Servers")
            .RequireAuthorization();

        group.MapGet("/", async (
            HttpContext context,
            IServerService service) =>
        {
            var userId = Guid.Parse(context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? throw new UnauthorizedAccessException());
            var servers = await service.GetUserServersAsync(userId);
            return Results.Ok(new { servers });
        })
        .WithName("GetUserServers")
        .WithSummary("Get servers for the current user");
    }
}