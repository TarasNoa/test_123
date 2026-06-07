using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Abstractions;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Models;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Rollout;
using Libr4.IDE.Application.AutonomousAppGeneration.Memory.Hermes;
using Microsoft.Extensions.Options;

namespace Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Hooks;

/// <summary>
/// PreToolUse Hermes prefetch for repair-mode mutating tools + rollout audit.
/// </summary>
public sealed class MemoryPrefetchToolHook : IAgentToolHook
{
    private readonly IHermesMemoryManager _memory;
    private readonly IRolloutRecorder? _rollout;
    private readonly HermesMemoryManagerOptions _options;

    public MemoryPrefetchToolHook(
        IHermesMemoryManager memory,
        IOptions<HermesMemoryManagerOptions> options,
        IRolloutRecorder? rollout = null)
    {
        _memory = memory;
        _options = options.Value;
        _rollout = rollout;
    }

    public int Order => 12;

    public async ValueTask OnBeforeToolAsync(IAgentTool tool, ToolContext context, CancellationToken ct)
    {
        if (!_options.EnablePrefetch
            || context.Mode != AgentSessionMode.Repair
            || !IsMutatingTool(tool.Name)
            || context.Session.RunId is not Guid runId)
        {
            return;
        }

        var fingerprint = ResolveFingerprint(context);
        if (string.IsNullOrWhiteSpace(fingerprint))
            return;

        var keywords = BuildKeywords(context);
        var nudge = await _memory.PrefetchBeforeTurnAsync(
            new HermesTurnContext(
                runId,
                fingerprint,
                "repair_tool",
                keywords,
                context.Session.TenantUserId),
            ct).ConfigureAwait(false);

        if (string.IsNullOrWhiteSpace(nudge))
            return;

        if (string.IsNullOrWhiteSpace(context.Session.ActiveLibr4Context))
            context.Session.ActiveLibr4Context = nudge;
        else if (!context.Session.ActiveLibr4Context.Contains("relevant_memory", StringComparison.Ordinal))
            context.Session.ActiveLibr4Context = context.Session.ActiveLibr4Context + "\n\n" + nudge;

        if (_rollout is not null)
        {
            await _rollout.RecordMemoryOperationAsync(
                runId,
                context.Session.SessionId,
                "prefetch",
                fingerprint,
                key: tool.Name,
                kind: "repair_tool",
                resultCount: nudge.Split('\n', StringSplitOptions.RemoveEmptyEntries).Length,
                ct).ConfigureAwait(false);
        }
    }

    public ValueTask OnAfterToolAsync(
        IAgentTool tool,
        ToolContext context,
        ToolExecutionResult result,
        CancellationToken ct) =>
        ValueTask.CompletedTask;

    private static string? ResolveFingerprint(ToolContext context)
    {
        if (context.Session.SpaceId is Guid spaceId)
            return HermesMemoryScopeResolver.BuildSpaceFingerprint(spaceId);

        if (context.Plan is null)
            return null;

        return HermesMemoryScopeResolver.ResolveProjectFingerprint(context.Plan);
    }

    private static IReadOnlyList<string>? BuildKeywords(ToolContext context)
    {
        if (context.Session.LastErrors is { Count: > 0 })
            return context.Session.LastErrors;

        if (!string.IsNullOrWhiteSpace(context.BuildLog))
        {
            return context.BuildLog
                .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Where(l => l.Contains("error", StringComparison.OrdinalIgnoreCase))
                .Take(6)
                .ToList();
        }

        return null;
    }

    private static bool IsMutatingTool(string toolName) =>
        string.Equals(toolName, "bash", StringComparison.OrdinalIgnoreCase)
        || string.Equals(toolName, "edit_file", StringComparison.OrdinalIgnoreCase)
        || string.Equals(toolName, "write_file", StringComparison.OrdinalIgnoreCase)
        || string.Equals(toolName, "apply_patch", StringComparison.OrdinalIgnoreCase)
        || string.Equals(toolName, "run_build", StringComparison.OrdinalIgnoreCase)
        || string.Equals(toolName, "run_tests", StringComparison.OrdinalIgnoreCase);
}
