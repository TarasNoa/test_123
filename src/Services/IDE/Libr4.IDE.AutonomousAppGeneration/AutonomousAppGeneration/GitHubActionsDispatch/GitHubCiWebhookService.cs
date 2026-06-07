using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Libr4.IDE.Application.AutonomousAppGeneration.Fleet;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Libr4.IDE.Application.AutonomousAppGeneration.GitHubActionsDispatch;

public sealed class GitHubCiWebhookOptions
{
    public const string SectionName = "AutonomousAppGeneration:GitHubCiWebhook";

    public bool Enabled { get; set; } = true;

    public string? WebhookSecret { get; set; }
}

public interface IGitHubCiWebhookService
{
    Task<bool> HandleAsync(string? signature256, string rawBody, CancellationToken ct = default);
}

public sealed class GitHubCiWebhookService : IGitHubCiWebhookService
{
    private readonly IFleetShipSyncService _fleetSync;
    private readonly GitHubCiWebhookOptions _options;
    private readonly ILogger<GitHubCiWebhookService> _logger;

    public GitHubCiWebhookService(
        IFleetShipSyncService fleetSync,
        IOptions<GitHubCiWebhookOptions> options,
        ILogger<GitHubCiWebhookService> logger)
    {
        _fleetSync = fleetSync;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<bool> HandleAsync(string? signature256, string rawBody, CancellationToken ct = default)
    {
        if (!_options.Enabled)
            return false;

        if (!string.IsNullOrWhiteSpace(_options.WebhookSecret)
            && !ValidateSignature(signature256, rawBody, _options.WebhookSecret))
        {
            _logger.LogWarning("GitHub CI webhook rejected: invalid signature");
            return false;
        }

        using var doc = JsonDocument.Parse(rawBody);
        var root = doc.RootElement;
        var eventType = root.TryGetProperty("action", out _) ? "check_run" : "workflow_run";

        string? action = null;
        string? headBranch = null;
        string? conclusion = null;
        string? htmlUrl = null;

        if (root.TryGetProperty("workflow_run", out var workflowRun))
        {
            eventType = "workflow_run";
            action = workflowRun.TryGetProperty("status", out var statusEl) ? statusEl.GetString() : null;
            conclusion = workflowRun.TryGetProperty("conclusion", out var conEl) ? conEl.GetString() : null;
            headBranch = workflowRun.TryGetProperty("head_branch", out var branchEl) ? branchEl.GetString() : null;
            htmlUrl = workflowRun.TryGetProperty("html_url", out var urlEl) ? urlEl.GetString() : null;
            if (root.TryGetProperty("action", out var actEl))
                action = actEl.GetString() ?? action;
        }
        else if (root.TryGetProperty("check_run", out var checkRun))
        {
            eventType = "check_run";
            action = root.TryGetProperty("action", out var actEl) ? actEl.GetString() : null;
            conclusion = checkRun.TryGetProperty("conclusion", out var conEl) ? conEl.GetString() : null;
            htmlUrl = checkRun.TryGetProperty("html_url", out var urlEl) ? urlEl.GetString() : null;
            if (checkRun.TryGetProperty("check_suite", out var suite)
                && suite.TryGetProperty("head_branch", out var branchEl))
                headBranch = branchEl.GetString();
        }

        var payload = new GitHubCiWebhookPayload(eventType, action, headBranch, conclusion, htmlUrl);
        await _fleetSync.ApplyCiWebhookAsync(payload, ct).ConfigureAwait(false);
        return true;
    }

    private static bool ValidateSignature(string? signatureHeader, string body, string secret)
    {
        if (string.IsNullOrWhiteSpace(signatureHeader) || !signatureHeader.StartsWith("sha256=", StringComparison.OrdinalIgnoreCase))
            return false;

        var expectedHex = signatureHeader["sha256=".Length..];
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(body));
        var actualHex = Convert.ToHexString(hash).ToLowerInvariant();
        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(actualHex),
            Encoding.UTF8.GetBytes(expectedHex.ToLowerInvariant()));
    }
}
