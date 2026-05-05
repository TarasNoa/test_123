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
    Task<ExecutionResult> RunAsync(string taskId, string code, string language = "python", int memoryLimitMb = 128, int timeoutSeconds = 30);
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
        int timeoutSeconds = 30)
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
            var result = await _client.ExecuteCodeAsync(request);
            
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
}
