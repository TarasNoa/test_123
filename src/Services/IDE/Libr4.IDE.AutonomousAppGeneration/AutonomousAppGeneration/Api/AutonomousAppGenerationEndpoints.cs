using Libr4.IDE.Application.AutonomousAppGeneration.AgentBackends;
using Libr4.IDE.Application.AutonomousAppGeneration.HostProfiles;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentIntegration;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Api;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Delegation;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Permissions;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Rollout;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.DMail;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.SlashCommands;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Subagents;
using Libr4.IDE.Application.AutonomousAppGeneration.Tooling.Flow;
using Libr4.IDE.Application.AutonomousAppGeneration.WorkspaceTrust;
using Libr4.IDE.Application.AutonomousAppGeneration.BatchCi;
using Libr4.IDE.Application.AutonomousAppGeneration.Scheduling;
using Libr4.IDE.Application.AutonomousAppGeneration.Extensions;
using Libr4.IDE.Application.AutonomousAppGeneration.GitHubActionsDispatch;
using Libr4.IDE.Application.AutonomousAppGeneration.Memory.Honcho;
using Libr4.IDE.Application.AutonomousAppGeneration.MetaAgent;
using Libr4.IDE.Application.AutonomousAppGeneration.FineTuning;
using Libr4.IDE.Application.AutonomousAppGeneration.Evaluation;
using Libr4.IDE.Application.AutonomousAppGeneration.InlineCompletion;
using Libr4.IDE.Application.AutonomousAppGeneration.LiveSearch;
using Libr4.IDE.Application.AutonomousAppGeneration.ModelRouting;
using Libr4.IDE.Application.AutonomousAppGeneration.Commands;
using Libr4.IDE.Application.AutonomousAppGeneration.DiffReview;
using Libr4.IDE.Application.AutonomousAppGeneration.RunHandoff;
using Libr4.IDE.Application.AutonomousAppGeneration.Queries;
using Libr4.IDE.Application.AutonomousAppGeneration.Runtime;
using Libr4.IDE.Application.AutonomousAppGeneration.Services;
using Libr4.IDE.Application.AutonomousAppGeneration.Services.Pipeline;
using Libr4.IDE.Application.AutonomousAppGeneration.Verify;
using Libr4.IDE.Domain.AutonomousAppGeneration;
using Microsoft.Extensions.Options;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace Libr4.IDE.Application.AutonomousAppGeneration.Api;

public sealed record CancelRunRequest(string? Actor, string? Reason);

public sealed record PermissionModeRequest(string Mode);

public sealed record PermissionResolveRequest(string PromptId, bool Accepted);

public sealed record WorkspaceTrustResolveRequestDto(
    string PromptId,
    string SandboxPolicy,
    string HostMode,
    bool RememberChoice);

public sealed record SlashCommandRequest(string Command);

public sealed record ApproveAgentSpecProposalRequest(string? Actor);

public sealed record RejectAgentSpecProposalRequest(string? Actor, string? Reason);

public sealed record InternalEvalRunRequest(Dictionary<string, string>? Candidates);

public sealed record LiveSearchWebRequest(string Query, string? SessionKey = null, int? MaxResults = null);

