using Libr4.Chat.Application.Chats.Commands;
using Libr4.Chat.Application.Chats.Queries;
using Libr4.Chat.Domain.Chats;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Libr4.Chat.Application.Abstractions;

namespace Libr4.Chat.Api.Endpoints;

public static class ChatEndpoints
{
    public static IEndpointRouteBuilder MapChatEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/chats")
            .WithTags("Chats")
            .WithOpenApi()
            .RequireAuthorization();

        // Get my chats
        group.MapGet("/my", async (
            [AsParameters] GetMyChatsQuery query,
            ISender sender,
            CancellationToken ct) =>
        {
            var result = await sender.Send(query, ct);
            return result.IsSuccess ? Results.Ok(result.Value) : Results.BadRequest(result.Error);
        });

        // Get chat by id
        group.MapGet("/{chatId:guid}", async (
            Guid chatId,
            ISender sender,
            CancellationToken ct) =>
        {
            var result = await sender.Send(new GetChatByIdQuery(chatId), ct);
            return result.IsSuccess ? Results.Ok(result.Value) : Results.NotFound(result.Error);
        });

        // Create direct chat
        group.MapPost("/direct", async (
            [FromBody] CreateDirectChatRequest request,
            ISender sender,
            CancellationToken ct) =>
        {
            var result = await sender.Send(new CreateDirectChatCommand(request.OtherUserId), ct);
            return result.IsSuccess 
                ? Results.Created($"/api/v1/chats/{result.Value}", result.Value) 
                : Results.BadRequest(result.Error);
        });

        // Create group chat
        group.MapPost("/group", async (
            [FromBody] CreateGroupChatRequest request,
            ISender sender,
            CancellationToken ct) =>
        {
            var result = await sender.Send(
                new CreateGroupChatCommand(request.Title, request.MemberIds, request.RelatedTaskId), ct);
            return result.IsSuccess 
                ? Results.Created($"/api/v1/chats/{result.Value}", result.Value) 
                : Results.BadRequest(result.Error);
        });

        // Join chat
        group.MapPost("/{chatId:guid}/join", async (
            Guid chatId,
            ISender sender,
            CancellationToken ct) =>
        {
            var result = await sender.Send(new JoinChatCommand(chatId), ct);
            return result.IsSuccess ? Results.Ok() : Results.BadRequest(result.Error);
        });

        // Leave chat
        group.MapPost("/{chatId:guid}/leave", async (
            Guid chatId,
            ISender sender,
            CancellationToken ct) =>
        {
            var result = await sender.Send(new LeaveChatCommand(chatId), ct);
            return result.IsSuccess ? Results.Ok() : Results.BadRequest(result.Error);
        });

        return app;
    }

    public static void MapChatEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/chat")
            .WithTags("Chat")
            .RequireAuthorization();

        group.MapGet("/chats", async (
            HttpContext context,
            IChatService service) =>
        {
            var userId = Guid.Parse(context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? throw new UnauthorizedAccessException());
            var chats = await service.GetUserChatsAsync(userId);
            return Results.Ok(new { chats });
        })
        .WithName("GetUserChats")
        .WithSummary("Get chats for the current user");

        group.MapPost("/chats", async (
            [FromBody] CreateChatRequest request,
            HttpContext context,
            IChatService service) =>
        {
            var creatorId = Guid.Parse(context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? throw new UnauthorizedAccessException());

            if (request == null || string.IsNullOrWhiteSpace(request.Name))
            {
                return Results.BadRequest(new { error = "Name is required" });
            }

            try
            {
                var chat = await service.CreateChatAsync(request, creatorId);
                return Results.Created($"/api/chat/chats/{chat.Id}", new { chat });
            }
            catch (Exception ex)
            {
                return Results.Problem(
                    detail: $"Failed to create chat: {ex.Message}",
                    statusCode: 500,
                    title: "Chat Creation Error");
            }
        })
        .WithName("CreateChat")
        .WithSummary("Create a new chat");

        group.MapGet("/chats/{chatId}/messages", async (
            Guid chatId,
            int page,
            int pageSize,
            IChatService service) =>
        {
            try
            {
                var messages = await service.GetChatMessagesAsync(chatId, page, pageSize);
                return Results.Ok(new { messages });
            }
            catch (Exception ex)
            {
                return Results.Problem(
                    detail: $"Failed to get messages: {ex.Message}",
                    statusCode: 500,
                    title: "Message Retrieval Error");
            }
        })
        .WithName("GetChatMessages")
        .WithSummary("Get messages for a chat");
    }
}

public record CreateDirectChatRequest(Guid OtherUserId);
public record CreateGroupChatRequest(string Title, List<Guid> MemberIds, Guid? RelatedTaskId = null);
