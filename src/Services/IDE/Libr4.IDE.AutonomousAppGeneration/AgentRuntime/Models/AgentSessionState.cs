using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Permissions;
using Libr4.IDE.Application.AutonomousAppGeneration.Infrastructure.Fim;
using Libr4.IDE.Application.AutonomousAppGeneration.Services;
using Libr4.IDE.Domain.AutonomousAppGeneration;

namespace Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Models;

public sealed class AgentSessionState
{
    public string SessionId { get; set; } = Guid.NewGuid().ToString("D");
    public Guid? RunId { get; init; }
    public string? TenantUserId { get; set; }
    public int SubagentDepth { get; set; }
    public bool PlanMode { get; set; }
    public AgentPermissionMode PermissionMode { get; set; } = AgentPermissionMode.BypassPermissions;
    public int CurrentStepNumber { get; set; }
    public string? LastToolInputJson { get; set; }
    public long LastToolDurationMs { get; set; }
    public List<string> ReasoningLog { get; } = new();
    public string? LastCheckpointId { get; set; }
    public List<AgentTodoItem> Todos { get; } = new();
    public Dictionary<string, CheckpointSnapshot> Checkpoints { get; } = new(StringComparer.OrdinalIgnoreCase);
    public bool DelegateBackgroundChild { get; set; }
    public FimGenerationContext? FimContext { get; set; }
    public string? LastAccessedRelativePath { get; set; }
    public string? ActiveLibr4Context { get; set; }
    public Guid? SpaceId { get; set; }
    public IReadOnlyList<string>? LastErrors { get; set; }
    public HashSet<string> ActivatedSkills { get; } = new(StringComparer.OrdinalIgnoreCase);
    public int MemoryWriteCount { get; set; }
}

public sealed record AgentTodoItem(
    string Id,
    string Content,
    string Status,
    string? ActiveForm = null);
