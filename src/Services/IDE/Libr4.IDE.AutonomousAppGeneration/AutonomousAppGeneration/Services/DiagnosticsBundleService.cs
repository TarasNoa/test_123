using Libr4.IDE.Application.AutonomousAppGeneration.AgentIntegration;
using Libr4.IDE.Application.AutonomousAppGeneration.DTOs;
using Libr4.IDE.Domain.AutonomousAppGeneration;
using Microsoft.Extensions.Logging;

namespace Libr4.IDE.Application.AutonomousAppGeneration.Services;

/// <summary>
/// Service for generating diagnostics bundles for debugging.
/// </summary>
public sealed class DiagnosticsBundleService : IDiagnosticsBundleService
{
    private readonly IAppGenerationRepository _repository;
    private readonly IExecutionManifestBuilder _manifestBuilder;
    private readonly IMcpLaneWatchdog _watchdog;
    private readonly ILogger<DiagnosticsBundleService> _logger;

    public DiagnosticsBundleService(
        IAppGenerationRepository repository,
        IExecutionManifestBuilder manifestBuilder,
        IMcpLaneWatchdog watchdog,
        ILogger<DiagnosticsBundleService> logger)
    {
        _repository = repository;
        _manifestBuilder = manifestBuilder;
        _watchdog = watchdog;
        _logger = logger;
    }

    public async Task<DiagnosticsBundleDto?> GenerateBundleAsync(Guid orchestratorId, CancellationToken ct = default)
    {
        var orchestrator = await _repository.GetAsync(orchestratorId, ct);
        if (orchestrator is null)
        {
            _logger.LogWarning("Orchestrator {OrchestratorId} not found for diagnostics bundle", orchestratorId);
            return null;
        }

        var bundleId = $"diagnostics-{orchestratorId:N}-{DateTime.UtcNow:yyyyMMddHHmmss}";
        
        var manifest = await _manifestBuilder.BuildAndPersistAsync(orchestrator, ct);
        
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

        var manifestDto = new DiagnosticsManifestDto(
            Status: orchestrator.Status.ToString(),
            FailureReason: orchestrator.FailureReason,
            IterationCount: orchestrator.Iterations.Count,
            FileCount: orchestrator.Files.Count,
            QualityGateCount: orchestrator.QualityGates.Count,
            BenchmarkSummary: manifest.BenchmarkSummary,
            McpLaneDiagnostics: BuildMcpLaneDiagnostics(orchestrator),
            McpLaneWatchdogSnapshot: watchdogSnapshot);

        var logsDto = new DiagnosticsLogsDto(
            SystemLogs: CollectSystemLogs(orchestrator),
            ApplicationLogs: CollectApplicationLogs(orchestrator),
            ErrorLogs: CollectErrorLogs(orchestrator));

        var filesDto = new DiagnosticsFilesDto(
            Files: orchestrator.Files
                .Select(f => new DiagnosticsFileEntryDto(
                    RelativePath: f.RelativePath,
                    Language: f.Language,
                    SizeBytes: f.Content?.Length ?? 0,
                    Content: f.Content ?? string.Empty))
                .ToList());

        return new DiagnosticsBundleDto(
            RunId: orchestrator.Id,
            BundleId: bundleId,
            GeneratedAtUtc: DateTime.UtcNow,
            Manifest: manifestDto,
            Logs: logsDto,
            Files: filesDto);
    }

