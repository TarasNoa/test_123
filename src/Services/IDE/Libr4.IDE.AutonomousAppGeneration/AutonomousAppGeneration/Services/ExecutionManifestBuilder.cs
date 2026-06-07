using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentIntegration;
using Libr4.IDE.Application.AutonomousAppGeneration.DTOs;
using Libr4.IDE.Domain.AutonomousAppGeneration;

namespace Libr4.IDE.Application.AutonomousAppGeneration.Services;

public interface IExecutionManifestBuilder
{
    Task<ExecutionManifestDto> BuildAndPersistAsync(
        AppGenerationOrchestrator orchestrator,
        CancellationToken ct = default);
}

public sealed class ExecutionManifestBuilder : IExecutionManifestBuilder
{
    private const string SchemaVersion = "1.3.4";
    private readonly string _manifestRoot;
    private readonly IRunQualityAssessmentService _qualityAssessment;
    private readonly IMcpLaneWatchdog _watchdog;
    private readonly ITaskGraphHydrationService _taskGraphHydration;

    public ExecutionManifestBuilder(
        IRunQualityAssessmentService qualityAssessment,
        IMcpLaneWatchdog watchdog,
        ITaskGraphHydrationService taskGraphHydration)
    {
        _qualityAssessment = qualityAssessment;
        _watchdog = watchdog;
        _taskGraphHydration = taskGraphHydration;
        _manifestRoot = Path.Combine(Path.GetTempPath(), "libr4-autogen-manifests");
        Directory.CreateDirectory(_manifestRoot);
    }

