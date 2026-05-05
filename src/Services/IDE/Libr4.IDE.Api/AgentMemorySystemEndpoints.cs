/*
using Libr4.IDE.Domain.AgentMemorySystem;
using Libr4.IDE.Application.AgentMemorySystem;
using Microsoft.AspNetCore.Mvc;

namespace Libr4.IDE.Api;

/// <summary>
/// Minimal API endpoints for Agent Memory System
/// Enhanced with concepts from GenericAgent, cursor-memory-bank, hindsight, aimemory
/// </summary>
public static class AgentMemorySystemEndpoints
{
    public static void MapAgentMemorySystemEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/ide/memory")
            .WithTags("Agent Memory System")
            .RequireAuthorization();

        // Cognitive Memory System operations
        group.MapPost("/system/create", (
            [FromBody] CognitiveMemorySystem system,
            CancellationToken ct) =>
        {
            return Results.Ok(new { systemId = system.SystemId, agentId = system.AgentId });
        })
        .WithName("CreateCognitiveMemorySystem")
        .WithSummary("Create cognitive memory system")
        .WithDescription("Create multi-sector memory system with layered memory support");

        // Layered memory operations (from GenericAgent)
        group.MapPost("/layered-fragment/add", (
            [FromBody] LayeredMemoryFragment fragment,
            Guid systemId,
            CancellationToken ct) =>
        {
            return Results.Ok(new { fragmentId = fragment.Id, layer = fragment.Layer });
        })
        .WithName("AddLayeredFragment")
        .WithSummary("Add layered memory fragment")
        .WithDescription("Store memory fragment in specific layer (L0-L4)");

        group.MapGet("/layered-fragment/search", (
            Guid systemId,
            MemoryLayer layer,
            string query,
            int topN = 10,
            CancellationToken ct) =>
        {
            return Results.Ok(new { systemId, layer, query, topN });
        })
        .WithName("SearchLayeredFragments")
        .WithSummary("Search layered memory")
        .WithDescription("Search memory fragments by layer with relevance ranking");

        // Self-evolving skills (from GenericAgent)
        group.MapPost("/skill/crystallize", (
            [FromBody] SelfEvolvingSkill skill,
            Guid systemId,
            CancellationToken ct) =>
        {
            return Results.Ok(new { skillId = skill.Id, name = skill.Name });
        })
        .WithName("CrystallizeSkill")
        .WithSummary("Crystallize skill")
        .WithDescription("Crystallize execution path into reusable skill");

        // Memory Bank operations (from cursor-memory-bank)
        group.MapPost("/memory-bank/initialize", (
            string path,
            Guid systemId,
            CancellationToken ct) =>
        {
            return Results.Ok(new { systemId, path });
        })
        .WithName("InitializeMemoryBank")
        .WithSummary("Initialize memory bank")
        .WithDescription("Initialize hierarchical memory bank with task complexity rules");

        group.MapGet("/memory-bank/file/{fileName}", (
            Guid systemId,
            string fileName,
            CancellationToken ct) =>
        {
            return Results.Ok(new { systemId, fileName });
        })
        .WithName("GetMemoryBankFile")
        .WithSummary("Get memory bank file")
        .WithDescription("Retrieve memory bank file content");

        // Hindsight memory operations (from hindsight)
        group.MapPost("/hindsight/retain", (
            string bankId,
            MemoryType type,
            string content,
            Guid systemId,
            CancellationToken ct) =>
        {
            return Results.Ok(new { systemId, bankId, type });
        })
        .WithName("HindsightRetain")
        .WithSummary("Retain information in hindsight")
        .WithDescription("Store information in biomimetic memory bank");

        group.MapGet("/hindsight/recall", (
            string bankId,
            string query,
            Guid systemId,
            CancellationToken ct) =>
        {
            return Results.Ok(new { systemId, bankId, query });
        })
        .WithName("HindsightRecall")
        .WithSummary("Recall from hindsight")
        .WithDescription("Retrieve memories using multiple strategies (semantic, keyword, graph, temporal)");

        // Sector operations
        group.MapPost("/sector/store", (
            MemorySector sector,
            string content,
            Guid systemId,
            CancellationToken ct) =>
        {
            return Results.Ok(new { systemId, sector });
        })
        .WithName("StoreInSector")
        .WithSummary("Store in sector")
        .WithDescription("Store memory fragment in specific sector");

        group.MapGet("/sector/retrieve", (
            MemorySector sector,
            string query,
            Guid systemId,
            CancellationToken ct) =>
        {
            return Results.Ok(new { systemId, sector, query });
        })
        .WithName("RetrieveFromSector")
        .WithSummary("Retrieve from sector")
        .WithDescription("Retrieve fragments from specific sector");

        // Statistics
        group.MapGet("/statistics/{systemId}", (
            Guid systemId,
            CancellationToken ct) =>
        {
            return Results.Ok(new { systemId });
        })
        .WithName("GetMemoryStatistics")
        .WithSummary("Get memory statistics")
        .WithDescription("Get comprehensive statistics for memory system");
    }
}
*/
