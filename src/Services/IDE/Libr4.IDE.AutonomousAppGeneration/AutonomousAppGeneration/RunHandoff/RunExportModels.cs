namespace Libr4.IDE.Application.AutonomousAppGeneration.RunHandoff;

public sealed record RunExportManifest(
    string SchemaVersion,
    Guid RunId,
    Guid? SourceRunId,
    string? TenantId,
    string? RequestFingerprint,
    Guid? ShadowWorkspaceId,
    int FileCount,
    int LastStepNumber,
    string Status,
    string? FailureReason,
    DateTime ExportedAtUtc,
    string BundleSha256,
    long BundleBytes,
    RunExportHandoffSnapshot Handoff);

public sealed record RunExportHandoffSnapshot(
    string PermissionMode,
    IReadOnlyList<RunExportPermissionPrompt> PermissionPrompts,
    RunExportFlowSnapshot? Flow,
    IReadOnlyList<RunExportPlaybookHint> PlaybookHints,
    IReadOnlyList<RunExportSpaceMembership> SpaceMembership);

public sealed record RunExportPermissionPrompt(
    string Id,
    string ToolName,
    string? Path,
    string Reason,
    DateTime CreatedAtUtc,
    bool? Accepted);

public sealed record RunExportFlowSnapshot(
    string FlowId,
    int CurrentStepIndex,
    string? CurrentStepId,
    DateTime UpdatedAtUtc);

public sealed record RunExportPlaybookHint(
    string ErrorSignature,
    string FixPattern,
    double Score,
    DateTime LastUsedAtUtc);

public sealed record RunExportSpaceMembership(
    Guid SpaceId,
    string SpaceName,
    string MemberId,
    string Role,
    string BranchName,
    string Status);

public sealed record RunExportResult(
    Guid RunId,
    string ExportId,
    string ContentSha256,
    string ArtifactPath,
    string DownloadPath,
    long BundleBytes,
    DateTime GeneratedAtUtc,
    DateTime ExpiresAtUtc);
