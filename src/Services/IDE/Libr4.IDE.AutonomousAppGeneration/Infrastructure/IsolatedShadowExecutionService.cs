using System.Collections.Concurrent;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentEvents;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Abstractions;
using Libr4.IDE.Application.AutonomousAppGeneration.FastContext;
using Libr4.IDE.Application.Obscura;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Models;
using Libr4.IDE.Application.AutonomousAppGeneration.Runtime;
using Libr4.IDE.Application.AutonomousAppGeneration.Services;
using Libr4.IDE.Application.GitAutomation;
using Libr4.IDE.Domain.AutonomousAppGeneration;
using Microsoft.Extensions.Logging;

namespace Libr4.IDE.Application.AutonomousAppGeneration.Infrastructure;

/// <summary>
/// <see cref="IShadowExecutionService"/> backed by the isolated runtime stack
/// (<see cref="IWorkspacePool"/> + <see cref="IIsolatedRuntime"/>). Executes
/// the exact build and test commands emitted by the planner — so the
/// orchestrator no longer cares whether the generated app is .NET, Python,
/// Node, Rust, Go or anything else.
/// </summary>
public sealed class IsolatedShadowExecutionService : IShadowExecutionService, IShadowWorkspaceAccessor
{
    private readonly IWorkspacePool _pool;
    private readonly IWorkspaceSyncService _sync;
    private readonly IRuntimeCommandPolicy _runtimeCommandPolicy;
    private readonly IShadowToolchainWarmCache _warmCache;
    private readonly IAgentEventEmitter _eventEmitter;
    private readonly IObscuraNetworkRouter? _networkRouter;
    private readonly IShadowGitCheckpointService? _gitCheckpoint;
    private readonly IFastContextPrefetcher? _fastContext;
    private readonly ILogger<IsolatedShadowExecutionService> _logger;
    private readonly Dictionary<Guid, int> _workspacePorts = new();
    private static readonly object _portLock = new();
    private static int _nextPort = 4001;
    private readonly ConcurrentDictionary<Guid, WorkspaceHandle> _handles = new();
    private readonly ConcurrentDictionary<Guid, GenerationPlan> _plans = new();

    public IsolatedShadowExecutionService(
        IWorkspacePool pool,
        IWorkspaceSyncService sync,
        IRuntimeCommandPolicy runtimeCommandPolicy,
        IShadowToolchainWarmCache warmCache,
        IAgentEventEmitter eventEmitter,
        ILogger<IsolatedShadowExecutionService> logger,
        IObscuraNetworkRouter? networkRouter = null,
        IShadowGitCheckpointService? gitCheckpoint = null,
        IFastContextPrefetcher? fastContext = null)
    {
        _pool = pool;
        _sync = sync;
        _runtimeCommandPolicy = runtimeCommandPolicy;
        _warmCache = warmCache;
        _eventEmitter = eventEmitter;
        _networkRouter = networkRouter;
        _gitCheckpoint = gitCheckpoint;
        _fastContext = fastContext;
        _logger = logger;
    }

    public async Task<Guid> PrepareWorkspaceAsync(
        IReadOnlyList<GeneratedFile> files,
        string runtimeImage,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(runtimeImage))
            runtimeImage = "mcr.microsoft.com/dotnet/sdk:8.0";

        var handle = await _pool.AcquireAsync(runtimeImage, ct);
        _handles[handle.WorkspaceId] = handle;
        _sync.StartWatching(handle);

        // Allocate unique port for this workspace
        int port;
        lock (_portLock)
        {
            port = _nextPort++;
            if (_nextPort > 4999) _nextPort = 4001; // Reset if we exceed range
        }
        _workspacePorts[handle.WorkspaceId] = port;
        _networkRouter?.RegisterWorkspace(handle.WorkspaceId, port);
        _logger.LogInformation("[{Ws}] Allocated port {Port}", handle.WorkspaceId, port);

