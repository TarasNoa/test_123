using Libr4.Trading.Application.Portfolios.Queries;
using MediatR;

namespace Libr4.Trading.Api.Endpoints;

public static class PortfolioEndpoints
{
    public static IEndpointRouteBuilder MapPortfolioEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/portfolio")
            .WithTags("Portfolio")
            .WithOpenApi()
            .RequireAuthorization();

        // Get my portfolio
        group.MapGet("/my", async (
            ISender sender,
            CancellationToken ct) =>
        {
            var result = await sender.Send(new GetMyPortfolioQuery(), ct);
            return result.IsSuccess ? Results.Ok(result.Value) : Results.NotFound(result.Error);
        });

        return app;
    }
}
