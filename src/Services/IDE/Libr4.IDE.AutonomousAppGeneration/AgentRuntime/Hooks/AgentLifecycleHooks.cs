using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Abstractions;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.ExecPolicy;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Models;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Rollout;
using Microsoft.Extensions.Options;

namespace Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Hooks;

public enum AgentHookKind
{
    SessionStart,
    SessionEnd,
    PreToolUse,
    PostToolUse,
    PreCompact,
    PostCompact,
    SubagentStart,
    SubagentStop,
    TaskCreated,
    TaskCompleted
}

public sealed class HookContext
{
    public Guid? RunId { get; init; }
    public string? SessionId { get; init; }
    public string? WorkspaceRoot { get; init; }
    public string? RequestFingerprint { get; init; }
    public string? Stage { get; init; }
    public IReadOnlyList<string>? MemoryKeywords { get; init; }
    public IAgentTool? Tool { get; init; }
    public ToolExecutionResult? ToolResult { get; init; }
}

public interface IAgentLifecycleHook
{
    AgentHookKind Kind { get; }
    int Order { get; }
    ValueTask ExecuteAsync(HookContext context, CancellationToken ct);
}

public sealed class ExecPolicyToolHook : IAgentToolHook
{
    private readonly IExecPolicyEngine _policy;
    private readonly IExecPolicyJsonlAudit? _audit;

    public ExecPolicyToolHook(IExecPolicyEngine policy, IExecPolicyJsonlAudit? audit = null)
    {
        _policy = policy;
        _audit = audit;
    }

    public int Order => 5;

    public ValueTask OnBeforeToolAsync(IAgentTool tool, ToolContext context, CancellationToken ct)
    {
        if (!string.Equals(tool.Name, "bash", StringComparison.OrdinalIgnoreCase))
            return ValueTask.CompletedTask;

        if (!context.ToolInput.TryGetProperty("command", out var cmd) || cmd.ValueKind != System.Text.Json.JsonValueKind.String)
            return ValueTask.CompletedTask;

        var command = cmd.GetString() ?? string.Empty;
        var eval = _policy.EvaluateBash(command);
        if (eval.Decision == ExecPolicyDecision.Forbid)
            throw new InvalidOperationException($"execpolicy_forbid: {eval.Reason ?? eval.MatchedRule}");

        var entry = new ExecPolicyAuditEntry("bash", command, eval.Decision, eval.MatchedRule, context.Session.RunId, DateTime.UtcNow);
        _policy.Audit(entry);
        if (_audit is not null)
            _ = _audit.WriteAsync(entry, ct);
        return ValueTask.CompletedTask;
    }

    public ValueTask OnAfterToolAsync(IAgentTool tool, ToolContext context, ToolExecutionResult result, CancellationToken ct) =>
        ValueTask.CompletedTask;
}

public sealed class RolloutAuditToolHook : IAgentToolHook
{
    private readonly IRolloutRecorder _rollout;

    public RolloutAuditToolHook(IRolloutRecorder rollout) => _rollout = rollout;

    public int Order => 100;

    public ValueTask OnBeforeToolAsync(IAgentTool tool, ToolContext context, CancellationToken ct) =>
        ValueTask.CompletedTask;

    public async ValueTask OnAfterToolAsync(IAgentTool tool, ToolContext context, ToolExecutionResult result, CancellationToken ct)
    {
        if (context.Session.RunId is not Guid runId)
            return;

        if (BrowserToolEventHook.IsBrowserTool(tool.Name))
            return;

        await _rollout.RecordToolUseAsync(
            runId,
            context.Session.SessionId ?? "unknown",
            context.Session.CurrentStepNumber,
            tool.Name,
            context.Session.LastToolInputJson ?? "{}",
            result.Output,
            result.Success,
            context.Session.LastToolDurationMs,
            ct: ct).ConfigureAwait(false);
    }
}

public sealed class ConfigurableScriptHookOptions
{
    public List<ScriptHookDefinition> Hooks { get; set; } = new();
}

public sealed class ScriptHookDefinition
{
    public string Kind { get; set; } = "PreToolUse";
    public string FileName { get; set; } = string.Empty;
    public string? Arguments { get; set; }
    public int TimeoutMs { get; set; } = 5000;
    public string OnFailure { get; set; } = "block";
}
