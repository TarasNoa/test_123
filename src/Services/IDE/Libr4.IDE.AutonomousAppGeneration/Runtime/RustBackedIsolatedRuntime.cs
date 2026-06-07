using System.Text.RegularExpressions;
using Libr4.IDE.Application.AutonomousAppGeneration.Infrastructure.Rust;
using Libr4.IDE.Domain.AutonomousAppGeneration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Libr4.IDE.Application.AutonomousAppGeneration.Runtime;

/// <summary>
/// <see cref="IIsolatedRuntime"/> decorator that executes python/node/shell commands via
/// <c>libr4_sandbox_executor</c> when enabled and available, otherwise delegates to
/// <see cref="ProcessIsolatedRuntime"/>.
/// </summary>
public sealed class RustBackedIsolatedRuntime : IIsolatedRuntime
{
    private static readonly Regex NodeCommand = new(
        @"^\s*(node|npm|npx|yarn|pnpm)\b",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

    private static readonly Regex PythonCommand = new(
        @"^\s*(python3?|pip3?|pytest)\b",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

    private static readonly Regex ShellCommand = new(
        @"^\s*(bash|sh|cmd|powershell|pwsh)\b",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

    private readonly ProcessIsolatedRuntime _inner;
    private readonly IsolatedRuntimeOptions _options;
    private readonly ILogger<RustBackedIsolatedRuntime> _logger;

    public RustBackedIsolatedRuntime(
        ProcessIsolatedRuntime inner,
        IOptions<IsolatedRuntimeOptions> options,
        ILogger<RustBackedIsolatedRuntime> logger)
    {
        _inner = inner;
        _options = options.Value;
        _logger = logger;
    }

    public string ProviderName => _inner.ProviderName;

    public async Task<IRuntimeSession> StartSessionAsync(
        string image,
        string hostMountPath,
        CancellationToken ct = default)
    {
        var session = await _inner.StartSessionAsync(image, hostMountPath, ct).ConfigureAwait(false);
        return new RustBackedRuntimeSession(session, _options, _logger);
    }

    private sealed class RustBackedRuntimeSession : IRuntimeSession
    {
        private readonly IRuntimeSession _inner;
        private readonly IsolatedRuntimeOptions _options;
        private readonly ILogger _logger;

        public RustBackedRuntimeSession(
            IRuntimeSession inner,
            IsolatedRuntimeOptions options,
            ILogger logger)
        {
            _inner = inner;
            _options = options;
            _logger = logger;
        }

        public string ProviderName => _inner.ProviderName;
        public string SessionId => _inner.SessionId;
        public string HostMountPath => _inner.HostMountPath;
        public string GuestMountPath => _inner.GuestMountPath;
        public string Image => _inner.Image;

        public async Task<ExecResult> ExecAsync(
            string command,
            string workingSubDirectory,
            IDictionary<string, string>? environmentVariables = null,
            TimeSpan? timeout = null,
            CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();

            if (!_options.UseRustSandboxExecutor
                || !RustSandboxExecutorBridge.IsAvailable
                || environmentVariables is { Count: > 0 }
                || !TryMapRustExecution(command, out var language, out var code))
            {
                return await _inner.ExecAsync(
                    command,
                    workingSubDirectory,
                    environmentVariables,
                    timeout,
                    ct).ConfigureAwait(false);
            }

            var workdir = ResolveWorkdir(HostMountPath, workingSubDirectory);
            if (!Directory.Exists(workdir))
            {
                return await _inner.ExecAsync(
                    command,
                    workingSubDirectory,
                    environmentVariables,
                    timeout,
                    ct).ConfigureAwait(false);
            }

            var effectiveTimeout = timeout ?? TimeSpan.FromMinutes(5);
            var start = DateTime.UtcNow;

            if (RustSandboxExecutorBridge.TryExecute(
                    workdir,
                    language,
                    code,
                    effectiveTimeout,
                    _logger,
                    out var rustResult))
            {
                _logger.LogDebug(
                    "Rust sandbox executor handled command (language={Language}, exit={ExitCode})",
                    language,
                    rustResult.ExitCode);

                var logs = BuildLogs(rustResult);
                return new ExecResult(rustResult.ExitCode, DateTime.UtcNow - start, logs);
            }

            _logger.LogDebug("Rust sandbox executor unavailable for command; falling back to process runtime");
            return await _inner.ExecAsync(
                command,
                workingSubDirectory,
                environmentVariables,
                timeout,
                ct).ConfigureAwait(false);
        }

        public ValueTask DisposeAsync() => _inner.DisposeAsync();
    }

    public static bool TryMapRustExecution(string command, out string language, out string code)
    {
        language = string.Empty;
        code = string.Empty;
        if (string.IsNullOrWhiteSpace(command))
            return false;

        var trimmed = command.Trim();

        if (PythonCommand.IsMatch(trimmed) || NodeCommand.IsMatch(trimmed))
        {
            language = "shell";
            code = trimmed;
            return true;
        }

        if (ShellCommand.IsMatch(trimmed) || trimmed.StartsWith("./", StringComparison.Ordinal) || trimmed.StartsWith(".\\", StringComparison.Ordinal))
        {
            language = "shell";
            code = trimmed;
            return true;
        }

        return false;
    }

    private static string ResolveWorkdir(string hostMountPath, string workingSubDirectory)
    {
        if (string.IsNullOrWhiteSpace(workingSubDirectory))
            return hostMountPath;

        return Path.Combine(
            hostMountPath,
            workingSubDirectory.Replace('/', Path.DirectorySeparatorChar));
    }

    private static List<ConsoleLogEntry> BuildLogs(SandboxExecutorBridgeResult result)
    {
        var logs = new List<ConsoleLogEntry>();
        var stamp = DateTime.UtcNow;

        if (!string.IsNullOrEmpty(result.Stdout))
        {
            foreach (var line in result.Stdout.Split('\n', StringSplitOptions.RemoveEmptyEntries))
                logs.Add(new ConsoleLogEntry(stamp, "stdout", line));
        }

        if (!string.IsNullOrEmpty(result.Stderr))
        {
            foreach (var line in result.Stderr.Split('\n', StringSplitOptions.RemoveEmptyEntries))
                logs.Add(new ConsoleLogEntry(stamp, "stderr", line));
        }

        if (result.TimedOut)
            logs.Add(new ConsoleLogEntry(stamp, "stderr", "[timeout]"));

        return logs;
    }
}


