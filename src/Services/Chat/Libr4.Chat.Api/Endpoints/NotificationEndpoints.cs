using Libr4.Chat.Application.Notifications.Commands;
using Libr4.Chat.Application.Notifications.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Libr4.Chat.Api.Endpoints;

public static class NotificationEndpoints
{
    public static IEndpointRouteBuilder MapNotificationEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/notifications")
            .WithTags("Notifications")
            .WithOpenApi()
            .RequireAuthorization();

        // Get my notifications
        group.MapGet("/my", async (
            bool unreadOnly = false,
            int page = 1,
            int pageSize = 20,
            ISender? sender = null,
            CancellationToken ct = default) =>
        {
            var result = await sender!.Send(new GetMyNotificationsQuery(unreadOnly, page, pageSize), ct);
            return result.IsSuccess ? Results.Ok(result.Value) : Results.BadRequest(result.Error);
        });

        // Mark as read
        group.MapPost("/{notificationId:guid}/read", async (
            Guid notificationId,
            ISender sender,
            CancellationToken ct) =>
        {
            var result = await sender.Send(new MarkNotificationReadCommand(notificationId), ct);
            return result.IsSuccess ? Results.Ok() : Results.NotFound(result.Error);
        });

        // Mark all as read
        group.MapPost("/read-all", async (
            ISender sender,
            CancellationToken ct) =>
        {
            var result = await sender.Send(new MarkAllNotificationsReadCommand(), ct);
            return result.IsSuccess ? Results.Ok(new { markedAsRead = result.Value }) : Results.BadRequest(result.Error);
        });

        return app;
    }
}
