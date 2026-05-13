using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace Libr4.IDE.Infrastructure.Containers;

/// <summary>
/// Manages Docker/Firecracker containers for shadow workspaces
/// </summary>
public interface IContainerManager
{
    Task<ContainerInfo> CreateContainerAsync(string workspaceId, string baseImage);
    Task<bool> StartContainerAsync(string containerId);
    Task<bool> StopContainerAsync(string containerId);
    Task<bool> RemoveContainerAsync(string containerId);
    Task<ContainerInfo?> GetContainerAsync(string workspaceId);
    Task<List<ContainerInfo>> ListActiveAsync();
    Task<bool> ExecuteCommandAsync(string containerId, string command);
    Task<string> GetLogsAsync(string containerId, int tail = 100);
    Task StopAndRemoveContainerAsync(string workspaceId);
}

public class ContainerManager : IContainerManager
{
    private readonly ILogger<ContainerManager> _logger;
    private readonly Dictionary<string, ContainerInfo> _containers = new();
    private readonly string _dockerBinary;

    public ContainerManager(ILogger<ContainerManager> logger)
    {
        _logger = logger;
        _dockerBinary = OperatingSystem.IsWindows() ? "docker.exe" : "docker";
    }

    public async Task<ContainerInfo> CreateContainerAsync(string workspaceId, string baseImage)
    {
        var containerName = $"libr4-shadow-{workspaceId}";
        var containerId = Guid.NewGuid().ToString("N")[..12];

        try
        {
            // Create Docker container with specific configuration for shadow workspace
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = _dockerBinary,
                    Arguments = $"run -d --name {containerName} " +
                                $"--label workspaceId={workspaceId} " +
                                $"--label type=shadow " +
                                $"-v libr4-shadow-{workspaceId}:/workspace " +
                                $"--network libr4-shadow-network " +
                                $"--memory=2g --cpus=1.0 " +
                                $"--read-only " +
                                $"--tmpfs /tmp:rw,noexec,nosuid,size=100m " +
                                $"{baseImage} " +
                                $"sleep infinity",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            process.Start();
            var output = await process.StandardOutput.ReadToEndAsync();
            var error = await process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync();

            if (process.ExitCode != 0)
            {
                _logger.LogError("Failed to create container: {Error}", error);
                throw new InvalidOperationException($"Docker create failed: {error}");
            }

            containerId = output.Trim();

            var info = new ContainerInfo
            {
                Id = containerId,
                Name = containerName,
                WorkspaceId = workspaceId,
                BaseImage = baseImage,
                Status = "created",
                CreatedAt = DateTime.UtcNow,
                PortMappings = new Dictionary<string, int>()
            };

            _containers[workspaceId] = info;
            _logger.LogInformation("Created container {ContainerId} for workspace {WorkspaceId}", containerId, workspaceId);

            return info;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create container for workspace {WorkspaceId}", workspaceId);
            throw;
        }
    }

    public async Task<bool> StartContainerAsync(string containerId)
    {
        try
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = _dockerBinary,
                    Arguments = $"start {containerId}",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            process.Start();
            await process.WaitForExitAsync();

            if (process.ExitCode == 0 && _containers.Values.FirstOrDefault(c => c.Id == containerId) is { } info)
            {
                info.Status = "running";
            }

            return process.ExitCode == 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start container {ContainerId}", containerId);
            return false;
        }
    }

    public async Task<bool> StopContainerAsync(string containerId)
    {
        try
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = _dockerBinary,
                    Arguments = $"stop {containerId}",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            process.Start();
            await process.WaitForExitAsync();

            if (process.ExitCode == 0 && _containers.Values.FirstOrDefault(c => c.Id == containerId) is { } info)
            {
                info.Status = "stopped";
            }

            return process.ExitCode == 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to stop container {ContainerId}", containerId);
            return false;
        }
    }

    public async Task<bool> RemoveContainerAsync(string containerId)
    {
        try
        {
            // First stop if running
            await StopContainerAsync(containerId);

            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = _dockerBinary,
                    Arguments = $"rm {containerId}",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            process.Start();
            await process.WaitForExitAsync();

            if (process.ExitCode == 0)
            {
                var entry = _containers.FirstOrDefault(c => c.Value.Id == containerId);
                if (entry.Key != null)
                {
                    _containers.Remove(entry.Key);
                }
            }

            return process.ExitCode == 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to remove container {ContainerId}", containerId);
            return false;
        }
    }

    public Task<ContainerInfo?> GetContainerAsync(string workspaceId)
    {
        _containers.TryGetValue(workspaceId, out var info);
        return Task.FromResult(info);
    }

    public Task<List<ContainerInfo>> ListActiveAsync()
    {
        var active = _containers.Values.Where(c => c.Status == "running").ToList();
        return Task.FromResult(active);
    }

    public async Task<bool> ExecuteCommandAsync(string containerId, string command)
    {
        try
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = _dockerBinary,
                    Arguments = $"exec {containerId} {command}",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            process.Start();
            var output = await process.StandardOutput.ReadToEndAsync();
            var error = await process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync();

            if (process.ExitCode != 0)
            {
                _logger.LogWarning("Command failed in container {ContainerId}: {Error}", containerId, error);
            }

            return process.ExitCode == 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to execute command in container {ContainerId}", containerId);
            return false;
        }
    }

    public async Task<string> GetLogsAsync(string containerId, int tail = 100)
    {
        try
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = _dockerBinary,
                    Arguments = $"logs {containerId} --tail {tail}",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            process.Start();
            var output = await process.StandardOutput.ReadToEndAsync();
            await process.WaitForExitAsync();

            return output;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get logs for container {ContainerId}", containerId);
            return string.Empty;
        }
    }

    /// <summary>
    /// Immediately stop and remove container when user closes tab
    /// </summary>
    public async Task StopAndRemoveContainerAsync(string workspaceId)
    {
        var container = await GetContainerAsync(workspaceId);
        if (container == null)
        {
            _logger.LogInformation("No container found for workspace {WorkspaceId}", workspaceId);
            return;
        }

        try
        {
            _logger.LogInformation(
                "Immediately stopping container {ContainerId} for workspace {WorkspaceId} (tab closed)",
                container.Id, workspaceId);

            // Stop container
            var stopProcess = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = _dockerBinary,
                    Arguments = $"stop -t 5 {container.Id}",  // 5 second timeout
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            stopProcess.Start();
            await stopProcess.WaitForExitAsync();

            // Remove container
            var rmProcess = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = _dockerBinary,
                    Arguments = $"rm -f {container.Id}",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            rmProcess.Start();
            await rmProcess.WaitForExitAsync();

            // Remove from tracking
            _containers.Remove(workspaceId);

            _logger.LogInformation(
                "Container {ContainerId} for workspace {WorkspaceId} stopped and removed immediately",
                container.Id, workspaceId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to stop and remove container for workspace {WorkspaceId}",
                workspaceId);
        }
    }
}

public class ContainerInfo
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string WorkspaceId { get; set; } = string.Empty;
    public string BaseImage { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? LastActivityAt { get; set; }
    public Dictionary<string, int> PortMappings { get; set; } = new();
    public string? SnapshotId { get; set; }
}
