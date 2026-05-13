using Libr4.Chat.Application.Abstractions;
using Libr4.Chat.Domain.Calls;
using Microsoft.AspNetCore.Mvc;

namespace Libr4.Chat.Api.Endpoints;

public static class CallEndpoints
{
    public static void MapCallEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/calls")
            .WithTags("Calls")
            .WithOpenApi()
            .RequireAuthorization();

        // Initiate call
        group.MapPost("/initiate", async (
            [FromBody] InitiateCallHttpRequest request,
            ICallService callService,
            HttpContext context) =>
        {
            var userId = Guid.Parse(context.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? throw new UnauthorizedAccessException());
            var call = await callService.InitiateCallAsync(new InitiateCallRequest(request.ChatId, request.Type), userId);
            return Results.Ok(call);
        });

        // Join call
        group.MapPost("/{callId:guid}/join", async (
            Guid callId,
            ICallService callService,
            HttpContext context) =>
        {
            var userId = Guid.Parse(context.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? throw new UnauthorizedAccessException());
            await callService.JoinCallAsync(callId, userId);
            return Results.Ok();
        });

        // End call
        group.MapPost("/{callId:guid}/end", async (
            Guid callId,
            ICallService callService) =>
        {
            await callService.EndCallAsync(callId);
            return Results.Ok();
        });

        // Get active call for chat
        group.MapGet("/chat/{chatId:guid}/active", async (
            Guid chatId,
            ICallService callService) =>
        {
            var call = await callService.GetActiveCallAsync(chatId);
            return call == null ? Results.NotFound() : Results.Ok(call);
        });
    }
}

public record InitiateCallHttpRequest(Guid ChatId, CallType Type);
