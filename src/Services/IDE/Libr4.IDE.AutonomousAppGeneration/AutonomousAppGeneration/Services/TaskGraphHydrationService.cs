using System.Text.Json;
using Libr4.IDE.Domain.AutonomousAppGeneration;

namespace Libr4.IDE.Application.AutonomousAppGeneration.Services;

public interface ITaskGraphHydrationService
{
    void EnsureHydrated(AppGenerationOrchestrator orchestrator);

    IReadOnlyList<AgentTaskGraphEntry> Resolve(AppGenerationOrchestrator orchestrator);
}

public sealed class TaskGraphHydrationService : ITaskGraphHydrationService
{
    private readonly string _manifestRoot;

    public TaskGraphHydrationService()
    {
        _manifestRoot = Path.Combine(Path.GetTempPath(), "libr4-autogen-manifests");
    }

    public void EnsureHydrated(AppGenerationOrchestrator orchestrator)
    {
        ArgumentNullException.ThrowIfNull(orchestrator);
        orchestrator.RestoreTaskGraphIfEmpty();

        if (orchestrator.TaskGraph.Count > 0)
            return;

        var fromManifest = TryLoadFromManifestArtifact(orchestrator.Id);
        if (fromManifest.Count > 0)
            orchestrator.ReplaceTaskGraph(fromManifest);
    }

    public IReadOnlyList<AgentTaskGraphEntry> Resolve(AppGenerationOrchestrator orchestrator)
    {
        EnsureHydrated(orchestrator);
        if (orchestrator.TaskGraph.Count > 0)
            return orchestrator.TaskGraph;

        return SynthesizeFromPlan(orchestrator);
    }

    private IReadOnlyList<AgentTaskGraphEntry> TryLoadFromManifestArtifact(Guid orchestratorId)
    {
        var path = Path.Combine(_manifestRoot, $"{orchestratorId:N}.manifest.json");
        if (!File.Exists(path))
            return Array.Empty<AgentTaskGraphEntry>();

        try
        {
            var json = File.ReadAllText(path);
            using var doc = JsonDocument.Parse(json);
            if (!doc.RootElement.TryGetProperty("taskGraph", out var graph) || graph.ValueKind != JsonValueKind.Array)
                return Array.Empty<AgentTaskGraphEntry>();

            var list = new List<AgentTaskGraphEntry>();
            foreach (var item in graph.EnumerateArray())
            {
                var taskId = item.TryGetProperty("taskId", out var tid) ? tid.GetString() : null;
                var title = item.TryGetProperty("title", out var t) ? t.GetString() : null;
                var stateRaw = item.TryGetProperty("state", out var st) ? st.GetString() : null;
                var notes = item.TryGetProperty("notes", out var n) ? n.GetString() : null;

                var blocked = new List<string>();
                if (item.TryGetProperty("blockedByTaskIds", out var blockedEl) && blockedEl.ValueKind == JsonValueKind.Array)
                {
                    foreach (var b in blockedEl.EnumerateArray())
                    {
                        var s = b.GetString();
                        if (!string.IsNullOrWhiteSpace(s))
                            blocked.Add(s);
                    }
                }

                var evidence = new List<string>();
                if (item.TryGetProperty("evidencePaths", out var evEl) && evEl.ValueKind == JsonValueKind.Array)
                {
                    foreach (var e in evEl.EnumerateArray())
                    {
                        var s = e.GetString();
                        if (!string.IsNullOrWhiteSpace(s))
                            evidence.Add(s);
                    }
                }

                if (string.IsNullOrWhiteSpace(taskId))
                    continue;

                var state = Enum.TryParse<AgentTaskState>(stateRaw, true, out var parsed)
                    ? parsed
                    : AgentTaskState.Pending;

                list.Add(new AgentTaskGraphEntry(
                    taskId,
                    title ?? taskId,
                    blocked,
                    state,
                    evidence,
                    notes));
            }

            return list;
        }
        catch (IOException)
        {
            return Array.Empty<AgentTaskGraphEntry>();
        }
        catch (JsonException)
        {
            return Array.Empty<AgentTaskGraphEntry>();
        }
    }

    private static IReadOnlyList<AgentTaskGraphEntry> SynthesizeFromPlan(AppGenerationOrchestrator orchestrator)
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
}
