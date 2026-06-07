using System.Collections.Concurrent;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Libr4.IDE.Application.AutonomousAppGeneration.AgentIntegration.McpHost;

public sealed class McpExternalServerDiscovery : IMcpExternalServerDiscovery
{
    private readonly IMcpServerPreflight _preflight;
    private readonly IOptions<McpExecutionOptions> _mcpOptions;
    private readonly IOptions<McpHostOptions> _hostOptions;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<McpExternalServerDiscovery> _logger;

    public McpExternalServerDiscovery(
        IMcpServerPreflight preflight,
        IOptions<McpExecutionOptions> mcpOptions,
        IOptions<McpHostOptions> hostOptions,
        IHttpClientFactory httpClientFactory,
        ILogger<McpExternalServerDiscovery> logger)
    {
        _preflight = preflight;
        _mcpOptions = mcpOptions;
        _hostOptions = hostOptions;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<IReadOnlyList<McpServerDiscoveryResult>> DiscoverAsync(CancellationToken ct = default)
    {
        var results = new List<McpServerDiscoveryResult>();
        var timeout = TimeSpan.FromSeconds(Math.Max(2, _hostOptions.Value.DiscoveryTimeoutSeconds));

        foreach (var (profileKey, _) in _mcpOptions.Value.ServerProfiles)
        {
            var pre = _preflight.CheckServerAvailability(profileKey);
            results.Add(new McpServerDiscoveryResult(
                profileKey,
                McpHostTransportKind.Stdio,
                pre.IsAvailable,
                pre.BlockerCode,
                pre.DiagnosticMessage,
                ToolCount: 0,
                ResourceCount: 0,
                PromptCount: 0));
        }

        if (_hostOptions.Value.EnableSseTransport)
        {
            foreach (var (profileKey, sse) in _hostOptions.Value.SseServers)
            {
                var ok = await ProbeSseAsync(profileKey, sse, timeout, ct).ConfigureAwait(false);
                results.Add(new McpServerDiscoveryResult(
                    profileKey,
                    McpHostTransportKind.Sse,
                    ok,
                    ok ? null : "sse_unreachable",
                    ok ? null : $"SSE server '{profileKey}' unreachable at {sse.BaseUrl}",
                    ToolCount: 0,
                    ResourceCount: 0,
                    PromptCount: 0));
            }
        }

        _logger.LogDebug("MCP discovery completed with {Count} profiles", results.Count);
        return results;
    }

    private async Task<bool> ProbeSseAsync(
        string profileKey,
        McpSseServerProfile profile,
        TimeSpan timeout,
        CancellationToken ct)
    {
        try
        {
            var client = _httpClientFactory.CreateClient($"McpSse:{profileKey}");
            var transport = new McpSseTransport(client, profile);
            return await transport.ProbeAsync(timeout, ct).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "SSE MCP probe failed for {ProfileKey}", profileKey);
            return false;
        }
    }
}
