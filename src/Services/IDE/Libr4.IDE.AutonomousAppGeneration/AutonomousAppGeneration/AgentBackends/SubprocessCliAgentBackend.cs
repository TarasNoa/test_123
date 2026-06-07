using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Models;
using Libr4.IDE.Application.AutonomousAppGeneration.Runtime;
using Libr4.IDE.Domain.AutonomousAppGeneration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Libr4.IDE.Application.AutonomousAppGeneration.AgentBackends;

public abstract class SubprocessCliAgentBackend : IAgentBackend
{
    private readonly ExternalAgentBackendOptions _options;
    private readonly IsolatedExternalBackendRunner? _isolatedRunner;
    private readonly ILogger _logger;
    private readonly ConcurrentDictionary<string, CliBackendState> _active = new();

    protected SubprocessCliAgentBackend(
        ExternalAgentBackendOptions options,
        ILogger logger,
        IsolatedExternalBackendRunner? isolatedRunner = null)
    {
        _options = options;
        _logger = logger;
        _isolatedRunner = isolatedRunner;
    }

    public abstract AgentBackendKind Kind { get; }

    protected abstract string ResolveExecutable();

    protected virtual IReadOnlyList<string> BuildArguments(AgentBackendSpawnRequest request, string prompt) =>
        ["exec", prompt];

    public Task<AgentBackendHandle> SpawnAsync(AgentBackendSpawnRequest request, CancellationToken ct = default)
    {
        var prompt = request.InitialMessage
                     ?? request.SessionRequest?.Objective
                     ?? throw new InvalidOperationException("cli_backend_requires_prompt");

        var workspace = request.SessionRequest?.Workspace.HostPath
                        ?? Directory.GetCurrentDirectory();

        var instanceId = Guid.NewGuid().ToString("N");
        var handle = new AgentBackendHandle(instanceId, request.RunId, Kind, DateTime.UtcNow);
        var state = new CliBackendState(handle, workspace, prompt);
        _active[instanceId] = state;
        state.RunTask = StartProcessAsync(instanceId, request, prompt, workspace, ct);
        return Task.FromResult(handle);
    }

    public Task SendMessageAsync(string backendInstanceId, string message, CancellationToken ct = default)
    {
        if (!_active.TryGetValue(backendInstanceId, out var state))
            throw new KeyNotFoundException($"backend_not_found:{backendInstanceId}");

        state.Events.Enqueue(AgentBackendEventMapper.CreateMessageEvent(
            state.Handle.RunId,
            backendInstanceId,
            "user",
            message));
        return Task.CompletedTask;
    }

    public async IAsyncEnumerable<AgentBackendEvent> StreamEventsAsync(
        string backendInstanceId,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        if (!_active.TryGetValue(backendInstanceId, out var state))
            yield break;

        yield return AgentBackendEventMapper.CreateStatusEvent(state.Handle.RunId, backendInstanceId, "spawned");

        var lastLine = 0;
        while (!ct.IsCancellationRequested)
        {
            while (lastLine < state.OutputLines.Count)
            {
                var line = state.OutputLines[lastLine++];
                yield return ParseOutputLine(state.Handle.RunId, backendInstanceId, line);
            }

            while (state.Events.TryDequeue(out var queued))
                yield return queued;

            if (state.RunTask is { IsCompleted: true })
                break;

            await Task.Delay(200, ct).ConfigureAwait(false);
        }
    }

    public Task CancelAsync(string backendInstanceId, CancellationToken ct = default)
    {
        if (!_active.TryGetValue(backendInstanceId, out var state))
            return Task.CompletedTask;

        try
        {
            state.ExecCts?.Cancel();
            if (state.IsolatedSession is not null)
            {
                _ = state.IsolatedSession.DisposeAsync().AsTask();
                state.IsolatedSession = null;
            }

            if (state.Process is { HasExited: false })
                state.Process.Kill(entireProcessTree: true);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to kill CLI backend process {InstanceId}", backendInstanceId);
        }

        state.Status = AgentBackendRunStatus.Cancelled;
        return Task.CompletedTask;
    }

    public Task<AgentBackendStatus> GetStatusAsync(string backendInstanceId, CancellationToken ct = default)
    {
        if (!_active.TryGetValue(backendInstanceId, out var state))
            throw new KeyNotFoundException($"backend_not_found:{backendInstanceId}");

        RefreshStatus(state);
        return Task.FromResult(new AgentBackendStatus(
            backendInstanceId,
            state.Status,
            state.Stage,
            null,
            null,
            state.Error));
    }

