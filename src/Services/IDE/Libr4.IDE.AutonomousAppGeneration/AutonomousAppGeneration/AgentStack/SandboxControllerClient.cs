using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Libr4.IDE.Application.AutonomousAppGeneration.AgentStack;

public interface ISandboxControllerClient
{
    Task<bool> EnsureHealthyAsync(CancellationToken ct = default);
}

public sealed class SandboxControllerClient : ISandboxControllerClient
{
    private readonly HttpClient _http;
    private readonly AgentStackOptions _options;
    private readonly ILogger<SandboxControllerClient> _logger;

    public SandboxControllerClient(
        HttpClient http,
        IOptions<AgentStackOptions> options,
        ILogger<SandboxControllerClient> logger)
    {
        _http = http;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<bool> EnsureHealthyAsync(CancellationToken ct = default)
    {
        if (!_options.EnableSandboxControllerGate)
            return true;

        try
        {
            using var response = await _http.GetAsync("/health", ct).ConfigureAwait(false);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Sandbox controller health check failed");
            return false;
        }
    }
}
