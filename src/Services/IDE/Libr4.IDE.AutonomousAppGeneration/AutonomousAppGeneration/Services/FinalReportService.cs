using System.Text.Json;
using System.Text.Json.Serialization;
using Libr4.IDE.Domain.AutonomousAppGeneration;
using Microsoft.Extensions.Logging;

namespace Libr4.IDE.Application.AutonomousAppGeneration.Services;

/// <summary>
/// Service for generating final reports with complete trace linkage and serialization validation.
/// </summary>
public sealed class FinalReportService : IFinalReportService
{
    private readonly ILogger<FinalReportService> _logger;
    private readonly ITaskGraphHydrationService _taskGraphHydration;

    public FinalReportService(
        ILogger<FinalReportService> logger,
        ITaskGraphHydrationService taskGraphHydration)
    {
        _logger = logger;
        _taskGraphHydration = taskGraphHydration;
    }

    public FinalGenerationReport GenerateFinalReport(
        AppGenerationOrchestrator orchestrator,
        string reviewGateVerdict,
        IReadOnlyList<string> artifacts)
    {
        _taskGraphHydration.EnsureHydrated(orchestrator);
        var taskGraph = _taskGraphHydration.Resolve(orchestrator);
        if (orchestrator.TaskGraph.Count == 0 && taskGraph.Count > 0)
            orchestrator.ReplaceTaskGraph(taskGraph);

        // Extract executed skills
        var executedSkills = ExtractExecutedSkills(orchestrator);

        // Extract MCP calls
        var mcpCalls = ExtractMcpCalls(orchestrator);

        // Extract memory hits
        var memoryHits = ExtractMemoryHits(orchestrator);

        // Build trace linkage
        var traceLinkage = BuildTraceLinkage(taskGraph, executedSkills, mcpCalls, memoryHits, reviewGateVerdict);

        var appName = orchestrator.Plan?.ApplicationName ?? "Unknown";
        var isSuccessful = orchestrator.Status == GenerationStatus.Completed;

        var report = new FinalGenerationReport(
            orchestrator.Id.ToString(),
            appName,
            isSuccessful,
            orchestrator.Iterations.Count,
            taskGraph,
            executedSkills,
            mcpCalls,
            memoryHits,
            reviewGateVerdict,
            traceLinkage,
            artifacts,
            DateTime.UtcNow);

        _logger.LogInformation(
            "Generated final report: runId={RunId}, app={App}, success={Success}, iterations={Iterations}, traceLinkageCount={TraceLinkageCount}",
            orchestrator.Id, appName, isSuccessful, orchestrator.Iterations.Count, traceLinkage.Count);

        return report;
    }

    public bool ValidateReportShape(
        FinalGenerationReport report,
        ReportSerializationContract contract)
    {
        var errors = new List<string>();

        // Check required fields
        if (string.IsNullOrEmpty(report.RunId))
            errors.Add("missing_runId");
        if (string.IsNullOrEmpty(report.ApplicationName))
            errors.Add("missing_applicationName");
        if (string.IsNullOrEmpty(report.ReviewGateVerdict))
            errors.Add("missing_reviewGateVerdict");
        if (report.TaskGraph == null || report.TaskGraph.Count == 0)
            errors.Add("missing_taskGraph");
        if (report.TraceLinkage == null || report.TraceLinkage.Count == 0)
            errors.Add("missing_traceLinkage");

        // Check payload size
        var json = JsonSerializer.Serialize(report);
        var payloadSize = System.Text.Encoding.UTF8.GetByteCount(json);
        if (payloadSize > contract.MaxPayloadSizeBytes)
            errors.Add($"payload_exceeds_max_size:{payloadSize}>{contract.MaxPayloadSizeBytes}");

        var isValid = errors.Count == 0;

        if (!isValid)
        {
            _logger.LogWarning(
                "Report shape validation failed: errors={ErrorCount}, payloadSize={PayloadSize}",
                errors.Count, payloadSize);
        }

        return isValid;
    }

    public ReportSerializationContract GetReportContract(string version)
    {
        return version switch
        {
            "1.0" => new ReportSerializationContract(
                "1.0",
                new[] { "runId", "applicationName", "success", "taskGraph", "reviewGateVerdict", "traceLinkage" },
                new[] { "executedSkills", "mcpCalls", "memoryHits", "artifacts" },
                5_000_000),
            _ => new ReportSerializationContract(
                "1.0",
                new[] { "runId", "applicationName", "success", "taskGraph", "reviewGateVerdict", "traceLinkage" },
                new[] { "executedSkills", "mcpCalls", "memoryHits", "artifacts" },
                5_000_000),
        };
    }