    public async Task<AgentSessionResult> WaitForCompletionAsync(string backendInstanceId, CancellationToken ct)
    {
        if (!_active.TryGetValue(backendInstanceId, out var state))
            throw new KeyNotFoundException($"backend_not_found:{backendInstanceId}");

        if (state.RunTask is not null)
            await state.RunTask.ConfigureAwait(false);

        RefreshStatus(state);
        return state.Result ?? new AgentSessionResult(
            false,
            state.Error ?? "cli_backend_no_result",
            Array.Empty<GeneratedFile>(),
            0,
            state.OutputLines.ToArray());
    }

    private async Task StartProcessAsync(
        string instanceId,
        AgentBackendSpawnRequest request,
        string prompt,
        string workspace,
        CancellationToken ct)
    {
        if (!_active.TryGetValue(instanceId, out var state))
            return;

        var timeoutSeconds = int.TryParse(
            request.Backend.Config.GetValueOrDefault("timeoutSeconds"),
            out var configured)
            ? configured
            : _options.DefaultTimeoutSeconds;

        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeoutCts.CancelAfter(TimeSpan.FromSeconds(Math.Clamp(timeoutSeconds, 30, 7200)));

        try
        {
            var executable = request.Backend.Config.GetValueOrDefault("executableOverride") ?? ResolveExecutable();
            var args = BuildArguments(request, prompt);

            if (ShouldIsolate(request))
            {
                await StartIsolatedAsync(instanceId, request, executable, args, workspace, timeoutSeconds, timeoutCts.Token)
                    .ConfigureAwait(false);
                return;
            }

            var psi = new ProcessStartInfo
            {
                FileName = executable,
                WorkingDirectory = workspace,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            foreach (var arg in args)
                psi.ArgumentList.Add(arg);

            state.Process = Process.Start(psi)
                            ?? throw new InvalidOperationException($"failed_to_start:{executable}");
            state.Status = AgentBackendRunStatus.Running;
            state.Stage = "running";

            var stdoutTask = PumpLinesAsync(state.Process.StandardOutput, state.OutputLines, timeoutCts.Token);
            var stderrTask = state.Process.StandardError.ReadToEndAsync(timeoutCts.Token);

            await state.Process.WaitForExitAsync(timeoutCts.Token).ConfigureAwait(false);
            await stdoutTask.ConfigureAwait(false);
            state.Stderr = await stderrTask.ConfigureAwait(false);

            var succeeded = state.Process.ExitCode == 0;
            state.Status = succeeded ? AgentBackendRunStatus.Completed : AgentBackendRunStatus.Failed;
            state.Stage = succeeded ? "completed" : "failed";
            state.Error = succeeded ? null : TrimForSummary(state.Stderr);

            var summary = state.OutputLines.LastOrDefault(l => !string.IsNullOrWhiteSpace(l))
                          ?? state.Stderr
                          ?? state.Stage;

            state.Result = new AgentSessionResult(
                succeeded,
                TrimForSummary(summary),
                Array.Empty<GeneratedFile>(),
                0,
                state.OutputLines.ToArray());
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            state.Status = AgentBackendRunStatus.Failed;
            state.Stage = "timed_out";
            state.Error = $"cli_backend_timeout:{timeoutSeconds}s";
            state.Result = new AgentSessionResult(false, state.Error, Array.Empty<GeneratedFile>(), 0, state.OutputLines.ToArray());
            await CancelAsync(instanceId, ct).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            state.Status = AgentBackendRunStatus.Failed;
            state.Error = ex.Message;
            state.Result = new AgentSessionResult(false, ex.Message, Array.Empty<GeneratedFile>(), 0, state.OutputLines.ToArray());
            _logger.LogError(ex, "CLI backend {Kind} failed for instance {InstanceId}", Kind, instanceId);
        }
    }

    private bool ShouldIsolate(AgentBackendSpawnRequest request) =>
        _isolatedRunner is not null
        && (_options.IsolateExternalBackends
            || string.Equals(request.Backend.Config.GetValueOrDefault("isolate"), "true", StringComparison.OrdinalIgnoreCase));

    private async Task StartIsolatedAsync(
        string instanceId,
        AgentBackendSpawnRequest request,
        string executable,
        IReadOnlyList<string> args,
        string workspace,
        int timeoutSeconds,
        CancellationToken ct)
    {
        if (!_active.TryGetValue(instanceId, out var state) || _isolatedRunner is null)
            return;

        state.Status = AgentBackendRunStatus.Running;
        state.Stage = "running_isolated";
        state.ExecCts = CancellationTokenSource.CreateLinkedTokenSource(ct);

        var command = IsolatedExternalBackendRunner.BuildShellCommand(executable, args);
        var env = BuildEnvironmentVariables(request);
        var timeout = TimeSpan.FromSeconds(Math.Clamp(timeoutSeconds, 30, 7200));

        try
        {
            var (outcome, session) = await _isolatedRunner.RunAsync(
                workspace,
                command,
                env,
                timeout,
                state.ExecCts.Token).ConfigureAwait(false);

            state.IsolatedSession = session;

            lock (state.OutputLines)
                state.OutputLines.AddRange(outcome.StdoutLines);

            state.Stderr = outcome.Stderr;
            var succeeded = outcome.ExitCode == 0;
            state.Status = succeeded ? AgentBackendRunStatus.Completed : AgentBackendRunStatus.Failed;
            state.Stage = succeeded ? "completed" : "failed";
            state.Error = succeeded ? null : TrimForSummary(outcome.Stderr);

            var summary = state.OutputLines.LastOrDefault(l => !string.IsNullOrWhiteSpace(l))
                          ?? state.Stderr
                          ?? state.Stage;

            state.Result = new AgentSessionResult(
                succeeded,
                TrimForSummary(summary),
                Array.Empty<GeneratedFile>(),
                0,
                state.OutputLines.ToArray());
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            state.Status = AgentBackendRunStatus.Failed;
            state.Stage = "timed_out";
            state.Error = $"cli_backend_timeout:{timeoutSeconds}s";
            state.Result = new AgentSessionResult(false, state.Error, Array.Empty<GeneratedFile>(), 0, state.OutputLines.ToArray());
        }
        catch (Exception ex)
        {
            state.Status = AgentBackendRunStatus.Failed;
            state.Error = ex.Message;
            state.Result = new AgentSessionResult(false, ex.Message, Array.Empty<GeneratedFile>(), 0, state.OutputLines.ToArray());
            _logger.LogError(ex, "Isolated CLI backend {Kind} failed for instance {InstanceId}", Kind, instanceId);
        }
        finally
        {
            if (state.IsolatedSession is not null)
            {
                await state.IsolatedSession.DisposeAsync().ConfigureAwait(false);
                state.IsolatedSession = null;
            }

            state.ExecCts?.Dispose();
            state.ExecCts = null;
        }
    }

    private static Dictionary<string, string>? BuildEnvironmentVariables(AgentBackendSpawnRequest request)
    {
        var env = request.Backend.Config
            .Where(kv => kv.Key.StartsWith("env.", StringComparison.OrdinalIgnoreCase))
            .ToDictionary(kv => kv.Key["env.".Length..], kv => kv.Value, StringComparer.OrdinalIgnoreCase);

        return env.Count == 0 ? null : env;
    }

    private static async Task PumpLinesAsync(TextReader reader, List<string> lines, CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            var line = await reader.ReadLineAsync(ct).ConfigureAwait(false);
            if (line is null)
                break;
            lock (lines)
                lines.Add(line);
        }
    }