    private static IReadOnlyList<McpLaneDiagnosticsDto> BuildMcpLaneDiagnostics(AppGenerationOrchestrator orchestrator)
    {
        static bool IsDegradedOutcome(string outcome) =>
            outcome.Equals("mcp_server_missing", StringComparison.OrdinalIgnoreCase)
            || outcome.Equals("mcp_server_unreachable", StringComparison.OrdinalIgnoreCase)
            || outcome.Equals("mcp_server_unavailable", StringComparison.OrdinalIgnoreCase);

        return orchestrator.McpExecutions
            .Where(e => IsDegradedOutcome(e.Outcome))
            .GroupBy(e => e.Lane.ToString(), StringComparer.OrdinalIgnoreCase)
            .Select(g => new McpLaneDiagnosticsDto(
                Lane: g.Key,
                DegradedEvents: g.Count(),
                TopBlockerCodes: g.GroupBy(x => x.Outcome, StringComparer.OrdinalIgnoreCase)
                    .OrderByDescending(x => x.Count())
                    .ThenBy(x => x.Key, StringComparer.OrdinalIgnoreCase)
                    .Take(5)
                    .Select(x => x.Key)
                    .ToList()))
            .OrderBy(x => x.Lane, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static string CollectSystemLogs(AppGenerationOrchestrator orchestrator)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"=== System Logs for Run {orchestrator.Id} ===");
        sb.AppendLine($"StartedAt: {orchestrator.StartedAt:O}");
        sb.AppendLine($"CompletedAt: {orchestrator.CompletedAt:O}");
        sb.AppendLine($"Status: {orchestrator.Status}");
        sb.AppendLine($"UserRequest: {orchestrator.UserRequest}");
        sb.AppendLine($"RequestFingerprint: {orchestrator.RequestFingerprint}");
        sb.AppendLine($"ShadowWorkspaceId: {orchestrator.ShadowWorkspaceId}");
        sb.AppendLine($"MultiAgentOrchestrationId: {orchestrator.MultiAgentOrchestrationId}");
        sb.AppendLine($"Iterations: {orchestrator.Iterations.Count}");
        sb.AppendLine($"Files: {orchestrator.Files.Count}");
        sb.AppendLine($"QualityGates: {orchestrator.QualityGates.Count}");
        sb.AppendLine($"McpExecutions: {orchestrator.McpExecutions.Count}");
        sb.AppendLine($"MemoryIngests: {orchestrator.MemoryIngests.Count}");
        sb.AppendLine($"MemoryRetrievals: {orchestrator.MemoryRetrievals.Count}");
        sb.AppendLine($"SkillInvocations: {orchestrator.SkillInvocations.Count}");
        sb.AppendLine($"SecurityReviews: {orchestrator.SecurityReviews.Count}");
        sb.AppendLine($"CascadePlans: {orchestrator.CascadePlans.Count}");
        sb.AppendLine($"Checkpoints: {orchestrator.Checkpoints.Count}");
        return sb.ToString();
    }

    private static string CollectApplicationLogs(AppGenerationOrchestrator orchestrator)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"=== Application Logs for Run {orchestrator.Id} ===");
        
        foreach (var iteration in orchestrator.Iterations)
        {
            sb.AppendLine($"Iteration {iteration.Number}: Succeeded={iteration.Succeeded}, Errors={iteration.Errors.Count}");
        }

        foreach (var gate in orchestrator.QualityGates)
        {
            sb.AppendLine($"QualityGate {gate.Stage}: Score={gate.Score}, Passed={gate.Passed}");
        }

        return sb.ToString();
    }

    private static string CollectErrorLogs(AppGenerationOrchestrator orchestrator)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"=== Error Logs for Run {orchestrator.Id} ===");
        
        if (!string.IsNullOrEmpty(orchestrator.FailureReason))
        {
            sb.AppendLine($"FailureReason: {orchestrator.FailureReason}");
        }

        foreach (var iteration in orchestrator.Iterations)
        {
            if (iteration.Errors.Count > 0)
            {
                sb.AppendLine($"Iteration {iteration.Number} Errors:");
                foreach (var error in iteration.Errors)
                {
                    sb.AppendLine($"  - {error}");
                }
            }
        }

        foreach (var mcp in orchestrator.McpExecutions)
        {
            if (!string.IsNullOrEmpty(mcp.Outcome) && mcp.Outcome.Contains("error", StringComparison.OrdinalIgnoreCase))
            {
                sb.AppendLine($"MCP Execution Error: {mcp.ToolName} - {mcp.Outcome}");
                if (!string.IsNullOrEmpty(mcp.Detail))
                {
                    sb.AppendLine($"  Detail: {mcp.Detail}");
                }
            }
        }

        return sb.ToString();
    }
}
