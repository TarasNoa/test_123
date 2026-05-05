using Libr4.IDE.Domain.AutonomousAppGeneration;

namespace Libr4.IDE.Application.AutonomousAppGeneration.AgentIntegration;

public sealed class AgentTaskGraphService : IAgentTaskGraphService
{
    private readonly IAutonomousCascadePlanner _cascade;

    public AgentTaskGraphService(IAutonomousCascadePlanner cascade) => _cascade = cascade;

    public IReadOnlyList<AgentTaskGraphEntry> BuildInitial(GenerationPlan plan, string userRequest)
    {
        var cascade = _cascade.Build(plan, userRequest);
        var entries = new List<AgentTaskGraphEntry>
        {
            new("t_plan", "Plan and gate user request", Array.Empty<string>(), AgentTaskState.Ready,
                Array.Empty<string>(), null),
            new("t_generate", "Generate source files (LLM)", new[] { "t_plan" }, AgentTaskState.Pending,
                Array.Empty<string>(), null),
            new("t_consistency", "Structural consistency validation", new[] { "t_generate" }, AgentTaskState.Pending,
                Array.Empty<string>(), null),
            new("t_workspace", "Prepare shadow workspace", new[] { "t_consistency" }, AgentTaskState.Pending,
                Array.Empty<string>(), null),
        };

        var phaseTaskIds = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var phase in cascade.Phases)
        {
            var id = $"t_phase_build_{phaseTaskIds.Count}";
            phaseTaskIds[phase.PhaseId] = id;
        }

        foreach (var phase in cascade.Phases)
        {
            var id = phaseTaskIds[phase.PhaseId];
            var blockers = phase.Dependencies
                .Select(d => phaseTaskIds.TryGetValue(d, out var taskId) ? taskId : null)
                .Where(taskId => !string.IsNullOrWhiteSpace(taskId))
                .Cast<string>()
                .ToList();

            if (blockers.Count == 0)
                blockers.Add("t_workspace");

            entries.Add(new AgentTaskGraphEntry(
                id,
                $"Compile-check phase: {phase.PhaseName}",
                blockers,
                AgentTaskState.Pending,
                Array.Empty<string>(),
                phase.ExpectedOutput));
        }

        entries.Add(new AgentTaskGraphEntry(
            "t_test_loop",
            "Execute build/tests and fix iterations",
            phaseTaskIds.Count > 0 ? phaseTaskIds.Values.ToList() : new[] { "t_workspace" },
            AgentTaskState.Pending,
            Array.Empty<string>(),
            null));

        return entries;
    }

    public IReadOnlyList<AgentTaskGraphEntry> Transition(
        IReadOnlyList<AgentTaskGraphEntry> current,
        string taskId,
        AgentTaskState newState,
        IReadOnlyList<string>? evidencePaths = null,
        string? notes = null)
    {
        var list = current.Select(e =>
        {
            if (!string.Equals(e.TaskId, taskId, StringComparison.Ordinal))
                return e;

            return e with
            {
                State = newState,
                EvidencePaths = evidencePaths ?? e.EvidencePaths,
                Notes = notes ?? e.Notes,
            };
        }).ToList();

        return PromoteReadiness(list);
    }

    public IReadOnlyList<AgentTaskGraphEntry> AppendRecoveryTasks(
        IReadOnlyList<AgentTaskGraphEntry> current,
        string failedStage,
        IReadOnlyList<string> reasons)
    {
        var id = $"t_recovery_{Guid.NewGuid():N}";
        var summary = string.Join("; ", reasons.Take(5));
        var entry = new AgentTaskGraphEntry(
            id,
            $"Recovery replan after {failedStage}",
            Array.Empty<string>(),
            AgentTaskState.Ready,
            Array.Empty<string>(),
            summary);
        return current.Concat(new[] { entry }).ToList();
    }

    private static IReadOnlyList<AgentTaskGraphEntry> PromoteReadiness(IReadOnlyList<AgentTaskGraphEntry> entries)
    {
        var byId = entries.ToDictionary(e => e.TaskId, StringComparer.Ordinal);
        var list = entries.ToList();
        for (var i = 0; i < list.Count; i++)
        {
            var e = list[i];
            if (e.State is not AgentTaskState.Pending and not AgentTaskState.Blocked)
                continue;

            var blockers = e.BlockedByTaskIds;
            if (blockers.Count == 0)
            {
                list[i] = e with { State = AgentTaskState.Ready };
                continue;
            }

            var allDone = blockers.All(b =>
                byId.TryGetValue(b, out var dep) && dep.State == AgentTaskState.Done);
            if (allDone)
                list[i] = e with { State = AgentTaskState.Ready };
        }

        return list;
    }
}
