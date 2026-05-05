using Libr4.AI.Infrastructure.SessionTracking;
using Libr4.AI.Domain.SessionTracking.FSharp;
using Microsoft.AspNetCore.Mvc;

namespace Libr4.AI.Api.Endpoints;

public static class SessionTrackingEndpoints
{
    public static void MapSessionTrackingEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/sessions")
            .WithTags("Sessions");

        group.MapGet("/{sessionId}", (string sessionId, SessionTrackingService service) =>
        {
            var session = service.GetSession(sessionId);
            return session is not null ? Results.Ok(session) : Results.NotFound();
        });

        group.MapPost("/get-or-create", (
            [FromBody] GetOrCreateSessionRequest request,
            SessionTrackingService service) =>
        {
            var sessionId = service.GetOrCreateSession(
                request.ProjectPath,
                request.UserId,
                request.AgentId);

            return Results.Ok(new { sessionId });
        });

        group.MapPost("/{sessionId}/events", (
            string sessionId,
            [FromBody] AddEventRequest request,
            SessionTrackingService service) =>
        {
            service.AddEvent(
                sessionId,
                request.EventType,
                request.Data,
                request.Metadata);

            return Results.Ok();
        });

        group.MapPost("/search", (
            [FromBody] SearchSessionsRequest request,
            SessionTrackingService service) =>
        {
            var results = service.SearchSessions(request.Query, request.TopK);
            return Results.Ok(results);
        });

        group.MapGet("/{sessionId}/messages", (
            string sessionId,
            [FromQuery] int limit = 10,
            SessionTrackingService service) =>
        {
            var messages = service.GetLastMessages(sessionId, limit);
            return Results.Ok(messages);
        });
    }

    public record GetOrCreateSessionRequest(
        string ProjectPath,
        string? UserId,
        Guid? AgentId);

    public record AddEventRequest(
        SessionEventType EventType,
        string Data,
        Dictionary<string, string>? Metadata);

    public record SearchSessionsRequest(
        string Query,
        int TopK = 10);
}
