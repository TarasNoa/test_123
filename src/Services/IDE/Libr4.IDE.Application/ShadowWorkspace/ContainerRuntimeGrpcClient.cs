using Grpc.Core;
using Grpc.Net.Client;
using Microsoft.Extensions.Logging;
using ContainerRuntime;

namespace Libr4.IDE.Application.ShadowWorkspace;

/// <summary>
/// Golden Stack: Thin C# gRPC client to Rust container-runtime service
/// All low-level Docker operations moved to Rust (obscura/crates/container-runtime)
/// This is just an orchestration layer calling the Rust service
/// </summary>
public class ContainerRuntimeGrpcClient : IContainerManager, IDisposable
{
    private readonly ILogger<ContainerRuntimeGrpcClient> _logger;
    private readonly ContainerRuntime.ContainerRuntime.ContainerRuntimeClient _client;
    private readonly GrpcChannel _channel;

    public ContainerRuntimeGrpcClient(
        ILogger<ContainerRuntimeGrpcClient> logger,
        string? address = null)
    {
        _logger = logger;
        var grpcAddress = address ?? Environment.GetEnvironmentVariable("CONTAINER_RUNTIME_ADDR") 
            ?? "http://localhost:50051";
        
        _logger.LogInformation("Connecting to Rust container runtime at {Address}", grpcAddress);
        
        _channel = GrpcChannel.ForAddress(grpcAddress, new GrpcChannelOptions
        {
            MaxReceiveMessageSize = 50 * 1024 * 1024, // 50MB
            MaxSendMessageSize = 50 * 1024 * 1024,
        });
        
        _client = new ContainerRuntime.ContainerRuntime.ContainerRuntimeClient(_channel);
    }

    public async Task<string> CreateContainerAsync(
        string image, 
        Dictionary<string, string>? env = null, 
        CancellationToken ct = default)
    {
        var request = new CreateContainerRequest
        {
            Image = image,
            Name = $"libr4-{Guid.NewGuid():N}"[..12],
            AutoStart = true,
            Command = "sleep infinity"
        };

        if (env != null)
        {
            foreach (var (key, value) in env)
            {
                request.Environment[key] = value;
            }
        }

        _logger.LogDebug("Creating container with image {Image}", image);
        
        var response = await _client.CreateContainerAsync(request, cancellationToken: ct);
        
        if (!response.Success)
        {
            throw new InvalidOperationException($"Failed to create container: {response.Error}");
        }

        _logger.LogInformation("Created container {ContainerId}", response.ContainerId);
        return response.ContainerId;
    }

    public async Task<bool> StartContainerAsync(string containerId, CancellationToken ct = default)
    {
        var request = new StartContainerRequest { ContainerId = containerId };
        
        _logger.LogDebug("Starting container {ContainerId}", containerId);
        
        var response = await _client.StartContainerAsync(request, cancellationToken: ct);
        
        if (!response.Success)
        {
            _logger.LogWarning("Failed to start container {ContainerId}: {Error}", 
                containerId, response.Error);
        }
        
        return response.Success;
    }

    public async Task<bool> StopContainerAsync(string containerId, CancellationToken ct = default)
    {
        var request = new StopContainerRequest 
        { 
            ContainerId = containerId,
            TimeoutSeconds = 10
        };
        
        _logger.LogDebug("Stopping container {ContainerId}", containerId);
        
        var response = await _client.StopContainerAsync(request, cancellationToken: ct);
        
        if (!response.Success)
        {
            _logger.LogWarning("Failed to stop container {ContainerId}: {Error}", 
                containerId, response.Error);
        }
        
        return response.Success;
    }

    public async Task<bool> DeleteContainerAsync(string containerId, CancellationToken ct = default)
    {
        var request = new DeleteContainerRequest 
        { 
            ContainerId = containerId,
            Force = true
        };
        
        _logger.LogDebug("Deleting container {ContainerId}", containerId);
        
        var response = await _client.DeleteContainerAsync(request, cancellationToken: ct);
        
        if (!response.Success)
        {
            _logger.LogWarning("Failed to delete container {ContainerId}: {Error}", 
                containerId, response.Error);
        }
        
        return response.Success;
    }

    public async Task<string> ExecuteCommandAsync(
        string containerId, 
        string command, 
        CancellationToken ct = default)
    {
        var request = new ExecuteCommandRequest
        {
            ContainerId = containerId,
            Command = command,
            TimeoutSeconds = 300 // 5 minutes default
        };

        _logger.LogDebug("Executing command in {ContainerId}: {Command}", containerId, command);
        
        var response = await _client.ExecuteCommandAsync(request, cancellationToken: ct);
        
        if (!response.Success)
        {
            throw new ContainerExecutionException(
                $"Command failed with exit code {response.ExitCode}",
                response.ExitCode,
                response.Stdout,
                response.Stderr);
        }

        return response.Stdout + (string.IsNullOrEmpty(response.Stderr) ? "" : $"\n[stderr]: {response.Stderr}");
    }

    public async Task<ContainerStatus> GetStatusAsync(string containerId, CancellationToken ct = default)
    {
        var request = new GetContainerStatusRequest { ContainerId = containerId };
        
        var response = await _client.GetContainerStatusAsync(request, cancellationToken: ct);
        
        return new ContainerStatus
        {
            ContainerId = response.ContainerId,
            State = response.State,
            ExitCode = response.ExitCode,
            CreatedAt = DateTime.UtcNow, // We don't track this in gRPC yet
            StartedAt = !string.IsNullOrEmpty(response.StartedAt) 
                ? DateTime.Parse(response.StartedAt) 
                : null,
            FinishedAt = !string.IsNullOrEmpty(response.FinishedAt) 
                ? DateTime.Parse(response.FinishedAt) 
                : null
        };
    }

    public async Task WarmPoolAsync(string environmentType, int count, CancellationToken ct = default)
    {
        var request = new WarmPoolRequest
        {
            EnvironmentType = environmentType,
            Count = count
        };

        _logger.LogInformation("Warming pool {EnvironmentType} with {Count} containers", 
            environmentType, count);
        
        var response = await _client.WarmPoolAsync(request, cancellationToken: ct);
        
        if (!response.Success)
        {
            throw new InvalidOperationException($"Failed to warm pool: {response.Error}");
        }

        _logger.LogInformation("Successfully warmed {WarmedCount} containers", response.WarmedCount);
    }

    public async Task<Dictionary<string, PoolStats>> GetPoolStatsAsync(CancellationToken ct = default)
    {
        var response = await _client.GetPoolStatsAsync(new GetPoolStatsRequest(), cancellationToken: ct);
        
        return response.Pools.ToDictionary(
            p => p.EnvironmentType,
            p => new PoolStats
            {
                Available = p.Available,
                Active = p.Active,
                MinSize = p.MinSize,
                MaxSize = p.MaxSize
            });
    }

    public void Dispose()
    {
        _channel?.Dispose();
    }
}

/// <summary>
/// Extended pool statistics
/// </summary>
public class PoolStats
{
    public int Available { get; set; }
    public int Active { get; set; }
    public int MinSize { get; set; }
    public int MaxSize { get; set; }
}

/// <summary>
/// Exception for container command execution failures
/// </summary>
public class ContainerExecutionException : Exception
{
    public int ExitCode { get; }
    public string Output { get; }
    public string ErrorOutput { get; }

    public ContainerExecutionException(string message, int exitCode, string output, string error)
        : base(message)
    {
        ExitCode = exitCode;
        Output = output;
        ErrorOutput = error;
    }
}
