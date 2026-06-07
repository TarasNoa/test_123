using System.Text.Json;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Delegation;
using Libr4.IDE.Application.AutonomousAppGeneration.Fleet;
using Libr4.IDE.Application.AutonomousAppGeneration.GitHubActionsDispatch;
using Libr4.IDE.Application.AutonomousAppGeneration.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace Libr4.IDE.Application.AutonomousAppGeneration.Api;

public static class AgentFleetEndpoints
{
    public static void MapAgentFleetEndpoints(this IEndpointRouteBuilder app, string routePrefix = "/api/v1/ide/agent-fleet")
    {
        var group = app.MapGroup(routePrefix).WithTags("Agent Fleet");

        group.MapGet("/", async (
            [FromQuery] string? status,
            [FromQuery] string? spaceId,
            [FromQuery] string? stack,
            [FromQuery] string? search,
            [FromQuery] bool includeArchived,
            [FromQuery] int limit,
            [FromQuery] string? sortBy,
            IAgentFleetRegistry fleet,
            CancellationToken ct) =>
        {
            AgentFleetStatus? parsed = null;
            if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<AgentFleetStatus>(status, true, out var s))
                parsed = s;

            var query = new AgentFleetListQuery(parsed, spaceId, stack, search, includeArchived, limit <= 0 ? 100 : limit, sortBy);
            var items = await fleet.ListAsync(query, ct).ConfigureAwait(false);
            return Results.Ok(items);
        })
        .WithName("ListAgentFleet");

        group.MapGet("/events/stream", async (
            IAgentFleetRegistry fleet,
            IAgentFleetEventHub hub,
            HttpContext http,
            CancellationToken ct) =>
        {
            http.Response.Headers.ContentType = "text/event-stream";
            http.Response.Headers.CacheControl = "no-cache";

            var snapshot = await fleet.ListAsync(new AgentFleetListQuery(), ct).ConfigureAwait(false);
            var snapshotJson = JsonSerializer.Serialize(new { type = "snapshot", items = snapshot });
            await http.Response.WriteAsync($"data: {snapshotJson}\n\n", ct).ConfigureAwait(false);
            await http.Response.Body.FlushAsync(ct).ConfigureAwait(false);

            Func<AgentFleetStatusEvent, Task> handler = async evt =>
            {
                var payload = JsonSerializer.Serialize(new
                {
                    type = "status",
                    runId = evt.RunId,
                    status = evt.Status.ToString(),
                    stage = evt.Stage,
                    timestampUtc = evt.TimestampUtc
                });
                await http.Response.WriteAsync($"data: {payload}\n\n", ct).ConfigureAwait(false);
                await http.Response.Body.FlushAsync(ct).ConfigureAwait(false);
            };

            hub.EventPublished += handler;
            try
            {
                await Task.Delay(Timeout.Infinite, ct).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                // client disconnected
            }
            finally
            {
                hub.EventPublished -= handler;
            }
        })
        .WithName("StreamAgentFleetEvents");

        group.MapGet("/background-delegations", async (
            [FromQuery] Guid? runId,
            [FromQuery] string? tenantUserId,
            [FromQuery] bool activeOnly,
            IBackgroundFleetScheduler scheduler,
            CancellationToken ct) =>
        {
            var summary = await scheduler.GetSummaryAsync(
                new BackgroundFleetListQuery(runId, tenantUserId, activeOnly),
                ct).ConfigureAwait(false);
            return Results.Ok(summary);
        })
        .WithName("ListBackgroundDelegations");

        group.MapGet("/delegation-metrics", () => Results.Ok(DelegationTelemetry.Snapshot()))
        .WithName("GetDelegationMetrics");

        group.MapGet("/{runId:guid}/summary", async (Guid runId, IAgentFleetRegistry fleet, CancellationToken ct) =>
        {
            var detail = await fleet.GetSummaryAsync(runId, ct).ConfigureAwait(false);
            return detail is null ? Results.NotFound() : Results.Ok(detail);
        })
        .WithName("GetAgentFleetSummary");

        group.MapGet("/{runId:guid}/timeline", async (Guid runId, ISessionTimelineService timeline, CancellationToken ct) =>
        {
            var response = await timeline.GetTimelineAsync(runId, ct).ConfigureAwait(false);
            return Results.Ok(response);
        })
        .WithName("GetAgentFleetTimeline");

        group.MapPatch("/{runId:guid}", async (
            Guid runId,
            [FromBody] AgentFleetPatchRequest patch,
            IAgentFleetRegistry fleet,
            CancellationToken ct) =>
        {
            await fleet.PatchAsync(runId, patch, ct).ConfigureAwait(false);
            await fleet.WriteAuditAsync("patch", runId, patch.Actor ?? "fleet-ui", ct).ConfigureAwait(false);
            var detail = await fleet.GetSummaryAsync(runId, ct).ConfigureAwait(false);
            return detail is null ? Results.NotFound() : Results.Ok(detail.Entry);
        })
        .WithName("PatchAgentFleetEntry");

