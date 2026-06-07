using System.Text.Json;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Abstractions;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Delegation;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Models;

namespace Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Tools;

public sealed class DelegationListTool : IAgentTool
{
    private readonly IDelegationManager _delegation;

    public DelegationListTool(IDelegationManager delegation) => _delegation = delegation;

    public string Name => "delegation_list";
    public string Description => "List background delegations for current run.";
    public bool IsReadOnly => true;
    public bool IsConcurrencySafe(JsonElement input) => true;

    public async Task<ToolExecutionResult> ExecuteAsync(JsonElement input, ToolContext context, CancellationToken ct)
    {
        var runId = context.Session.RunId;
        if (runId is null)
            return Fail("run id unavailable");

        var list = await _delegation.ListAsync(runId.Value, ct).ConfigureAwait(false);
        var lines = list.Select(d => $"{d.Id} [{d.Status}] {d.Task}");
        return new ToolExecutionResult(Name, true, string.Join('\n', lines), Array.Empty<Domain.AutonomousAppGeneration.GeneratedFile>());
    }

    private static ToolExecutionResult Fail(string msg) =>
        new("delegation_list", false, msg, Array.Empty<Domain.AutonomousAppGeneration.GeneratedFile>());
}
