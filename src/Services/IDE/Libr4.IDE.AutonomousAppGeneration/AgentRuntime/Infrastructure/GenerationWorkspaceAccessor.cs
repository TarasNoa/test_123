using System.Diagnostics;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Abstractions;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Models;
using Libr4.IDE.Application.AutonomousAppGeneration.Runtime;
using Libr4.IDE.Domain.AutonomousAppGeneration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Infrastructure;

/// <summary>
/// Host-side materialized workspace for agent-driven generation (pre-shadow).
/// </summary>
public sealed class GenerationWorkspaceAccessor : IShadowWorkspaceAccessor
{
    private readonly GenerationWorkspaceStore _store;
    private readonly IRuntimeCommandPolicy _commandPolicy;
    private readonly AgentRuntimeOptions _options;
    private readonly ILogger<GenerationWorkspaceAccessor> _logger;

    public GenerationWorkspaceAccessor(
        GenerationWorkspaceStore store,
        IRuntimeCommandPolicy commandPolicy,
        IOptions<AgentRuntimeOptions> options,
        ILogger<GenerationWorkspaceAccessor> logger)
    {
        _store = store;
        _commandPolicy = commandPolicy;
        _options = options.Value;
        _logger = logger;
    }

    public bool TryGetWorkspace(Guid workspaceId, out ShadowWorkspaceContext context)
    {
        if (!_store.TryGetHostPath(workspaceId, out var hostPath))
        {
            context = default!;
            return false;
        }

        context = new ShadowWorkspaceContext(
            workspaceId,
            hostPath,
            string.Empty,
            NullRuntimeSession.Instance);
        return true;
    }

    public Task<ExecResult> ExecAsync(Guid workspaceId, string command, CancellationToken ct = default)
    {
        if (!_options.AllowBashDuringGeneration)
            throw new InvalidOperationException("bash is disabled during agent generation");

        if (!_store.TryGetHostPath(workspaceId, out var hostPath))
            throw new InvalidOperationException($"Generation workspace {workspaceId} not found");

        _commandPolicy.EnsureCommandAllowed(command);
        return ExecOnHostAsync(hostPath, command, _commandPolicy.GetCommandTimeout(command), ct);
    }

    public async Task<string> ReadFileAsync(Guid workspaceId, string relativePath, CancellationToken ct = default)
    {
        var abs = ResolvePath(workspaceId, relativePath);
        if (!File.Exists(abs))
            throw new FileNotFoundException($"File not found: {relativePath}", abs);

        return await File.ReadAllTextAsync(abs, ct).ConfigureAwait(false);
    }

    public async Task WriteFileAsync(Guid workspaceId, string relativePath, string content, CancellationToken ct = default)
    {
        var abs = ResolvePath(workspaceId, relativePath);
        var dir = Path.GetDirectoryName(abs);
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);

        await File.WriteAllTextAsync(abs, content, ct).ConfigureAwait(false);
    }

    public IReadOnlyList<string> GlobFiles(Guid workspaceId, string globPattern)
    {
        if (!_store.TryGetHostPath(workspaceId, out var hostPath))
            return Array.Empty<string>();

        var pattern = globPattern.Replace('\\', '/').TrimStart('/');
        if (pattern.Contains("..", StringComparison.Ordinal))
            return Array.Empty<string>();

        var dirPart = Path.GetDirectoryName(pattern.Replace('/', Path.DirectorySeparatorChar)) ?? string.Empty;
        var filePart = Path.GetFileName(pattern);
        var searchRoot = string.IsNullOrEmpty(dirPart)
            ? hostPath
            : Path.Combine(hostPath, dirPart);

        if (!Directory.Exists(searchRoot))
            return Array.Empty<string>();

        return Directory.EnumerateFiles(searchRoot, filePart, SearchOption.AllDirectories)
            .Select(f => f[(hostPath.Length + 1)..].Replace('\\', '/'))
            .OrderBy(f => f, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private string ResolvePath(Guid workspaceId, string relativePath)
    {
        if (!_store.TryGetHostPath(workspaceId, out var hostPath))
            throw new InvalidOperationException($"Generation workspace {workspaceId} not found");

        var safe = relativePath.Replace('/', Path.DirectorySeparatorChar).TrimStart(Path.DirectorySeparatorChar);
        if (safe.Contains("..", StringComparison.Ordinal))
            throw new InvalidOperationException("Path traversal denied");

        return Path.Combine(hostPath, safe);
    }

    private async Task<ExecResult> ExecOnHostAsync(string workingDir, string command, TimeSpan timeout, CancellationToken ct)
    {
        var start = DateTime.UtcNow;
        using var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = OperatingSystem.IsWindows() ? "cmd.exe" : "/bin/bash",
                Arguments = OperatingSystem.IsWindows() ? $"/c {command}" : $"-lc \"{command.Replace("\"", "\\\"")}\"",
                WorkingDirectory = workingDir,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };

        process.Start();
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeoutCts.CancelAfter(timeout);
        try
        {
            var stdout = await process.StandardOutput.ReadToEndAsync(timeoutCts.Token).ConfigureAwait(false);
            var stderr = await process.StandardError.ReadToEndAsync(timeoutCts.Token).ConfigureAwait(false);
            await process.WaitForExitAsync(timeoutCts.Token).ConfigureAwait(false);

            var logs = new List<ConsoleLogEntry>();
            var at = DateTime.UtcNow;
            if (!string.IsNullOrWhiteSpace(stdout))
                logs.Add(new ConsoleLogEntry(at, "stdout", stdout));
            if (!string.IsNullOrWhiteSpace(stderr))
                logs.Add(new ConsoleLogEntry(at, "stderr", stderr));

            return new ExecResult(process.ExitCode, DateTime.UtcNow - start, logs);
        }
        catch (OperationCanceledException)
        {
            try { process.Kill(entireProcessTree: true); } catch { /* best effort */ }
            throw new TimeoutException($"Generation bash exceeded timeout of {(int)timeout.TotalSeconds}s.");
        }
    }

    private sealed class NullRuntimeSession : IRuntimeSession
    {
        public static readonly NullRuntimeSession Instance = new();
        public string SessionId => "generation-host";
        public string ProviderName => "host";
        public string Image => "host";
        public string HostMountPath => string.Empty;
        public string GuestMountPath => "/workspace";

        public Task<ExecResult> ExecAsync(
            string command,
            string workingSubDirectory,
            IDictionary<string, string>? environmentVariables = null,
            TimeSpan? timeout = null,
            CancellationToken ct = default) =>
            throw new NotSupportedException("Use GenerationWorkspaceAccessor.ExecAsync");

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}
