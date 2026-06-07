using System.Text.Json;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Abstractions;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Delegation;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Models;

namespace Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Tools;

/// <summary>Background detached explore-only delegation.</summary>
public sealed class DelegateTool : IAgentTool
{
    private readonly IDelegationManager _delegation;
    private readonly IDelegationExploreRunner _exploreRunner;

    public DelegateTool(IDelegationManager delegation, IDelegationExploreRunner exploreRunner)
    {
        _delegation = delegation;
        _exploreRunner = exploreRunner;
    }

    public string Name => "delegate";
    public string Description => "Background explore delegation. Input: { \"task\": \"...\" }";
    public bool IsReadOnly => true;
    public bool IsConcurrencySafe(JsonElement input) => true;

    public async Task<ToolExecutionResult> ExecuteAsync(JsonElement input, ToolContext context, CancellationToken ct)
    {
        if (_delegation.IsBackgroundChild() || context.Session.DelegateBackgroundChild)
            return Fail("nested background delegation denied");

        var task = input.TryGetProperty("task", out var t) && t.ValueKind == JsonValueKind.String ? t.GetString() : null;
        if (string.IsNullOrWhiteSpace(task))
            return Fail("task is required");

        var runId = context.Session.RunId ?? Guid.NewGuid();
        var record = await _delegation.StartExploreAsync(
            runId,
            task!,
            workerCt => _exploreRunner.RunExploreAsync(task!, context, workerCt),
            DelegationFleetPriority.UserInitiated,
            context.Session.TenantUserId,
            ct).ConfigureAwait(false);

        return new ToolExecutionResult(
            Name,
            true,
            $"delegation_started:{record.Id}",
            Array.Empty<Domain.AutonomousAppGeneration.GeneratedFile>());
    }

    private static ToolExecutionResult Fail(string msg) =>
        new("delegate", false, msg, Array.Empty<Domain.AutonomousAppGeneration.GeneratedFile>());
}
