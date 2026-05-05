using System.Diagnostics;
using Libr4.IDE.Domain.AutonomousAppGeneration;

namespace Libr4.IDE.Application.AutonomousAppGeneration.Runtime.Docker;

/// <summary>
/// Thin helper around the <c>docker</c> CLI: spawns the process, streams
/// stdout/stderr into <see cref="ConsoleLogEntry"/> list, enforces a timeout.
/// Kept internal to the Docker runtime so we don't leak CLI specifics outward.
/// </summary>
internal static class DockerProcess
{
    public static async Task<(int exitCode, IReadOnlyList<ConsoleLogEntry> logs)> RunAsync(
        string arguments,
        TimeSpan timeout,
        CancellationToken ct)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "docker",
            Arguments = arguments,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        var logs = new List<ConsoleLogEntry>();
        Process process;
        try
        {
            process = Process.Start(psi) ?? throw new InvalidOperationException("docker failed to start");
        }
        catch (Exception ex)
        {
            logs.Add(new ConsoleLogEntry(DateTime.UtcNow, "stderr",
                $"[docker launch error] {ex.Message}. Is Docker Desktop installed and running?"));
            return (-1, logs);
        }

        var stdout = StreamAsync(process.StandardOutput, "stdout", logs, ct);
        var stderr = StreamAsync(process.StandardError, "stderr", logs, ct);

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(timeout);

        try
        {
            await process.WaitForExitAsync(cts.Token);
        }
        catch (OperationCanceledException) when (cts.IsCancellationRequested && !ct.IsCancellationRequested)
        {
            try { process.Kill(entireProcessTree: true); }
            catch (Exception ex)
            {
                // Best-effort cleanup - ignore kill errors
                System.Diagnostics.Debug.WriteLine($"Failed to kill docker process: {ex.Message}");
            }
            logs.Add(new ConsoleLogEntry(DateTime.UtcNow, "stderr",
                $"[timeout] docker {arguments} exceeded {timeout.TotalSeconds}s"));
            return (-1, logs);
        }

        await Task.WhenAll(stdout, stderr);
        return (process.ExitCode, logs);
    }

    private static async Task StreamAsync(
        StreamReader reader, string stream, List<ConsoleLogEntry> logs, CancellationToken ct)
    {
        string? line;
        while ((line = await reader.ReadLineAsync(ct)) != null)
        {
            lock (logs) { logs.Add(new ConsoleLogEntry(DateTime.UtcNow, stream, line)); }
        }
    }
}