        await WriteFilesAsync(handle.HostPath, files, ct);
        if (_gitCheckpoint is not null)
            await _gitCheckpoint.EnsureInitializedAsync(handle.HostPath, ct).ConfigureAwait(false);
        await _warmCache.PrepareWorkspaceAsync(handle.HostPath, ct).ConfigureAwait(false);
        if (_fastContext is not null)
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    await _fastContext.WarmIndexAsync(handle.HostPath, ct: CancellationToken.None).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "[{Ws}] Fast context warm index failed (non-fatal)", handle.WorkspaceId);
                }
            }, CancellationToken.None);
        }

        return handle.WorkspaceId;
    }

    public Task UpdateWorkspaceAsync(
        Guid workspaceId,
        IReadOnlyList<GeneratedFile> files,
        CancellationToken ct = default)
    {
        if (!_handles.TryGetValue(workspaceId, out var handle))
            throw new InvalidOperationException($"Workspace {workspaceId} not prepared");
        return WriteFilesAsync(handle.HostPath, files, ct);
    }

    public async Task<ExecutionResult> RunAsync(
        Guid workspaceId, GenerationPlan plan, CancellationToken ct = default)
    {
        if (!_handles.TryGetValue(workspaceId, out var handle))
            throw new InvalidOperationException($"Workspace {workspaceId} not prepared");

        // If the plan requests a different runtime image than the current
        // session, migrate the workspace to a session that matches.
        handle = await EnsureCorrectImageAsync(handle, plan.RuntimeImage, ct);

        _plans[workspaceId] = plan;
        var start = DateTime.UtcNow;
        var combinedLogs = new List<ConsoleLogEntry>();
        var commandExecutions = new List<CommandExecutionRecord>();

        // The workspace's files live under <session-mount>/<workspaceFolder>.
        var subdir = Path.GetFileName(handle.HostPath);

        // 1. Build
        foreach (var cmd in ResolveBuildCommands(plan, handle.Runtime))
        {
            _runtimeCommandPolicy.EnsureCommandAllowed(cmd);
            _logger.LogInformation("[{Ws}] BUILD: {Cmd}", workspaceId, cmd);
            
            // Emit build start event
            await _eventEmitter.EmitBuildStartAsync(workspaceId, cmd);
            
            var env = new Dictionary<string, string> { ["PORT"] = _workspacePorts[workspaceId].ToString() };
            var timeout = _runtimeCommandPolicy.GetCommandTimeout(cmd);
            var cmdStartedAt = DateTime.UtcNow;
            var result = await handle.Runtime.ExecAsync(cmd, subdir, env, timeout, ct);
            
            var output = string.Join("\n", result.Logs.Select(l => l.Message));
            var durationMs = (long)result.Duration.TotalMilliseconds;
            
            // Emit build complete event
            await _eventEmitter.EmitBuildCompleteAsync(workspaceId, cmd, output, result.ExitCode, durationMs);
            
            commandExecutions.Add(new CommandExecutionRecord(
                phase: "build",
                command: cmd,
                exitCode: result.ExitCode,
                duration: result.Duration,
                runtimeProvider: handle.Runtime.ProviderName,
                runtimeSessionId: handle.Runtime.SessionId,
                executedAtUtc: cmdStartedAt));
            combinedLogs.AddRange(result.Logs);
            if (!result.Succeeded)
            {
                return new ExecutionResult(
                    succeeded: false,
                    exitCode: result.ExitCode,
                    duration: DateTime.UtcNow - start,
                    logs: combinedLogs,
                    commandExecutions: commandExecutions);
            }
        }

        // 2. Test
        foreach (var cmd in ResolveTestCommands(plan, handle.Runtime))
        {
            _runtimeCommandPolicy.EnsureCommandAllowed(cmd);
            _logger.LogInformation("[{Ws}] TEST: {Cmd}", workspaceId, cmd);
            
            // Emit test start event
            await _eventEmitter.EmitTestStartAsync(workspaceId, cmd);
            
            var env = new Dictionary<string, string> { ["PORT"] = _workspacePorts[workspaceId].ToString() };
            var timeout = _runtimeCommandPolicy.GetCommandTimeout(cmd);
            var cmdStartedAt = DateTime.UtcNow;
            var result = await handle.Runtime.ExecAsync(cmd, subdir, env, timeout, ct);
            
            var output = string.Join("\n", result.Logs.Select(l => l.Message));
            var durationMs = (long)result.Duration.TotalMilliseconds;
            
            // Emit test complete event
            await _eventEmitter.EmitTestCompleteAsync(workspaceId, cmd, output, result.ExitCode, durationMs);
            
            commandExecutions.Add(new CommandExecutionRecord(
                phase: "test",
                command: cmd,
                exitCode: result.ExitCode,
                duration: result.Duration,
                runtimeProvider: handle.Runtime.ProviderName,
                runtimeSessionId: handle.Runtime.SessionId,
                executedAtUtc: cmdStartedAt));
            combinedLogs.AddRange(result.Logs);
            if (!result.Succeeded)
            {
                return new ExecutionResult(
                    succeeded: false,
                    exitCode: result.ExitCode,
                    duration: DateTime.UtcNow - start,
                    logs: combinedLogs,
                    commandExecutions: commandExecutions);
            }
        }

        return new ExecutionResult(
            succeeded: true,
            exitCode: 0,
            duration: DateTime.UtcNow - start,
            logs: combinedLogs,
            commandExecutions: commandExecutions);
    }

    public async Task DisposeWorkspaceAsync(Guid workspaceId, CancellationToken ct = default)
    {
        _plans.TryRemove(workspaceId, out _);
        _workspacePorts.Remove(workspaceId);
        _networkRouter?.UnregisterWorkspace(workspaceId);
        if (!_handles.TryRemove(workspaceId, out var handle)) return;
        _sync.StopWatching(workspaceId);
        await _pool.ReleaseAsync(handle, ct);
    }

    public bool TryGetWorkspace(Guid workspaceId, out ShadowWorkspaceContext context)
    {
        if (!_handles.TryGetValue(workspaceId, out var handle))
        {
            context = default!;
            return false;
        }

        context = new ShadowWorkspaceContext(
            handle.WorkspaceId,
            handle.HostPath,
            Path.GetFileName(handle.HostPath),
            handle.Runtime);
        return true;
    }

    public async Task<ExecResult> ExecAsync(Guid workspaceId, string command, CancellationToken ct = default)
    {
        if (!_handles.TryGetValue(workspaceId, out var handle))
            throw new InvalidOperationException($"Workspace {workspaceId} not prepared");

        _runtimeCommandPolicy.EnsureCommandAllowed(command);
        var subdir = Path.GetFileName(handle.HostPath);
        var env = _workspacePorts.TryGetValue(workspaceId, out var port)
            ? new Dictionary<string, string> { ["PORT"] = port.ToString() }
            : null;
        var timeout = _runtimeCommandPolicy.GetCommandTimeout(command);
        return await handle.Runtime.ExecAsync(command, subdir, env, timeout, ct).ConfigureAwait(false);
    }

    public async Task<string> ReadFileAsync(Guid workspaceId, string relativePath, CancellationToken ct = default)
    {
        if (!_handles.TryGetValue(workspaceId, out var handle))
            throw new InvalidOperationException($"Workspace {workspaceId} not prepared");

        var safe = relativePath.Replace('/', Path.DirectorySeparatorChar).TrimStart(Path.DirectorySeparatorChar);
        if (safe.Contains("..", StringComparison.Ordinal))
            throw new InvalidOperationException("Path traversal denied");

        var abs = Path.Combine(handle.HostPath, safe);
        if (!File.Exists(abs))
            throw new FileNotFoundException($"File not found: {relativePath}", abs);

        return await File.ReadAllTextAsync(abs, ct).ConfigureAwait(false);
    }

    public async Task WriteFileAsync(Guid workspaceId, string relativePath, string content, CancellationToken ct = default)
    {
        if (!_handles.TryGetValue(workspaceId, out var handle))
            throw new InvalidOperationException($"Workspace {workspaceId} not prepared");

        var safe = relativePath.Replace('/', Path.DirectorySeparatorChar).TrimStart(Path.DirectorySeparatorChar);
        if (safe.Contains("..", StringComparison.Ordinal))
            throw new InvalidOperationException("Path traversal denied");

        var abs = Path.Combine(handle.HostPath, safe);
        var dir = Path.GetDirectoryName(abs);
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);

        await File.WriteAllTextAsync(abs, content, ct).ConfigureAwait(false);
    }

    public IReadOnlyList<string> GlobFiles(Guid workspaceId, string globPattern)
    {
        if (!_handles.TryGetValue(workspaceId, out var handle))
            return Array.Empty<string>();

        var pattern = globPattern.Replace('\\', '/').TrimStart('/');
        if (pattern.Contains("..", StringComparison.Ordinal))
            return Array.Empty<string>();

        var dirPart = Path.GetDirectoryName(pattern.Replace('/', Path.DirectorySeparatorChar)) ?? string.Empty;
        var filePart = Path.GetFileName(pattern);
        var searchRoot = string.IsNullOrEmpty(dirPart)
            ? handle.HostPath
            : Path.Combine(handle.HostPath, dirPart);

        if (!Directory.Exists(searchRoot))
            return Array.Empty<string>();

        return Directory.EnumerateFiles(searchRoot, filePart, SearchOption.AllDirectories)
            .Select(f => f[(handle.HostPath.Length + 1)..].Replace('\\', '/'))
            .OrderBy(f => f, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    // --- helpers -----------------------------------------------------------

    private async Task<WorkspaceHandle> EnsureCorrectImageAsync(
        WorkspaceHandle current, string requiredImage, CancellationToken ct)
    {
        if (string.Equals(current.Runtime.Image, requiredImage, StringComparison.Ordinal))
            return current;

        _logger.LogInformation(
            "Migrating workspace {Id} from image {From} to {To}",
            current.WorkspaceId, current.Runtime.Image, requiredImage);

        var migrated = await _pool.AcquireAsync(requiredImage, ct);

        // Copy files from the old workspace to the new one. Since both sides
        // of the bind-mount see the same bytes, we just use host-side File IO.
        CopyDirectory(current.HostPath, migrated.HostPath);

        _sync.StopWatching(current.WorkspaceId);
        await _pool.ReleaseAsync(current, ct);

        _handles.TryRemove(current.WorkspaceId, out _);
        _handles[migrated.WorkspaceId] = migrated;
        _sync.StartWatching(migrated);
        return migrated;
    }

    private IEnumerable<string> ResolveBuildCommands(GenerationPlan plan, IRuntimeSession runtime)
    {
        var cmds = BuildOrDefault(plan).ToList();
        ApplyJavaReactToolchain(cmds, runtime, plan, prependBootstrap: true);
        return cmds;
    }

    private IEnumerable<string> ResolveTestCommands(GenerationPlan plan, IRuntimeSession runtime)
    {
        var cmds = TestOrDefault(plan).ToList();
        ApplyJavaReactToolchain(cmds, runtime, plan, prependBootstrap: false);
        return cmds;
    }

    private void ApplyJavaReactToolchain(
        IList<string> cmds,
        IRuntimeSession runtime,
        GenerationPlan plan,
        bool prependBootstrap)
    {
        if (JavaReactWindowsToolchainBootstrap.ShouldPrepend(runtime.ProviderName, plan))
        {
            var needsMaven = cmds.Any(c => c.Contains("mvn", StringComparison.OrdinalIgnoreCase));
            var warmMavenReady = _warmCache.IsEnabled && _warmCache.IsMavenReady;
            if ((prependBootstrap || needsMaven) && !warmMavenReady)
                cmds.Insert(0, JavaReactWindowsToolchainBootstrap.Command);

            var mavenExports = _warmCache.IsEnabled
                ? _warmCache.BuildMavenEnvironmentExports()
                : JavaReactWindowsToolchainBootstrap.MavenPathExports;
            var npmExports = _warmCache.IsEnabled ? _warmCache.BuildNpmCacheExports() : string.Empty;

            for (var i = 0; i < cmds.Count; i++)
            {
                if (JavaReactWindowsToolchainBootstrap.IsMavenInvocation(cmds[i]))
                {
                    cmds[i] = _warmCache.EnrichMavenInvocation(cmds[i]);
                    cmds[i] = QualifyMavenCommand(cmds[i], warmMavenReady);
                    cmds[i] = $"{mavenExports} && {cmds[i]}";
                }
                else if (IsNpmInvocation(cmds[i]) && !string.IsNullOrEmpty(npmExports))
                {
                    cmds[i] = $"{npmExports} && {cmds[i]}";
                }
            }
        }
        else if (JavaReactWslToolchainBootstrap.ShouldPrepend(runtime.ProviderName, plan))
        {
            if (prependBootstrap)
                cmds.Insert(0, JavaReactWslToolchainBootstrap.Command);

            for (var i = 0; i < cmds.Count; i++)
            {
                if (cmds[i].Contains("mvn", StringComparison.OrdinalIgnoreCase)
                    || cmds[i].Contains("npm", StringComparison.OrdinalIgnoreCase))
                {
                    cmds[i] = $"{JavaReactWslToolchainBootstrap.JavaHomeExports} && {cmds[i]}";
                }
            }
        }
    }

    private string QualifyMavenCommand(string command, bool warmMavenReady)
    {
        if (warmMavenReady)
        {
            var mvnPath = _warmCache.ResolveMavenExecutablePath().Replace("/", "\\");
            return System.Text.RegularExpressions.Regex.Replace(
                command,
                @"\bmvn\b",
                $"\"{mvnPath}\"",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        }

        return JavaReactWindowsToolchainBootstrap.QualifyMavenExecutable(command);
    }

    private static bool IsNpmInvocation(string command) =>
        !string.IsNullOrWhiteSpace(command)
        && System.Text.RegularExpressions.Regex.IsMatch(command, @"\bnpm\b", System.Text.RegularExpressions.RegexOptions.IgnoreCase);

    private static IEnumerable<string> BuildOrDefault(GenerationPlan plan)
    {
        if (plan.BuildCommands.Count > 0) return plan.BuildCommands;
        // Sensible defaults by image family.
        return plan.RuntimeImage switch
        {
            var img when img.Contains("dotnet") => new[] { "dotnet restore", "dotnet build -c Release --nologo" },
            var img when img.Contains("python") => new[] { "python -m pip install -r requirements.txt || true" },
            var img when img.Contains("node")   => new[] { "npm ci || npm install" },
            var img when img.Contains("rust")   => new[] { "cargo build --release" },
            var img when img.Contains("golang") => new[] { "go build ./..." },
            _ => Array.Empty<string>()
        };
    }

    private static IEnumerable<string> TestOrDefault(GenerationPlan plan)
    {
        if (plan.TestCommands.Count > 0) return plan.TestCommands;
        return plan.RuntimeImage switch
        {
            var img when img.Contains("dotnet") => new[] { "dotnet test -c Release --no-build --nologo --logger:\"console;verbosity=minimal\"" },
            var img when img.Contains("python") => new[] { "python -m pytest -q" },
            var img when img.Contains("node")   => new[] { "npm test --silent" },
            var img when img.Contains("rust")   => new[] { "cargo test --release" },
            var img when img.Contains("golang") => new[] { "go test ./..." },
            _ => Array.Empty<string>()
        };
    }

    private static async Task WriteFilesAsync(
        string root, IReadOnlyList<GeneratedFile> files, CancellationToken ct)
    {
        foreach (var file in files)
        {
            var repaired = StackArtifactCompleteness.RepairGeneratedFile(file);
            if (repaired is null
                || !StackArtifactCompleteness.IsPlausibleFilePath(repaired.RelativePath))
                continue;

            var safe = repaired.RelativePath.Replace('/', Path.DirectorySeparatorChar);
            var abs = Path.Combine(root, safe);
            var dir = Path.GetDirectoryName(abs);
            if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
            await File.WriteAllTextAsync(abs, repaired.Content ?? string.Empty, ct);
        }
    }

    private static void CopyDirectory(string src, string dst)
    {
        Directory.CreateDirectory(dst);
        foreach (var dir in Directory.GetDirectories(src, "*", SearchOption.AllDirectories))
        {
            Directory.CreateDirectory(dir.Replace(src, dst));
        }
        foreach (var file in Directory.GetFiles(src, "*", SearchOption.AllDirectories))
        {
            File.Copy(file, file.Replace(src, dst), overwrite: true);
        }
    }
}
