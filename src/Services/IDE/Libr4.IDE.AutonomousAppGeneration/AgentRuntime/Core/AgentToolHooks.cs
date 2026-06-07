using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Abstractions;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Models;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Playbook;
using Microsoft.Extensions.Logging;

namespace Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Core;

/// <summary>Deny mutating tools while plan mode is active (Claude Code EnterPlanMode).</summary>
public sealed class PlanModeToolHook : IAgentToolHook
{
    public int Order => 10;

    public ValueTask OnBeforeToolAsync(IAgentTool tool, ToolContext context, CancellationToken ct)
    {
        if (!context.Session.PlanMode)
            return ValueTask.CompletedTask;

        if (string.Equals(tool.Name, "bash", StringComparison.OrdinalIgnoreCase)
            || string.Equals(tool.Name, "edit_file", StringComparison.OrdinalIgnoreCase)
            || string.Equals(tool.Name, "write_file", StringComparison.OrdinalIgnoreCase)
            || string.Equals(tool.Name, "run_build", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Plan mode active: only read/search/inspect tools allowed until exit_plan_mode.");
        }

        return ValueTask.CompletedTask;
    }

    public ValueTask OnAfterToolAsync(IAgentTool tool, ToolContext context, ToolExecutionResult result, CancellationToken ct) =>
        ValueTask.CompletedTask;
}

public sealed class RepairPlaybookToolHook : IAgentToolHook
{
    private readonly RepairPlaybookService _playbook;
    private readonly AgentRuntimeOptions _options;

    public RepairPlaybookToolHook(RepairPlaybookService playbook, Microsoft.Extensions.Options.IOptions<AgentRuntimeOptions> options)
    {
        _playbook = playbook;
        _options = options.Value;
    }

    public int Order => 100;

    public ValueTask OnBeforeToolAsync(IAgentTool tool, ToolContext context, CancellationToken ct) =>
        ValueTask.CompletedTask;

    public async ValueTask OnAfterToolAsync(IAgentTool tool, ToolContext context, ToolExecutionResult result, CancellationToken ct)
    {
        if (!_options.EnableRepairPlaybook
            || context.Mode != AgentSessionMode.Repair
            || string.IsNullOrWhiteSpace(context.BuildLog))
            return;

        var signature = RepairPlaybookSignature.FromBuildLog(context.BuildLog);
        var stack = RepairPlaybookSignature.BuildStackPattern(context.Plan);

        if (result.Success && result.FilePatches.Count > 0)
        {
            var fix = $"{tool.Name}:{result.FilePatches.Count}_files";
            await _playbook.RecordOutcomeAsync(signature, fix, succeeded: true, stack, ct).ConfigureAwait(false);
            return;
        }

        if (!result.Success && IsMutatingTool(tool.Name))
            await _playbook.RecordOutcomeAsync(signature, $"{tool.Name}:failed", succeeded: false, stack, ct).ConfigureAwait(false);
    }

    private static bool IsMutatingTool(string toolName) =>
        string.Equals(toolName, "bash", StringComparison.OrdinalIgnoreCase)
        || string.Equals(toolName, "edit_file", StringComparison.OrdinalIgnoreCase)
        || string.Equals(toolName, "write_file", StringComparison.OrdinalIgnoreCase)
        || string.Equals(toolName, "apply_patch", StringComparison.OrdinalIgnoreCase)
        || string.Equals(toolName, "run_build", StringComparison.OrdinalIgnoreCase)
        || string.Equals(toolName, "run_tests", StringComparison.OrdinalIgnoreCase);
}

public sealed class AgentToolAuditHook : IAgentToolHook
{
    private readonly ILogger<AgentToolAuditHook> _logger;

    public AgentToolAuditHook(ILogger<AgentToolAuditHook> logger) => _logger = logger;

    public int Order => 1000;

    public ValueTask OnBeforeToolAsync(IAgentTool tool, ToolContext context, CancellationToken ct)
    {
        _logger.LogDebug("Agent tool before: {Tool} mode={Mode} plan={Plan}", tool.Name, context.Mode, context.Session.PlanMode);
        return ValueTask.CompletedTask;
    }

    public ValueTask OnAfterToolAsync(IAgentTool tool, ToolContext context, ToolExecutionResult result, CancellationToken ct)
    {
        _logger.LogDebug(
            "Agent tool after: {Tool} success={Success} patches={Patches}",
            tool.Name,
            result.Success,
            result.FilePatches.Count);
        return ValueTask.CompletedTask;
    }
}
