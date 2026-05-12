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

        group.MapPost("/", async (
            [FromBody] CreateServerRequest request,
            HttpContext context,
            IServerService service) =>
        {
            var ownerId = Guid.Parse(context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? throw new UnauthorizedAccessException());

            if (string.IsNullOrWhiteSpace(request.Name))
            {
                return Results.BadRequest(new { error = "Name is required" });
            }

            try
            {
                var server = await service.CreateServerAsync(request, ownerId);
                return Results.Created($"/api/chat/servers/{server.Id}", new { server });
            }
            catch (Exception ex)
            {
                return Results.Problem(
                    detail: $"Failed to create server: {ex.Message}",
                    statusCode: 500,
                    title: "Server Creation Error");
            }
        })
        .WithName("CreateServer")
        .WithSummary("Create a new server");

        group.MapPost("/{serverId}/channels", async (
            Guid serverId,
            [FromBody] CreateChannelRequest request,
            IServerService service) =>
        {
            request = request with { ServerId = serverId };

            try
            {
                await service.AddChannelAsync(request);
                return Results.Ok(new { message = "Channel added" });
            }
            catch (Exception ex)
            {
                return Results.Problem(
                    detail: $"Failed to add channel: {ex.Message}",
                    statusCode: 500,
                    title: "Channel Addition Error");
            }
        })
        .WithName("AddChannel")
        .WithSummary("Add a channel to a server");

        group.MapPost("/{serverId}/schedule-call", async (
            Guid serverId,
            [FromBody] ScheduleCallRequest request,
            HttpContext context,
            IServerService service) =>
        {
            var organizerId = Guid.Parse(context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? throw new UnauthorizedAccessException());
            request = request with { ServerId = serverId };

            try
            {
                await service.ScheduleCallAsync(request, organizerId);
                return Results.Ok(new { message = "Call scheduled" });
            }
            catch (Exception ex)
            {
                return Results.Problem(
                    detail: $"Failed to schedule call: {ex.Message}",
                    statusCode: 500,
                    title: "Call Scheduling Error");
            }
        })
        .WithName("ScheduleCall")
        .WithSummary("Schedule a call for a server");
    }
}