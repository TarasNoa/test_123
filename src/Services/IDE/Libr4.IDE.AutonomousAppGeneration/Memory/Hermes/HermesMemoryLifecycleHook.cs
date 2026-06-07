using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Hooks;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Models;

namespace Libr4.IDE.Application.AutonomousAppGeneration.Memory.Hermes;

public sealed class HermesMemoryLifecycleHook : IAgentLifecycleHook
{
    private readonly IHermesMemoryManager _manager;

    public HermesMemoryLifecycleHook(IHermesMemoryManager manager) => _manager = manager;

    public AgentHookKind Kind => AgentHookKind.PreCompact;

    public int Order => 50;

    public async ValueTask ExecuteAsync(HookContext context, CancellationToken ct)
    {
        if (context.RunId is not Guid runId || string.IsNullOrWhiteSpace(context.RequestFingerprint))
            return;

        await _manager.OnPreCompactAsync(
            new HermesTurnContext(
                runId,
                context.RequestFingerprint,
                context.Stage ?? "compact"),
            ct).ConfigureAwait(false);
    }
}
