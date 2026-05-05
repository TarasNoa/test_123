using Libr4.Trading.Application.MarketData.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Libr4.Trading.Api.Endpoints;

public static class MarketDataEndpoints
{
    public static IEndpointRouteBuilder MapMarketDataEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/market")
            .WithTags("Market Data")
            .WithOpenApi();

        // Get price for symbol
        group.MapGet("/price/{symbol}", async (
            string symbol,
            ISender sender,
            CancellationToken ct) =>
        {
            var result = await sender.Send(new GetMarketDataQuery(symbol.ToUpper()), ct);
            return result.IsSuccess ? Results.Ok(result.Value) : Results.NotFound(result.Error);
        });

        // Get top assets
        group.MapGet("/top", async (
            int limit = 20,
            ISender? sender = null,
            CancellationToken ct = default) =>
        {
            var result = await sender!.Send(new GetTopAssetsQuery(limit), ct);
            return result.IsSuccess ? Results.Ok(result.Value) : Results.BadRequest(result.Error);
        });

        return app;
    }
}
