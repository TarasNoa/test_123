using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Libr4.IDE.Infrastructure.Sandbox;

/// <summary>
/// C# client for Rust Sandbox Controller.
/// Executes code securely via HTTP to sandbox-controller:9090/execute
/// </summary>
public class RustSandboxExecutor : ISandboxClient
{
    private readonly HttpClient _httpClient;
    private readonly string _sandboxUrl;
    private readonly ILogger<RustSandboxExecutor> _logger;

    public RustSandboxExecutor(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<RustSandboxExecutor> logger)
    {
        _httpClient = httpClient;
        _sandboxUrl = configuration["Sandbox:Url"] ?? "http://sandbox-controller:9090";
        _logger = logger;
        _httpClient.BaseAddress = new Uri(_sandboxUrl);
        _httpClient.Timeout = TimeSpan.FromSeconds(35); // Slightly longer than max timeout
    }

    /// <summary>
    /// Execute code in the Rust sandbox
    /// </summary>
    public async Task<SandboxExecutionResult> ExecuteAsync(
        string code,
        string language = "csharp",
        int timeoutSeconds = 30,
        int memoryLimitMb = 512,
        CancellationToken cancellationToken = default)
    {
        var request = new ExecuteRequest
        {
            code = code,
            language = language,
            timeout = (uint)timeoutSeconds,
            memory_limit_mb = (uint)memoryLimitMb
        };

        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        _logger.LogDebug("Executing {Language} code in sandbox (timeout: {Timeout}s, memory: {Memory}MB)",
            language, timeoutSeconds, memoryLimitMb);

        try
        {
            var response = await _httpClient.PostAsync("/execute", content, cancellationToken);
            response.EnsureSuccessStatusCode();

            var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
            var result = JsonSerializer.Deserialize<ExecuteResponse>(responseJson);

            if (result == null)
            {
                return new SandboxExecutionResult
                {
                    Success = false,
                    Output = null,
                    Error = "Failed to parse sandbox response",
                    DurationMs = 0
                };
            }

            _logger.LogInformation("Sandbox execution completed: Success={Success}, Duration={Duration}ms",
                result.success, result.duration_ms);

            return new SandboxExecutionResult
            {
                Success = result.success,
                Output = result.output,
                Error = result.error,
                DurationMs = result.duration_ms
            };
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Failed to communicate with sandbox controller at {Url}", _sandboxUrl);
            return new SandboxExecutionResult
            {
                Success = false,
                Output = null,
                Error = $"Sandbox controller unavailable: {ex.Message}",
                DurationMs = 0
            };
        }
        catch (TaskCanceledException ex) when (cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning("Sandbox execution cancelled by client");
            return new SandboxExecutionResult
            {
                Success = false,
                Output = null,
                Error = "Execution cancelled",
                DurationMs = 0
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during sandbox execution");
            return new SandboxExecutionResult
            {
                Success = false,
                Output = null,
                Error = $"Unexpected error: {ex.Message}",
                DurationMs = 0
            };
        }
    }

    /// <summary>
    /// Check if sandbox controller is healthy
    /// </summary>
    public async Task<bool> HealthCheckAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync("/health", cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }
}

/// <summary>
/// Request payload for sandbox execution
/// </summary>
internal record ExecuteRequest
{
    public string code { get; init; } = string.Empty;
    public string language { get; init; } = "csharp";
    public uint timeout { get; init; } = 30;
    public uint memory_limit_mb { get; init; } = 512;
}

/// <summary>
/// Response from sandbox execution
/// </summary>
internal record ExecuteResponse
{
    public bool success { get; init; }
    public string? output { get; init; }
    public string? error { get; init; }
    public ulong duration_ms { get; init; }
}

/// <summary>
/// Public result type for sandbox execution
/// </summary>
public record SandboxExecutionResult
{
    public bool Success { get; init; }
    public string? Output { get; init; }
    public string? Error { get; init; }
    public ulong DurationMs { get; init; }
}