    private static void RefreshStatus(CliBackendState state)
    {
        if (state.RunTask is { IsCompleted: true, IsFaulted: false, IsCanceled: false })
            return;

        if (state.RunTask is { IsFaulted: true })
        {
            state.Status = AgentBackendRunStatus.Failed;
            state.Error = state.RunTask.Exception?.GetBaseException().Message;
        }
        else if (state.RunTask is { IsCanceled: true })
        {
            state.Status = AgentBackendRunStatus.Cancelled;
        }
    }

    private static AgentBackendEvent ParseOutputLine(Guid runId, string instanceId, string line)
    {
        if (line.TrimStart().StartsWith('{'))
        {
            return new AgentBackendEvent(
                AgentBackendEventKind.Message,
                runId,
                instanceId,
                DateTime.UtcNow,
                line);
        }

        return AgentBackendEventMapper.CreateMessageEvent(runId, instanceId, "assistant", line);
    }

    private static string TrimForSummary(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return string.Empty;

        var trimmed = text.Trim();
        return trimmed.Length <= 500 ? trimmed : trimmed[..500];
    }

    protected sealed class CliBackendState
    {
        public CliBackendState(AgentBackendHandle handle, string workspace, string prompt)
        {
            Handle = handle;
            Workspace = workspace;
            Prompt = prompt;
        }

