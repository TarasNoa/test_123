using Microsoft.AspNetCore.Mvc;

namespace Libr4.Gateway;

/// <summary>
/// API endpoints for managing preview routes
/// </summary>
public static class PreviewManagementEndpoints
{
    public static void MapPreviewManagementEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/gateway/previews")
            .WithTags("Preview Management")
            .RequireAuthorization()
            .RequireRateLimiting("PreviewStrict");  // Strict rate limiting for previews

        // Register new preview route
        group.MapPost("/register", async (
            [FromBody] RegisterPreviewRequest request,
            DynamicPreviewRouter router,
            ILogger<DynamicPreviewRouter> logger) =>
        {
            try
            {
                var pathPrefix = await router.RegisterPreviewRouteAsync(
                    request.OrderId,
                    request.CustomerId,
                    request.ContainerEndpoint,
                    request.ContainerPort);

                return Results.Ok(new RegisterPreviewResponse
                {
                    OrderId = request.OrderId,
                    PreviewUrl = pathPrefix,
                    ExpiresAt = DateTime.UtcNow.AddHours(2)
                });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to register preview route for order {OrderId}", request.OrderId);
                return Results.Problem($"Failed to register preview: {ex.Message}");
            }
        })
        .WithName("RegisterPreview")
        .WithSummary("Register a new preview route")
        .WithDescription("Creates a dynamic YARP route for shadow workspace preview");

        // Unregister preview route
        group.MapDelete("/{orderId}", async (
            string orderId,
            DynamicPreviewRouter router,
            ILogger<DynamicPreviewRouter> logger) =>
        {
            await router.UnregisterPreviewRouteAsync(orderId);
            return Results.NoContent();
        })
        .WithName("UnregisterPreview")
        .WithSummary("Unregister a preview route");

        // Get preview route info
        group.MapGet("/{orderId}", (
            string orderId,
            DynamicPreviewRouter router) =>
        {
            var route = router.GetPreviewRoute(orderId);
            if (route == null)
            {
                return Results.NotFound();
            }

            return Results.Ok(new PreviewRouteInfo
            {
                OrderId = route.OrderId,
                CustomerId = route.CustomerId,
                PreviewUrl = route.PathPrefix,
                ContainerEndpoint = route.ContainerEndpoint,
                CreatedAt = route.CreatedAt,
                ExpiresAt = route.ExpiresAt,
                IsExpired = route.IsExpired
            });
        })
        .WithName("GetPreviewRoute")
        .WithSummary("Get preview route information");

        // List all active previews
        group.MapGet("/", (DynamicPreviewRouter router) =>
        {
            var routes = router.GetActivePreviews();
            var response = routes.Select(r => new PreviewRouteInfo
            {
                OrderId = r.OrderId,
                CustomerId = r.CustomerId,
                PreviewUrl = r.PathPrefix,
                ContainerEndpoint = r.ContainerEndpoint,
                CreatedAt = r.CreatedAt,
                ExpiresAt = r.ExpiresAt,
                IsExpired = r.IsExpired
            });

            return Results.Ok(response);
        })
        .WithName("ListActivePreviews")
        .WithSummary("List all active preview routes");

        // Cleanup expired routes
        group.MapPost("/cleanup", async (
            DynamicPreviewRouter router,
            ILogger<DynamicPreviewRouter> logger) =>
        {
            await router.CleanupExpiredRoutesAsync();
            return Results.Ok(new { message = "Cleanup completed" });
        })
        .WithName("CleanupExpiredPreviews")
        .WithSummary("Clean up expired preview routes");
    }
}

// Request/Response DTOs
public class RegisterPreviewRequest
{
    public string OrderId { get; set; } = string.Empty;
    public string CustomerId { get; set; } = string.Empty;
    public string ContainerEndpoint { get; set; } = string.Empty;
    public int ContainerPort { get; set; } = 3000;
}

public class RegisterPreviewResponse
{
    public string OrderId { get; set; } = string.Empty;
    public string PreviewUrl { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
}

public class PreviewRouteInfo
{
    public string OrderId { get; set; } = string.Empty;
    public string CustomerId { get; set; } = string.Empty;
    public string PreviewUrl { get; set; } = string.Empty;
    public string ContainerEndpoint { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public bool IsExpired { get; set; }
}
