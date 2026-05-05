using System.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Libr4.IDE.Infrastructure.Bridge;

/// <summary>
/// Bridge service connecting C# orchestrator with Rust sandbox monitor
/// This is the "glue" that enables communication between languages
/// </summary>
public class SandboxBridge
{
    private readonly ILogger<SandboxBridge> _logger;
    private readonly string _rustMonitorUrl;
    private readonly HttpClient _httpClient;

    public SandboxBridge(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<SandboxBridge> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _rustMonitorUrl = configuration["RustMonitor:Url"] ?? "http://localhost:9090";
        _httpClient.BaseAddress = new Uri(_rustMonitorUrl);
    }

    /// <summary>
    /// Execute code in sandbox with Rust monitoring
    /// </summary>
    public async Task<SandboxExecutionResult> ExecuteInSandboxAsync(
        string code,
        string language = "python3",
        CancellationToken cancellationToken = default)
    {
        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = language,
                Arguments = $"-c \"{code}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false
            }
        };

        process.Start();
        var pid = (uint)process.Id;

        _logger.LogInformation("Started sandbox process {Pid}", pid);

        // Register process with Rust monitor for resource tracking
        await RegisterProcessWithMonitorAsync(pid, cancellationToken);

        // Start monitoring task (simulates calling Rust SysMonitor)
        var monitoringTask = Task.Run(async () =>
        {
            while (!process.HasExited && !cancellationToken.IsCancellationRequested)
            {
                // Check with Rust monitor if process should be terminated
                var shouldTerminate = await CheckProcessStatusAsync(pid, cancellationToken);
                if (shouldTerminate)
                {
                    _logger.LogWarning("Rust monitor requested termination of process {Pid}", pid);
                    process.Kill(entireProcessTree: true);
                    break;
                }

                await Task.Delay(1000, cancellationToken);
            }
        }, cancellationToken);

        try
        {
            await process.WaitForExitAsync(cancellationToken);
            var output = await process.StandardOutput.ReadToEndAsync(cancellationToken);
            var error = await process.StandardError.ReadToEndAsync(cancellationToken);

            // Unregister from monitor
            await UnregisterProcessAsync(pid, cancellationToken);

            return new SandboxExecutionResult
            {
                Success = process.ExitCode == 0,
                Output = output,
                Error = error,
                DurationMs = (ulong)process.TotalProcessorTime.TotalMilliseconds
            };
        }
        finally
        {
            await monitoringTask;
        }
    }

    /// <summary>
    /// Register process with Rust monitor
    /// In production, this would be a gRPC call to SysMonitor.rs
    /// </summary>
    private async Task RegisterProcessWithMonitorAsync(uint pid, CancellationToken cancellationToken)
    {
        try
        {
            // Placeholder for gRPC call to Rust
            // var response = await _rustMonitorClient.RegisterProcessAsync(new RegisterRequest { Pid = pid });
            _logger.LogDebug("Registered process {Pid} with Rust monitor", pid);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to register process {Pid} with Rust monitor", pid);
        }
    }

    /// <summary>
    /// Check process status with Rust monitor
    /// Returns true if process should be terminated
    /// </summary>
    private async Task<bool> CheckProcessStatusAsync(uint pid, CancellationToken cancellationToken)
    {
        try
        {
            // Placeholder for gRPC call to Rust
            // var response = await _rustMonitorClient.CheckProcessAsync(new CheckRequest { Pid = pid });
            // return response.ShouldTerminate;
            
            // Simulate check by calling HTTP endpoint
            var response = await _httpClient.GetAsync($"/monitor/{pid}", cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync(cancellationToken);
                return content.Contains("terminate") || content.Contains("exceeded");
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to check process {Pid} status with Rust monitor", pid);
        }
        return false;
    }

    /// <summary>
    /// Unregister process from Rust monitor
    /// </summary>
    private async Task UnregisterProcessAsync(uint pid, CancellationToken cancellationToken)
    {
        try
        {
            // Placeholder for gRPC call to Rust
            _logger.LogDebug("Unregistered process {Pid} from Rust monitor", pid);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to unregister process {Pid} from Rust monitor", pid);
        }
    }

    /// <summary>
    /// Get resource usage from Rust monitor
    /// </summary>
    public async Task<ResourceUsage?> GetResourceUsageAsync(uint pid, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/monitor/{pid}/usage", cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync(cancellationToken);
                // Parse JSON response from Rust
                // For now, return placeholder
                return new ResourceUsage
                {
                    MemoryKb = 0,
                    CpuPercent = 0,
                    Status = "unknown"
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get resource usage for process {Pid}", pid);
        }
        return null;
    }
}

/// <summary>
/// Resource usage data from Rust monitor
/// </summary>
public record ResourceUsage
{
    public ulong MemoryKb { get; init; }
    public double CpuPercent { get; init; }
    public string Status { get; init; } = "unknown";
}
