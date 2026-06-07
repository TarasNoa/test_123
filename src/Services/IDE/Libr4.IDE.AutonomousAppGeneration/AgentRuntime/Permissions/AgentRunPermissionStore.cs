using System.Collections.Concurrent;

namespace Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Permissions;

public interface IAgentRunPermissionStore
{
    AgentPermissionMode Get(Guid runId);
    void Set(Guid runId, AgentPermissionMode mode);
    IReadOnlyList<AgentPermissionPrompt> GetPendingPrompts(Guid runId);
    IReadOnlyList<AgentPermissionPrompt> GetAllPrompts(Guid runId);
    void EnqueuePrompt(Guid runId, AgentPermissionPrompt prompt);
    void ResolvePrompt(Guid runId, string promptId, bool accepted);
    bool IsDenied(Guid runId, string toolName, string? path);
}

public sealed record AgentPermissionPrompt(
    string Id,
    string ToolName,
    string? Path,
    string Reason,
    DateTime CreatedAtUtc,
    bool? Accepted,
    string Kind = "tool");

public sealed class AgentRunPermissionStore : IAgentRunPermissionStore
{
    private readonly ConcurrentDictionary<Guid, AgentPermissionMode> _modes = new();
    private readonly ConcurrentDictionary<Guid, List<AgentPermissionPrompt>> _prompts = new();

    public AgentPermissionMode Get(Guid runId) =>
        _modes.TryGetValue(runId, out var mode) ? mode : AgentPermissionMode.BypassPermissions;

    public void Set(Guid runId, AgentPermissionMode mode) => _modes[runId] = mode;

    public IReadOnlyList<AgentPermissionPrompt> GetPendingPrompts(Guid runId) =>
        GetAllPrompts(runId).Where(p => p.Accepted is null).ToList();

    public IReadOnlyList<AgentPermissionPrompt> GetAllPrompts(Guid runId)
    {
        if (!_prompts.TryGetValue(runId, out var list))
            return Array.Empty<AgentPermissionPrompt>();

        lock (list)
            return list.ToList();
    }

    public bool IsDenied(Guid runId, string toolName, string? path) =>
        GetAllPrompts(runId).Any(p =>
            string.Equals(p.ToolName, toolName, StringComparison.OrdinalIgnoreCase)
            && (path is null || string.Equals(p.Path, path, StringComparison.OrdinalIgnoreCase))
            && p.Accepted == false);

    public void EnqueuePrompt(Guid runId, AgentPermissionPrompt prompt)
    {
        var list = _prompts.GetOrAdd(runId, _ => new List<AgentPermissionPrompt>());
        lock (list)
        {
            list.Add(prompt);
        }
    }

    public void ResolvePrompt(Guid runId, string promptId, bool accepted)
    {
        if (!_prompts.TryGetValue(runId, out var list))
            return;

        lock (list)
        {
            for (var i = 0; i < list.Count; i++)
            {
                if (!string.Equals(list[i].Id, promptId, StringComparison.OrdinalIgnoreCase))
                    continue;
                var p = list[i];
                list[i] = p with { Accepted = accepted };
                break;
            }
        }
    }
}
