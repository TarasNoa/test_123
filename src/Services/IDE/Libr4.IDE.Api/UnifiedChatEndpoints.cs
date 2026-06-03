using Libr4.IDE.Application.AI.Commands;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Libr4.IDE.Api;

public static class UnifiedChatEndpoints
{
    public static IEndpointRouteBuilder MapUnifiedChatEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/v1/ai/chat/message", async (
            [FromBody] ChatMessageRequest request,
            IAppGenerationChatBridge appGenerationChat,
            IMediator mediator,
            ILogger<ChatMessageRequest> logger,
            CancellationToken ct) =>
        {
            logger.LogInformation("Chat message from session {SessionId}", request.SessionId);

            if (appGenerationChat.ShouldStartGeneration(request.Message))
            {
                try
                {
                    var generation = await appGenerationChat.TryStartFromChatAsync(
                        request.Message,
                        request.SessionId,
                        tenantId: null,
                        ct).ConfigureAwait(false);

                    return Results.Ok(new
                    {
                        messageId = Guid.NewGuid(),
                        status = "generation_started",
                        response = generation.AssistantMessage,
                        generationRunId = generation.RunId,
                        generationStatus = generation.Status,
                        generationReportUrl = generation.ReportUrl,
                        mode = "autonomous_app_generation",
                    });
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed to start app generation from IDE chat");
                    return Results.Problem(
                        detail: ex.Message,
                        title: "App generation failed to start",
                        statusCode: StatusCodes.Status500InternalServerError);
                }
            }

            var conversationId = Guid.TryParse(request.SessionId, out var sid)
                ? sid
                : Guid.NewGuid();

            var command = new ChatCommand(conversationId, request.Message, request.Provider);
            var result = await mediator.Send(command, ct).ConfigureAwait(false);

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
                    mode = "ai_chat",
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
    public string? Provider { get; set; }
    public ChatContext? Context { get; set; }
}

public class ChatContext
{
    public string? CurrentFile { get; set; }
    public string? SelectedCode { get; set; }
    public string[]? AttachedFiles { get; set; }
    public string[]? OpenTabs { get; set; }
}
