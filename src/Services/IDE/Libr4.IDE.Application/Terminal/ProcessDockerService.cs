using System.Diagnostics;
using Libr4.AI.Domain.Terminal;
using Microsoft.Extensions.Logging;

namespace Libr4.IDE.Application.Terminal;

/// <summary>
/// Docker service implementation using process execution
/// For production, this should use Docker.DotNet for proper Docker API integration
/// </summary>
public class ProcessDockerService : IDockerService
{
    private readonly ILogger<ProcessDockerService> _logger;
    private readonly Dictionary<string, Process> _shellProcesses = new();
    private readonly object _lock = new();

    public ProcessDockerService(ILogger<ProcessDockerService> logger)
    {
        _logger = logger;
    }

    public async Task<string> ExecuteCommandAsync(
        string containerId,
        string command,
        CancellationToken ct = default)
    {
        return await ExecuteCommandAsync(containerId, new[] { "/bin/sh", "-c", command }, null, ct);
    }

    public async Task<string> ExecuteCommandAsync(
        string containerId,
        string[] command,
        string? workingDirectory = null,
        CancellationToken ct = default)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = command[0],
            Arguments = string.Join(" ", command.Skip(1)),
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };

        if (!string.IsNullOrEmpty(workingDirectory))
        {
            startInfo.WorkingDirectory = workingDirectory;
        }

        using var process = new Process { StartInfo = startInfo };
        
        var outputBuilder = new System.Text.StringBuilder();
        var errorBuilder = new System.Text.StringBuilder();

        process.OutputDataReceived += (sender, e) => outputBuilder.AppendLine(e.Data);
        process.ErrorDataReceived += (sender, e) => errorBuilder.AppendLine(e.Data);

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        await process.WaitForExitAsync(ct);

        var output = outputBuilder.ToString();
        var error = errorBuilder.ToString();

        if (!string.IsNullOrEmpty(error))
        {
            output += "\n" + error;
        }

        _logger.LogInformation(
            "Executed command in container {Container}: {Command}, ExitCode: {ExitCode}",
            containerId,
            string.Join(" ", command),
            process.ExitCode);

        return output;
    }

    public async Task CreateShellSessionAsync(
        string containerId,
        string sessionId,
        ShellType shell,
        CancellationToken ct = default)
    {
        var shellPath = GetShellPath(shell);
        
        var startInfo = new ProcessStartInfo
        {
            FileName = shellPath,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };

        var process = new Process { StartInfo = startInfo };
        
        process.Start();

        lock (_lock)
        {
            _shellProcesses[sessionId] = process;
        }

        _logger.LogInformation(
            "Created shell session {SessionId} in container {Container} with shell {Shell}",
            sessionId,
            containerId,
            shell);

        await Task.CompletedTask;
    }

    public async Task<string> GetShellOutputAsync(
        string containerId,
        string sessionId,
        CancellationToken ct = default)
    {
        Process? process;
        lock (_lock)
        {
            if (!_shellProcesses.TryGetValue(sessionId, out process))
                throw new KeyNotFoundException($"Shell session {sessionId} not found");
        }

        if (process.HasExited)
        {
            lock (_lock)
            {
                _shellProcesses.Remove(sessionId);
            }
            return string.Empty;
        }

        // Read available output
        var output = await process.StandardOutput.ReadToEndAsync(ct);
        var error = await process.StandardError.ReadToEndAsync(ct);

        return string.IsNullOrEmpty(error) ? output : output + "\n" + error;
    }

    public async Task TerminateShellSessionAsync(
        string containerId,
        string sessionId,
        CancellationToken ct = default)
    {
        Process? process;
        lock (_lock)
        {
            if (!_shellProcesses.TryGetValue(sessionId, out process))
                return;

            _shellProcesses.Remove(sessionId);
        }

        try
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
                await process.WaitForExitAsync(ct);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to terminate shell session {SessionId}", sessionId);
        }

        _logger.LogInformation("Terminated shell session {SessionId}", sessionId);
    }

    private static string GetShellPath(ShellType shell)
    {
        return shell switch
        {
            ShellType.Bash => "/bin/bash",
            ShellType.Zsh => "/bin/zsh",
            ShellType.Fish => "/usr/bin/fish",
            ShellType.PowerShell => "/usr/bin/pwsh",
            ShellType.Cmd => "/bin/sh", // Fallback for Windows
            _ => "/bin/bash"
        };
    }
}
