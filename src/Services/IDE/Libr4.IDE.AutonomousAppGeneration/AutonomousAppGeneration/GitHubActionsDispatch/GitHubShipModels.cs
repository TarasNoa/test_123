namespace Libr4.IDE.Application.AutonomousAppGeneration.GitHubActionsDispatch;

public sealed record GitHubShipResult(
    bool Success,
    bool Skipped,
    string Summary,
    long? WorkflowRunId,
    int? PullRequestNumber,
    string? PullRequestUrl,
    string? HeadBranch)
{
    public static GitHubShipResult SkippedResult(string reason) =>
        new(false, true, reason, null, null, null, null);

    public static GitHubShipResult Failed(string reason) =>
        new(false, false, reason, null, null, null, null);

    public static GitHubShipResult Succeeded(
        string summary,
        long? workflowRunId,
        int? pullRequestNumber,
        string? pullRequestUrl,
        string headBranch) =>
        new(true, false, summary, workflowRunId, pullRequestNumber, pullRequestUrl, headBranch);
}

public sealed record GitHubRepositoryRef(string Owner, string Repository);

public sealed record GitHubFileCommit(string Path, string Content);

public sealed record GitHubWorkflowDispatchRequest(
    GitHubRepositoryRef Repository,
    string WorkflowFile,
    string Ref,
    IReadOnlyDictionary<string, string> Inputs);

public sealed record GitHubPullRequestRequest(
    GitHubRepositoryRef Repository,
    string BaseBranch,
    string HeadBranch,
    string Title,
    string Body,
    IReadOnlyList<GitHubFileCommit> Files);

public interface IGitHubApiClient
{
    Task DispatchWorkflowAsync(GitHubWorkflowDispatchRequest request, CancellationToken ct = default);

    Task<GitHubPullRequestCreateResult> CreatePullRequestWithFilesAsync(
        GitHubPullRequestRequest request,
        CancellationToken ct = default);

    Task<string?> TryFetchWorkflowRunLogExcerptAsync(long runId, int maxChars, CancellationToken ct = default);

    Task CreatePullRequestCommentAsync(
        GitHubRepositoryRef repository,
        int pullRequestNumber,
        string body,
        CancellationToken ct = default);
}

public sealed record GitHubPullRequestCreateResult(
    int Number,
    string HtmlUrl,
    string HeadBranch);
