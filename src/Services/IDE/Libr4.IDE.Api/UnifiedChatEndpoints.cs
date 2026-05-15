using Libr4.IDE.Application.AgentEvents;
using Microsoft.AspNetCore.Mvc;

namespace Libr4.IDE.Api;

public static class UnifiedChatEndpoints
{
    public static IEndpointRouteBuilder MapUnifiedChatEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/v1/ai/chat/message", async (
            [FromBody] ChatMessageRequest request,
            IAgentSpawnerService spawner,
            ILogger<ChatMessageRequest> logger) =>
        {
            logger.LogInformation("Chat message from session {SessionId}", request.SessionId);
            // TODO: Route to AI service or spawn agent based on message content
            return Results.Ok(new { messageId = Guid.NewGuid(), status = "accepted" });
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
