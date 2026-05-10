using Libr4.Analytics.Application.Abstractions;
using Microsoft.AspNetCore.Mvc;

namespace Libr4.Analytics.Api.Endpoints;

public static class MetricsEndpoints
{
    public static void MapMetricsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/analytics/metrics")
            .WithTags("Metrics")
            .RequireAuthorization();

        group.MapGet("/", async (
            string? name,
            DateTimeOffset? from,
            DateTimeOffset? to,
            IMetricsService service) =>
        {
            var metrics = await service.GetMetricsAsync(name, from, to);
            return Results.Ok(new { metrics });
        })
        .WithName("GetMetrics")
        .WithSummary("Get metrics with optional filters");

        group.MapPost("/", async (
            [FromBody] CreateMetricRequest request,
            IMetricsService service) =>
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Name))
            {
                return Results.BadRequest(new { error = "Name is required" });
            }

            try
            {
                var metric = await service.CreateMetricAsync(request);
                return Results.Created($"/api/analytics/metrics/{metric.Id}", new { metric });
            }
            catch (Exception ex)
            {
                return Results.Problem(
                    detail: $"Failed to create metric: {ex.Message}",
                    statusCode: 500,
                    title: "Metric Creation Error");
            }
        })
        .WithName("CreateMetric")
        .WithSummary("Create a new metric");

        group.MapPut("/{id}/value", async (
            Guid id,
            [FromBody] UpdateMetricValueRequest request,
            IMetricsService service) =>
        {
            try
            {
                await service.UpdateMetricAsync(id, request.Value);
                return Results.Ok(new { message = "Metric updated" });
            }
            catch (Exception ex)
            {
                return Results.Problem(
                    detail: $"Failed to update metric: {ex.Message}",
                    statusCode: 500,
                    title: "Metric Update Error");
            }
        })
        .WithName("UpdateMetricValue")
        .WithSummary("Update metric value");
    }
}

public record UpdateMetricValueRequest(double Value);