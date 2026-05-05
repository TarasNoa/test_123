using System.Diagnostics;
using Libr4.IDE.Domain.AutonomousAppGeneration;
using Microsoft.Extensions.Logging;

namespace Libr4.IDE.Application.AutonomousAppGeneration.Runtime;

/// <summary>
/// Fallback <see cref="IIsolatedRuntime"/> that runs commands directly on
/// the host. WARNING: this provides NO isolation and should only be used on
/// trusted developer machines where Docker / VM is unavailable.
/// </summary>
public sealed class ProcessIsolatedRuntime : IIsolatedRuntime
{
    private readonly ILogger<ProcessIsolatedRuntime> _logger;

    public string ProviderName => "process";

    public ProcessIsolatedRuntime(ILogger<ProcessIsolatedRuntime> logger)
    {
        _logger = logger;
    }

    public Task<IRuntimeSession> StartSessionAsync(
        string image, string hostMountPath, CancellationToken ct = default)
    {
        if (!Directory.Exists(hostMountPath))
            throw new DirectoryNotFoundException(hostMountPath);

        _logger.LogWarning(
            "ProcessIsolatedRuntime is NOT a security boundary. Image hint {Image} is ignored.", image);

        var session = new ProcessRuntimeSession(
            sessionId: $"proc-{Guid.NewGuid():N}",
            image: image,
            hostPath: hostMountPath);
        return Task.FromResult<IRuntimeSession>(session);
    }

    private sealed class ProcessRuntimeSession : IRuntimeSession
    {
        public string ProviderName => "process";
        public string SessionId { get; }
        public string HostMountPath { get; }
        public string GuestMountPath => HostMountPath; // same path, no virtualisation
        public string Image { get; }

        public ProcessRuntimeSession(string sessionId, string image, string hostPath)
        {
            SessionId = sessionId;
            Image = image;
            HostMountPath = hostPath;
        }

        public async Task<ExecResult> ExecAsync(
            string command,
            string workingSubDirectory,
            IDictionary<string, string>? environmentVariables = null,
            TimeSpan? timeout = null,
            CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(command))
                return new ExecResult(0, TimeSpan.Zero, Array.Empty<ConsoleLogEntry>());

            var workdir = string.IsNullOrWhiteSpace(workingSubDirectory)
                ? HostMountPath
                : Path.Combine(HostMountPath, workingSubDirectory.Replace('/', Path.DirectorySeparatorChar));

            // Use the platform shell: cmd on Windows, sh elsewhere.
            var (shell, argsPrefix) = OperatingSystem.IsWindows()
                ? ("cmd.exe", "/c ")
                : ("/bin/sh", "-c ");

            var psi = new ProcessStartInfo
            {
                FileName = shell,
                Arguments = argsPrefix + (OperatingSystem.IsWindows() ? command : $"\"{command.Replace("\"", "\\\"")}\""),
                WorkingDirectory = workdir,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            // Add environment variables if provided
            if (environmentVariables != null)
            {
                foreach (var kv in environmentVariables)
                {
                    psi.Environment[kv.Key] = kv.Value;
                }
            }

            var logs = new List<ConsoleLogEntry>();
            var start = DateTime.UtcNow;
            Process process;
            try
            {
                process = Process.Start(psi) ?? throw new InvalidOperationException("failed to start shell");
            }
            catch (Exception ex)
            {
                logs.Add(new ConsoleLogEntry(DateTime.UtcNow, "stderr", $"[launch error] {ex.Message}"));
                return new ExecResult(-1, DateTime.UtcNow - start, logs);
            }

            var stdout = StreamAsync(process.StandardOutput, "stdout", logs, ct);
            var stderr = StreamAsync(process.StandardError, "stderr", logs, ct);

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(timeout ?? TimeSpan.FromMinutes(5));

            try { await process.WaitForExitAsync(cts.Token); }
            catch (OperationCanceledException) when (cts.IsCancellationRequested && !ct.IsCancellationRequested)
            {
                try { process.Kill(entireProcessTree: true); }
                catch (Exception killEx)
                {
                    logs.Add(new ConsoleLogEntry(DateTime.UtcNow, "stderr", $"[timeout] [kill failed: {killEx.Message}]"));
                    return new ExecResult(-1, DateTime.UtcNow - start, logs);
                }
                logs.Add(new ConsoleLogEntry(DateTime.UtcNow, "stderr", "[timeout]"));
                return new ExecResult(-1, DateTime.UtcNow - start, logs);
            }

            await Task.WhenAll(stdout, stderr);
            return new ExecResult(process.ExitCode, DateTime.UtcNow - start, logs);
        }

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;

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
}
