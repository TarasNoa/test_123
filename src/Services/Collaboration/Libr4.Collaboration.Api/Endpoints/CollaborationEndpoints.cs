using Libr4.Collaboration.Application.Abstractions;
using Microsoft.AspNetCore.Mvc;

namespace Libr4.Collaboration.Api.Endpoints;

public static class CollaborationEndpoints
{
    public static void MapCollaborationEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/collaboration")
            .WithTags("Collaboration")
            .RequireAuthorization();

        group.MapPost("/rooms", async (
            [FromBody] CreateRoomRequest request,
            HttpContext context,
            ICollaborationService service) =>
        {
            var creatorId = Guid.Parse(context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? throw new UnauthorizedAccessException());

            try
            {
                var room = await service.CreateRoomAsync(request, creatorId);
                return Results.Created($"/api/collaboration/rooms/{room.Id}", new { room });
            }
            catch (Exception ex)
            {
                return Results.Problem(detail: ex.Message, statusCode: 500, title: "Room Creation Error");
            }
        })
        .WithName("CreateCollaborationRoom")
        .WithSummary("Create a new collaboration room");

        group.MapGet("/rooms", async (
            HttpContext context,
            ICollaborationService service) =>
        {
            var userId = Guid.Parse(context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? throw new UnauthorizedAccessException());
            var rooms = await service.GetUserRoomsAsync(userId);
            return Results.Ok(new { rooms });
        })
        .WithName("GetUserRooms")
        .WithSummary("Get user's collaboration rooms");

        group.MapPost("/rooms/{roomId}/join", async (
            Guid roomId,
            HttpContext context,
            ICollaborationService service) =>
        {
            var userId = Guid.Parse(context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? throw new UnauthorizedAccessException());

            try
            {
                await service.JoinRoomAsync(roomId, userId);
                return Results.Ok(new { message = "Joined room" });
            }
            catch (Exception ex)
            {
                return Results.Problem(detail: ex.Message, statusCode: 500, title: "Join Error");
            }
        })
        .WithName("JoinRoom")
        .WithSummary("Join a collaboration room");

        group.MapPost("/documents", async (
            [FromBody] CreateDocumentRequest request,
            HttpContext context,
            ICollaborationService service) =>
        {
            var ownerId = Guid.Parse(context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? throw new UnauthorizedAccessException());

            try
            {
                var document = await service.CreateDocumentAsync(request, ownerId);
                return Results.Created($"/api/collaboration/documents/{document.Id}", new { document });
            }
            catch (Exception ex)
            {
                return Results.Problem(detail: ex.Message, statusCode: 500, title: "Document Creation Error");
            }
        })
        .WithName("CreateDocument")
        .WithSummary("Create a collaborative document");

        group.MapPut("/documents/{documentId}", async (
            Guid documentId,
            [FromBody] UpdateDocumentRequest request,
            HttpContext context,
            ICollaborationService service) =>
        {
            var userId = Guid.Parse(context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? throw new UnauthorizedAccessException());
            var updateRequest = request with { DocumentId = documentId };

            try
            {
                await service.UpdateDocumentAsync(updateRequest, userId);
                return Results.Ok(new { message = "Document updated" });
            }
            catch (Exception ex)
            {
                return Results.Problem(detail: ex.Message, statusCode: 500, title: "Update Error");
            }
        })
        .WithName("UpdateDocument")
        .WithSummary("Update a collaborative document");

        group.MapPost("/whiteboards", async (
            [FromBody] CreateWhiteboardRequest request,
            ICollaborationService service) =>
        {
            try
            {
                var whiteboard = await service.CreateWhiteboardAsync(request);
                return Results.Created($"/api/collaboration/whiteboards/{whiteboard.Id}", new { whiteboard });
            }
            catch (Exception ex)
            {
                return Results.Problem(detail: ex.Message, statusCode: 500, title: "Whiteboard Creation Error");
            }
        })
        .WithName("CreateWhiteboard")
        .WithSummary("Create a collaborative whiteboard");

        group.MapPost("/calls", async (
            [FromBody] InitiateVideoCallRequest request,
            HttpContext context,
            ICollaborationService service) =>
        {
            var initiatorId = Guid.Parse(context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? throw new UnauthorizedAccessException());

            try
            {
                var call = await service.InitiateVideoCallAsync(request, initiatorId);
                return Results.Created($"/api/collaboration/calls/{call.Id}", new { call });
            }
            catch (Exception ex)
            {
                return Results.Problem(detail: ex.Message, statusCode: 500, title: "Call Error");
            }
        })
        .WithName("InitiateVideoCall")
        .WithSummary("Initiate a video call in a collaboration room");
    }
}