        public AgentBackendHandle Handle { get; }
        public string Workspace { get; }
        public string Prompt { get; }
        public Process? Process { get; set; }
        public IRuntimeSession? IsolatedSession { get; set; }
        public CancellationTokenSource? ExecCts { get; set; }
        public Task? RunTask { get; set; }
        public AgentBackendRunStatus Status { get; set; } = AgentBackendRunStatus.Queued;
        public string? Stage { get; set; }
        public string? Error { get; set; }
        public string? Stderr { get; set; }
        public AgentSessionResult? Result { get; set; }
        public List<string> OutputLines { get; } = new();
        public ConcurrentQueue<AgentBackendEvent> Events { get; } = new();
    }
}

public sealed class CodexCliAgentBackend : SubprocessCliAgentBackend
{
    private readonly ExternalAgentBackendOptions _options;

    public CodexCliAgentBackend(
        IOptions<ExternalAgentBackendOptions> options,
        ILogger<CodexCliAgentBackend> logger,
        IsolatedExternalBackendRunner? isolatedRunner = null)
        : base(options.Value, logger, isolatedRunner) => _options = options.Value;

    public override AgentBackendKind Kind => AgentBackendKind.CodexCli;

    protected override string ResolveExecutable() =>
        _options.CodexExecutable;

    protected override IReadOnlyList<string> BuildArguments(AgentBackendSpawnRequest request, string prompt)
    {
        if (request.Backend.Config.TryGetValue("args", out var custom) && !string.IsNullOrWhiteSpace(custom))
            return custom.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Concat([prompt])
                .ToArray();

        return ["exec", "--json", prompt];
    }
}

public sealed class OpenCodeCliAgentBackend : SubprocessCliAgentBackend
{
    private readonly ExternalAgentBackendOptions _options;

    public OpenCodeCliAgentBackend(
        IOptions<ExternalAgentBackendOptions> options,
        ILogger<OpenCodeCliAgentBackend> logger,
        IsolatedExternalBackendRunner? isolatedRunner = null)
        : base(options.Value, logger, isolatedRunner) => _options = options.Value;

    public override AgentBackendKind Kind => AgentBackendKind.OpenCodeCli;

    protected override string ResolveExecutable() =>
        _options.OpenCodeExecutable;

    protected override IReadOnlyList<string> BuildArguments(AgentBackendSpawnRequest request, string prompt)
    {
        if (request.Backend.Config.TryGetValue("args", out var custom) && !string.IsNullOrWhiteSpace(custom))
            return custom.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Concat([prompt])
                .ToArray();

        return ["run", prompt];
    }
}

public sealed class CursorSdkAgentBackend : SubprocessCliAgentBackend
{
    private readonly ExternalAgentBackendOptions _options;

    public CursorSdkAgentBackend(
        IOptions<ExternalAgentBackendOptions> options,
        ILogger<CursorSdkAgentBackend> logger,
        IsolatedExternalBackendRunner? isolatedRunner = null)
        : base(options.Value, logger, isolatedRunner) => _options = options.Value;

    public override AgentBackendKind Kind => AgentBackendKind.CursorSdk;

    protected override string ResolveExecutable() =>
        _options.NodeExecutable;

    protected override IReadOnlyList<string> BuildArguments(AgentBackendSpawnRequest request, string prompt)
    {
        var script = ResolveRunnerScript();
        var model = request.Backend.Config.GetValueOrDefault("model") ?? "composer-2.5";
        var args = new List<string> { script, "--prompt", prompt, "--model", model };

        if (request.Backend.Config.TryGetValue("apiKeyEnv", out var apiKeyEnv) && !string.IsNullOrWhiteSpace(apiKeyEnv))
            args.AddRange(["--api-key-env", apiKeyEnv]);

        return args;
    }

    private string ResolveRunnerScript()
    {
        var configured = _options.CursorSdkRunnerScript;
        if (Path.IsPathRooted(configured) && File.Exists(configured))
            return configured;

        var candidates = new[]
        {
            Path.GetFullPath(configured),
            Path.Combine(AppContext.BaseDirectory, configured),
            Path.Combine(Directory.GetCurrentDirectory(), configured)
        };

        foreach (var candidate in candidates)
        {
            if (File.Exists(candidate))
                return candidate;
        }

        throw new FileNotFoundException($"cursor_sdk_runner_not_found:{configured}");
    }
}
