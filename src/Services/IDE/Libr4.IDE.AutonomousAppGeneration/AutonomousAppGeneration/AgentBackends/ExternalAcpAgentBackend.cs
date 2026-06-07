using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Models;
using Libr4.IDE.Domain.AutonomousAppGeneration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Libr4.IDE.Application.AutonomousAppGeneration.AgentBackends;

/// <summary>
/// Optional JSON-RPC 2.0 over stdio adapter for external ACP-compatible agent processes.
/// Method names are configurable until the upstream ACP spec stabilizes (Phase 5.15 overlap).
/// </summary>
public sealed class ExternalAcpAgentBackend : IAgentBackend
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly ExternalAgentBackendOptions _options;
    private readonly IsolatedExternalBackendRunner? _isolatedRunner;
    private readonly ILogger<ExternalAcpAgentBackend> _logger;
    private readonly ConcurrentDictionary<string, AcpBackendState> _active = new();

    public ExternalAcpAgentBackend(
        IOptions<ExternalAgentBackendOptions> options,
        ILogger<ExternalAcpAgentBackend> logger,
        IsolatedExternalBackendRunner? isolatedRunner = null)
    {
        _options = options.Value;
        _logger = logger;
        _isolatedRunner = isolatedRunner;
    }

    public AgentBackendKind Kind => AgentBackendKind.ExternalAcp;

    public Task<AgentBackendHandle> SpawnAsync(AgentBackendSpawnRequest request, CancellationToken ct = default)
    {
        var prompt = request.InitialMessage
                     ?? request.SessionRequest?.Objective
                     ?? throw new InvalidOperationException("acp_backend_requires_prompt");

        var workspace = request.SessionRequest?.Workspace.HostPath ?? Directory.GetCurrentDirectory();
        var instanceId = Guid.NewGuid().ToString("N");
        var handle = new AgentBackendHandle(instanceId, request.RunId, Kind, DateTime.UtcNow);
        var state = new AcpBackendState(handle, workspace, prompt, request);
        _active[instanceId] = state;
        state.RunTask = RunAcpSessionAsync(instanceId, ct);
        return Task.FromResult(handle);
    }

    public Task SendMessageAsync(string backendInstanceId, string message, CancellationToken ct = default)
    {
        if (!_active.TryGetValue(backendInstanceId, out var state))
            throw new KeyNotFoundException($"backend_not_found:{backendInstanceId}");

        if (state.Process is { HasExited: false } && state.Process.StandardInput.BaseStream.CanWrite)
        {
            var request = BuildJsonRpcRequest(
                state.NextId(),
                ResolvePromptMethod(state.Request),
                new { message, workspace = state.Workspace });
            WriteLine(state.Process.StandardInput, request);
        }

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
                foreach (var evt in ParseJsonRpcLine(state, line))
                    yield return evt;
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
            state.Cts.Cancel();
            if (state.Process is { HasExited: false })
                state.Process.Kill(entireProcessTree: true);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to cancel ACP backend {InstanceId}", backendInstanceId);
        }

        state.Status = AgentBackendRunStatus.Cancelled;
        return Task.CompletedTask;
    }

    public Task<AgentBackendStatus> GetStatusAsync(string backendInstanceId, CancellationToken ct = default)
    {
        if (!_active.TryGetValue(backendInstanceId, out var state))
            throw new KeyNotFoundException($"backend_not_found:{backendInstanceId}");

        if (state.RunTask is { IsCompleted: true, IsFaulted: false, IsCanceled: false } && state.Result is not null)
        {
            state.Status = state.Result.Succeeded
                ? AgentBackendRunStatus.Completed
                : AgentBackendRunStatus.Failed;
        }

        RefreshStatus(state);
        return Task.FromResult(new AgentBackendStatus(
            backendInstanceId,
            state.Status,
            state.Stage,
            null,
            null,
            state.Error));
    }

    private async Task RunAcpSessionAsync(string instanceId, CancellationToken ct)
    {
        if (!_active.TryGetValue(instanceId, out var state))
            return;

        var timeoutSeconds = int.TryParse(
            state.Request.Backend.Config.GetValueOrDefault("timeoutSeconds"),
            out var configured)
            ? configured
            : _options.DefaultTimeoutSeconds;

        state.Cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        state.Cts.CancelAfter(TimeSpan.FromSeconds(Math.Clamp(timeoutSeconds, 30, 7200)));

        try
        {
            if (ShouldIsolate(state.Request))
            {
                await RunIsolatedAsync(state, timeoutSeconds, state.Cts.Token).ConfigureAwait(false);
                return;
            }

            var executable = ResolveExecutable(state.Request);
            var args = BuildProcessArguments(state.Request);
            var psi = new ProcessStartInfo
            {
                FileName = executable,
                WorkingDirectory = state.Workspace,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            foreach (var arg in args)
                psi.ArgumentList.Add(arg);

            ApplyEnv(psi, state.Request);
            state.Process = Process.Start(psi)
                            ?? throw new InvalidOperationException($"failed_to_start:{executable}");
            state.Status = AgentBackendRunStatus.Running;
            state.Stage = "running";

            var stdoutTask = PumpLinesAsync(state.Process.StandardOutput, state.OutputLines, state.Cts.Token);
            var stderrTask = state.Process.StandardError.ReadToEndAsync(state.Cts.Token);

            WriteLine(state.Process.StandardInput, BuildJsonRpcRequest(
                state.NextId(),
                ResolveInitializeMethod(state.Request),
                new { workspace = state.Workspace, client = "libr4" }));

            WriteLine(state.Process.StandardInput, BuildJsonRpcRequest(
                state.NextId(),
                ResolvePromptMethod(state.Request),
                new { prompt = state.Prompt, workspace = state.Workspace }));

            state.Process.StandardInput.Close();

            await state.Process.WaitForExitAsync(state.Cts.Token).ConfigureAwait(false);
            await stdoutTask.ConfigureAwait(false);
            state.Stderr = await stderrTask.ConfigureAwait(false);

            FinalizeResult(state, state.Process.ExitCode);
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            state.Status = AgentBackendRunStatus.Failed;
            state.Stage = "timed_out";
            state.Error = $"acp_backend_timeout:{timeoutSeconds}s";
            state.Result = new AgentSessionResult(false, state.Error, Array.Empty<GeneratedFile>(), 0, state.OutputLines.ToArray());
        }
        catch (Exception ex)
        {
            state.Status = AgentBackendRunStatus.Failed;
            state.Error = ex.Message;
            state.Result = new AgentSessionResult(false, ex.Message, Array.Empty<GeneratedFile>(), 0, state.OutputLines.ToArray());
            _logger.LogError(ex, "ACP backend failed for instance {InstanceId}", instanceId);
        }
    }

    private async Task RunIsolatedAsync(AcpBackendState state, int timeoutSeconds, CancellationToken ct)
    {
        if (_isolatedRunner is null)
            throw new InvalidOperationException("acp_isolated_runner_missing");

        var executable = ResolveExecutable(state.Request);
        var args = BuildProcessArguments(state.Request);
        var init = BuildJsonRpcRequest(state.NextId(), ResolveInitializeMethod(state.Request), new { workspace = "/workspace", client = "libr4" });
        var prompt = BuildJsonRpcRequest(state.NextId(), ResolvePromptMethod(state.Request), new { prompt = state.Prompt, workspace = "/workspace" });
        var command = $"{IsolatedExternalBackendRunner.BuildShellCommand(executable, args)} <<'EOF'\n{init}\n{prompt}\nEOF";

        var (outcome, session) = await _isolatedRunner.RunAsync(
            state.Workspace,
            command,
            BuildEnvironmentVariables(state.Request),
            TimeSpan.FromSeconds(timeoutSeconds),
            ct).ConfigureAwait(false);

        await using (session)
        {
            lock (state.OutputLines)
                state.OutputLines.AddRange(outcome.StdoutLines);
            state.Stderr = outcome.Stderr;
            FinalizeResult(state, outcome.ExitCode);
        }
    }

    private void FinalizeResult(AcpBackendState state, int exitCode)
    {
        var succeeded = exitCode == 0 || state.OutputLines.Any(l => l.Contains("\"result\"", StringComparison.Ordinal));
        state.Status = succeeded ? AgentBackendRunStatus.Completed : AgentBackendRunStatus.Failed;
        state.Stage = succeeded ? "completed" : "failed";
        state.Error = succeeded ? null : Trim(state.Stderr ?? "acp_backend_failed");

        var summary = state.OutputLines.LastOrDefault(l => !string.IsNullOrWhiteSpace(l))
                      ?? state.Stderr
                      ?? state.Stage;

        state.Result = new AgentSessionResult(
            succeeded,
            Trim(summary),
            Array.Empty<GeneratedFile>(),
            0,
            state.OutputLines.ToArray());
    }

    private static IEnumerable<AgentBackendEvent> ParseJsonRpcLine(AcpBackendState state, string line)
    {
        if (!line.TrimStart().StartsWith('{'))
        {
            yield return AgentBackendEventMapper.CreateMessageEvent(
                state.Handle.RunId,
                state.Handle.BackendInstanceId,
                "assistant",
                line);
            yield break;
        }

        JsonDocument? doc = TryParseJson(line);
        if (doc is null)
        {
            yield return AgentBackendEventMapper.CreateMessageEvent(
                state.Handle.RunId,
                state.Handle.BackendInstanceId,
                "assistant",
                line);
            yield break;
        }

        using (doc)
        {
            var root = doc.RootElement;
            if (root.TryGetProperty("method", out var methodEl))
            {
                var method = methodEl.GetString() ?? "notification";
                if (root.TryGetProperty("params", out var paramsEl))
                {
                    yield return AgentBackendEventMapper.CreateMessageEvent(
                        state.Handle.RunId,
                        state.Handle.BackendInstanceId,
                        "assistant",
                        paramsEl.GetRawText());

                    if (method.Contains("cost", StringComparison.OrdinalIgnoreCase)
                        && paramsEl.TryGetProperty("costUsd", out var costEl)
                        && costEl.TryGetDecimal(out var cost))
                    {
                        yield return new AgentBackendEvent(
                            AgentBackendEventKind.Cost,
                            state.Handle.RunId,
                            state.Handle.BackendInstanceId,
                            DateTime.UtcNow,
                            JsonSerializer.Serialize(new { costUsd = cost }, JsonOptions));
                    }
                }

                yield break;
            }

            if (root.TryGetProperty("result", out var resultEl))
            {
                yield return AgentBackendEventMapper.CreateMessageEvent(
                    state.Handle.RunId,
                    state.Handle.BackendInstanceId,
                    "assistant",
                    resultEl.GetRawText());
            }
            else if (root.TryGetProperty("error", out var errorEl))
            {
                yield return new AgentBackendEvent(
                    AgentBackendEventKind.Error,
                    state.Handle.RunId,
                    state.Handle.BackendInstanceId,
                    DateTime.UtcNow,
                    errorEl.GetRawText());
            }
        }
    }

    private static JsonDocument? TryParseJson(string line)
    {
        try
        {
            return JsonDocument.Parse(line);
        }
        catch
        {
            return null;
        }
    }

    private static void WriteLine(TextWriter writer, string line)
    {
        writer.WriteLine(line);
        writer.Flush();
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

    private static void RefreshStatus(AcpBackendState state)
    {
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

    private string ResolveExecutable(AgentBackendSpawnRequest request) =>
        request.Backend.Config.GetValueOrDefault("executable")
        ?? _options.AcpExecutable;

    private static IReadOnlyList<string> BuildProcessArguments(AgentBackendSpawnRequest request)
    {
        if (request.Backend.Config.TryGetValue("args", out var custom) && !string.IsNullOrWhiteSpace(custom))
            return custom.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        return Array.Empty<string>();
    }

    private static string ResolveInitializeMethod(AgentBackendSpawnRequest request) =>
        request.Backend.Config.GetValueOrDefault("initializeMethod") ?? "initialize";

    private static string ResolvePromptMethod(AgentBackendSpawnRequest request) =>
        request.Backend.Config.GetValueOrDefault("promptMethod") ?? "session/prompt";

    private static string BuildJsonRpcRequest(int id, string method, object parameters) =>
        JsonSerializer.Serialize(new { jsonrpc = "2.0", id, method, @params = parameters }, JsonOptions);

    private bool ShouldIsolate(AgentBackendSpawnRequest request) =>
        _isolatedRunner is not null
        && (_options.IsolateExternalBackends
            || string.Equals(request.Backend.Config.GetValueOrDefault("isolate"), "true", StringComparison.OrdinalIgnoreCase));

    private static Dictionary<string, string>? BuildEnvironmentVariables(AgentBackendSpawnRequest request)
    {
        var env = request.Backend.Config
            .Where(kv => kv.Key.StartsWith("env.", StringComparison.OrdinalIgnoreCase))
            .ToDictionary(kv => kv.Key["env.".Length..], kv => kv.Value, StringComparer.OrdinalIgnoreCase);
        return env.Count == 0 ? null : env;
    }

    private static void ApplyEnv(ProcessStartInfo psi, AgentBackendSpawnRequest request)
    {
        var env = BuildEnvironmentVariables(request);
        if (env is null)
            return;

        foreach (var kv in env)
            psi.Environment[kv.Key] = kv.Value;
    }

    private static string Trim(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return string.Empty;
        var trimmed = text.Trim();
        return trimmed.Length <= 500 ? trimmed : trimmed[..500];
    }

    private sealed class AcpBackendState
    {
        private int _id;

        public AcpBackendState(AgentBackendHandle handle, string workspace, string prompt, AgentBackendSpawnRequest request)
        {
            Handle = handle;
            Workspace = workspace;
            Prompt = prompt;
            Request = request;
        }

        public AgentBackendHandle Handle { get; }
        public string Workspace { get; }
        public string Prompt { get; }
        public AgentBackendSpawnRequest Request { get; }
        public Process? Process { get; set; }
        public CancellationTokenSource Cts { get; set; } = new();
        public AgentBackendRunStatus Status { get; set; } = AgentBackendRunStatus.Queued;
        public string? Stage { get; set; }
        public string? Error { get; set; }
        public string? Stderr { get; set; }
        public AgentSessionResult? Result { get; set; }
        public Task? RunTask { get; set; }
        public List<string> OutputLines { get; } = new();
        public ConcurrentQueue<AgentBackendEvent> Events { get; } = new();

        public int NextId() => Interlocked.Increment(ref _id);
    }
}
