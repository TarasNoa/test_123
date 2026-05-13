using Libr4.IDE.Application.AI.Commands;
using Libr4.IDE.Application.AI.Queries;
using Libr4.IDE.Application.AI.DTOs;
using Microsoft.AspNetCore.Mvc;
using MediatR;

namespace Libr4.IDE.Api;

public static class AIEndpoints
{
    public static void MapAIEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/ai")
            .RequireAuthorization();

        // Conversations
        group.MapPost("/conversations", async (
            [FromBody] CreateAIConversationCommand command,
            IMediator mediator,
            CancellationToken ct) =>
        {
            var result = await mediator.Send(command, ct);
            return result.IsSuccess 
                ? Results.Ok(result.Value) 
                : Results.BadRequest(result.Error);
        });

        group.MapGet("/conversations", async (
            IMediator mediator,
            CancellationToken ct,
            Guid userId,
            [FromQuery] int skip = 0,
            [FromQuery] int limit = 20,
            [FromQuery] bool archivedOnly = false) =>
        {
            var query = new GetAIConversationsQuery(userId, skip, limit, archivedOnly);
            var result = await mediator.Send(query, ct);
            return result.IsSuccess 
                ? Results.Ok(result.Value) 
                : Results.BadRequest(result.Error);
        });

        group.MapGet("/conversations/{conversationId}/messages", async (
            Guid conversationId,
            Guid userId,
            IMediator mediator,
            CancellationToken ct) =>
        {
            var query = new GetConversationMessagesQuery(conversationId, userId);
            var result = await mediator.Send(query, ct);
            return result.IsSuccess 
                ? Results.Ok(result.Value) 
                : Results.NotFound(result.Error.Message);
        });

        // Chat
        group.MapPost("/chat", async (
            [FromBody] ChatCommand command,
            IMediator mediator,
            CancellationToken ct) =>
        {
            var result = await mediator.Send(command, ct);
            return result.IsSuccess 
                ? Results.Ok(result.Value) 
                : Results.BadRequest(result.Error);
        });

        // Quality Scoring
        group.MapPost("/chat/{messageId}/score", async (
            Guid messageId,
            [FromQuery] int score,
            [FromQuery] string? feedback,
            Guid userId,
            IMediator mediator,
            CancellationToken ct) =>
        {
            var command = new ScoreAIMessageCommand(messageId, userId, score, feedback);
            var result = await mediator.Send(command, ct);
            return result.IsSuccess 
                ? Results.Ok(result.Value) 
                : Results.BadRequest(result.Error);
        });

        // Tambo AI Integration - UI Generation
        group.MapPost("/generate-ui", async (
            [FromBody] GenerateUICommand command,
            IMediator mediator,
            CancellationToken ct) =>
        {
            var result = await mediator.Send(command, ct);
            return result.Success 
                ? Results.Ok(result) 
                : Results.BadRequest(result.Error);
        });
    }
}
