using Libr4.IDE.Application.AutonomousAppGeneration.Services.Pipeline;
using Libr4.IDE.Application.Obscura;
using Libr4.IDE.Domain.AutonomousAppGeneration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Libr4.IDE.Application.AutonomousAppGeneration.GitHubActionsDispatch;

public interface IGitHubShipService
{
    Task<GitHubShipResult> ShipAsync(GenerationContext context, CancellationToken ct = default);
}

public sealed class GitHubShipService : IGitHubShipService
{
    private readonly GitHubActionsDispatchOptions _options;
    private readonly IGitHubApiClient _client;
    private readonly IObscuraEvidenceStore? _obscuraEvidence;
    private readonly ILogger<GitHubShipService> _logger;

    public GitHubShipService(
        IOptions<GitHubActionsDispatchOptions> options,
        IGitHubApiClient client,
        ILogger<GitHubShipService> logger,
        IObscuraEvidenceStore? obscuraEvidence = null)
    {
        _options = options.Value;
        _client = client;
        _logger = logger;
        _obscuraEvidence = obscuraEvidence;
    }

    public async Task<GitHubShipResult> ShipAsync(GenerationContext context, CancellationToken ct = default)
    {
        if (!_options.Enabled)
            return GitHubShipResult.SkippedResult("github_ship_disabled");

        if (!_options.HasCredentials)
            return GitHubShipResult.SkippedResult("github_credentials_missing");

        var verifyPassed = context.Items.TryGetValue("verify_passed", out var passedObj) && passedObj is true;
        if (_options.RequireVerifyPass && !verifyPassed)
            return GitHubShipResult.SkippedResult("verify_not_passed");

        var repo = new GitHubRepositoryRef(_options.Owner, _options.Repository);
        var runId = context.Orchestrator.Id;
        var appName = context.Plan?.ApplicationName ?? "generated-app";
        var headBranch = BuildHeadBranch(runId);
        var verifySummary = context.Items.TryGetValue("verify_summary", out var summaryObj)
            ? summaryObj?.ToString()
            : null;

        try
        {
            if (_options.DispatchWorkflow)
            {
                await _client.DispatchWorkflowAsync(
                    new GitHubWorkflowDispatchRequest(
                        repo,
                        _options.WorkflowFile,
                        _options.BaseBranch,
                        new Dictionary<string, string>(StringComparer.Ordinal)
                        {
                            ["run_id"] = runId.ToString("D"),
                            ["app_name"] = appName,
                            ["head_branch"] = headBranch,
                            ["verify_summary"] = Truncate(verifySummary ?? string.Empty, 500)
                        }),
                    ct).ConfigureAwait(false);
            }

            GitHubPullRequestCreateResult? pr = null;
            if (_options.CreatePullRequest)
            {
                var files = SelectFiles(context);
                if (files.Count == 0)
                {
                    _logger.LogWarning("GitHub ship skipped PR for run {RunId}: no files", runId);
                }
                else
                {
                    pr = await _client.CreatePullRequestWithFilesAsync(
                        new GitHubPullRequestRequest(
                            repo,
                            _options.BaseBranch,
                            headBranch,
                            $"Libr4 autogen: {appName}",
                            BuildPullRequestBody(context, verifySummary),
                            files),
                        ct).ConfigureAwait(false);

                    await TryAttachObscuraManifestCommentAsync(repo, runId, pr.Number, ct).ConfigureAwait(false);
                }
            }

            var summary = pr is null
                ? $"workflow_dispatched branch={headBranch}"
                : $"workflow_dispatched pr=#{pr.Number} url={pr.HtmlUrl}";

            _logger.LogInformation("GitHub ship completed for run {RunId}: {Summary}", runId, summary);

            return GitHubShipResult.Succeeded(
                summary,
                workflowRunId: null,
                pullRequestNumber: pr?.Number,
                pullRequestUrl: pr?.HtmlUrl,
                headBranch);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GitHub ship failed for run {RunId}", runId);
            return GitHubShipResult.Failed($"{ex.GetType().Name}: {ex.Message}");
        }
    }

    public static string BuildHeadBranch(Guid runId, string prefix = "libr4/autogen-") =>
        $"{prefix}{runId:N}".ToLowerInvariant();

    private string BuildHeadBranch(Guid runId) =>
        BuildHeadBranch(runId, _options.BranchPrefix);

    private IReadOnlyList<GitHubFileCommit> SelectFiles(GenerationContext context)
    {
        var source = context.Files.Count > 0
            ? context.Files
            : context.Orchestrator.Files.ToList();

        return source
            .Where(f => !string.IsNullOrWhiteSpace(f.RelativePath))
            .Where(f => !string.IsNullOrWhiteSpace(f.Content))
            .Where(f => f.Content!.Length <= _options.MaxFileBytes)
            .Take(_options.MaxFilesPerPullRequest)
            .Select(f => new GitHubFileCommit(f.RelativePath, f.Content!))
            .ToList();
    }

    private static string BuildPullRequestBody(GenerationContext context, string? verifySummary)
    {
        var lines = new List<string>
        {
            "## Libr4 Autonomous App Generation",
            string.Empty,
            $"- Run ID: `{context.Orchestrator.Id:D}`",
            $"- Application: `{context.Plan?.ApplicationName ?? "unknown"}`",
            $"- Files: {context.Orchestrator.Files.Count}",
            string.Empty,
            "Generated by Libr4 ship stage after verify pass."
        };

        if (!string.IsNullOrWhiteSpace(verifySummary))
        {
            lines.Add(string.Empty);
            lines.Add("### Verify summary");
            lines.Add("```");
            lines.Add(Truncate(verifySummary, 4000));
            lines.Add("```");
        }

        return string.Join('\n', lines);
    }

    private static string Truncate(string value, int max) =>
        value.Length <= max ? value : value[..max] + "…";

    private async Task TryAttachObscuraManifestCommentAsync(
        GitHubRepositoryRef repo,
        Guid runId,
        int pullRequestNumber,
        CancellationToken ct)
    {
        if (!_options.AttachObscuraManifestComment || _obscuraEvidence is null)
            return;

        try
        {
            var bundle = _obscuraEvidence.List(runId);
            var comment = ObscuraPrCommentFormatter.BuildCommentBody(
                runId,
                bundle.Artifacts,
                _options.PublicApiBaseUrl);
            if (string.IsNullOrWhiteSpace(comment))
                return;

            await _client.CreatePullRequestCommentAsync(repo, pullRequestNumber, comment, ct).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to attach Obscura manifest comment to PR #{Number} for run {RunId}", pullRequestNumber, runId);
        }
    }
}
