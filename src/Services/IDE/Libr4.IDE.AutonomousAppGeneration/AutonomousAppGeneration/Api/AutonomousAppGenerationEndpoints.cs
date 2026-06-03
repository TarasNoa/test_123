using Libr4.IDE.Application.AutonomousAppGeneration.Commands;
using Libr4.IDE.Application.AutonomousAppGeneration.Queries;
using Libr4.IDE.Application.AutonomousAppGeneration.Runtime;
using Libr4.IDE.Application.AutonomousAppGeneration.Services;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace Libr4.IDE.Application.AutonomousAppGeneration.Api;

public sealed record CancelRunRequest(string? Actor, string? Reason);

public static class AutonomousAppGenerationEndpoints
{
    public static void MapAutonomousAppGenerationEndpoints(
        this IEndpointRouteBuilder app,
        string routePrefix = "/api/ide/app-generation")
    {
        var group = app.MapGroup(routePrefix)
            .WithTags("Autonomous App Generation");

        group.MapPost("/start", async (
            [FromBody] StartAppGenerationCommand command,
            IAppGenerationRunStarter starter,
            CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(command.UserRequest) && command.ResumeFromRunId is null)
                return Results.BadRequest(new { error = "userRequest is required (or set resumeFromRunId)" });

            var started = await starter.StartInBackgroundAsync(command, ct).ConfigureAwait(false);
            if (started.RunId is null)
            {
                return Results.Accepted($"{routePrefix}/list", new
                {
                    status = started.Status,
                    message = started.Message,
                });
            }

            return Results.Accepted($"{routePrefix}/{started.RunId:D}", new
            {
                id = started.RunId,
                status = started.Status,
                message = started.Message,
                reportUrl = $"{routePrefix}/{started.RunId:D}",
            });
        })
        .WithName("StartAutonomousAppGeneration");

        group.MapGet("/list", async (IMediator mediator, CancellationToken ct) =>
        {
            var runs = await mediator.Send(new ListAppGenerationRunsQuery(), ct).ConfigureAwait(false);
            return Results.Ok(runs);
        })
        .WithName("ListAutonomousAppGenerationRuns");

        group.MapGet("/{id:guid}", async (Guid id, IMediator mediator, CancellationToken ct) =>
        {
            var report = await mediator.Send(new GetAppGenerationReportQuery(id), ct).ConfigureAwait(false);
            return report is null ? Results.NotFound() : Results.Ok(report);
        })
        .WithName("GetAutonomousAppGenerationReport");

        group.MapGet("/{id:guid}/manifest", async (Guid id, IMediator mediator, CancellationToken ct) =>
        {
            var manifest = await mediator.Send(new GetAppGenerationManifestQuery(id), ct).ConfigureAwait(false);
            return manifest is null ? Results.NotFound() : Results.Ok(manifest);
        })
        .WithName("GetAutonomousAppGenerationManifest");

        group.MapGet("/{id:guid}/diagnostics", async (Guid id, IMediator mediator, CancellationToken ct) =>
        {
            var diagnostics = await mediator.Send(new GetDiagnosticsBundleQuery(id), ct).ConfigureAwait(false);
            return diagnostics is null ? Results.NotFound() : Results.Ok(diagnostics);
        })
        .WithName("GetAutonomousAppGenerationDiagnosticsBundle");

        group.MapGet("/{id:guid}/diagnostics/export", async (Guid id, IMediator mediator, CancellationToken ct) =>
        {
            var export = await mediator.Send(new ExportDiagnosticsPackageQuery(id), ct).ConfigureAwait(false);
            return export is null ? Results.NotFound() : Results.Ok(export);
        })
        .WithName("ExportAutonomousAppGenerationDiagnosticsPackage");

        group.MapGet("/dashboard/benchmark", async (
            [FromQuery] int? limit,
            IMediator mediator,
            CancellationToken ct) =>
        {
            var dashboard = await mediator.Send(new GetBenchmarkDashboardQuery(limit ?? 20), ct).ConfigureAwait(false);
            return Results.Ok(dashboard);
        })
        .WithName("GetAutonomousBenchmarkDashboard");

        group.MapGet("/dashboard/benchmark/export", async (
            [FromQuery] int? limit,
            IMediator mediator,
            CancellationToken ct) =>
        {
            var export = await mediator.Send(new GetBenchmarkDashboardExportQuery(limit ?? 20), ct).ConfigureAwait(false);
            return Results.Ok(export);
        })
        .WithName("ExportAutonomousBenchmarkDashboard");

        group.MapGet("/dashboard/readiness", async (IMediator mediator, CancellationToken ct) =>
        {
            var readiness = await mediator.Send(new GetStageCReadinessQuery(), ct).ConfigureAwait(false);
            return Results.Ok(readiness);
        })
        .WithName("GetAutonomousStageCReadiness");

        group.MapGet("/runtime/diagnostics", (IRuntimeDiagnostics diagnostics) =>
            Results.Ok(diagnostics.GetSnapshot()))
        .WithName("GetAutonomousRuntimeDiagnostics");

        group.MapPost("/{id:guid}/cancel", (
            Guid id,
            [FromBody] CancelRunRequest? request,
            IAutonomousRunControlService runControl) =>
        {
            var ok = runControl.CancelRun(id, request?.Actor, request?.Reason);
            return ok ? Results.Ok(new { runId = id, cancelled = true })
                : Results.NotFound(new { runId = id, cancelled = false, reason = "run_not_active" });
        })
        .WithName("CancelAutonomousAppGenerationRun");

        group.MapPost("/{id:guid}/pause", (Guid id, IAutonomousRunControlService runControl) =>
        {
            var ok = runControl.PauseRun(id);
            return ok ? Results.Ok(new { runId = id, paused = true })
                : Results.NotFound(new { runId = id, paused = false, reason = "run_not_active" });
        })
        .WithName("PauseAutonomousAppGenerationRun");

        group.MapPost("/{id:guid}/resume", (Guid id, IAutonomousRunControlService runControl) =>
        {
            var ok = runControl.ResumeRun(id);
            return ok ? Results.Ok(new { runId = id, resumed = true })
                : Results.NotFound(new { runId = id, resumed = false, reason = "run_not_active" });
        })
        .WithName("ResumeAutonomousAppGenerationRun");

        group.MapGet("/runs/health", (IAutonomousRunControlService runControl) =>
            Results.Ok(runControl.GetHealthSnapshot()))
        .WithName("GetAutonomousRunsHealth");

        group.MapGet("/{id:guid}/state", (Guid id, IAutonomousRunControlService runControl) =>
        {
            var state = runControl.GetRunState(id);
            return state is null
                ? Results.NotFound(new { runId = id, reason = "run_not_active" })
                : Results.Ok(state);
        })
        .WithName("GetAutonomousRunState");
    }
}
