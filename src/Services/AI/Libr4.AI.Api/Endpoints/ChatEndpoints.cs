using Libr4.AI.Application.Chats.Commands;
using Libr4.AI.Application.Chats.Queries;
using Libr4.AI.Domain.Chats;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Libr4.AI.Api.Endpoints;

public static class ChatEndpoints
{
    public static IEndpointRouteBuilder MapChatEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/ai/chats")
            .WithTags("AI Chats")
            .WithOpenApi()
            .RequireAuthorization();

        // Get my chats
        group.MapGet("/my", async (
            ISender sender,
            CancellationToken ct,
            int page = 1,
            int pageSize = 20) =>
        {
            var result = await sender.Send(new GetMyChatsQuery(page, pageSize), ct);
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

        // Send message (create or continue chat)
        group.MapPost("/message", async (
            [FromBody] SendMessageRequest request,
            ISender sender,
            CancellationToken ct) =>
        {
            var result = await sender.Send(new SendMessageCommand(
                request.ChatId,
                request.Content,
                request.Model,
                request.Provider), ct);

            return result.IsSuccess
                ? Results.Created($"/api/v1/ai/chats/{result.Value}", new { chatId = result.Value })
                : Results.BadRequest(result.Error);
        });

        return app;
    }
}

public record SendMessageRequest(
    Guid? ChatId,
    string Content,
    string Model = "llama2",
    AIProviderType Provider = AIProviderType.Ollama);
