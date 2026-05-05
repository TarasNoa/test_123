using Libr4.IDE.Application.AutonomousAppGeneration.Commands;
using Libr4.IDE.Application.AutonomousAppGeneration.Queries;
using Libr4.IDE.Application.AutonomousAppGeneration.Runtime;
using Libr4.IDE.Application.AutonomousAppGeneration.Services;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Libr4.IDE.AutonomousAppGeneration.Host.Endpoints;

public sealed record CancelRunRequest(string? Actor, string? Reason);

/// <summary>
/// HTTP surface for the top-level autonomous app generation orchestrator.
/// Clients POST a natural-language request (e.g. "build a banking API") and
/// then poll the report endpoint for plan, iterations and generated files.
/// </summary>
public static class AutonomousAppGenerationEndpoints
{
    public static void MapAutonomousAppGenerationEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/ide/app-generation")
            .WithTags("Autonomous App Generation")
            .WithOpenApi();

        group.MapPost("/start", async (
            [FromBody] StartAppGenerationCommand command,
            IMediator mediator,
            IServiceScopeFactory scopeFactory,
            CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(command.UserRequest) && command.ResumeFromRunId is null)
                return Results.BadRequest(new { error = "userRequest is required (or set resumeFromRunId)" });

            // Fire-and-forget: return runId immediately, generation runs in background.
            // Poll GET /{id} for status and results.
            var runId = Guid.NewGuid();
            var commandWithId = command; // handler assigns its own ID; we return it after first save.

            // Run generation in a background Task, decoupled from the HTTP request lifetime.
            _ = Task.Run(async () =>
            {
                using var scope = scopeFactory.CreateScope();
                var scopedMediator = scope.ServiceProvider.GetRequiredService<IMediator>();
                try
                {
                    await scopedMediator.Send(commandWithId, CancellationToken.None);
                }
                catch
                {
                    // Errors are captured inside the handler and persisted on the orchestrator aggregate.
                }
            });

            // Brief wait to let the handler save the orchestrator (with its real ID) before we respond.
            await Task.Delay(800, ct);

            // Return a 202 Accepted with a polling hint.
            return Results.Accepted($"/api/ide/app-generation/poll-hint", new
            {
                message = "Generation started. Poll GET /api/ide/app-generation/list for your run.",
                hint = "Use GET /api/ide/app-generation/{id} once you have the run ID from /list endpoint."
            });
        })
        .WithName("StartAutonomousAppGeneration")
        .WithSummary("Kick off the orchestrator asynchronously. Returns 202 immediately; poll GET /{id} for results.");

        group.MapGet("/list", async (
            IMediator mediator,
            CancellationToken ct) =>
        {
            var runs = await mediator.Send(new Libr4.IDE.Application.AutonomousAppGeneration.Queries.ListAppGenerationRunsQuery(), ct);
            return Results.Ok(runs);
        })
        .WithName("ListAutonomousAppGenerationRuns")
        .WithSummary("List all known generation runs with their current status.");

        group.MapGet("/{id:guid}", async (
            Guid id, IMediator mediator, CancellationToken ct) =>
        {
            var report = await mediator.Send(new GetAppGenerationReportQuery(id), ct);
            return report is null ? Results.NotFound() : Results.Ok(report);
        })
        .WithName("GetAutonomousAppGenerationReport")
        .WithSummary("Full report for a specific orchestrator run: plan, iterations, files, errors.");

        group.MapGet("/{id:guid}/manifest", async (
            Guid id, IMediator mediator, CancellationToken ct) =>
        {
            var manifest = await mediator.Send(new GetAppGenerationManifestQuery(id), ct);
            return manifest is null ? Results.NotFound() : Results.Ok(manifest);
        })
        .WithName("GetAutonomousAppGenerationManifest")
        .WithSummary("Machine-readable execution manifest with command-level audit trail.");

        group.MapGet("/{id:guid}/diagnostics", async (
            Guid id, IMediator mediator, CancellationToken ct) =>
        {
            var diagnostics = await mediator.Send(new GetDiagnosticsBundleQuery(id), ct);
            return diagnostics is null ? Results.NotFound() : Results.Ok(diagnostics);
        })
        .WithName("GetAutonomousAppGenerationDiagnosticsBundle")
        .WithSummary("Diagnostics bundle with logs/files/benchmark and MCP lane degradation summary.");

        group.MapGet("/{id:guid}/diagnostics/export", async (
            Guid id, IMediator mediator, CancellationToken ct) =>
        {
            var export = await mediator.Send(new ExportDiagnosticsPackageQuery(id), ct);
            return export is null ? Results.NotFound() : Results.Ok(export);
        })
        .WithName("ExportAutonomousAppGenerationDiagnosticsPackage")
        .WithSummary("Export diagnostics bundle as zipped JSON artifact.");

        group.MapGet("/dashboard/benchmark", async (
            [FromQuery] int? limit,
            IMediator mediator,
            CancellationToken ct) =>
        {
            var dashboard = await mediator.Send(new GetBenchmarkDashboardQuery(limit ?? 20), ct);
            return Results.Ok(dashboard);
        })
        .WithName("GetAutonomousBenchmarkDashboard")
        .WithSummary("Benchmark dashboard export: trends for quality, latency and failure reasons.");

        group.MapGet("/dashboard/benchmark/export", async (
            [FromQuery] int? limit,
            IMediator mediator,
            CancellationToken ct) =>
        {
            var export = await mediator.Send(new GetBenchmarkDashboardExportQuery(limit ?? 20), ct);
            return Results.Ok(export);
        })
        .WithName("ExportAutonomousBenchmarkDashboard")
        .WithSummary("Persist benchmark dashboard snapshot to JSON artifact and return metadata.");

        group.MapGet("/dashboard/readiness", async (
            IMediator mediator,
            CancellationToken ct) =>
        {
            var readiness = await mediator.Send(new GetStageCReadinessQuery(), ct);
            return Results.Ok(readiness);
        })
        .WithName("GetAutonomousStageCReadiness")
        .WithSummary("Stage C readiness checklist for MCP lanes and observability pipeline.");

        group.MapGet("/runtime/diagnostics", (IRuntimeDiagnostics diagnostics) =>
            Results.Ok(diagnostics.GetSnapshot()))
        .WithName("GetAutonomousRuntimeDiagnostics")
        .WithSummary("Runtime diagnostics: provider attempts, fallback usage, and recent failures.");

        group.MapPost("/{id:guid}/cancel", (
            Guid id,
            [FromBody] CancelRunRequest? request,
            IAutonomousRunControlService runControl) =>
        {
            var ok = runControl.CancelRun(id, request?.Actor, request?.Reason);
            return ok ? Results.Ok(new { runId = id, cancelled = true })
                      : Results.NotFound(new { runId = id, cancelled = false, reason = "run_not_active" });
        })
        .WithName("CancelAutonomousAppGenerationRun")
        .WithSummary("Cancel an active app generation run with actor and reason metadata.");

        group.MapPost("/{id:guid}/pause", (Guid id, IAutonomousRunControlService runControl) =>
        {
            var ok = runControl.PauseRun(id);
            return ok ? Results.Ok(new { runId = id, paused = true })
                      : Results.NotFound(new { runId = id, paused = false, reason = "run_not_active" });
        })
        .WithName("PauseAutonomousAppGenerationRun")
        .WithSummary("Pause an active app generation run.");

        group.MapPost("/{id:guid}/resume", (Guid id, IAutonomousRunControlService runControl) =>
        {
            var ok = runControl.ResumeRun(id);
            return ok ? Results.Ok(new { runId = id, resumed = true })
                      : Results.NotFound(new { runId = id, resumed = false, reason = "run_not_active" });
        })
        .WithName("ResumeAutonomousAppGenerationRun")
        .WithSummary("Resume a paused app generation run.");

        group.MapGet("/runs/health", (IAutonomousRunControlService runControl) =>
            Results.Ok(runControl.GetHealthSnapshot()))
        .WithName("GetAutonomousRunsHealth")
        .WithSummary("Health and aggregate stats of autonomous app generation runs.");

        group.MapGet("/{id:guid}/state", (Guid id, IAutonomousRunControlService runControl) =>
        {
            var state = runControl.GetRunState(id);
            return state is null
                ? Results.NotFound(new { runId = id, reason = "run_not_active" })
                : Results.Ok(state);
        })
        .WithName("GetAutonomousRunState")
        .WithSummary("Current live state for an active autonomous app generation run.");
    }
}
