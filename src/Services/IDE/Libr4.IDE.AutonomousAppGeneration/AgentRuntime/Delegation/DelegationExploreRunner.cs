using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Abstractions;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Models;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Subagents;
using Microsoft.Extensions.DependencyInjection;

namespace Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Delegation;

public interface IDelegationExploreRunner
{
    Task<string> RunExploreAsync(string task, ToolContext context, CancellationToken ct);
}

public sealed class DelegationExploreRunner : IDelegationExploreRunner
{
    private readonly IServiceScopeFactory _scopes;
    private readonly IAgentSpecRegistry _specs;

    public DelegationExploreRunner(IServiceScopeFactory scopes, IAgentSpecRegistry specs)
    {
        _scopes = scopes;
        _specs = specs;
    }

    public async Task<string> RunExploreAsync(string task, ToolContext context, CancellationToken ct)
    {
        if (!_specs.TryGet("explore", out var exploreSpec))
            throw new InvalidOperationException("explore agent spec not found");

        using var scope = _scopes.CreateScope();
        var runner = scope.ServiceProvider.GetRequiredService<IAgentSpecSubagentRunner>();
        context.Session.DelegateBackgroundChild = true;
        try
        {
            var result = await runner.RunAsync(exploreSpec, task, context, ct).ConfigureAwait(false);
            return result.Succeeded ? result.Summary ?? "done" : $"delegate_failed: {result.Summary}";
        }
        finally
        {
            context.Session.DelegateBackgroundChild = false;
        }
    }
}
