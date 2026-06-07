namespace Libr4.IDE.Application.AutonomousAppGeneration.WorkspaceTrust;

public enum WorkspaceSandboxPolicy
{
    Strict,
    Standard,
    Permissive
}

public enum WorkspaceHostMode
{
    LocalOnly,
    CloudAllowed
}

public sealed record WorkspaceTrustRecord(
    string WorkspaceHash,
    WorkspaceSandboxPolicy SandboxPolicy,
    WorkspaceHostMode HostMode,
    DateTime DecidedAtUtc);

public sealed record WorkspaceTrustDecision(
    string WorkspaceHash,
    WorkspaceSandboxPolicy SandboxPolicy,
    WorkspaceHostMode HostMode,
    bool FromStore,
    bool FromConfigOverride,
    bool DenyCloudInference);

public sealed record WorkspaceTrustPrompt(
    string PromptId,
    string WorkspaceHash,
    WorkspaceSandboxPolicy SuggestedSandboxPolicy,
    WorkspaceHostMode SuggestedHostMode,
    string Message,
    DateTime CreatedAtUtc);

public sealed record WorkspaceTrustRunState(
    Guid RunId,
    string WorkspaceHash,
    bool IsReady,
    bool AwaitingPrompt,
    WorkspaceTrustPrompt? PendingPrompt,
    WorkspaceTrustDecision? Decision);

public sealed record WorkspaceTrustResolveRequest(
    string PromptId,
    WorkspaceSandboxPolicy SandboxPolicy,
    WorkspaceHostMode HostMode,
    bool RememberChoice);
