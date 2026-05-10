using Libr4.Analytics.Application.Abstractions;
using Microsoft.AspNetCore.Mvc;

namespace Libr4.Analytics.Api.Endpoints;

public static class DashboardEndpoints
{
    public static void MapDashboardEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/analytics/dashboards")
            .WithTags("Dashboards")
            .RequireAuthorization();

        group.MapGet("/", async (
            Guid ownerId,
            IDashboardService service) =>
        {
            var dashboards = await service.GetDashboardsAsync(ownerId);
            return Results.Ok(new { dashboards });
        })
        .WithName("GetDashboards")
        .WithSummary("Get dashboards for a user");

        group.MapPost("/", async (
            [FromBody] CreateDashboardRequest request,
            IDashboardService service) =>
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Title))
            {
                return Results.BadRequest(new { error = "Title is required" });
            }

            try
            {
                var dashboard = await service.CreateDashboardAsync(request);
                return Results.Created($"/api/analytics/dashboards/{dashboard.Id}", new { dashboard });
            }
            catch (Exception ex)
            {
                return Results.Problem(
                    detail: $"Failed to create dashboard: {ex.Message}",
                    statusCode: 500,
                    title: "Dashboard Creation Error");
            }
        })
        .WithName("CreateDashboard")
        .WithSummary("Create a new dashboard");

        group.MapPost("/{id}/widgets", async (
            Guid id,
            [FromBody] AddWidgetRequest request,
            IDashboardService service) =>
        {
            try
            {
                await service.AddWidgetAsync(id, request.WidgetType, request.Config);
                return Results.Ok(new { message = "Widget added" });
            }
            catch (Exception ex)
            {
                return Results.Problem(
                    detail: $"Failed to add widget: {ex.Message}",
                    statusCode: 500,
                    title: "Widget Addition Error");
            }
        })
        .WithName("AddWidget")
        .WithSummary("Add a widget to a dashboard");
    }
}

public record AddWidgetRequest(string WidgetType, string Config);