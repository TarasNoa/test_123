namespace Libr4.IDE.Application.AutonomousAppGeneration.Spaces;

public sealed record GitWorktreeInfo(string Path, string Branch, string HeadCommit);

public sealed record GitMergeReport(bool Success, string Output, IReadOnlyList<string> Conflicts);

public sealed record GitMergePreviewFile(
    string Path,
    int Insertions,
    int Deletions,
    string ChangeKind);

public sealed record GitMergePreview(
    string SourceBranch,
    string IntegrationBranch,
    IReadOnlyList<GitMergePreviewFile> Files,
    string DiffStat,
    string UnifiedDiff);

public interface IGitWorktreeService
{
    Task<string> InitOrCloneMainWorktreeAsync(
        string spaceRoot,
        string? repositoryUrl,
        string baseBranch,
        CancellationToken ct = default);

    Task<GitWorktreeInfo> AddAgentWorktreeAsync(
        string spaceRoot,
        string mainWorktreePath,
        string memberId,
        SpaceMemberRole role,
        string branchName,
        CancellationToken ct = default);

    Task RemoveWorktreeAsync(string spaceRoot, string worktreePath, bool force = false, CancellationToken ct = default);

    Task<GitMergeReport> MergeBranchAsync(
        string integrationWorktreePath,
        string sourceBranch,
        string integrationBranch,
        CancellationToken ct = default);

    Task<GitMergePreview> PreviewMergeAsync(
        string integrationWorktreePath,
        string sourceBranch,
        string integrationBranch,
        int maxDiffChars = 32_000,
        CancellationToken ct = default);

    void EnsurePathWithinSpace(string spaceRoot, string targetPath);
}
