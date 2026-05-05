using Libr4.Chat.Application.Messages.Commands;
using Libr4.Chat.Application.Messages.Queries;
using Libr4.Chat.Domain.Messages;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Libr4.Chat.Api.Endpoints;

public static class MessageEndpoints
{
    public static IEndpointRouteBuilder MapMessageEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/messages")
            .WithTags("Messages")
            .WithOpenApi()
            .RequireAuthorization();

        // Get messages for chat
        group.MapGet("/chat/{chatId:guid}", async (
            Guid chatId,
            int page = 1,
            int pageSize = 50,
            ISender? sender = null,
            CancellationToken ct = default) =>
        {
            var result = await sender!.Send(new GetChatMessagesQuery(chatId, page, pageSize), ct);
            return result.IsSuccess ? Results.Ok(result.Value) : Results.BadRequest(result.Error);
        });

        // Send message
        group.MapPost("/send", async (
            [FromBody] SendMessageRequest request,
            ISender sender,
            CancellationToken ct) =>
        {
            var result = await sender.Send(
                new SendMessageCommand(
                    request.ChatId,
                    request.Content,
                    request.Type,
                    request.ReplyToMessageId,
                    request.FileUrl,
                    request.FileName,
                    request.FileSize), ct);
            return result.IsSuccess 
                ? Results.Created($"/api/v1/messages/{result.Value}", result.Value) 
                : Results.BadRequest(result.Error);
        });

        // Edit message
        group.MapPut("/{messageId:guid}", async (
            Guid messageId,
            [FromBody] EditMessageRequest request,
            ISender sender,
            CancellationToken ct) =>
        {
            var result = await sender.Send(new EditMessageCommand(messageId, request.NewContent), ct);
            return result.IsSuccess ? Results.Ok() : Results.BadRequest(result.Error);
        });

        // Delete message
        group.MapDelete("/{messageId:guid}", async (
            Guid messageId,
            ISender sender,
            CancellationToken ct) =>
        {
            var result = await sender.Send(new DeleteMessageCommand(messageId), ct);
            return result.IsSuccess ? Results.Ok() : Results.BadRequest(result.Error);
        });

        return app;
    }
}

public record SendMessageRequest(
    Guid ChatId,
    string Content,
    MessageType Type = MessageType.Text,
    Guid? ReplyToMessageId = null,
    string? FileUrl = null,
    string? FileName = null,
    long? FileSize = null);

public record EditMessageRequest(string NewContent);
