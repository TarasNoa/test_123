using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Abstractions;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Events;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.ExecPolicy;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Models;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Permissions;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Tools.Browser;
using Libr4.IDE.Application.Obscura;

namespace Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Hooks;

/// <summary>
/// PreToolUse hook enforcing Obscura browser URL/script policy for all browser_* tools.
/// </summary>
public sealed class ObscuraExecPolicyToolHook : IAgentToolHook
{
    private readonly IObscuraExecPolicyEngine _policy;
    private readonly IObscuraExecPolicyJsonlAudit? _audit;
    private readonly IAgentRunPermissionStore? _permissionStore;
    private readonly INdjsonEventWriter? _ndjson;

    public ObscuraExecPolicyToolHook(
        IObscuraExecPolicyEngine policy,
        IObscuraExecPolicyJsonlAudit? audit = null,
        IAgentRunPermissionStore? permissionStore = null,
        INdjsonEventWriter? ndjson = null)
    {
        _policy = policy;
        _audit = audit;
        _permissionStore = permissionStore;
        _ndjson = ndjson;
    }

    public int Order => 4;

    public async ValueTask OnBeforeToolAsync(IAgentTool tool, ToolContext context, CancellationToken ct)
    {
        if (!BrowserToolEventHook.IsBrowserTool(tool.Name))
            return;

        var targets = ExtractTargets(tool.Name, context.ToolInput);
        if (targets.Count == 0)
            return;

        foreach (var target in targets)
        {
            if (string.IsNullOrWhiteSpace(target))
                continue;

            var eval = _policy.Evaluate(tool.Name, target);
            if (eval.Decision == ExecPolicyDecision.Forbid)
            {
                ObscuraTelemetry.RecordPolicyDenial(tool.Name, "forbid");
                throw new InvalidOperationException(
                    $"obscura_execpolicy_forbid: {eval.Reason ?? eval.MatchedRule} target={target}");
            }

            if (eval.Decision == ExecPolicyDecision.Prompt)
            {
                if (context.Session.RunId is Guid runId && _permissionStore is not null)
                {
                    if (_permissionStore.IsDenied(runId, tool.Name, target))
                    {
                        ObscuraTelemetry.RecordPolicyDenial(tool.Name, "consent_denied");
                        throw new InvalidOperationException(
                            $"obscura_execpolicy_forbid: consent_denied target={target}");
                    }

                    var accepted = _permissionStore.GetAllPrompts(runId).Any(p =>
                        string.Equals(p.ToolName, tool.Name, StringComparison.OrdinalIgnoreCase)
                        && string.Equals(p.Path, target, StringComparison.OrdinalIgnoreCase)
                        && p.Accepted == true);
                    if (!accepted)
                    {
                        var alreadyPrompted = _permissionStore.GetAllPrompts(runId).Any(p =>
                            string.Equals(p.ToolName, tool.Name, StringComparison.OrdinalIgnoreCase)
                            && string.Equals(p.Path, target, StringComparison.OrdinalIgnoreCase)
                            && p.Accepted is null);
                        if (!alreadyPrompted)
                        {
                            var promptId = Guid.NewGuid().ToString("D");
                            _permissionStore.EnqueuePrompt(runId, new AgentPermissionPrompt(
                                promptId,
                                tool.Name,
                                target,
                                eval.Reason ?? "obscura_external_consent_required",
                                DateTime.UtcNow,
                                null,
                                Kind: "obscura_execpolicy"));
                            await EmitExecPolicyPromptEventAsync(
                                runId,
                                promptId,
                                tool.Name,
                                target,
                                eval,
                                ct).ConfigureAwait(false);
                        }

                        throw new InvalidOperationException(
                            $"obscura_execpolicy_prompt: {eval.Reason ?? eval.MatchedRule} target={target}");
                    }
                }
                else
                {
                    ObscuraTelemetry.RecordPolicyDenial(tool.Name, "prompt");
                    throw new InvalidOperationException(
                        $"obscura_execpolicy_prompt: {eval.Reason ?? eval.MatchedRule} target={target}");
                }
            }

            var entry = new ObscuraExecPolicyAuditEntry(
                tool.Name,
                target,
                eval.Decision,
                eval.MatchedRule,
                context.Session.RunId,
                DateTime.UtcNow);
            _policy.Audit(entry);
            if (_audit is not null)
                await _audit.WriteAsync(entry, ct).ConfigureAwait(false);
        }

        return;
    }

    public ValueTask OnAfterToolAsync(IAgentTool tool, ToolContext context, ToolExecutionResult result, CancellationToken ct) =>
        ValueTask.CompletedTask;

    private async Task EmitExecPolicyPromptEventAsync(
        Guid runId,
        string promptId,
        string toolName,
        string target,
        ObscuraExecPolicyEvaluation eval,
        CancellationToken ct)
    {
        if (_ndjson is null)
            return;

        await _ndjson.WriteAsync(
            runId,
            new
            {
                type = "obscura_execpolicy_prompt",
                promptId,
                toolName,
                target,
                reason = eval.Reason ?? "obscura_external_consent_required",
                matchedRule = eval.MatchedRule,
                kind = "obscura_execpolicy",
                timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
            },
            ct).ConfigureAwait(false);
    }

    private static IReadOnlyList<string> ExtractTargets(string toolName, System.Text.Json.JsonElement input)
    {
        if (string.Equals(toolName, BrowserToolNames.Navigate, StringComparison.OrdinalIgnoreCase))
        {
            var url = ObscuraBrowserToolFacade.GetString(input, "url");
            return string.IsNullOrWhiteSpace(url) ? [] : [url];
        }

        if (string.Equals(toolName, BrowserToolNames.ExecuteJs, StringComparison.OrdinalIgnoreCase))
        {
            var script = ObscuraBrowserToolFacade.GetString(input, "script");
            return string.IsNullOrWhiteSpace(script) ? [] : [script];
        }

        if (string.Equals(toolName, BrowserToolNames.Research, StringComparison.OrdinalIgnoreCase))
        {
            if (!input.TryGetProperty("sources", out var sources) || sources.ValueKind != System.Text.Json.JsonValueKind.Array)
                return [];

            return sources.EnumerateArray()
                .Where(e => e.ValueKind == System.Text.Json.JsonValueKind.String)
                .Select(e => e.GetString() ?? string.Empty)
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .ToList();
        }

        return [];
    }
}
