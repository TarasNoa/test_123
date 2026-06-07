using Libr4.IDE.Application.Obscura;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Libr4.IDE.Application.AutonomousAppGeneration.AgentStack;

public interface IAgentStackHealthService
{
    Task<AgentStackHealthStatus> CheckAsync(CancellationToken ct = default);
}

public sealed class AgentStackHealthService : IAgentStackHealthService
{
    private readonly IObscuraHealthService _obscuraHealth;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly AgentStackOptions _options;
    private readonly ILogger<AgentStackHealthService> _logger;

    public AgentStackHealthService(
        IObscuraHealthService obscuraHealth,
        IHttpClientFactory httpClientFactory,
        IOptions<AgentStackOptions> options,
        ILogger<AgentStackHealthService> logger)
    {
        _obscuraHealth = obscuraHealth;
        _httpClientFactory = httpClientFactory;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<AgentStackHealthStatus> CheckAsync(CancellationToken ct = default)
    {
        var components = new List<AgentStackComponentHealth>();

        var obscura = await _obscuraHealth.CheckAsync(ct).ConfigureAwait(false);
        var obscuraHealthy = obscura.GrpcHealthy || obscura.CdpHealthy;
        components.Add(new AgentStackComponentHealth(
            "obscura",
            obscuraHealthy,
            obscuraHealthy ? null : $"{obscura.GrpcError ?? obscura.CdpError ?? "unreachable"}"));

        var (shadowHealthy, shadowComponent) = await ProbeOptionalAsync(
            _options.EnableShadowSyncGate,
            "shadow-sync",
            _options.ShadowSyncBaseUrl,
            "/health",
            ct).ConfigureAwait(false);
        components.Add(shadowComponent);

        var (sandboxHealthy, sandboxComponent) = await ProbeOptionalAsync(
            _options.EnableSandboxControllerGate,
            "sandbox-controller",
            _options.SandboxControllerBaseUrl,
            "/health",
            ct).ConfigureAwait(false);
        components.Add(sandboxComponent);

        var (scannerHealthy, scannerComponent) = await ProbeOptionalAsync(
            _options.EnableSecurityScannerGate,
            "security-scanner",
            _options.SecurityScannerBaseUrl,
            "/health",
            ct).ConfigureAwait(false);
        components.Add(scannerComponent);

        var (qdrantHealthy, qdrantComponent) = await ProbeOptionalAsync(
            _options.EnableQdrantGate,
            "qdrant",
            _options.QdrantBaseUrl,
            "/healthz",
            ct).ConfigureAwait(false);
        components.Add(qdrantComponent);

        return new AgentStackHealthStatus(
            obscuraHealthy,
            shadowHealthy,
            sandboxHealthy,
            scannerHealthy,
            qdrantHealthy,
            components)
        {
            ShadowSyncRequired = _options.EnableShadowSyncGate,
            SandboxRequired = _options.EnableSandboxControllerGate,
            ScannerRequired = _options.EnableSecurityScannerGate,
            QdrantRequired = _options.EnableQdrantGate
        };
    }

    private async Task<(bool Healthy, AgentStackComponentHealth Component)> ProbeOptionalAsync(
        bool enabled,
        string name,
        string baseUrl,
        string path,
        CancellationToken ct)
    {
        if (!enabled)
            return (true, new AgentStackComponentHealth(name, true, "disabled"));

        var probe = await ProbeHttpHealthAsync(name, baseUrl, path, ct).ConfigureAwait(false);
        return (probe.healthy, new AgentStackComponentHealth(name, probe.healthy, probe.error));
    }

    private async Task<(bool healthy, string? error)> ProbeHttpHealthAsync(
        string clientName,
        string baseUrl,
        string path,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(baseUrl))
            return (false, "base_url_not_configured");

        try
        {
            var client = _httpClientFactory.CreateClient("AgentStackHealth");
            client.Timeout = TimeSpan.FromSeconds(_options.HealthCheckTimeoutSeconds);
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(TimeSpan.FromSeconds(_options.HealthCheckTimeoutSeconds));
            var uri = new Uri(new Uri(baseUrl.TrimEnd('/') + "/"), path.TrimStart('/'));
            using var response = await client.GetAsync(uri, cts.Token).ConfigureAwait(false);
            return response.IsSuccessStatusCode
                ? (true, null)
                : (false, $"status_{(int)response.StatusCode}");
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Agent stack health probe failed for {Name} {BaseUrl}{Path}", clientName, baseUrl, path);
            return (false, ex.Message);
        }
    }
}
