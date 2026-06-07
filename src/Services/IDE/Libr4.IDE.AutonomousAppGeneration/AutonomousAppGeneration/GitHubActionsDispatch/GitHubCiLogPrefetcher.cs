using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Libr4.IDE.Application.AutonomousAppGeneration.GitHubActionsDispatch;

public interface IGitHubCiLogPrefetcher
{
    Task<string?> PrefetchAsync(string? ciLogsUrl, CancellationToken ct = default);
}

public sealed class GitHubCiLogPrefetcher : IGitHubCiLogPrefetcher
{
    private readonly IGitHubApiClient _client;
    private readonly CiRepairOptions _options;
    private readonly ILogger<GitHubCiLogPrefetcher> _logger;

    public GitHubCiLogPrefetcher(
        IGitHubApiClient client,
        IOptions<CiRepairOptions> options,
        ILogger<GitHubCiLogPrefetcher> logger)
    {
        _client = client;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<string?> PrefetchAsync(string? ciLogsUrl, CancellationToken ct = default)
    {
        var runId = CiRepairLogParser.TryParseRunIdFromLogsUrl(ciLogsUrl);
        if (runId is null)
        {
            _logger.LogDebug("CI log prefetch skipped: no run id in url {Url}", ciLogsUrl);
            return null;
        }

        var logs = await _client.TryFetchWorkflowRunLogExcerptAsync(runId.Value, _options.MaxLogChars, ct)
            .ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(logs))
            _logger.LogWarning("CI log prefetch returned empty for run {RunId}", runId);

        return logs;
    }
}
