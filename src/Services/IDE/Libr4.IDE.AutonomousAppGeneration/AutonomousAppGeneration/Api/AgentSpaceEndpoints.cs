using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Libr4.IDE.Application.AutonomousAppGeneration.Spaces;

namespace Libr4.IDE.Application.AutonomousAppGeneration.Api;

public static class AgentSpaceEndpoints
{
    public static void MapAgentSpaceEndpoints(this IEndpointRouteBuilder app, string routePrefix = "/api/v1/ide/spaces")
    {
        var group = app.MapGroup(routePrefix).WithTags("Agent Spaces");

        group.MapPost("/", async ([FromBody] CreateSpaceRequest body, IAgentSpaceService spaces, CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(body.Name))
                return Results.BadRequest(new { error = "name_required" });

            var space = await spaces.CreateSpaceAsync(body, ct).ConfigureAwait(false);
            return Results.Created($"{routePrefix}/{space.SpaceId:D}", space);
        })
        .WithName("CreateAgentSpace");

        group.MapGet("/", async ([FromQuery] string? ownerId, IAgentSpaceService spaces, CancellationToken ct) =>
        {
            var items = await spaces.ListSpacesAsync(ownerId, ct).ConfigureAwait(false);
            return Results.Ok(items);
        })
        .WithName("ListAgentSpaces");

        group.MapGet("/{spaceId:guid}", async (Guid spaceId, IAgentSpaceService spaces, CancellationToken ct) =>
        {
            var detail = await spaces.GetSpaceDetailAsync(spaceId, ct).ConfigureAwait(false);
            return detail is null ? Results.NotFound() : Results.Ok(detail);
        })
        .WithName("GetAgentSpace");

        group.MapPost("/{spaceId:guid}/agents", async (
            Guid spaceId,
            [FromBody] SpawnSpaceAgentRequest body,
            IAgentSpaceService spaces,
            CancellationToken ct) =>
        {
            try
            {
                var member = await spaces.SpawnAgentAsync(spaceId, body, ct).ConfigureAwait(false);
                return Results.Ok(member);
            }
            catch (KeyNotFoundException)
            {
                return Results.NotFound(new { error = "space_not_found" });
            }
            catch (InvalidOperationException ex)
            {
                return Results.Conflict(new { error = ex.Message });
            }
        })
        .WithName("SpawnSpaceAgent");

        group.MapPost("/{spaceId:guid}/merge/{memberId}", async (
            Guid spaceId,
            string memberId,
            IAgentSpaceService spaces,
            CancellationToken ct) =>
        {
            try
            {
                var result = await spaces.MergeMemberAsync(spaceId, memberId, ct).ConfigureAwait(false);
                return result.Success ? Results.Ok(result) : Results.Conflict(result);
            }
            catch (KeyNotFoundException ex)
            {
                return Results.NotFound(new { error = ex.Message });
            }
        })
        .WithName("MergeSpaceMember");

        group.MapPost("/{spaceId:guid}/orchestrate", async (
            Guid spaceId,
            [FromBody] SpaceOrchestrationRequest body,
            ISpaceOrchestrator orchestrator,
            CancellationToken ct) =>
        {
            try
            {
                var result = await orchestrator.RunParallelPipelineAsync(spaceId, body, ct).ConfigureAwait(false);
                return Results.Ok(result);
            }
            catch (KeyNotFoundException)
            {
                return Results.NotFound(new { error = "space_not_found" });
            }
            catch (InvalidOperationException ex)
            {
                return Results.Conflict(new { error = ex.Message });
            }
        })
        .WithName("OrchestrateAgentSpace");

        group.MapPost("/{spaceId:guid}/context/{memberId}", async (
            Guid spaceId,
            string memberId,
            [FromBody] SpaceContextSignalRequest body,
            ISpaceOrchestrator orchestrator,
            CancellationToken ct) =>
        {
            await orchestrator.SignalContextReadyAsync(spaceId, memberId, body.Kind, body.Title, body.Payload, ct).ConfigureAwait(false);
            return Results.Ok(new { signaled = true });
        })
        .WithName("SignalSpaceContext");

        group.MapGet("/{spaceId:guid}/members/{memberId}/files", async (
            Guid spaceId,
            string memberId,
            [FromQuery] string? path,
            IAgentSpaceService spaces,
            CancellationToken ct) =>
        {
            try
            {
                var listing = await spaces.ListWorktreeFilesAsync(spaceId, memberId, path, ct).ConfigureAwait(false);
                return listing is null ? Results.NotFound() : Results.Ok(listing);
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("path_traversal", StringComparison.Ordinal))
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        })
        .WithName("ListSpaceWorktreeFiles");

        group.MapGet("/{spaceId:guid}/merge/{memberId}/preview", async (
            Guid spaceId,
            string memberId,
            IAgentSpaceService spaces,
            CancellationToken ct) =>
        {
            var preview = await spaces.PreviewMergeAsync(spaceId, memberId, ct).ConfigureAwait(false);
            return preview is null ? Results.NotFound() : Results.Ok(preview);
        })
        .WithName("PreviewSpaceMerge");
    }
}

public sealed record SpaceContextSignalRequest(string Kind, string Title, string? Payload);
