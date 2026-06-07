using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Libr4.IDE.Application.AutonomousAppGeneration.GitHubActionsDispatch;

public sealed class GitHubApiClient : IGitHubApiClient
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private readonly HttpClient _http;
    private readonly GitHubActionsDispatchOptions _options;
    private readonly ILogger<GitHubApiClient> _logger;

    public GitHubApiClient(
        HttpClient http,
        IOptions<GitHubActionsDispatchOptions> options,
        ILogger<GitHubApiClient> logger)
    {
        _http = http;
        _options = options.Value;
        _logger = logger;
    }

    public async Task DispatchWorkflowAsync(GitHubWorkflowDispatchRequest request, CancellationToken ct = default)
    {
        var url = $"repos/{request.Repository.Owner}/{request.Repository.Repository}/actions/workflows/{request.WorkflowFile}/dispatches";
        var payload = new
        {
            @ref = request.Ref,
            inputs = request.Inputs
        };

        using var response = await SendAsync(HttpMethod.Post, url, payload, ct).ConfigureAwait(false);
        await EnsureSuccessAsync(response, "workflow_dispatch", ct).ConfigureAwait(false);
        _logger.LogInformation(
            "Dispatched GitHub workflow {Workflow} on {Owner}/{Repo} ref={Ref}",
            request.WorkflowFile,
            request.Repository.Owner,
            request.Repository.Repository,
            request.Ref);
    }

    public async Task<GitHubPullRequestCreateResult> CreatePullRequestWithFilesAsync(
        GitHubPullRequestRequest request,
        CancellationToken ct = default)
    {
        var repo = request.Repository;
        var baseSha = await GetBranchCommitShaAsync(repo, request.BaseBranch, ct).ConfigureAwait(false);

        var treeItems = new List<object>();
        foreach (var file in request.Files)
        {
            var blobSha = await CreateBlobAsync(repo, file.Content, ct).ConfigureAwait(false);
            treeItems.Add(new
            {
                path = NormalizeTreePath(file.Path),
                mode = "100644",
                type = "blob",
                sha = blobSha
            });
        }

        var treeSha = await CreateTreeAsync(repo, treeItems, ct).ConfigureAwait(false);
        var commitSha = await CreateCommitAsync(
            repo,
            $"Libr4 autogen: {request.Title}",
            treeSha,
            new[] { baseSha },
            ct).ConfigureAwait(false);

        await CreateBranchRefAsync(repo, request.HeadBranch, commitSha, ct).ConfigureAwait(false);

        var pr = await CreatePullRequestAsync(
            repo,
            request.Title,
            request.Body,
            request.HeadBranch,
            request.BaseBranch,
            ct).ConfigureAwait(false);

        return pr;
    }

    public async Task<string?> TryFetchWorkflowRunLogExcerptAsync(long runId, int maxChars, CancellationToken ct = default)
    {
        if (!_options.HasCredentials || maxChars <= 0)
            return null;

        var repo = new GitHubRepositoryRef(_options.Owner, _options.Repository);
        var jobsUrl = $"repos/{repo.Owner}/{repo.Repository}/actions/runs/{runId}/jobs?per_page=20";
        using var jobsResponse = await SendAsync(HttpMethod.Get, jobsUrl, body: null, ct).ConfigureAwait(false);
        if (!jobsResponse.IsSuccessStatusCode)
        {
            _logger.LogWarning(
                "GitHub CI log prefetch failed to list jobs for run {RunId}: {Status}",
                runId,
                (int)jobsResponse.StatusCode);
            return null;
        }

        var jobsDoc = await jobsResponse.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: ct).ConfigureAwait(false);
        if (!jobsDoc.TryGetProperty("jobs", out var jobs) || jobs.ValueKind != JsonValueKind.Array)
            return null;

        long? selectedJobId = null;
        foreach (var job in jobs.EnumerateArray())
        {
            var conclusion = job.TryGetProperty("conclusion", out var conEl) ? conEl.GetString() : null;
            if (!string.Equals(conclusion, "failure", StringComparison.OrdinalIgnoreCase)
                && !string.Equals(conclusion, "cancelled", StringComparison.OrdinalIgnoreCase))
                continue;

            if (job.TryGetProperty("id", out var idEl) && idEl.TryGetInt64(out var jobId))
            {
                selectedJobId = jobId;
                break;
            }
        }

        if (selectedJobId is null)
        {
            foreach (var job in jobs.EnumerateArray())
            {
                if (job.TryGetProperty("id", out var idEl) && idEl.TryGetInt64(out var jobId))
                {
                    selectedJobId = jobId;
                    break;
                }
            }
        }

        if (selectedJobId is null)
            return null;

        var logsUrl = $"repos/{repo.Owner}/{repo.Repository}/actions/jobs/{selectedJobId.Value}/logs";
        using var logsResponse = await SendAsync(HttpMethod.Get, logsUrl, body: null, ct).ConfigureAwait(false);
        if (!logsResponse.IsSuccessStatusCode)
        {
            _logger.LogWarning(
                "GitHub CI log prefetch failed for job {JobId}: {Status}",
                selectedJobId,
                (int)logsResponse.StatusCode);
            return null;
        }

        var raw = await logsResponse.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(raw))
            return null;

        return raw.Length <= maxChars ? raw : raw[^maxChars..];
    }

    public async Task CreatePullRequestCommentAsync(
        GitHubRepositoryRef repo,
        int pullRequestNumber,
        string body,
        CancellationToken ct = default)
    {
        var url = $"repos/{repo.Owner}/{repo.Repository}/issues/{pullRequestNumber}/comments";
        using var response = await SendAsync(HttpMethod.Post, url, new { body }, ct).ConfigureAwait(false);
        await EnsureSuccessAsync(response, "create_pr_comment", ct).ConfigureAwait(false);
        _logger.LogInformation(
            "Posted PR comment on {Owner}/{Repo}#{Number}",
            repo.Owner,
            repo.Repository,
            pullRequestNumber);
    }

    private async Task<string> GetBranchCommitShaAsync(
        GitHubRepositoryRef repo,
        string branch,
        CancellationToken ct)
    {
        var url = $"repos/{repo.Owner}/{repo.Repository}/git/ref/heads/{Uri.EscapeDataString(branch)}";
        using var response = await SendAsync(HttpMethod.Get, url, body: null, ct).ConfigureAwait(false);
        await EnsureSuccessAsync(response, "get_ref", ct).ConfigureAwait(false);
        var doc = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: ct).ConfigureAwait(false);
        return doc.GetProperty("object").GetProperty("sha").GetString()
               ?? throw new InvalidOperationException("github_ref_missing_sha");
    }

    private async Task<string> CreateBlobAsync(GitHubRepositoryRef repo, string content, CancellationToken ct)
    {
        var url = $"repos/{repo.Owner}/{repo.Repository}/git/blobs";
        var bytes = Encoding.UTF8.GetBytes(content);
        var payload = new
        {
            content = Convert.ToBase64String(bytes),
            encoding = "base64"
        };

        using var response = await SendAsync(HttpMethod.Post, url, payload, ct).ConfigureAwait(false);
        await EnsureSuccessAsync(response, "create_blob", ct).ConfigureAwait(false);
        var doc = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: ct).ConfigureAwait(false);
        return doc.GetProperty("sha").GetString()
               ?? throw new InvalidOperationException("github_blob_missing_sha");
    }

    private async Task<string> CreateTreeAsync(
        GitHubRepositoryRef repo,
        IReadOnlyList<object> tree,
        CancellationToken ct)
    {
        var url = $"repos/{repo.Owner}/{repo.Repository}/git/trees";
        var payload = new { tree };
        using var response = await SendAsync(HttpMethod.Post, url, payload, ct).ConfigureAwait(false);
        await EnsureSuccessAsync(response, "create_tree", ct).ConfigureAwait(false);
        var doc = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: ct).ConfigureAwait(false);
        return doc.GetProperty("sha").GetString()
               ?? throw new InvalidOperationException("github_tree_missing_sha");
    }

    private async Task<string> CreateCommitAsync(
        GitHubRepositoryRef repo,
        string message,
        string treeSha,
        IReadOnlyList<string> parents,
        CancellationToken ct)
    {
        var url = $"repos/{repo.Owner}/{repo.Repository}/git/commits";
        var payload = new
        {
            message,
            tree = treeSha,
            parents
        };

        using var response = await SendAsync(HttpMethod.Post, url, payload, ct).ConfigureAwait(false);
        await EnsureSuccessAsync(response, "create_commit", ct).ConfigureAwait(false);
        var doc = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: ct).ConfigureAwait(false);
        return doc.GetProperty("sha").GetString()
               ?? throw new InvalidOperationException("github_commit_missing_sha");
    }

    private async Task CreateBranchRefAsync(
        GitHubRepositoryRef repo,
        string branch,
        string commitSha,
        CancellationToken ct)
    {
        var url = $"repos/{repo.Owner}/{repo.Repository}/git/refs";
        var payload = new
        {
            @ref = $"refs/heads/{branch}",
            sha = commitSha
        };

        using var response = await SendAsync(HttpMethod.Post, url, payload, ct).ConfigureAwait(false);
        if (response.StatusCode == System.Net.HttpStatusCode.UnprocessableEntity
            || response.StatusCode == System.Net.HttpStatusCode.Conflict)
        {
            var patchUrl = $"repos/{repo.Owner}/{repo.Repository}/git/refs/heads/{Uri.EscapeDataString(branch)}";
            using var patchResponse = await SendAsync(HttpMethod.Patch, patchUrl, new { sha = commitSha, force = true }, ct)
                .ConfigureAwait(false);
            await EnsureSuccessAsync(patchResponse, "update_ref", ct).ConfigureAwait(false);
            return;
        }

        await EnsureSuccessAsync(response, "create_ref", ct).ConfigureAwait(false);
    }

    private async Task<GitHubPullRequestCreateResult> CreatePullRequestAsync(
        GitHubRepositoryRef repo,
        string title,
        string body,
        string headBranch,
        string baseBranch,
        CancellationToken ct)
    {
        var url = $"repos/{repo.Owner}/{repo.Repository}/pulls";
        var payload = new
        {
            title,
            body,
            head = headBranch,
            @base = baseBranch
        };

        using var response = await SendAsync(HttpMethod.Post, url, payload, ct).ConfigureAwait(false);
        await EnsureSuccessAsync(response, "create_pull_request", ct).ConfigureAwait(false);
        var doc = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: ct).ConfigureAwait(false);
        var number = doc.GetProperty("number").GetInt32();
        var htmlUrl = doc.GetProperty("html_url").GetString() ?? string.Empty;
        var head = doc.GetProperty("head").GetProperty("ref").GetString() ?? headBranch;
        return new GitHubPullRequestCreateResult(number, htmlUrl, head);
    }

    private Task<HttpResponseMessage> SendAsync(
        HttpMethod method,
        string relativeUrl,
        object? body,
        CancellationToken ct)
    {
        var request = new HttpRequestMessage(method, relativeUrl);
        ApplyAuth(request);
        if (body is not null)
            request.Content = JsonContent.Create(body, options: JsonOptions);
        return _http.SendAsync(request, ct);
    }

    private void ApplyAuth(HttpRequestMessage request)
    {
        if (!string.IsNullOrWhiteSpace(_options.PersonalAccessToken))
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.PersonalAccessToken);
        request.Headers.UserAgent.ParseAdd("Libr4-AutonomousAppGeneration");
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
    }

    private static async Task EnsureSuccessAsync(HttpResponseMessage response, string operation, CancellationToken ct)
    {
        if (response.IsSuccessStatusCode)
            return;

        var body = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
        throw new InvalidOperationException(
            $"github_{operation}_failed: {(int)response.StatusCode} {response.ReasonPhrase}: {body}");
    }

    private static string NormalizeTreePath(string path) =>
        path.Replace('\\', '/').TrimStart('/');
}
