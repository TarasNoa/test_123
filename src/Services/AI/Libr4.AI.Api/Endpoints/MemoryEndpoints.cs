using Libr4.AI.Infrastructure.EnhancedMemory;
using Libr4.AI.Domain.Memory.Enhanced.FSharp;
using Microsoft.AspNetCore.Mvc;

namespace Libr4.AI.Api.Endpoints;

public static class MemoryEndpoints
{
    public static void MapMemoryEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/memory")
            .WithTags("Memory");

        group.MapPost("/", async (
            EnhancedMemoryService service,
            [FromBody] CreateMemoryRequest request) =>
        {
            var memoryId = service.CreateMemory(
                request.Level,
                request.Content,
                request.UserId,
                request.SessionId,
                request.AgentId);

            return Results.Ok(new { memoryId });
        });

        group.MapGet("/{memoryId}", (Guid memoryId, EnhancedMemoryService service) =>
        {
            var memory = service.GetMemory(memoryId);
            return memory is not null ? Results.Ok(memory) : Results.NotFound();
        });

        group.MapPost("/{memoryId}/embedding", async (
            Guid memoryId,
            EnhancedMemoryService service,
            [FromBody] UpdateEmbeddingRequest request) =>
        {
            service.UpdateMemoryEmbedding(memoryId, request.Embedding);
            return Results.Ok();
        });

        group.MapPost("/{memoryId}/access", (Guid memoryId, EnhancedMemoryService service) =>
        {
            service.AccessMemory(memoryId);
            return Results.Ok();
        });

        group.MapPost("/consolidate", async (
            EnhancedMemoryService service,
            [FromBody] ConsolidateRequest request) =>
        {
            service.ConsolidateMemories(request.MemoryIds);
            return Results.Ok(new { message = "Memories consolidated" });
        });

        group.MapPost("/search", (
            EnhancedMemoryService service,
            [FromBody] SearchMemoryRequest request) =>
        {
            var results = service.Search(
                request.Query,
                request.QueryEmbedding,
                request.Level,
                request.UserId,
                request.SessionId,
                request.AgentId,
                request.TopK,
                request.Threshold);

            return Results.Ok(results);
        });

        group.MapGet("/", (EnhancedMemoryService service) =>
        {
            return Results.Ok(service.GetAllMemories());
        });

        group.MapPost("/cleanup", (EnhancedMemoryService service) =>
        {
            service.CleanupExpiredMemories();
            return Results.Ok(new { message = "Expired memories cleaned up" });
        });
    }

    public record CreateMemoryRequest(
        MemoryLevel Level,
        string Content,
        string? UserId,
        string? SessionId,
        Guid? AgentId);

    public record UpdateEmbeddingRequest(float[] Embedding);

    public record ConsolidateRequest(List<Guid> MemoryIds);

    public record SearchMemoryRequest(
        string Query,
        float[]? QueryEmbedding,
        MemoryLevel? Level,
        string? UserId,
        string? SessionId,
        Guid? AgentId,
        int TopK = 10,
        float? Threshold = null);
}
