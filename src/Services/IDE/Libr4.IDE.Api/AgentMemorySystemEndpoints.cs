using Libr4.IDE.Domain.AgentMemorySystem;
using Microsoft.AspNetCore.Mvc;

namespace Libr4.IDE.Api;

public static class AgentMemorySystemEndpoints
{
    public static void MapAgentMemorySystemEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/ide/memory")
            .RequireAuthorization();

        group.MapPost("/system/create", (
            [FromBody] CognitiveMemorySystem system,
            CancellationToken ct) =>
        {
            return Results.Ok(new { systemId = system.SystemId, agentId = system.AgentId });
        });

        group.MapPost("/layered-fragment/add", (
            [FromBody] LayeredMemoryFragment fragment,
            Guid systemId,
            CancellationToken ct) =>
        {
            return Results.Ok(new { fragmentId = fragment.Id, layer = fragment.Layer });
        });

        group.MapGet("/layered-fragment/search", (
            Guid systemId,
            MemoryLayer layer,
            string query,
            CancellationToken ct,
            int topN = 10) =>
        {
            return Results.Ok(new { systemId, layer, query, topN });
        });

        group.MapPost("/skill/crystallize", (
            [FromBody] SelfEvolvingSkill skill,
            Guid systemId,
            CancellationToken ct) =>
        {
            return Results.Ok(new { skillId = skill.Id, name = skill.Name });
        });

        group.MapPost("/memory-bank/initialize", (
            string path,
            Guid systemId,
            CancellationToken ct) =>
        {
            return Results.Ok(new { systemId, path });
        });

        group.MapGet("/memory-bank/file/{fileName}", (
            Guid systemId,
            string fileName,
            CancellationToken ct) =>
        {
            return Results.Ok(new { systemId, fileName });
        });

        group.MapPost("/hindsight/retain", (
            string bankId,
            CognitiveMemorySystem.MemoryType type,
            string content,
            Guid systemId,
            CancellationToken ct) =>
        {
            return Results.Ok(new { systemId, bankId, type });
        });

        group.MapGet("/hindsight/recall", (
            string bankId,
            string query,
            Guid systemId,
            CancellationToken ct) =>
        {
            return Results.Ok(new { systemId, bankId, query });
        });

        group.MapPost("/sector/store", (
            MemorySector sector,
            string content,
            Guid systemId,
            CancellationToken ct) =>
        {
            return Results.Ok(new { systemId, sector });
        });

        group.MapGet("/sector/retrieve", (
            MemorySector sector,
            string query,
            Guid systemId,
            CancellationToken ct) =>
        {
            return Results.Ok(new { systemId, sector, query });
        });

        group.MapGet("/statistics/{systemId}", (
            Guid systemId,
            CancellationToken ct) =>
        {
            return Results.Ok(new { systemId });
        });
    }
}
