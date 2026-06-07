using System.Text.Json;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Abstractions;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Models;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Permissions;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Rollout;
using Libr4.IDE.Application.AutonomousAppGeneration.Runtime;
using Microsoft.Extensions.Options;

namespace Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Core;

public sealed class DefaultPermissionGate : IPermissionGate
{
    private readonly IRuntimeCommandPolicy _commandPolicy;
    private readonly IRolloutRecorder? _rollout;
    private readonly IAgentRunPermissionStore? _permissionStore;
    private readonly AgentRuntimeOptions _options;

    public DefaultPermissionGate(
        IRuntimeCommandPolicy commandPolicy,
        IOptions<AgentRuntimeOptions> options,
        IRolloutRecorder? rollout = null,
        IAgentRunPermissionStore? permissionStore = null)
    {
        _commandPolicy = commandPolicy;
        _rollout = rollout;
        _permissionStore = permissionStore;
        _options = options.Value;
    }

    public ValueTask<PermissionDecision> EvaluateAsync(
        IAgentTool tool,
        JsonElement input,
        ToolContext context,
        CancellationToken ct)
    {
        if (!_options.EnablePlanModeTools
            && (string.Equals(tool.Name, "enter_plan_mode", StringComparison.OrdinalIgnoreCase)
                || string.Equals(tool.Name, "exit_plan_mode", StringComparison.OrdinalIgnoreCase)))
            return ValueTask.FromResult(new PermissionDecision(PermissionDecisionKind.Deny, "plan mode tools disabled"));

        if (!_options.EnableMcpToolsInAgentLoop && string.Equals(tool.Name, "mcp", StringComparison.OrdinalIgnoreCase))
            return ValueTask.FromResult(new PermissionDecision(PermissionDecisionKind.Deny, "mcp tool disabled in agent loop"));

        if (!_options.EnableSkillToolsInAgentLoop
            && (string.Equals(tool.Name, "skill", StringComparison.OrdinalIgnoreCase)
                || string.Equals(tool.Name, "activate_skill", StringComparison.OrdinalIgnoreCase)))
            return ValueTask.FromResult(new PermissionDecision(PermissionDecisionKind.Deny, "skill tools disabled in agent loop"));

        if (!_options.EnableSubagentTool && string.Equals(tool.Name, "agent", StringComparison.OrdinalIgnoreCase))
            return ValueTask.FromResult(new PermissionDecision(PermissionDecisionKind.Deny, "subagent tool disabled"));

        var mode = context.Session.PlanMode
            ? AgentPermissionMode.Plan
            : context.Session.RunId is Guid permissionRunId && _permissionStore is not null
                ? _permissionStore.Get(permissionRunId)
                : context.Session.PermissionMode;
        if (mode == AgentPermissionMode.Plan
            && (string.Equals(tool.Name, "bash", StringComparison.OrdinalIgnoreCase)
                || string.Equals(tool.Name, "edit_file", StringComparison.OrdinalIgnoreCase)
                || string.Equals(tool.Name, "write_file", StringComparison.OrdinalIgnoreCase)
                || string.Equals(tool.Name, "apply_patch", StringComparison.OrdinalIgnoreCase)
                || string.Equals(tool.Name, "run_build", StringComparison.OrdinalIgnoreCase)))
        {
            LogPermission(context, tool.Name, "deny", "plan mode");
            return ValueTask.FromResult(new PermissionDecision(
                PermissionDecisionKind.Deny,
                "plan mode active: use exit_plan_mode before mutating tools"));
        }

        if (mode == AgentPermissionMode.Plan
            && !tool.IsReadOnly
            && !string.Equals(tool.Name, "enter_plan_mode", StringComparison.OrdinalIgnoreCase)
            && !string.Equals(tool.Name, "exit_plan_mode", StringComparison.OrdinalIgnoreCase))
        {
            LogPermission(context, tool.Name, "deny", "plan mode blocks mutating tools");
            return ValueTask.FromResult(new PermissionDecision(PermissionDecisionKind.Deny, "plan mode blocks mutating tools"));
        }

        if (mode == AgentPermissionMode.AcceptEdits
            && (string.Equals(tool.Name, "write_file", StringComparison.OrdinalIgnoreCase)
                || string.Equals(tool.Name, "edit_file", StringComparison.OrdinalIgnoreCase)
                || string.Equals(tool.Name, "apply_patch", StringComparison.OrdinalIgnoreCase)))
        {
            var path = input.TryGetProperty("path", out var pathEl) && pathEl.ValueKind == JsonValueKind.String
                ? pathEl.GetString()
                : null;
            if (context.Session.RunId is Guid activeRunId && _permissionStore is not null)
            {
                if (_permissionStore.IsDenied(activeRunId, tool.Name, path))
                {
                    LogPermission(context, tool.Name, "deny", "permission_denied_by_user");
                    return ValueTask.FromResult(new PermissionDecision(
                        PermissionDecisionKind.Deny,
                        "permission_denied_by_user"));
                }

                var alreadyPrompted = _permissionStore.GetAllPrompts(activeRunId).Any(p =>
                    string.Equals(p.ToolName, tool.Name, StringComparison.OrdinalIgnoreCase)
                    && (path is null || string.Equals(p.Path, path, StringComparison.OrdinalIgnoreCase)));
                if (!alreadyPrompted)
                {
                    _permissionStore.EnqueuePrompt(activeRunId, new AgentPermissionPrompt(
                        Guid.NewGuid().ToString("D"),
                        tool.Name,
                        path,
                        "accept_edits_auto",
                        DateTime.UtcNow,
                        true));
                    LogPermission(context, tool.Name, "auto_accept", "accept_edits");
                }
            }
        }

        if (!AgentPermissionModeExtensions.AllowsMutatingTools(mode)
            && (string.Equals(tool.Name, "write_file", StringComparison.OrdinalIgnoreCase)
                || string.Equals(tool.Name, "edit_file", StringComparison.OrdinalIgnoreCase)
                || string.Equals(tool.Name, "apply_patch", StringComparison.OrdinalIgnoreCase)))
        {
            LogPermission(context, tool.Name, "deny", $"permission mode {mode}");
            return ValueTask.FromResult(new PermissionDecision(PermissionDecisionKind.Deny, $"permission mode {mode} blocks file mutation"));
        }

        if (!AgentPermissionModeExtensions.AllowsBash(mode)
            && string.Equals(tool.Name, "bash", StringComparison.OrdinalIgnoreCase))
        {
            LogPermission(context, tool.Name, "deny", $"permission mode {mode}");
            return ValueTask.FromResult(new PermissionDecision(PermissionDecisionKind.Deny, $"permission mode {mode} blocks bash"));
        }

        if (string.Equals(tool.Name, "run_build", StringComparison.OrdinalIgnoreCase)
            && context.Mode == AgentSessionMode.Generation
            && !_options.AllowBashDuringGeneration)
            return ValueTask.FromResult(new PermissionDecision(PermissionDecisionKind.Deny, "run_build disabled during generation"));

        if (string.Equals(tool.Name, "bash", StringComparison.OrdinalIgnoreCase))
        {
            if (context.Mode == AgentSessionMode.Generation && !_options.AllowBashDuringGeneration)
                return ValueTask.FromResult(new PermissionDecision(PermissionDecisionKind.Deny, "bash disabled during generation"));

            if (!input.TryGetProperty("command", out var cmdEl) || cmdEl.ValueKind != JsonValueKind.String)
                return ValueTask.FromResult(new PermissionDecision(PermissionDecisionKind.Deny, "bash requires command string"));

            var command = cmdEl.GetString() ?? string.Empty;
            try
            {
                _commandPolicy.EnsureCommandAllowed(command);
            }
            catch (Exception ex)
            {
                return ValueTask.FromResult(new PermissionDecision(PermissionDecisionKind.Deny, ex.Message));
            }
        }

        if (string.Equals(tool.Name, "edit_file", StringComparison.OrdinalIgnoreCase)
            || string.Equals(tool.Name, "write_file", StringComparison.OrdinalIgnoreCase))
        {
            if (!input.TryGetProperty("path", out var pathEl) || pathEl.ValueKind != JsonValueKind.String)
                return ValueTask.FromResult(new PermissionDecision(PermissionDecisionKind.Deny, "file tools require path"));

            var path = pathEl.GetString() ?? string.Empty;
            if (path.Contains("..", StringComparison.Ordinal))
                return ValueTask.FromResult(new PermissionDecision(PermissionDecisionKind.Deny, "path traversal denied"));
        }

        LogPermission(context, tool.Name, "allow", null);
        return ValueTask.FromResult(new PermissionDecision(PermissionDecisionKind.Allow));
    }

    private void LogPermission(ToolContext context, string toolName, string decision, string? reason)
    {
        if (_rollout is null || context.Session.RunId is not Guid auditRunId || !_options.EnableRolloutRecorder)
            return;
        _ = _rollout.RecordPermissionDecisionAsync(auditRunId, toolName, decision, reason);
    }
}