    public async Task<ExecutionManifestDto> BuildAndPersistAsync(
        AppGenerationOrchestrator orchestrator,
        CancellationToken ct = default)
    {
        var commandDtos = orchestrator.Iterations
            .Where(i => i.Execution is not null)
            .SelectMany(i => i.Execution!.CommandExecutions)
            .Select(c => new CommandExecutionDto(
                Phase: c.Phase,
                Command: c.Command,
                ExitCode: c.ExitCode,
                DurationMs: (long)c.Duration.TotalMilliseconds,
                RuntimeProvider: c.RuntimeProvider,
                RuntimeSessionId: c.RuntimeSessionId,
                ExecutedAtUtc: c.ExecutedAtUtc))
            .ToList();

        var retryEvents = orchestrator.Iterations
            .SelectMany(i => i.RetryEvents)
            .Select(r => new RetryEventDto(
                Attempt: r.Attempt,
                Reason: r.Reason,
                BackoffMs: r.BackoffMs,
                TimestampUtc: r.TimestampUtc))
            .ToList();

        var qualityGates = orchestrator.QualityGates
            .Select(g => new QualityGateResultDto(
                Stage: g.Stage,
                Score: g.Score,
                Passed: g.Passed,
                Reasons: g.Reasons.ToList(),
                EvaluatedAtUtc: g.EvaluatedAtUtc))
            .ToList();

        var mcpDtos = orchestrator.McpExecutions
            .Select(m => new McpExecutionDto(
                ToolName: m.ToolName,
                ServerName: m.ServerName,
                Lane: m.Lane.ToString(),
                RiskLevel: m.RiskLevel.ToString(),
                ArgumentsSha256: m.ArgumentsSha256,
                StartedAtUtc: m.StartedAtUtc,
                DurationMs: m.DurationMs,
                Outcome: m.Outcome,
                Detail: m.Detail))
            .ToList();

        var memoryDtos = orchestrator.MemoryIngests
            .Select(m => new MemoryIngestDto(
                RunId: m.RunId,
                Stage: m.Stage,
                Kind: m.Kind.ToString(),
                Key: m.Key,
                Summary: m.Summary,
                TokenEstimate: m.TokenEstimate,
                StoredAtUtc: m.StoredAtUtc))
            .ToList();

        var memoryRetrievalDtos = orchestrator.MemoryRetrievals
            .Select(r => new MemoryRetrievalDto(
                RunId: r.RunId,
                Stage: r.Stage,
                Kind: r.Kind.ToString(),
                Key: r.Key,
                Summary: r.Summary,
                RetrievalReason: r.RetrievalReason,
                RelevanceScore: r.RelevanceScore,
                RetrievedAtUtc: r.RetrievedAtUtc))
            .ToList();

        var skillDtos = orchestrator.SkillInvocations
            .Select(s => new SkillInvocationDto(
                SkillId: s.SkillId,
                Version: s.Version,
                Stage: s.Stage,
                SafetyLabel: s.SafetyLabel,
                StartedAtUtc: s.StartedAtUtc,
                DurationMs: s.DurationMs,
                Outcome: s.Outcome,
                Detail: s.Detail))
            .ToList();

        _taskGraphHydration.EnsureHydrated(orchestrator);
        var resolvedGraph = _taskGraphHydration.Resolve(orchestrator);
        if (orchestrator.TaskGraph.Count == 0 && resolvedGraph.Count > 0)
            orchestrator.ReplaceTaskGraph(resolvedGraph);

        var taskGraphDtos = orchestrator.TaskGraph
            .Select(t => new TaskGraphEntryDto(
                TaskId: t.TaskId,
                Title: t.Title,
                BlockedByTaskIds: t.BlockedByTaskIds.ToList(),
                State: t.State.ToString(),
                EvidencePaths: t.EvidencePaths.ToList(),
                Notes: t.Notes))
            .ToList();

        var securityDtos = orchestrator.SecurityReviews
            .Select(s => new SecurityReviewDto(
                Stage: s.Stage,
                Score: s.Score,
                Passed: s.Passed,
                Reasons: s.Reasons.ToList(),
                RemediationHints: s.RemediationHints.ToList(),
                EvaluatedAtUtc: s.EvaluatedAtUtc))
            .ToList();

        var cascadePlan = orchestrator.CascadePlans
            .OrderByDescending(c => c.CreatedAtUtc)
            .Select(c => new CascadePlanTraceDto(
                Rationale: c.Rationale,
                SerializedPlanJson: c.SerializedPlanJson,
                PhaseCount: c.PhaseCount,
                RoutingProfile: c.RoutingProfile,
                ModelHint: c.ModelHint,
                PlannerMode: c.PlannerMode,
                CreatedAtUtc: c.CreatedAtUtc))
            .FirstOrDefault();
        var checkpoints = orchestrator.Checkpoints
            .Select(c => new CheckpointAuditDto(
                CheckpointId: c.CheckpointId,
                Label: c.Label,
                Action: c.Action,
                FileCount: c.FileCount,
                ChangedFiles: c.ChangedFiles,
                Detail: c.Detail,
                CreatedAtUtc: c.CreatedAtUtc))
            .ToList();
        var triggers = orchestrator.Triggers
            .Select(t => new TriggerIngestionDto(
                Source: t.Source,
                AdapterName: t.AdapterName,
                NormalizedRequest: t.NormalizedRequest,
                Actor: t.Actor,
                CorrelationId: t.CorrelationId,
                ReceivedAtUtc: t.ReceivedAtUtc))
            .ToList();

        var benchmarkSummary = new BenchmarkSummaryDto(
            TotalQualityEvaluations: qualityGates.Count,
            TotalFailedEvaluations: qualityGates.Count(g => !g.Passed),
            TotalCommandDurationMs: commandDtos.Sum(c => c.DurationMs),
            AvgCommandDurationMs: commandDtos.Count > 0 ? commandDtos.Sum(c => c.DurationMs) / commandDtos.Count : 0,
            TopFailureReasons: qualityGates
                .Where(g => !g.Passed)
                .SelectMany(g => g.Reasons)
                .GroupBy(r => r)
                .OrderByDescending(g => g.Count())
                .Take(5)
                .Select(g => g.Key)
                .ToList(),
            Stages: qualityGates
                .GroupBy(g => g.Stage)
                .Select(g => new BenchmarkStageSummaryDto(
                    g.Key,
                    g.Count(),
                    g.Count(x => x.Passed),
                    g.Count(x => !x.Passed),
                    (int)g.Average(x => x.Score),
                    0))
                .ToList());

        var qualityAssessment = _qualityAssessment.Assess(orchestrator);
        var remediationHints = BuildRunRemediationHints(orchestrator);

        var recoveryTrace = orchestrator.RecoveryEfficiencyRecords
            .Select(r => new RecoveryTraceDto(
                StrategyName: r.Mechanism.ToString(),
                Reason: $"{r.PrimaryErrorClass}:{r.RootCauseCategory}",
                TimestampUtc: r.AttemptedAtUtc,
                DurationMs: r.RepairDurationMs ?? 0,
                Success: r.BuildSucceededAfterRepair == true,
                ContextSnapshot: $"patches={r.PatchesApplied};sig={r.ErrorSignature ?? "n/a"}"))
            .ToList();

        // Perform watchdog check to get latest status
        _watchdog.PerformWatchdogCheck();
        var watchdogSnapshot = _watchdog.GetSnapshot()
            .Select(s => new McpLaneWatchdogSnapshotDto(
                ProfileKey: s.ProfileKey,
                Lane: s.Lane,
                LastCheckTimeUtc: s.LastCheckTimeUtc,
                Status: s.Status,
                BlockerCode: s.BlockerCode,
                DiagnosticMessage: s.DiagnosticMessage,
                History: _watchdog.GetHistory(s.ProfileKey)
                    .Select(h => new McpLaneWatchdogHistoryEntryDto(
                        CheckTimeUtc: h.CheckTimeUtc,
                        Status: h.Status,
                        BlockerCode: h.BlockerCode))
                    .ToList()))
            .ToList();

        var manifestId = $"manifest-{orchestrator.Id:N}";
        var preimage = BuildHashPreimage(
            orchestrator,
            commandDtos,
            retryEvents,
            qualityGates,
            qualityAssessment,
            mcpDtos,
            memoryDtos,
            memoryRetrievalDtos,
            skillDtos,
            taskGraphDtos,
            securityDtos,
            cascadePlan,
            checkpoints,
            triggers,
            benchmarkSummary,
            watchdogSnapshot,
            remediationHints,
            recoveryTrace);
        var hash = ComputeSha256(preimage);

        var manifest = new ExecutionManifestDto(
            SchemaVersion: SchemaVersion,
            ManifestId: manifestId,
            ContentSha256: hash,
            ArtifactPath: null,
            OrchestratorId: orchestrator.Id,
            UserRequest: orchestrator.UserRequest,
            RequestFingerprint: orchestrator.RequestFingerprint,
            FinalStatus: orchestrator.Status.ToString(),
            GeneratedAtUtc: DateTime.UtcNow,
            IterationCount: orchestrator.Iterations.Count,
            TotalCommands: commandDtos.Count,
            TotalRetries: retryEvents.Count,
            RetryEvents: retryEvents,
            QualityGates: qualityGates,
            QualityAssessment: qualityAssessment,
            Commands: commandDtos,
            McpExecutions: mcpDtos,
            MemoryIngests: memoryDtos,
            MemoryRetrievals: memoryRetrievalDtos,
            SkillInvocations: skillDtos,
            TaskGraph: taskGraphDtos,
            SecurityReviews: securityDtos,
            CascadePlan: cascadePlan,
            Checkpoints: checkpoints,
            Triggers: triggers,
            BenchmarkSummary: benchmarkSummary,
            McpLaneWatchdogSnapshots: watchdogSnapshot,
            RunRemediationHints: remediationHints,
            RecoveryTrace: recoveryTrace);

        var artifactPath = Path.Combine(_manifestRoot, $"{orchestrator.Id:N}.manifest.json");
        var json = JsonSerializer.Serialize(manifest, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(artifactPath, json, Encoding.UTF8, ct);

        return manifest with { ArtifactPath = artifactPath };
    }

    private static IReadOnlyList<string> BuildRunRemediationHints(AppGenerationOrchestrator orchestrator)
    {
        var list = new List<string>();
        foreach (var g in orchestrator.QualityGates.Where(x => !x.Passed))
        {
            foreach (var reason in g.Reasons)
                list.Add(FormatGateRemediation(g.Stage, reason));
        }

        foreach (var s in orchestrator.SecurityReviews.Where(x => !x.Passed))
            list.AddRange(s.RemediationHints);

        return list
            .Where(h => !string.IsNullOrWhiteSpace(h))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(48)
            .ToList();
    }

    private static string FormatGateRemediation(string stage, string reason)
    {
        var r = reason.ToLowerInvariant();
        var hint = r switch
        {
            _ when r.Contains("build_failed") || r.Contains("build_non_zero") =>
                "Fix build: read shadow/build logs, add missing deps, resolve compile errors; verify stack (pip vs dotnet) matches plan.",
            _ when r.Contains("missing_entrypoint") =>
                "Add stack entrypoint (e.g. Program.cs, app.py/main.py, index.js).",
            _ when r.Contains("missing_controllers") || r.Contains("missing_routes") =>
                "Add HTTP routing (controllers, minimal APIs, Flask/FastAPI routes).",
            _ when r.Contains("intent_") =>
                "Align code with plan keywords (auth/API/domain) or adjust plan; check intent heuristics.",
            _ when r.Contains("test") && r.Contains("fail") =>
                "Stabilize tests: fix assertions, fixtures, or production code to match expected behavior.",
            _ when r.Contains("consistency") =>
                "Resolve consistency validator findings: paths, stack, and manifest coherence.",
            _ when r.Contains("generation") && r.Contains("few") =>
                "Increase file coverage: entrypoint, routes, services, tests, manifests.",
            _ => $"Review gate '{stage}' reason: {reason}"
        };
        return $"[{stage}] {hint}";
    }

    private static string BuildHashPreimage(
        AppGenerationOrchestrator orchestrator,
        IReadOnlyList<CommandExecutionDto> commands,
        IReadOnlyList<RetryEventDto> retries,
        IReadOnlyList<QualityGateResultDto> qualityGates,
        RunQualityAssessmentDto qualityAssessment,
        IReadOnlyList<McpExecutionDto> mcp,
        IReadOnlyList<MemoryIngestDto> memory,
        IReadOnlyList<MemoryRetrievalDto> memoryRetrievals,
        IReadOnlyList<SkillInvocationDto> skills,
        IReadOnlyList<TaskGraphEntryDto> tasks,
        IReadOnlyList<SecurityReviewDto> security,
        CascadePlanTraceDto? cascadePlan,
        IReadOnlyList<CheckpointAuditDto> checkpoints,
        IReadOnlyList<TriggerIngestionDto> triggers,
        BenchmarkSummaryDto benchmarkSummary,
        IReadOnlyList<McpLaneWatchdogSnapshotDto> watchdogSnapshot,
        IReadOnlyList<string> remediationHints,
        IReadOnlyList<RecoveryTraceDto> recoveryTrace)
    {
        var sb = new StringBuilder();
        sb.Append(SchemaVersion).Append('|')
          .Append(orchestrator.Id).Append('|')
          .Append(orchestrator.Status).Append('|')
          .Append(orchestrator.StartedAt.ToUniversalTime().ToString("O")).Append('|')
          .Append(orchestrator.CompletedAt?.ToUniversalTime().ToString("O") ?? string.Empty).Append('|')
          .Append(orchestrator.Iterations.Count).Append('|');

        foreach (var c in commands)
        {
            sb.Append(c.Phase).Append('|')
              .Append(c.Command).Append('|')
              .Append(c.ExitCode).Append('|')
              .Append(c.DurationMs).Append('|')
              .Append(c.RuntimeProvider).Append('|')
              .Append(c.RuntimeSessionId).Append('|')
              .Append(c.ExecutedAtUtc.ToUniversalTime().ToString("O")).Append('|');
        }

        foreach (var r in retries)
        {
            sb.Append(r.Attempt).Append('|')
              .Append(r.Reason).Append('|')
              .Append(r.BackoffMs).Append('|')
              .Append(r.TimestampUtc.ToUniversalTime().ToString("O")).Append('|');
        }

        foreach (var g in qualityGates)
        {
            sb.Append(g.Stage).Append('|')
              .Append(g.Score).Append('|')
              .Append(g.Passed).Append('|')
              .Append(string.Join(",", g.Reasons)).Append('|')
              .Append(g.EvaluatedAtUtc.ToUniversalTime().ToString("O")).Append('|');
        }

        sb.Append(qualityAssessment.OverallScore).Append('|')
          .Append(qualityAssessment.Verdict).Append('|');

        foreach (var s in qualityAssessment.StageScores)
        {
            sb.Append(s.Stage).Append('|')
              .Append(s.LatestScore).Append('|')
              .Append(s.AverageScore).Append('|')
              .Append(s.Evaluations).Append('|')
              .Append(s.LastPassed).Append('|');
        }

        foreach (var x in mcp)
        {
            sb.Append(x.ToolName).Append('|').Append(x.Outcome).Append('|')
              .Append(x.ArgumentsSha256).Append('|');
        }

        foreach (var x in memory)
        {
            sb.Append(x.Stage).Append('|').Append(x.Key).Append('|').Append(x.Summary).Append('|');
        }

        foreach (var x in skills)
        {
            sb.Append(x.SkillId).Append('|').Append(x.Stage).Append('|').Append(x.Outcome).Append('|');
        }

        foreach (var x in tasks)
        {
            sb.Append(x.TaskId).Append('|').Append(x.State).Append('|')
              .Append(string.Join(",", x.BlockedByTaskIds)).Append('|');
        }

        foreach (var x in security)
        {
            sb.Append(x.Stage).Append('|').Append(x.Score).Append('|').Append(x.Passed).Append('|')
              .Append(string.Join(",", x.Reasons)).Append('|');
        }

        if (cascadePlan is not null)
        {
            sb.Append(cascadePlan.PhaseCount).Append('|')
              .Append(cascadePlan.Rationale).Append('|')
              .Append(cascadePlan.SerializedPlanJson).Append('|')
              .Append(cascadePlan.CreatedAtUtc.ToUniversalTime().ToString("O")).Append('|');
        }

        foreach (var c in checkpoints)
        {
            sb.Append(c.CheckpointId).Append('|')
              .Append(c.Label).Append('|')
              .Append(c.Action).Append('|')
              .Append(c.FileCount).Append('|')
              .Append(c.ChangedFiles).Append('|')
              .Append(c.Detail ?? string.Empty).Append('|')
              .Append(c.CreatedAtUtc.ToUniversalTime().ToString("O")).Append('|');
        }

        foreach (var t in triggers)
        {
            sb.Append(t.Source).Append('|')
              .Append(t.AdapterName).Append('|')
              .Append(t.NormalizedRequest).Append('|')
              .Append(t.Actor ?? string.Empty).Append('|')
              .Append(t.CorrelationId ?? string.Empty).Append('|')
              .Append(t.ReceivedAtUtc.ToUniversalTime().ToString("O")).Append('|');
        }

        foreach (var r in recoveryTrace)
        {
            sb.Append(r.StrategyName).Append('|')
              .Append(r.Reason).Append('|')
              .Append(r.TimestampUtc.ToUniversalTime().ToString("O")).Append('|')
              .Append(r.DurationMs).Append('|')
              .Append(r.Success).Append('|')
              .Append(r.ContextSnapshot ?? string.Empty).Append('|');
        }

        sb.Append(benchmarkSummary.TotalQualityEvaluations).Append('|')
          .Append(benchmarkSummary.TotalFailedEvaluations).Append('|')
          .Append(benchmarkSummary.TotalCommandDurationMs).Append('|')
          .Append(benchmarkSummary.AvgCommandDurationMs).Append('|')
          .Append(string.Join(",", benchmarkSummary.TopFailureReasons)).Append('|');

        foreach (var s in benchmarkSummary.Stages)
        {
            sb.Append(s.Stage).Append('|')
              .Append(s.Evaluations).Append('|')
              .Append(s.Passed).Append('|')
              .Append(s.Failed).Append('|')
              .Append(s.AvgScore).Append('|')
              .Append(s.AvgDurationMs).Append('|');
        }

        foreach (var h in remediationHints)
            sb.Append(h).Append('|');

        foreach (var w in watchdogSnapshot)
            sb.Append(w.ProfileKey).Append('|')
              .Append(w.Lane).Append('|')
              .Append(w.Status).Append('|')
              .Append(w.BlockerCode ?? string.Empty).Append('|')
              .Append(w.DiagnosticMessage ?? string.Empty).Append('|');

        return sb.ToString();
    }

    private static string ComputeSha256(string input)
    {
        var bytes = Encoding.UTF8.GetBytes(input);
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
