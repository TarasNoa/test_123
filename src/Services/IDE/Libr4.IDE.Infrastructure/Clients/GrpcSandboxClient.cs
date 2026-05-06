using Grpc.Net.Client;
using Libr4.IDE.Infrastructure.Protos;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Libr4.IDE.Infrastructure.Clients;

/// <summary>
/// gRPC client for Rust sandbox service
/// Provides type-safe, high-performance communication with Rust execution engine
/// </summary>
public interface ISandboxClient
{
    Task<ExecutionResult> RunAsync(string taskId, string code, string language = "python", int memoryLimitMb = 128, int timeoutSeconds = 30, CancellationToken ct = default);
    Task<bool> HealthCheckAsync(CancellationToken ct = default);
}

public class GrpcSandboxClient : ISandboxClient
{
    private readonly SandboxService.SandboxServiceClient _client;
    private readonly ILogger<GrpcSandboxClient> _logger;
    private readonly string _endpoint;

    public GrpcSandboxClient(
        IConfiguration configuration,
        ILogger<GrpcSandboxClient> logger)
    {
        _endpoint = configuration["Sandbox:GrpcEndpoint"] ?? "http://localhost:50051";
        var channel = GrpcChannel.ForAddress(_endpoint);
        _client = new SandboxService.SandboxServiceClient(channel);
        _logger = logger;
    }

    public async Task<ExecutionResult> RunAsync(
        string taskId,
        string code,
        string language = "python",
        int memoryLimitMb = 128,
        int timeoutSeconds = 30,
        CancellationToken ct = default)
    {
        var request = new ExecutionRequest
        {
            TaskId = taskId,
            Code = code,
            Language = language,
            MemoryLimitMb = memoryLimitMb,
            TimeoutSeconds = timeoutSeconds
        };

        try
        {
            _logger.LogInformation("C#: Sending code to Rust sandbox for task {TaskId} (endpoint: {Endpoint})", taskId, _endpoint);
            var result = await _client.ExecuteCodeAsync(request, ct);
            
            _logger.LogInformation("C#: Execution completed for task {TaskId}. ExitCode: {ExitCode}, Termination: {Termination}", 
                taskId, result.ExitCode, result.TerminationReason);
            
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "C#: Error communicating with Rust server at {Endpoint}", _endpoint);
            throw;
        }
    }

    public async Task<bool> HealthCheckAsync(CancellationToken ct = default)
    {
        try
        {
            _logger.LogDebug("C#: Checking Rust server health at {Endpoint}", _endpoint);
            // Use a simple health check - try to connect to the channel
            // For gRPC, we can try a simple call or just check channel state
            // For now, we'll assume the channel creation was successful
            // In production, implement a proper health check endpoint in Rust
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "C#: Rust server health check failed at {Endpoint}", _endpoint);
            return false;
        }
    }
}
