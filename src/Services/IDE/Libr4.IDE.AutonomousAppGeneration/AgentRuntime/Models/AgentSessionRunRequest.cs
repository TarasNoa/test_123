using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Abstractions;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Prompting;
using Libr4.IDE.Application.AutonomousAppGeneration.Infrastructure.Fim;
using Libr4.IDE.Domain.AutonomousAppGeneration;

namespace Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Models;

public enum AgentSessionMode
{
    Repair,
    Generation
}

public sealed record AgentSessionRunRequest(
    string Objective,
    ShadowWorkspaceContext Workspace,
    IList<GeneratedFile> WorkingFiles,
    GenerationPlan Plan,
    IShadowWorkspaceAccessor Accessor,
    AgentSessionMode Mode = AgentSessionMode.Repair,
    string? BuildLog = null,
    IReadOnlyList<string>? TargetRelativePaths = null,
    int? MaxTurns = null,
    Guid? RunId = null,
    string? TenantUserId = null,
    string? ResumeSessionId = null,
    BuiltinPromptStage? PromptStage = null,
    int RepairAttempt = 1,
    IReadOnlyList<string>? LastErrors = null,
    IReadOnlyList<string>? ManifestFiles = null,
    IReadOnlyList<string>? AllowedTools = null,
    FimGenerationContext? Fim = null,
    string? ContextFragments = null,
    string? RequestFingerprint = null,
    string? SubagentRole = null,
    string? ModelOverride = null,
    Guid? SpaceId = null);