    public string SerializeReport(
        FinalGenerationReport report,
        ReportSerializationContract contract)
    {
        if (!ValidateReportShape(report, contract))
        {
            _logger.LogWarning("Report shape validation failed before serialization");
        }

        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        };

        var json = JsonSerializer.Serialize(report, options);

        _logger.LogInformation(
            "Serialized report: runId={RunId}, payloadSize={PayloadSize}",
            report.RunId, System.Text.Encoding.UTF8.GetByteCount(json));

        return json;
    }

    private static IReadOnlyList<AgentTaskGraphEntry> SynthesizeTaskGraphFromPlan(AppGenerationOrchestrator orchestrator)
    {
        var plan = orchestrator.Plan;
        if (plan is null || plan.Phases.Count == 0)
        {
            return new[]
            {
                new AgentTaskGraphEntry(
                    "t_summary",
                    "Autonomous generation run",
                    Array.Empty<string>(),
                    orchestrator.Status == GenerationStatus.Completed ? AgentTaskState.Done : AgentTaskState.Failed,
                    Array.Empty<string>(),
                    orchestrator.FailureReason)
            };
        }

        return plan.Phases
            .Select((phase, index) => new AgentTaskGraphEntry(
                $"t_phase_{index + 1}",
                phase.Name,
                index == 0 ? Array.Empty<string>() : new[] { $"t_phase_{index}" },
                orchestrator.Status == GenerationStatus.Completed ? AgentTaskState.Done : AgentTaskState.Failed,
                Array.Empty<string>(),
                phase.Description))
            .ToList();
    }

    private static IReadOnlyList<string> ExtractExecutedSkills(AppGenerationOrchestrator orchestrator)
    {
        // Extract from skill invocations audit log
        var skills = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // Skill invocations are tracked in the orchestrator
        // For now, return empty list as placeholder
        return skills.ToList();
    }

    private static IReadOnlyList<string> ExtractMcpCalls(AppGenerationOrchestrator orchestrator)
    {
        var calls = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var iteration in orchestrator.Iterations)
        {
            if (iteration.Execution?.CommandExecutions != null)
            {
                foreach (var cmd in iteration.Execution.CommandExecutions)
                {
                    if (!string.IsNullOrEmpty(cmd.Command))
                        calls.Add(cmd.Command);
                }
            }
        }

        return calls.ToList();
    }

    private static IReadOnlyList<string> ExtractMemoryHits(AppGenerationOrchestrator orchestrator)
    {
        var hits = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // Extract memory ingest entries
        foreach (var entry in orchestrator.MemoryIngests)
        {
            hits.Add($"memory:{entry.Key}");
        }

        return hits.ToList();
    }

    private static IReadOnlyList<TraceLinkageReference> BuildTraceLinkage(
        IReadOnlyList<AgentTaskGraphEntry> taskGraph,
        IReadOnlyList<string> executedSkills,
        IReadOnlyList<string> mcpCalls,
        IReadOnlyList<string> memoryHits,
        string reviewGateVerdict)
    {
        var linkage = new List<TraceLinkageReference>();

        // Task graph linkage
        linkage.Add(new TraceLinkageReference(
            "task_graph",
            $"tasks:{taskGraph.Count}",
            $"Contains {taskGraph.Count} task entries"));

        // Skills linkage
        if (executedSkills.Count > 0)
        {
            linkage.Add(new TraceLinkageReference(
                "executed_skills",
                string.Join(",", executedSkills.Take(5)),
                $"Executed {executedSkills.Count} distinct skills"));
        }

        // MCP calls linkage
        if (mcpCalls.Count > 0)
        {
            linkage.Add(new TraceLinkageReference(
                "mcp_calls",
                string.Join(",", mcpCalls.Take(5)),
                $"Made {mcpCalls.Count} MCP calls"));
        }

        // Memory hits linkage
        if (memoryHits.Count > 0)
        {
            linkage.Add(new TraceLinkageReference(
                "memory_hits",
                string.Join(",", memoryHits.Take(5)),
                $"Retrieved {memoryHits.Count} memory entries"));
        }

        // Review gate verdict linkage
        linkage.Add(new TraceLinkageReference(
            "review_gate_verdict",
            reviewGateVerdict,
            $"Final review verdict: {reviewGateVerdict}"));

        return linkage;
    }
}
