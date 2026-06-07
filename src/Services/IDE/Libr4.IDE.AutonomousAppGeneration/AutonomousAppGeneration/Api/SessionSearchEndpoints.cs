using Libr4.IDE.Application.AutonomousAppGeneration.Memory.Cognitive;
using Libr4.IDE.Application.AutonomousAppGeneration.Memory.Consolidation;
using Libr4.IDE.Application.AutonomousAppGeneration.Memory.Search;
using Libr4.IDE.Domain.AgentMemorySystem;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Libr4.IDE.Application.AutonomousAppGeneration.Api;

public static class SessionSearchEndpoints
{
    public static void MapSessionSearchEndpoints(this IEndpointRouteBuilder app, string routePrefix = "/api/ide/memory")
    {
        var group = app.MapGroup(routePrefix)
            .WithTags("Session Memory Search");

        group.MapGet("/search", async (
            string q,
            int? limit,
            ISessionSearchService search,
            CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(q))
                return Results.BadRequest(new { error = "q is required" });

            var hits = await search.SearchAsync(q, limit ?? 25, ct).ConfigureAwait(false);
            return Results.Ok(new { query = q, count = hits.Count, hits });
        })
        .WithName("SearchSessionMemory");

        group.MapGet("/consolidation/stats", (IDreamConsolidationService consolidation) =>
        {
            var last = consolidation.GetLastResult();
            return last is null
                ? Results.Ok(new { status = "never_run" })
                : Results.Ok(new
                {
                    status = last.Success ? "ok" : "failed",
                    last.StartedAtUtc,
                    last.CompletedAtUtc,
                    last.TotalBefore,
                    last.EpisodicMergedToSemantic,
                    last.StalePruned,
                    last.DuplicatesRemoved,
                    last.EpisodicRetentionPruned,
                    last.SemanticAfter,
                    last.ErrorMessage
                });
        })
        .WithName("GetDreamConsolidationStats");

        group.MapPost("/consolidation/run", async (IDreamConsolidationService consolidation, CancellationToken ct) =>
        {
            var result = await consolidation.RunAsync(ct).ConfigureAwait(false);
            return Results.Ok(result);
        })
        .WithName("RunDreamConsolidation");

        group.MapGet("/cognitive/stats", (string fingerprint, ICognitiveMemoryBridge bridge) =>
        {
            if (string.IsNullOrWhiteSpace(fingerprint))
                return Results.BadRequest(new { error = "fingerprint is required" });

            var stats = bridge.GetStatistics(fingerprint);
            return Results.Ok(stats);
        })
        .WithName("GetCognitiveMemoryStats");

        group.MapGet("/cognitive/search", async (
            string fingerprint,
            MemoryLayer layer,
            string q,
            int? topN,
            ICognitiveMemoryBridge bridge,
            CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(fingerprint))
                return Results.BadRequest(new { error = "fingerprint is required" });
            if (string.IsNullOrWhiteSpace(q))
                return Results.BadRequest(new { error = "q is required" });

            var hits = await bridge.SearchLayerAsync(fingerprint, layer, q, topN ?? 10, ct).ConfigureAwait(false);
            return Results.Ok(new
            {
                fingerprint,
                layer,
                query = q,
                count = hits.Count,
                hits = hits.Select(hit => new
                {
                    hit.Id,
                    layer = hit.Layer,
                    content = hit.Content,
                    score = hit.RelevanceScore,
                    metadata = hit.Metadata
                })
            });
        })
        .WithName("SearchCognitiveMemoryLayer");
    }
}
