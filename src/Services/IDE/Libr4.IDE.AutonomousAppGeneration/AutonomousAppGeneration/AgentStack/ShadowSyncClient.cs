using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Libr4.IDE.Application.AutonomousAppGeneration.AgentStack;

public interface IShadowSyncClient
{
    Task<bool> EnsureHealthyAsync(CancellationToken ct = default);

    Task<bool> TriggerSyncAsync(string workspaceId, CancellationToken ct = default);
}

public sealed class ShadowSyncClient : IShadowSyncClient
{
    private readonly HttpClient _http;
    private readonly AgentStackOptions _options;
    private readonly ILogger<ShadowSyncClient> _logger;

    public ShadowSyncClient(
        HttpClient http,
        IOptions<AgentStackOptions> options,
        ILogger<ShadowSyncClient> logger)
    {
        _http = http;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<bool> EnsureHealthyAsync(CancellationToken ct = default)
    {
        if (!_options.EnableShadowSyncGate)
            return true;

        try
        {
            using var response = await _http.GetAsync("/health", ct).ConfigureAwait(false);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Shadow sync health check failed");
            return false;
        }
    }

    public async Task<bool> TriggerSyncAsync(string workspaceId, CancellationToken ct = default)
    {
        if (!_options.EnableShadowSyncGate)
            return true;

        if (!await EnsureHealthyAsync(ct).ConfigureAwait(false))
            return false;

        try
        {
            using var response = await _http.PostAsJsonAsync(
                "/sync",
                new { workspaceId },
                ct).ConfigureAwait(false);
            if (response.IsSuccessStatusCode)
                return true;

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                _logger.LogDebug("Shadow sync /sync endpoint unavailable — health gate only");
                return true;
            }

            _logger.LogWarning("Shadow sync trigger failed with status {Status}", (int)response.StatusCode);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Shadow sync trigger failed for workspace {WorkspaceId}", workspaceId);
            return false;
        }
    }
}
