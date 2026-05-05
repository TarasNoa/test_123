using System.Diagnostics;
using Libr4.IDE.Domain.AutonomousAppGeneration;
using Microsoft.Extensions.Logging;

namespace Libr4.IDE.Application.AutonomousAppGeneration.Runtime.Stubs;

/// <summary>
/// WSL-backed runtime provider.
///
/// This implementation executes commands in the default WSL distro and maps
/// the host mount path to the corresponding <c>/mnt/&lt;drive&gt;/...</c> path.
/// It is a practical execution environment for Windows developers when Docker
/// is unavailable.
/// </summary>
public sealed class WslIsolatedRuntime : IIsolatedRuntime
{
    private readonly ILogger<WslIsolatedRuntime> _logger;

    public string ProviderName => "wsl";

    public WslIsolatedRuntime(ILogger<WslIsolatedRuntime> logger)
    {
        _logger = logger;
    }

    public async Task<IRuntimeSession> StartSessionAsync(
        string image,
        string hostMountPath,
        CancellationToken ct = default)
    {
        if (!OperatingSystem.IsWindows())
            throw new PlatformNotSupportedException("WSL runtime is available only on Windows hosts.");
        if (!Directory.Exists(hostMountPath))
            throw new DirectoryNotFoundException(hostMountPath);

        var (exitCode, _, stderr) = await RunWslAsync("--status", ct);
        if (exitCode != 0)
        {
            throw new InvalidOperationException(
                $"WSL is not available or not initialized. stderr: {stderr}");
        }

        _logger.LogInformation(
            "Starting WSL runtime session. Image hint {Image} is ignored for default distro execution.",
            image);

        var sessionId = $"wsl-{Guid.NewGuid():N}";
        var guestMount = ToWslPath(hostMountPath);
        return new WslRuntimeSession(sessionId, image, hostMountPath, guestMount);
    }

    private static async Task<(int ExitCode, string Stdout, string Stderr)> RunWslAsync(
        string arguments,
        CancellationToken ct)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "wsl.exe",
            Arguments = arguments,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = Process.Start(psi) ?? throw new InvalidOperationException("failed to start wsl.exe");
        var stdoutTask = process.StandardOutput.ReadToEndAsync(ct);
        var stderrTask = process.StandardError.ReadToEndAsync(ct);
        await process.WaitForExitAsync(ct);

        return (process.ExitCode, await stdoutTask, await stderrTask);
    }

    private static string ToWslPath(string windowsPath)
    {
        var full = Path.GetFullPath(windowsPath).Replace('\\', '/');
        if (full.Length < 3 || full[1] != ':')
            throw new InvalidOperationException($"Cannot convert path to WSL format: {windowsPath}");

        var drive = char.ToLowerInvariant(full[0]);
        var tail = full[2..].TrimStart('/');
        return $"/mnt/{drive}/{tail}";
    }

    private sealed class WslRuntimeSession : IRuntimeSession
    {
        public string ProviderName => "wsl";
        public string SessionId { get; }
        public string HostMountPath { get; }
        public string GuestMountPath { get; }
        public string Image { get; }

        public WslRuntimeSession(string sessionId, string image, string hostMountPath, string guestMountPath)
        {
            SessionId = sessionId;
            HostMountPath = hostMountPath;
            GuestMountPath = guestMountPath;
            Image = image;
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

            var relative = (workingSubDirectory ?? string.Empty).Trim().Replace('\\', '/').Trim('/');
            var wslWorkingDir = string.IsNullOrWhiteSpace(relative)
                ? GuestMountPath
                : $"{GuestMountPath}/{relative}";

            var exports = environmentVariables is { Count: > 0 }
                ? string.Join(" ", environmentVariables.Select(kv => $"{kv.Key}={ShellEscape(kv.Value)}")) + " "
                : string.Empty;

            var shellCommand = $"{exports}{command}";
            var start = DateTime.UtcNow;
            var logs = new List<ConsoleLogEntry>();

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(timeout ?? TimeSpan.FromMinutes(5));

            var args = $"--cd {ShellEscape(wslWorkingDir)} -- sh -lc {ShellEscape(shellCommand)}";
            try
            {
                var (exitCode, stdout, stderr) = await RunWslAsync(args, cts.Token);
                foreach (var line in SplitLines(stdout))
                    logs.Add(new ConsoleLogEntry(DateTime.UtcNow, "stdout", line));
                foreach (var line in SplitLines(stderr))
                    logs.Add(new ConsoleLogEntry(DateTime.UtcNow, "stderr", line));

                return new ExecResult(exitCode, DateTime.UtcNow - start, logs);
            }
            catch (OperationCanceledException) when (cts.IsCancellationRequested && !ct.IsCancellationRequested)
            {
                logs.Add(new ConsoleLogEntry(DateTime.UtcNow, "stderr", "[timeout]"));
                return new ExecResult(-1, DateTime.UtcNow - start, logs);
            }
        }

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;

        private static IEnumerable<string> SplitLines(string text) =>
            text.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);

        private static string ShellEscape(string value) =>
            $"'{value.Replace("'", "'\"'\"'")}'";
    }
}