public sealed record InlineCompletionApiRequest(
    string FilePath,
    string Language,
    string FileContent,
    int Line,
    int Column,
    string? SessionIntent = null,
    Guid? RunId = null,
    bool SuppressWhileAgentRunning = false);

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
            if (string.IsNullOrWhiteSpace(command.UserRequest)
                && command.ResumeFromRunId is null
                && string.IsNullOrWhiteSpace(command.ResumeSeedPath))
                return Results.BadRequest(new { error = "userRequest is required (or set resumeFromRunId / resumeSeedPath)" });

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

        group.MapGet("/{id:guid}/dashboard/build", async (
            Guid id,
            string? stackFilter,
            IMediator mediator,
            CancellationToken ct) =>
        {
            var dashboard = await mediator.Send(
                new GetBuildDiagnosticsDashboardQuery(id, stackFilter),
                ct).ConfigureAwait(false);
            return dashboard is null ? Results.NotFound() : Results.Ok(dashboard);
        })
        .WithName("GetAutonomousBuildDiagnosticsDashboard");

        group.MapGet("/{id:guid}/verify/artifacts/{fileName}", (Guid id, string fileName, IVerifyEvidenceStore evidenceStore) =>
        {
            var artifact = evidenceStore.TryGet(id, fileName);
            return artifact is null
                ? Results.NotFound()
                : Results.File(artifact.AbsolutePath, artifact.ContentType ?? "application/octet-stream", artifact.FileName);
        })
        .WithName("GetAutonomousVerifyArtifact");

        group.MapGet("/{id:guid}/obscura/artifacts/{fileName}", (Guid id, string fileName, Libr4.IDE.Application.Obscura.IObscuraEvidenceStore evidenceStore) =>
        {
            var artifact = evidenceStore.TryGet(id, fileName);
            return artifact is null
                ? Results.NotFound()
                : Results.File(artifact.AbsolutePath, artifact.ContentType ?? "application/octet-stream", artifact.FileName);
        })
        .WithName("GetAutonomousObscuraArtifact");

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

        group.MapGet("/{id:guid}/permission-mode", (Guid id, IAgentRunPermissionStore store) =>
            Results.Ok(new
            {
                runId = id,
                mode = store.Get(id).ToString(),
                pendingPrompts = store.GetPendingPrompts(id)
            }))
        .WithName("GetAutonomousRunPermissionMode");

        group.MapPost("/{id:guid}/permission-mode/resolve", (
            Guid id,
            [FromBody] PermissionResolveRequest request,
            IAgentRunPermissionStore store) =>
        {
            if (string.IsNullOrWhiteSpace(request.PromptId))
                return Results.BadRequest(new { error = "promptId is required" });

            store.ResolvePrompt(id, request.PromptId, request.Accepted);
            return Results.Ok(new { runId = id, promptId = request.PromptId, accepted = request.Accepted });
        })
        .WithName("ResolveAutonomousRunPermissionPrompt");

        group.MapPatch("/{id:guid}/permission-mode", (
            Guid id,
            [FromBody] PermissionModeRequest request,
            IAgentRunPermissionStore store) =>
        {
            if (!Enum.TryParse<AgentPermissionMode>(request.Mode, ignoreCase: true, out var mode))
                return Results.BadRequest(new { error = $"invalid mode: {request.Mode}" });

            store.Set(id, mode);
            return Results.Ok(new { runId = id, mode = mode.ToString() });
        })
        .WithName("PatchAutonomousRunPermissionMode");

        group.MapGet("/{id:guid}/workspace-trust", (Guid id, IWorkspaceTrustRunGate gate) =>
        {
            var state = gate.GetRunState(id);
            return state is null
                ? Results.NotFound(new { runId = id, reason = "workspace_trust_not_started" })
                : Results.Ok(state);
        })
        .WithName("GetAutonomousRunWorkspaceTrust");

        group.MapPost("/{id:guid}/workspace-trust/resolve", async (
            Guid id,
            [FromBody] WorkspaceTrustResolveRequestDto request,
            IWorkspaceTrustRunGate gate,
            CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(request.PromptId))
                return Results.BadRequest(new { error = "promptId is required" });
            if (!Enum.TryParse<WorkspaceSandboxPolicy>(request.SandboxPolicy, ignoreCase: true, out var sandbox))
                return Results.BadRequest(new { error = $"invalid sandboxPolicy: {request.SandboxPolicy}" });
            if (!Enum.TryParse<WorkspaceHostMode>(request.HostMode, ignoreCase: true, out var host))
                return Results.BadRequest(new { error = $"invalid hostMode: {request.HostMode}" });

            try
            {
                await gate.ResolveAsync(
                        id,
                        new WorkspaceTrustResolveRequest(
                            request.PromptId,
                            sandbox,
                            host,
                            request.RememberChoice),
                        ct)
                    .ConfigureAwait(false);
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }

            return Results.Ok(gate.GetRunState(id));
        })
        .WithName("ResolveAutonomousRunWorkspaceTrust");

        group.MapGet("/benchmark/regression/scenarios", (IBenchmarkRegressionHarness harness) =>
            Results.Ok(harness.GetNightlyScenarios()))
        .WithName("GetBenchmarkRegressionScenarios");

        group.MapGet("/benchmark/regression/evaluate/{id:guid}", async (
            Guid id,
            string? scenarioId,
            IBenchmarkRegressionHarness harness,
            IAppGenerationRepository repository,
            CancellationToken ct) =>
        {
            var orchestrator = await repository.GetAsync(id, ct).ConfigureAwait(false);
            if (orchestrator is null)
                return Results.NotFound(new { runId = id });

            var scenario = harness.GetNightlyScenarios()
                .FirstOrDefault(s => string.Equals(s.Id, scenarioId, StringComparison.OrdinalIgnoreCase))
                ?? harness.GetNightlyScenarios().FirstOrDefault();

            if (scenario is null)
                return Results.NotFound(new { error = "no_regression_scenarios_configured" });

            return Results.Ok(harness.EvaluateScenario(scenario, orchestrator));
        })
        .WithName("EvaluateBenchmarkRegressionRun");

        group.MapGet("/schedules", async (IScheduledAgentRunService schedules, CancellationToken ct) =>
            Results.Ok(await schedules.ListAsync(ct).ConfigureAwait(false)))
        .WithName("ListScheduledAgentRuns");

        group.MapPost("/schedules/{scheduleId}/run", async (
            string scheduleId,
            IScheduledAgentRunService schedules,
            CancellationToken ct) =>
            Results.Ok(await schedules.ExecuteAsync(scheduleId, ct).ConfigureAwait(false)))
        .WithName("TriggerScheduledAgentRun");

        group.MapGet("/extensions", async (IExtensionHost host, CancellationToken ct) =>
        {
            await host.RefreshAsync(ct: ct).ConfigureAwait(false);
            return Results.Ok(host.Extensions.Select(e => new
            {
                id = e.Id,
                name = e.Manifest.Name,
                version = e.Manifest.Version,
                description = e.Manifest.Description,
                source = e.Source.ToString(),
                hooks = e.Manifest.Hooks.Count,
                tools = e.Manifest.Tools.Select(t => t.Name).ToArray(),
                skills = e.Manifest.Skills.Select(s => s.Id).ToArray()
            }));
        })
        .WithName("ListAutonomousExtensions");

        group.MapPost("/extensions/refresh", async (IExtensionHost host, CancellationToken ct) =>
        {
            await host.RefreshAsync(ct: ct).ConfigureAwait(false);
            return Results.Ok(new { count = host.Extensions.Count });
        })
        .WithName("RefreshAutonomousExtensions");

        group.MapPost("/{id:guid}/github/ship", async (
            Guid id,
            IAppGenerationRepository repository,
            IGitHubShipService ship,
            CancellationToken ct) =>
        {
            var orchestrator = await repository.GetAsync(id, ct).ConfigureAwait(false);
            if (orchestrator is null)
                return Results.NotFound(new { runId = id });

            var context = new GenerationContext
            {
                Orchestrator = orchestrator,
                UserRequest = orchestrator.UserRequest,
                Plan = orchestrator.Plan
            };
            context.Files.AddRange(orchestrator.Files);

            var verifyGate = orchestrator.QualityGates
                .LastOrDefault(g => g.Stage.Equals("verify_subagent", StringComparison.OrdinalIgnoreCase));
            if (verifyGate is not null)
                context.Items["verify_passed"] = verifyGate.Passed;

            var result = await ship.ShipAsync(context, ct).ConfigureAwait(false);
            return Results.Ok(result);
        })
        .WithName("TriggerGitHubShipForRun");

        group.MapGet("/honcho/persona", async (
            string userId,
            string projectKey,
            IHonchoMemoryService honcho,
            CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(projectKey))
                return Results.BadRequest(new { error = "userId_and_projectKey_required" });

            var persona = await honcho.GetPersonaAsync(userId, projectKey, ct).ConfigureAwait(false);
            return persona is null ? Results.NotFound(new { userId, projectKey }) : Results.Ok(persona);
        })
        .WithName("GetHonchoPersona");

        group.MapGet("/meta-agent/proposals", async (
            IAgentSpecEvolutionService evolution,
            string? status,
            CancellationToken ct) =>
        {
            AgentSpecProposalStatus? parsed = null;
            if (!string.IsNullOrWhiteSpace(status)
                && Enum.TryParse<AgentSpecProposalStatus>(status, ignoreCase: true, out var s))
                parsed = s;

            var proposals = await evolution.ListProposalsAsync(parsed, ct).ConfigureAwait(false);
            return Results.Ok(proposals.Select(p => new
            {
                id = p.Id,
                runId = p.RunId,
                specName = p.SpecName,
                diff = p.Diff,
                rationale = p.Rationale,
                status = p.Status.ToString(),
                createdAtUtc = p.CreatedAtUtc,
                resolvedAtUtc = p.ResolvedAtUtc,
                resolvedBy = p.ResolvedBy,
                rejectionReason = p.RejectionReason,
                appliedVersion = p.AppliedVersion
            }));
        })
        .WithName("ListAgentSpecProposals");

        group.MapPost("/meta-agent/proposals/{proposalId:guid}/approve", async (
            Guid proposalId,
            IAgentSpecEvolutionService evolution,
            ApproveAgentSpecProposalRequest? body,
            CancellationToken ct) =>
        {
            try
            {
                var result = await evolution.ApproveAsync(proposalId, body?.Actor, ct).ConfigureAwait(false);
                return Results.Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        })
        .WithName("ApproveAgentSpecProposal");

        group.MapPost("/meta-agent/proposals/{proposalId:guid}/reject", async (
            Guid proposalId,
            IAgentSpecEvolutionService evolution,
            RejectAgentSpecProposalRequest? body,
            CancellationToken ct) =>
        {
            try
            {
                await evolution.RejectAsync(proposalId, body?.Actor, body?.Reason, ct).ConfigureAwait(false);
                return Results.Ok(new { proposalId, status = AgentSpecProposalStatus.Rejected.ToString() });
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        })
        .WithName("RejectAgentSpecProposal");

        group.MapGet("/meta-agent/specs/{specName}/changelog", async (
            string specName,
            IAgentSpecVersionStore versions,
            CancellationToken ct) =>
        {
            var changelog = await versions.GetChangelogAsync(specName, ct).ConfigureAwait(false);
            return Results.Ok(changelog);
        })
        .WithName("GetAgentSpecChangelog");

        group.MapPost("/{id:guid}/fine-tuning/export", async (
            Guid id,
            IAppGenerationRepository repository,
            IFineTuningDataPipelineService pipeline,
            CancellationToken ct) =>
        {
            var orchestrator = await repository.GetAsync(id, ct).ConfigureAwait(false);
            if (orchestrator is null)
                return Results.NotFound(new { runId = id });

            var result = await pipeline.ExportRunAsync(orchestrator, ct).ConfigureAwait(false);
            return Results.Ok(result);
        })
        .WithName("ExportFineTuningExampleForRun");

        group.MapPost("/fine-tuning/build-dataset", async (
            IAppGenerationRepository repository,
            IFineTuningDataPipelineService pipeline,
            CancellationToken ct) =>
        {
            var runs = await repository.ListAsync(ct).ConfigureAwait(false);
            var completed = runs.Where(r => r.Status == GenerationStatus.Completed).ToList();
            var result = await pipeline.BuildDatasetAsync(completed, ct).ConfigureAwait(false);
            return Results.Ok(result);
        })
        .WithName("BuildFineTuningDataset");

        group.MapGet("/evaluation/benchmarks", (IInternalEvalHarness harness) =>
        {
            var benchmarks = harness.LoadBenchmarks();
            return Results.Ok(benchmarks.Select(b => new
            {
                id = b.Id,
                stack = b.Stack,
                style = b.Style,
                prompt = b.Prompt
            }));
        })
        .WithName("ListInternalEvalBenchmarks");

        group.MapPost("/evaluation/run", (IInternalEvalHarness harness, InternalEvalRunRequest? body) =>
        {
            var report = harness.RunSuite(body?.Candidates);
            return Results.Ok(report);
        })
        .WithName("RunInternalEvalSuite");

        group.MapPost("/evaluation/regression-gate", (IInternalEvalHarness harness) =>
        {
            var report = harness.RunSuite();
            var gate = harness.CheckRegressionGate(report);
            return gate.Passed
                ? Results.Ok(gate)
                : Results.Json(gate, statusCode: StatusCodes.Status409Conflict);
        })
        .WithName("CheckInternalEvalRegressionGate");

        group.MapPost("/live-search/web", async (
            LiveSearchWebRequest body,
            ILiveSearchService search,
            CancellationToken ct) =>
        {
            try
            {
                var result = await search.SearchWebAsync(
                    new LiveSearchRequest(body.Query, body.SessionKey, body.MaxResults),
                    ct).ConfigureAwait(false);
                return Results.Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        })
        .WithName("LiveSearchWeb");

        group.MapPost("/live-search/x", async (
            LiveSearchWebRequest body,
            ILiveSearchService search,
            CancellationToken ct) =>
        {
            try
            {
                var result = await search.SearchXAsync(
                    new LiveSearchRequest(body.Query, body.SessionKey, body.MaxResults),
                    ct).ConfigureAwait(false);
                return Results.Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        })
        .WithName("LiveSearchX");

        group.MapPost("/inline-complete", async (
            InlineCompletionApiRequest body,
            IInlineCompletionService completion,
            CancellationToken ct) =>
        {
            var result = await completion.CompleteAsync(
                new InlineCompletionRequest(
                    body.FilePath,
                    body.Language,
                    body.FileContent,
                    body.Line,
                    body.Column,
                    body.SessionIntent,
                    RunId: body.RunId,
                    SuppressWhileAgentRunning: body.SuppressWhileAgentRunning),
                ct).ConfigureAwait(false);
            return Results.Ok(result);
        })
        .WithName("InlineComplete");

        group.MapGet("/host-profile", (IAutonomousHostProfileService profiles) =>
        {
            var descriptor = profiles.DescribeActive();
            return Results.Ok(new
            {
                profile = descriptor.Profile.ToString(),
                aiDefaultProvider = descriptor.AiDefaultProvider,
                providerMatrixDefault = descriptor.ProviderMatrixDefault,
                agentModelRoutingProfile = descriptor.AgentModelRoutingProfile,
                batchLlmProfileEnabled = descriptor.BatchLlmProfileEnabled,
                benchmarkModeEnabled = descriptor.BenchmarkModeEnabled,
                gpuThrottleEnabled = descriptor.GpuThrottleEnabled,
                switchHint = descriptor.SwitchHint
            });
        })
        .WithName("GetAutonomousHostProfile");

        group.MapGet("/agent-backends", (IAgentBackendRegistry registry, IOptions<ExternalAgentBackendOptions> options) =>
            Results.Ok(new
            {
                supported = registry.SupportedKinds.Select(k => k.ToString()).ToArray(),
                defaultBackend = AgentBackendKind.Libr4Native.ToString(),
                allowedBackends = options.Value.AllowedBackends.Count == 0
                    ? registry.SupportedKinds.Select(k => k.ToString()).ToArray()
                    : options.Value.AllowedBackends.ToArray(),
                enableNativeFallback = options.Value.EnableNativeFallback
            }))
        .WithName("ListAgentBackends");

        group.MapGet("/agent-models/route", (IAgentModelRouter router, string role) =>
        {
            var decision = router.Route(role);
            return Results.Ok(decision);
        })
        .WithName("GetAgentModelRoute");

        group.MapGet("/{id:guid}/rollout", async (
            Guid id,
            AgentRuntimeStreamService stream,
            CancellationToken ct) =>
        {
            var rollout = await stream.GetRolloutAsync(id, ct).ConfigureAwait(false);
            return rollout.Count == 0 ? Results.NotFound(new { runId = id }) : Results.Ok(rollout);
        })
        .WithName("GetAutonomousRunRollout");

        group.MapGet("/{id:guid}/usage", (
            Guid id,
            IRunUsageRollupService rollup,
            IBudgetService budget) =>
        {
            var usage = rollup.Rollup(id);
            var budgetUsage = budget.GetUsage(id);
            var stageUsage = budget.GetStageUsage(id);
            var backendUsage = budget.GetBackendUsage(id);
            var costUsd = Math.Max(usage.CostUsd, (double)budgetUsage.CostUsdUsed);
            var totalTokens = Math.Max(usage.TotalTokens, budgetUsage.TokensUsed);
            return Results.Ok(new
            {
                runId = id,
                stepCount = usage.StepCount,
                toolCallCount = usage.ToolCallCount,
                inputTokens = usage.InputTokens,
                outputTokens = usage.OutputTokens,
                totalTokens,
                costUsd,
                llmRequestCount = usage.LlmRequestCount,
                lastActivityAtUtc = usage.LastActivityAtUtc,
                lastToolActivityAtUtc = usage.LastToolActivityAtUtc,
                budgetRequests = budgetUsage.RequestsIssued,
                stageUsage = stageUsage.Values.Select(s => new
                {
                    stage = s.Stage,
                    tokensUsed = s.TokensUsed,
                    costUsdUsed = s.CostUsdUsed,
                    requestsIssued = s.RequestsIssued
                }),
                backendUsage = backendUsage.Values.Select(b => new
                {
                    backendKind = b.BackendKind,
                    tokensUsed = b.TokensUsed,
                    costUsdUsed = b.CostUsdUsed,
                    requestsIssued = b.RequestsIssued
                })
            });
        })
        .WithName("GetAutonomousRunUsage");

        group.MapGet("/{id:guid}/provider-costs", (Guid id, IRunProviderCostTracker costTracker) =>
        {
            var entries = costTracker.GetEntries(id);
            var rollup = costTracker.RollupByProvider(id);
            return Results.Ok(new
            {
                runId = id,
                entries = entries.Select(e => new
                {
                    providerId = e.ProviderId,
                    stage = e.Stage,
                    modelId = e.ModelId,
                    tokens = e.Tokens,
                    costUsd = e.CostUsd,
                    recordedAtUtc = e.RecordedAtUtc
                }),
                byProvider = rollup.Values.Select(r => new
                {
                    providerId = r.ProviderId,
                    totalTokens = r.TotalTokens,
                    totalCostUsd = r.TotalCostUsd,
                    requestCount = r.RequestCount
                })
            });
        })
        .WithName("GetAutonomousRunProviderCosts");

        group.MapGet("/{id:guid}/generated-files", async (
            Guid id,
            IAppGenerationRepository repository,
            CancellationToken ct) =>
        {
            var orchestrator = await repository.GetAsync(id, ct).ConfigureAwait(false);
            if (orchestrator is null)
                return Results.NotFound(new { runId = id });

            return Results.Ok(new
            {
                runId = id,
                fileCount = orchestrator.Files.Count,
                files = orchestrator.Files.Select(f => new
                {
                    relativePath = f.RelativePath,
                    language = f.Language,
                    contentLength = f.Content?.Length ?? 0,
                    content = f.Content
                })
            });
        })
        .WithName("GetAutonomousRunGeneratedFiles");

        group.MapGet("/{id:guid}/diffs", async (
            Guid id,
            [FromQuery] string? path,
            [FromQuery] int? step,
            [FromQuery] int? offset,
            [FromQuery] int? limit,
            [FromQuery] string? checkpoint,
            IRunDiffAggregator diffs,
            CancellationToken ct) =>
        {
            var result = await diffs.ListAsync(
                id,
                new RunDiffQuery(path, step, offset ?? 0, limit ?? 50, checkpoint),
                ct).ConfigureAwait(false);
            return Results.Ok(result);
        })
        .WithName("ListAutonomousRunDiffs");

        group.MapGet("/{id:guid}/diffs/checkpoints", async (
            Guid id,
            IRunDiffAggregator diffs,
            CancellationToken ct) =>
        {
            var result = await diffs.ListCheckpointsAsync(id, ct).ConfigureAwait(false);
            return Results.Ok(result);
        })
        .WithName("ListAutonomousRunDiffCheckpoints");

        group.MapGet("/{id:guid}/diffs/detail", async (
            Guid id,
            [FromQuery] string path,
            [FromQuery] string? checkpoint,
            IRunDiffAggregator diffs,
            CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(path))
                return Results.BadRequest(new { error = "path_required" });

            var detail = await diffs.GetDetailAsync(id, path, checkpoint, ct).ConfigureAwait(false);
            return detail is null ? Results.NotFound(new { runId = id, path }) : Results.Ok(detail);
        })
        .WithName("GetAutonomousRunDiffDetail");

        group.MapGet("/{id:guid}/diffs/evidence", async (
            Guid id,
            [FromQuery] string? path,
            IEvidenceDiffCorrelator correlator,
            CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                var overlays = await correlator.GetOverlaysAsync(id, ct).ConfigureAwait(false);
                return Results.Ok(overlays);
            }

            var evidence = await correlator.GetForPathAsync(id, path, ct).ConfigureAwait(false);
            return evidence is null ? Results.NotFound(new { runId = id, path }) : Results.Ok(evidence);
        })
        .WithName("GetAutonomousRunDiffEvidence");

        group.MapPost("/{id:guid}/export", async (
            Guid id,
            IRunExportService export,
            CancellationToken ct) =>
        {
            try
            {
                var result = await export.ExportAsync(id, ct).ConfigureAwait(false);
                return result is null ? Results.NotFound(new { runId = id }) : Results.Ok(result);
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("max bundle size", StringComparison.OrdinalIgnoreCase))
            {
                return Results.BadRequest(new { error = "export_too_large", message = ex.Message });
            }
        })
        .WithName("ExportAutonomousRunPackage");

        group.MapGet("/{id:guid}/export/{exportId}/download", async (
            Guid id,
            string exportId,
            IRunExportService export,
            CancellationToken ct) =>
        {
            var resolved = await export.TryResolveDownloadAsync(id, exportId, ct).ConfigureAwait(false);
            return resolved is null
                ? Results.NotFound(new { runId = id, exportId })
                : Results.File(resolved.Value.Path, "application/gzip", resolved.Value.FileName);
        })
        .WithName("DownloadAutonomousRunExport");

        group.MapPost("/import", async (HttpRequest request, IRunImportService import, CancellationToken ct) =>
        {
            if (!request.HasFormContentType)
                return Results.BadRequest(new { error = "multipart_required" });

            var file = request.Form.Files.FirstOrDefault();
            if (file is null || file.Length == 0)
                return Results.BadRequest(new { error = "bundle_file_required" });

            try
            {
                await using var stream = file.OpenReadStream();
                var result = await import.ImportBundleStreamAsync(stream, file.FileName, ct).ConfigureAwait(false);
                return Results.Ok(result);
            }
            catch (RunImportException ex)
            {
                return Results.BadRequest(new { error = ex.ErrorCode, message = ex.Message });
            }
        })
        .DisableAntiforgery()
        .WithName("ImportAutonomousRunPackage");

        group.MapPost("/{id:guid}/promote-to-cloud", async (
            Guid id,
            IRunPromoteService promote,
            CancellationToken ct) =>
        {
            var result = await promote.PromoteAsync(id, ct).ConfigureAwait(false);
            return result is null ? Results.NotFound(new { runId = id }) : Results.Ok(result);
        })
        .WithName("PromoteAutonomousRunToCloud");

        group.MapGet("/{id:guid}/sync/conflicts", async (
            Guid id,
            IRunSyncCoordinator sync,
            CancellationToken ct) =>
        {
            var conflicts = await sync.GetPendingConflictsAsync(id, ct).ConfigureAwait(false);
            return Results.Ok(new { runId = id, conflicts });
        })
        .WithName("GetRunSyncConflicts");

        group.MapGet("/{id:guid}/review", async (
            Guid id,
            IRunReviewService reviews,
            CancellationToken ct) =>
        {
            var status = await reviews.GetStatusAsync(id, ct).ConfigureAwait(false);
            return Results.Ok(status);
        })
        .WithName("GetAutonomousRunReviewStatus");

        group.MapPost("/{id:guid}/review", async (
            Guid id,
            ReviewSubmissionRequest request,
            IRunReviewService reviews,
            CancellationToken ct) =>
        {
            try
            {
                var status = await reviews.SubmitAsync(id, request, ct).ConfigureAwait(false);
                return Results.Ok(status);
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        })
        .WithName("SubmitAutonomousRunReview");

        group.MapGet("/rollout/search", async (
            [FromQuery] string q,
            [FromQuery] int? limit,
            AgentRuntimeStreamService stream,
            CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(q))
                return Results.BadRequest(new { error = "q is required" });

            var hits = await stream.SearchRolloutAsync(q, limit ?? 25, ct).ConfigureAwait(false);
            return Results.Ok(hits);
        })
        .WithName("SearchAutonomousRunRollout");

        group.MapGet("/{id:guid}/events/stream", async (
            Guid id,
            AgentRuntimeStreamService stream,
            HttpContext http,
            CancellationToken ct) =>
        {
            http.Response.Headers.ContentType = "text/event-stream";
            http.Response.Headers.CacheControl = "no-cache";
            await foreach (var line in stream.StreamEventsAsync(id, ct).ConfigureAwait(false))
            {
                await http.Response.WriteAsync($"data: {line}\n\n", ct).ConfigureAwait(false);
                await http.Response.Body.FlushAsync(ct).ConfigureAwait(false);
            }
        })
        .WithName("StreamAutonomousRunEvents");

        group.MapGet("/{id:guid}/subagents", async (Guid id, ISubagentStore store, CancellationToken ct) =>
        {
            var subagents = await store.ListAsync(id, ct).ConfigureAwait(false);
            return Results.Ok(new { runId = id, subagents });
        })
        .WithName("ListAutonomousRunSubagents");

        group.MapGet("/{id:guid}/delegations", async (Guid id, IDelegationManager delegations, CancellationToken ct) =>
        {
            var items = await delegations.ListAsync(id, ct).ConfigureAwait(false);
            return Results.Ok(new { runId = id, delegations = items });
        })
        .WithName("ListAutonomousRunDelegations");

        group.MapGet("/{id:guid}/flow", async (Guid id, IFlowEngine flow, CancellationToken ct) =>
        {
            var progress = await flow.GetProgressAsync(id, ct).ConfigureAwait(false);
            return progress is null ? Results.NotFound(new { runId = id }) : Results.Ok(progress);
        })
        .WithName("GetAutonomousRunFlowProgress");

        group.MapGet("/slash-commands", (ISlashCommandRegistry registry) =>
            Results.Ok(registry.All))
        .WithName("ListAutonomousSlashCommands");

        group.MapPost("/{id:guid}/slash", (
            Guid id,
            [FromBody] SlashCommandRequest request,
            ISlashCommandRegistry registry,
            IAutonomousRunControlService runControl) =>
        {
            if (string.IsNullOrWhiteSpace(request.Command))
                return Results.BadRequest(new { error = "command is required" });

            var command = request.Command.Trim();
            if (!registry.TryGet(command, out var definition))
                return Results.BadRequest(new { error = $"unknown slash command: {command}" });

            if (definition.RequiresActiveRun && runControl.GetRunState(id) is null)
                return Results.NotFound(new { runId = id, reason = "run_not_active" });

            return Results.Ok(new
            {
                runId = id,
                command = command,
                accepted = true,
                description = definition.Description
            });
        })
        .WithName("ExecuteAutonomousRunSlashCommand");

        group.MapGet("/{id:guid}/dmail", async (Guid id, IDMailBus bus, CancellationToken ct) =>
        {
            var messages = await bus.ReadAsync(id, ct: ct).ConfigureAwait(false);
            return Results.Ok(new { runId = id, messages });
        })
        .WithName("ListAutonomousRunDMail");
    }
}
