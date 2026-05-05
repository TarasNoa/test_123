using Libr4.AI.Infrastructure;
using Microsoft.AspNetCore.Mvc;

namespace Libr4.AI.Api.Endpoints;

public static class RouterEndpoints
{
    public static void MapRouterEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/router")
            .WithTags("LLM Router");

        group.MapGet("/models", (LLMRouter router) =>
        {
            return Results.Ok(router.GetAllModels());
        });

        group.MapGet("/models/{modelId}", (string modelId, LLMRouter router) =>
        {
            var model = router.GetModel(modelId);
            return model is not null ? Results.Ok(model) : Results.NotFound();
        });

        group.MapPost("/route", (
            [FromBody] RouteRequest request,
            LLMRouter router) =>
        {
            var decision = router.Route(
                request.Task,
                request.Context,
                request.RequiredFeatures,
                request.MaxCost);

            return Results.Ok(decision);
        });
    }

    public record RouteRequest(
        string Task,
        string Context,
        List<string> RequiredFeatures,
        double MaxCost);
}