        group.MapPost("/{runId:guid}/cancel", async (
            Guid runId,
            IAutonomousRunControlService runControl,
            IAgentFleetRegistry fleet,
            CancellationToken ct) =>
        {
            var cancelled = runControl.CancelRun(runId, actor: "fleet-ui", reason: "user_cancel_from_fleet");
            if (!cancelled)
                return Results.NotFound(new { error = "run_not_active" });

            await fleet.WriteAuditAsync("cancel", runId, "fleet-ui", ct).ConfigureAwait(false);
            await fleet.UpsertFromRunAsync(runId, ct).ConfigureAwait(false);
            return Results.Ok(new { runId, cancelled = true });
        })
        .WithName("CancelAgentFleetRun");

        group.MapPost("/bulk-archive", async (
            [FromBody] AgentFleetBulkArchiveRequest request,
            IAgentFleetRegistry fleet,
            CancellationToken ct) =>
        {
            await fleet.BulkArchiveAsync(request, ct).ConfigureAwait(false);
            return Results.Ok(new { archived = true, olderThanDays = request.OlderThanDays });
        })
        .WithName("BulkArchiveAgentFleetRuns");

        group.MapGet("/search", async (
            [FromQuery] string q,
            [FromQuery] string? stack,
            [FromQuery] string? outcome,
            [FromQuery] string? spaceId,
            [FromQuery] string? dateBucket,
            [FromQuery] int limit,
            IFleetSessionSearchService search,
            CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(q))
                return Results.BadRequest(new { error = "q is required" });

            var result = await search.SearchAsync(
                new FleetSessionSearchQuery(q, stack, outcome, spaceId, dateBucket, limit <= 0 ? 50 : limit),
                ct).ConfigureAwait(false);
            return Results.Ok(result);
        })
        .WithName("SearchAgentFleetSessions");

        group.MapGet("/{runId:guid}/similar", async (
            Guid runId,
            [FromQuery] int limit,
            IFleetSimilarRunsService similarRuns,
            CancellationToken ct) =>
        {
            var result = await similarRuns.FindSimilarAsync(
                runId,
                limit <= 0 ? null : limit,
                ct).ConfigureAwait(false);
            return Results.Ok(result);
        })
        .WithName("GetSimilarAgentFleetRuns");

        group.MapPost("/{runId:guid}/fork", async (
            Guid runId,
            IRunForkService fork,
            CancellationToken ct) =>
        {
            var result = await fork.ForkAsync(runId, ct).ConfigureAwait(false);
            return result is null ? Results.NotFound(new { error = "run_not_found" }) : Results.Ok(result);
        })
        .WithName("ForkAgentFleetRun");

        group.MapDelete("/{runId:guid}/gdpr-erase", async (
            Guid runId,
            IFleetGdprEraseService erase,
            IAgentFleetRegistry fleet,
            CancellationToken ct) =>
        {
            var result = await erase.EraseAsync(runId, ct).ConfigureAwait(false);
            if (result is null)
                return Results.NotFound(new { error = "run_not_found" });
            await fleet.WriteAuditAsync("gdpr_erase", runId, "fleet-api", ct).ConfigureAwait(false);
            return Results.Ok(result);
        })
        .WithName("GdprEraseAgentFleetRun");

        group.MapGet("/{runId:guid}/gdpr-export", async (
            Guid runId,
            IFleetGdprExportService export,
            IAgentFleetRegistry fleet,
            CancellationToken ct) =>
        {
            var bundle = await export.ExportAsync(runId, ct).ConfigureAwait(false);
            if (bundle is null)
                return Results.NotFound(new { error = "run_not_found" });
            await fleet.WriteAuditAsync("gdpr_export", runId, "fleet-api", ct).ConfigureAwait(false);
            return Results.File(
                System.Text.Encoding.UTF8.GetBytes(bundle.JsonPayload),
                "application/json",
                bundle.FileName);
        })
        .WithName("GdprExportAgentFleetRun");

        group.MapPost("/retention/sweep", async (
            IFleetRetentionService retention,
            IAgentFleetRegistry fleet,
            CancellationToken ct) =>
        {
            var result = await retention.ApplyRetentionAsync(ct).ConfigureAwait(false);
            await fleet.WriteAuditAsync("retention_sweep", Guid.Empty, "fleet-api", ct).ConfigureAwait(false);
            return Results.Ok(result);
        })
        .WithName("AgentFleetRetentionSweep");

        group.MapPost("/rebuild", async (IAgentFleetRegistry fleet, CancellationToken ct) =>
        {
            var count = await fleet.RebuildIndexAsync(ct).ConfigureAwait(false);
            return Results.Ok(new { indexed = count });
        })
        .WithName("RebuildAgentFleetIndex");

        group.MapPost("/{runId:guid}/pull-request", async (
            Guid runId,
            IPullRequestService pullRequests,
            CancellationToken ct) =>
        {
            var result = await pullRequests.CreatePrAsync(runId, ct).ConfigureAwait(false);
            if (result.Summary.Contains("run_not_found", StringComparison.OrdinalIgnoreCase))
                return Results.NotFound(new { error = result.Summary });
            if (!result.Success && !result.Skipped)
                return Results.BadRequest(result);
            return Results.Ok(result);
        })
        .WithName("CreateAgentFleetPullRequest");
    }
}
