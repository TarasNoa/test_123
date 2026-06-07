namespace Libr4.IDE.Application.AutonomousAppGeneration.Tooling.Flow;

public interface IFlowEngine
{
    bool TryResolveFlowName(string userRequest, out string flowName);
    Task InitializeAsync(Guid runId, string flowName, CancellationToken ct = default);
    Task<FlowAdvanceResult> OnPhaseCompletedAsync(
        Guid runId,
        string phase,
        bool succeeded,
        FlowRuntimeContext context,
        CancellationToken ct = default);
    Task<FlowProgress?> GetProgressAsync(Guid runId, CancellationToken ct = default);
}

public sealed class YamlFlowEngine : IFlowEngine
{
    private readonly IFlowRegistry _registry;
    private readonly IFlowProgressStore _store;
    private readonly FlowEngineOptions _options;

    public YamlFlowEngine(
        IFlowRegistry registry,
        IFlowProgressStore store,
        Microsoft.Extensions.Options.IOptions<FlowEngineOptions> options)
    {
        _registry = registry;
        _store = store;
        _options = options.Value;
    }

    public bool TryResolveFlowName(string userRequest, out string flowName)
    {
        flowName = string.Empty;
        if (!_options.EnableFlowOrchestration || string.IsNullOrWhiteSpace(userRequest))
            return false;

        var match = System.Text.RegularExpressions.Regex.Match(
            userRequest,
            @"(?:^|\s)/flow:([A-Za-z0-9_-]+)",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        if (!match.Success)
            return false;

        flowName = match.Groups[1].Value;
        return _registry.TryGet(flowName, out _);
    }

    public async Task InitializeAsync(Guid runId, string flowName, CancellationToken ct = default)
    {
        if (!_registry.TryGet(flowName, out var flow))
            throw new InvalidOperationException($"flow not found: {flowName}");

        var first = flow.Nodes.FirstOrDefault();
        var progress = new FlowProgress(
            runId,
            flow.Name,
            first?.Id,
            "running",
            flow.Nodes.Select(n => new FlowNodeProgress(
                n.Id,
                n.Id == first?.Id ? "running" : "pending")).ToArray(),
            DateTime.UtcNow);
        await _store.SaveAsync(progress, ct).ConfigureAwait(false);
    }

    public async Task<FlowAdvanceResult> OnPhaseCompletedAsync(
        Guid runId,
        string phase,
        bool succeeded,
        FlowRuntimeContext context,
        CancellationToken ct = default)
    {
        var progress = await _store.LoadAsync(runId, ct).ConfigureAwait(false);
        if (progress is null || string.IsNullOrWhiteSpace(progress.CurrentNodeId))
            return FlowAdvanceResult.Ok();

        if (!_registry.TryGet(progress.FlowName, out var flow))
            return FlowAdvanceResult.Abort($"flow definition missing: {progress.FlowName}");

        var current = flow.Nodes.FirstOrDefault(n => n.Id == progress.CurrentNodeId);
        if (current is null)
            return FlowAdvanceResult.Abort($"unknown flow node: {progress.CurrentNodeId}");

        if (!NodeMatchesPhase(current, phase))
            return FlowAdvanceResult.Ok(message: "phase does not match current node");

        var nodeStates = progress.Nodes.ToDictionary(n => n.NodeId, n => n);
        var currentState = nodeStates[current.Id];

        if (current.Type == FlowNodeType.Gate)
        {
            var (gatePassed, gateReason) = FlowPreconditionEvaluator.EvaluateAll(current.Preconditions, context);
            if (!gatePassed)
            {
                currentState = currentState with
                {
                    Status = "failed",
                    Attempts = currentState.Attempts + 1,
                    LastError = gateReason
                };
                nodeStates[current.Id] = currentState;
                return await RouteAsync(runId, flow, progress, current.Id, false, nodeStates, ct).ConfigureAwait(false);
            }
        }

        if (!succeeded)
        {
            currentState = currentState with
            {
                Status = "failed",
                Attempts = currentState.Attempts + 1,
                LastError = $"phase '{phase}' failed"
            };
            nodeStates[current.Id] = currentState;
            return await RouteAsync(runId, flow, progress, current.Id, false, nodeStates, ct).ConfigureAwait(false);
        }

        currentState = currentState with { Status = "completed" };
        nodeStates[current.Id] = currentState;
        return await RouteAsync(runId, flow, progress, current.Id, true, nodeStates, ct).ConfigureAwait(false);
    }

    public Task<FlowProgress?> GetProgressAsync(Guid runId, CancellationToken ct = default) =>
        _store.LoadAsync(runId, ct);

    private async Task<FlowAdvanceResult> RouteAsync(
        Guid runId,
        FlowDefinition flow,
        FlowProgress progress,
        string fromNodeId,
        bool success,
        Dictionary<string, FlowNodeProgress> nodeStates,
        CancellationToken ct)
    {
        var outcome = success ? FlowEdgeOutcome.Success : FlowEdgeOutcome.Failure;
        var edge = flow.Edges.FirstOrDefault(e =>
            e.From.Equals(fromNodeId, StringComparison.OrdinalIgnoreCase) && e.On == outcome);

        if (edge is null)
        {
            var updated = progress with
            {
                Status = success ? "completed" : "failed",
                CurrentNodeId = (string?)null,
                Nodes = nodeStates.Values.ToArray(),
                UpdatedAtUtc = DateTime.UtcNow
            };
            await _store.SaveAsync(updated, ct).ConfigureAwait(false);
            return success
                ? FlowAdvanceResult.Ok(message: "flow completed")
                : FlowAdvanceResult.Abort("flow failed without recovery edge");
        }

        if (!success && edge.Action == FlowFailureAction.Abort)
        {
            var aborted = progress with
            {
                Status = "aborted",
                CurrentNodeId = null,
                Nodes = nodeStates.Values.ToArray(),
                UpdatedAtUtc = DateTime.UtcNow
            };
            await _store.SaveAsync(aborted, ct).ConfigureAwait(false);
            return FlowAdvanceResult.Abort($"flow aborted at {fromNodeId}");
        }

        var targetId = edge.To;
        if (!success && edge.Action == FlowFailureAction.Retry)
        {
            var attempts = nodeStates.TryGetValue(fromNodeId, out var state) ? state.Attempts : 0;
            if (attempts < edge.MaxRetries)
                targetId = fromNodeId;
        }

        if (nodeStates.TryGetValue(targetId, out var targetState))
            nodeStates[targetId] = targetState with { Status = "running" };

        var next = progress with
        {
            CurrentNodeId = targetId,
            Nodes = nodeStates.Values.ToArray(),
            UpdatedAtUtc = DateTime.UtcNow
        };
        await _store.SaveAsync(next, ct).ConfigureAwait(false);

        return FlowAdvanceResult.Ok(
            nextNodeId: targetId,
            message: edge.Action?.ToString());
    }

    private static bool NodeMatchesPhase(FlowNode node, string phase)
    {
        if (!string.IsNullOrWhiteSpace(node.Stage)
            && phase.Equals(node.Stage, StringComparison.OrdinalIgnoreCase))
            return true;

        if (!string.IsNullOrWhiteSpace(node.Phase)
            && phase.Contains(node.Phase, StringComparison.OrdinalIgnoreCase))
            return true;

        return node.Id.Equals(phase, StringComparison.OrdinalIgnoreCase);
    }
}
