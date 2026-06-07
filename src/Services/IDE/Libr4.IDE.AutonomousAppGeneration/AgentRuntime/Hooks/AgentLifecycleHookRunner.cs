using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Models;

namespace Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Hooks;

public interface IAgentLifecycleHookRunner
{
    Task RunAsync(AgentHookKind kind, HookContext context, CancellationToken ct = default);
}

public sealed class AgentLifecycleHookRunner : IAgentLifecycleHookRunner
{
    private readonly IReadOnlyList<IAgentLifecycleHook> _hooks;

    public AgentLifecycleHookRunner(IEnumerable<IAgentLifecycleHook> hooks) =>
        _hooks = hooks.OrderBy(h => h.Order).ToList();

    public async Task RunAsync(AgentHookKind kind, HookContext context, CancellationToken ct = default)
    {
        foreach (var hook in _hooks.Where(h => h.Kind == kind))
            await hook.ExecuteAsync(context, ct).ConfigureAwait(false);
    }
}
