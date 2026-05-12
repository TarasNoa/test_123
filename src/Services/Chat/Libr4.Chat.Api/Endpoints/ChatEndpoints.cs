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
}

public record CreateDirectChatRequest(Guid OtherUserId);
public record CreateGroupChatRequest(string Title, List<Guid> MemberIds, Guid? RelatedTaskId = null);
