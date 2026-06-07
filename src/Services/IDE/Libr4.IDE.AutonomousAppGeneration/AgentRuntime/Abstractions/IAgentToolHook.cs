using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Models;

namespace Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Abstractions;

public interface IAgentToolHook
{
    int Order { get; }
    ValueTask OnBeforeToolAsync(IAgentTool tool, ToolContext context, CancellationToken ct);
    ValueTask OnAfterToolAsync(IAgentTool tool, ToolContext context, ToolExecutionResult result, CancellationToken ct);
}

public sealed class AgentToolHookPipeline
{
    private readonly IReadOnlyList<IAgentToolHook> _hooks;

    public AgentToolHookPipeline(IEnumerable<IAgentToolHook> hooks) =>
        _hooks = hooks.OrderBy(h => h.Order).ToList();

    public async Task RunBeforeAsync(IAgentTool tool, ToolContext context, CancellationToken ct)
    {
        foreach (var hook in _hooks)
            await hook.OnBeforeToolAsync(tool, context, ct).ConfigureAwait(false);
    }

    public async Task RunAfterAsync(IAgentTool tool, ToolContext context, ToolExecutionResult result, CancellationToken ct)
    {
        foreach (var hook in _hooks)
            await hook.OnAfterToolAsync(tool, context, result, ct).ConfigureAwait(false);
    }
}
