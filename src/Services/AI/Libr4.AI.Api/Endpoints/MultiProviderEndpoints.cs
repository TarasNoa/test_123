using Libr4.AI.Infrastructure.Orchestration;
using Microsoft.AspNetCore.Mvc;

namespace Libr4.AI.Api.Endpoints;

/// <summary>
/// API endpoints for multi-provider orchestration
/// </summary>
public static class MultiProviderEndpoints
{
    public static IEndpointRouteBuilder MapMultiProviderEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/multi-provider")
            .WithTags("MultiProvider")
            .WithOpenApi();

        // Execute with multiple providers
        group.MapPost("/execute", async (
            [FromServices] IMultiProviderOrchestrator orchestrator,
            CancellationToken ct,
            [FromBody] MultiProviderRequest request,
            [FromQuery] string mode = "Parallel") =>
        {
            var executionMode = Enum.Parse<ProviderExecutionMode>(mode, true);
            var providers = request.ProviderIds ?? new List<string> { "claude-opus-4.7", "gpt-5.4" };
            
            var result = await orchestrator.ExecuteWithProvidersAsync(request.Task, executionMode, providers, ct);
            return Results.Ok(result);
        })
        .WithName("ExecuteWithProviders");

        // Get consensus
        group.MapGet("/consensus/{taskId:guid}", async (
            [FromServices] IMultiProviderOrchestrator orchestrator,
            Guid taskId,
            CancellationToken ct,
            [FromQuery] float threshold = 0.75f) =>
        {
            var consensus = await orchestrator.GetConsensusAsync(taskId, threshold, ct);
            return Results.Ok(consensus);
        })
        .WithName("GetConsensus");

        // Run adversarial review
        group.MapPost("/adversarial-review/{taskId:guid}", async (
            [FromServices] IMultiProviderOrchestrator orchestrator,
            Guid taskId,
            CancellationToken ct,
            [FromBody] List<string> reviewerIds) =>
        {
            var review = await orchestrator.RunAdversarialReviewAsync(taskId, reviewerIds, ct);
            return Results.Ok(review);
        })
        .WithName("RunAdversarialReview");

        return app;
    }
}

public class MultiProviderRequest
{
    public AgentTask Task { get; set; } = new();
    public List<string>? ProviderIds { get; set; }
}
