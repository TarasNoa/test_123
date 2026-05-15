using Libr4.IDE.Application.AgentEvents;
using Libr4.IDE.Application.AI.Commands;
using Microsoft.AspNetCore.Mvc;
using MediatR;

namespace Libr4.IDE.Api;

public static class UnifiedChatEndpoints
{
    public static IEndpointRouteBuilder MapUnifiedChatEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/v1/ai/chat/message", async (
            [FromBody] ChatMessageRequest request,
            IAgentSpawnerService spawner,
            IMediator mediator,
            ILogger<ChatMessageRequest> logger) =>
        {
            logger.LogInformation("Chat message from session {SessionId}", request.SessionId);

            var conversationId = Guid.TryParse(request.SessionId, out var sid)
                ? sid
                : Guid.NewGuid();

            var command = new ChatCommand(conversationId, request.Message);
            var result = await mediator.Send(command);

            if (result.IsSuccess)
            {
                return Results.Ok(new
                {
                    messageId = result.Value.Id,
                    status = "completed",
                    response = result.Value.Content,
                    model = result.Value.Model,
                    tokensUsed = result.Value.TokensUsed,
                    responseTimeMs = result.Value.ResponseTimeMs,
                });
            }

            return Results.BadRequest(new { error = result.Error?.Message ?? "AI service error" });
        });

        return app;
    }
}

public class ChatMessageRequest
{
    public string SessionId { get; set; } = "";
    public string Message { get; set; } = "";
    public string AutonomyLevel { get; set; } = "semi-auto";
    public ChatContext? Context { get; set; }
}

public class ChatContext
{
    public string? CurrentFile { get; set; }
    public string? SelectedCode { get; set; }
    public string[]? AttachedFiles { get; set; }
    public string[]? OpenTabs { get; set; }
}
