using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Rollout;
using Libr4.IDE.Application.AutonomousAppGeneration.Memory.Hermes;

namespace Libr4.IDE.Application.AutonomousAppGeneration.Memory.Search;

public sealed class CompositeSessionSearchService : ISessionSearchService
{
    private readonly IRolloutRecorder _rollout;
    private readonly IHermesMemoryStore _memory;

    public CompositeSessionSearchService(IRolloutRecorder rollout, IHermesMemoryStore memory)
    {
        _rollout = rollout;
        _memory = memory;
    }

    public async Task<IReadOnlyList<SessionSearchHit>> SearchAsync(string query, int limit = 25, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(query))
            return Array.Empty<SessionSearchHit>();

        var perSource = Math.Max(1, limit / 2);
        var rolloutTask = _rollout.SearchAsync(FtsQueryHelper.ToMatchExpression(query), perSource, ct);
        var memoryTask = _memory.SearchSummariesAsync(query, perSource, ct);
        await Task.WhenAll(rolloutTask, memoryTask).ConfigureAwait(false);

        var hits = new List<SessionSearchHit>(limit);
        foreach (var rolloutHit in rolloutTask.Result)
        {
            hits.Add(new SessionSearchHit(
                Source: "rollout",
                RunId: rolloutHit.RunId,
                StepNumber: rolloutHit.StepNumber,
                ToolName: rolloutHit.ToolName,
                MemoryKey: null,
                MemoryKind: null,
                Snippet: rolloutHit.Snippet,
                Score: rolloutHit.Score));
        }

        foreach (var memoryHit in memoryTask.Result)
        {
            hits.Add(new SessionSearchHit(
                Source: "memory",
                RunId: memoryHit.RunId,
                StepNumber: null,
                ToolName: null,
                MemoryKey: memoryHit.Key,
                MemoryKind: memoryHit.Kind,
                Snippet: memoryHit.Snippet,
                Score: memoryHit.Score));
        }

        return hits
            .OrderByDescending(hit => hit.Score)
            .ThenByDescending(hit => hit.RunId)
            .Take(limit)
            .ToList();
    }
}
