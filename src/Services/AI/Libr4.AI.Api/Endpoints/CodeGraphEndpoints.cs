using Libr4.AI.Infrastructure.CodeGraph;
using Microsoft.AspNetCore.Mvc;

namespace Libr4.AI.Api.Endpoints;

public static class CodeGraphEndpoints
{
    public static void MapCodeGraphEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/code-graph")
            .WithTags("Code Graph");

        group.MapGet("/", async (CodeGraphService service) =>
        {
            return Results.Ok(new
            {
                nodes = service.GetNodes(),
                edges = service.GetEdges(),
                stats = service.GetStats()
            });
        });

        group.MapGet("/nodes", (CodeGraphService service) =>
        {
            return Results.Ok(service.GetNodes());
        });

        group.MapGet("/nodes/{type}", (string type, CodeGraphService service) =>
        {
            return Results.Ok(service.FindNodesByType(type));
        });

        group.MapGet("/nodes/label/{label}", (string label, CodeGraphService service) =>
        {
            return Results.Ok(service.FindNodesByLabel(label));
        });

        group.MapGet("/related/{nodeId}", (string nodeId, CodeGraphService service) =>
        {
            return Results.Ok(service.GetRelatedNodes(nodeId));
        });

        group.MapPost("/build", async (CodeGraphService service, [FromBody] BuildGraphRequest request) =>
        {
            await service.BuildGraphFromDirectory(request.DirectoryPath, request.FileExtensions);
            return Results.Ok(new { message = "Graph built successfully" });
        });

        group.MapPost("/build/file", async (CodeGraphService service, [FromBody] BuildFileRequest request) =>
        {
            await service.BuildGraphFromFile(request.FilePath);
            return Results.Ok(new { message = "File added to graph successfully" });
        });

        group.MapDelete("/", (CodeGraphService service) =>
        {
            service.Clear();
            return Results.Ok(new { message = "Graph cleared" });
        });
    }

    public record BuildGraphRequest(string DirectoryPath, string[]? FileExtensions);
    public record BuildFileRequest(string FilePath);
}
