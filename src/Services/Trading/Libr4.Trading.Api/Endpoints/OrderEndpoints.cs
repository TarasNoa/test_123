using Libr4.Trading.Application.Dtos;
using Libr4.Trading.Application.Orders.Commands;
using Libr4.Trading.Application.Orders.Queries;
using Libr4.Trading.Domain.Orders;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Libr4.Trading.Api.Endpoints;

public static class OrderEndpoints
{
    public static IEndpointRouteBuilder MapOrderEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/orders")
            .WithTags("Orders")
            .WithOpenApi()
            .RequireAuthorization();

        // Get my orders
        group.MapGet("/my", async (
            [FromQuery] OrderStatus? status,
            int page = 1,
            int pageSize = 20,
            ISender? sender = null,
            CancellationToken ct = default) =>
        {
            var result = await sender!.Send(new GetMyOrdersQuery(status, page, pageSize), ct);
            return result.IsSuccess ? Results.Ok(result.Value) : Results.BadRequest(result.Error);
        });

        // Create order
        group.MapPost("/create", async (
            [FromBody] CreateOrderRequest request,
            ISender sender,
            CancellationToken ct) =>
        {
            var result = await sender.Send(
                new CreateOrderCommand(
                    request.AssetId,
                    request.Type,
                    request.Side,
                    request.Quantity,
                    request.Price,
                    request.StopPrice,
                    request.TimeInForce,
                    request.ExpiresAt), ct);

            return result.IsSuccess
                ? Results.Created($"/api/v1/orders/{result.Value}", result.Value)
                : Results.BadRequest(result.Error);
        });

        // Cancel order
        group.MapPost("/{orderId:guid}/cancel", async (
            Guid orderId,
            ISender sender,
            CancellationToken ct) =>
        {
            var result = await sender.Send(new CancelOrderCommand(orderId), ct);
            return result.IsSuccess ? Results.Ok() : Results.BadRequest(result.Error);
        });

        return app;
    }
}

public record CreateOrderRequest(
    Guid AssetId,
    OrderType Type,
    OrderSide Side,
    decimal Quantity,
    decimal? Price = null,
    decimal? StopPrice = null,
    TimeInForce TimeInForce = TimeInForce.GTC,
    DateTime? ExpiresAt = null);
