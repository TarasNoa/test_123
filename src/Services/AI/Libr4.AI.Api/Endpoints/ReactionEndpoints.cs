using Libr4.AI.Infrastructure.Harness;
using Microsoft.AspNetCore.Mvc;

namespace Libr4.AI.Api.Endpoints;

/// <summary>
/// API endpoints for reaction engine
/// </summary>
public static class ReactionEndpoints
{
    public static IEndpointRouteBuilder MapReactionEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/reactions")
            .WithTags("Reactions")
            .WithOpenApi();

        // Get reaction configuration
        group.MapGet("/configuration", async (
            [FromServices] IReactionEngine reactionEngine,
            CancellationToken ct) =>
        {
            var config = await reactionEngine.GetConfigurationAsync(ct);
            return Results.Ok(config);
        })
        .WithName("GetReactionConfiguration");

        // Update reaction configuration
        group.MapPut("/configuration", async (
            [FromBody] ReactionConfiguration config,
            [FromServices] IReactionEngine reactionEngine,
            CancellationToken ct) =>
        {
            await reactionEngine.UpdateConfigurationAsync(config, ct);
            return Results.Ok();
        })
        .WithName("UpdateReactionConfiguration");

        // Process event
        group.MapPost("/events", async (
            [FromBody] AgentLifecycleEvent @event,
            [FromServices] IReactionEngine reactionEngine,
            CancellationToken ct) =>
        {
            await reactionEngine.ProcessEventAsync(@event, ct);
            return Results.Ok();
        })
        .WithName("ProcessEvent");

        return app;
    }
}
