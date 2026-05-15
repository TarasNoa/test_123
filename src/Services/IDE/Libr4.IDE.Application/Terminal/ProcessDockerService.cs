using System.Diagnostics;
using System.Text;
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
    private readonly Dictionary<string, StringBuilder> _outputBuffers = new();
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

        if (!process.Start())
        {
            throw new InvalidOperationException($"Failed to start shell {shellPath}");
        }

        lock (_lock)
        {
            _shellProcesses[sessionId] = process;
            _outputBuffers[sessionId] = new StringBuilder();
        }

        // Start background readers for stdout and stderr
        _ = Task.Run(() => StreamReaderAsync(sessionId, process.StandardOutput, ct), ct);
        _ = Task.Run(() => StreamReaderAsync(sessionId, process.StandardError, ct), ct);

        _logger.LogInformation(
            "Created shell session {SessionId} in container {Container} with shell {Shell}",
            sessionId,
            containerId,
            shell);

        await Task.CompletedTask;
    }

    private async Task StreamReaderAsync(string sessionId, StreamReader reader, CancellationToken ct)
    {
        try
        {
            var buffer = new char[1024];
            while (!ct.IsCancellationRequested)
            {
                var read = await reader.ReadAsync(buffer.AsMemory(0, buffer.Length), ct);
                if (read == 0) break;
                lock (_lock)
                {
                    if (_outputBuffers.TryGetValue(sessionId, out var sb))
                    {
                        sb.Append(buffer, 0, read);
                    }
                }
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Stream reader ended for session {SessionId}", sessionId);
        }
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

        // Return buffered output without blocking
        lock (_lock)
        {
            if (_outputBuffers.TryGetValue(sessionId, out var sb))
            {
                var output = sb.ToString();
                sb.Clear();
                return output;
            }
        }
        return string.Empty;
    }

    public async Task WriteToShellAsync(string sessionId, string input, CancellationToken ct = default)
    {
        Process? process;
        lock (_lock)
        {
            if (!_shellProcesses.TryGetValue(sessionId, out process))
                throw new KeyNotFoundException($"Shell session {sessionId} not found");
        }

        if (process.HasExited)
            throw new InvalidOperationException($"Shell session {sessionId} has exited");

        await process.StandardInput.WriteAsync(input.AsMemory(), ct);
        await process.StandardInput.FlushAsync(ct);
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
            _outputBuffers.Remove(sessionId);
        }

        try
        {
            if (!process.HasExited)
            {
                process.Kill();
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
            ShellType.Bash => "/bin/sh", // Alpine fallback; change to /bin/bash if available
            ShellType.Zsh => "/bin/sh",
            ShellType.Fish => "/bin/sh",
            ShellType.PowerShell => "/bin/sh",
            ShellType.Cmd => "/bin/sh",
            _ => "/bin/sh"
        };
    }
}
