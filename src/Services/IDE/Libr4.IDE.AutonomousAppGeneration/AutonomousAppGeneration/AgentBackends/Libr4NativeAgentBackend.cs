using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Text.Json;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Abstractions;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Models;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Rollout;
using Libr4.IDE.Application.AutonomousAppGeneration.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Libr4.IDE.Application.AutonomousAppGeneration.AgentBackends;

public sealed class Libr4NativeAgentBackend : IAgentBackend
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IRolloutRecorder _rollout;
    private readonly IAutonomousRunControlService? _runControl;
    private readonly ILogger<Libr4NativeAgentBackend> _logger;
    private readonly ConcurrentDictionary<string, NativeBackendState> _active = new();

    public Libr4NativeAgentBackend(
        IServiceScopeFactory scopeFactory,
        IRolloutRecorder rollout,
        ILogger<Libr4NativeAgentBackend> logger,
        IAutonomousRunControlService? runControl = null)
    {
        _scopeFactory = scopeFactory;
        _rollout = rollout;
        _logger = logger;
        _runControl = runControl;
    }

    public AgentBackendKind Kind => AgentBackendKind.Libr4Native;

    public Task<AgentBackendHandle> SpawnAsync(AgentBackendSpawnRequest request, CancellationToken ct = default)
    {
        var instanceId = Guid.NewGuid().ToString("N");
        var handle = new AgentBackendHandle(instanceId, request.RunId, Kind, DateTime.UtcNow);
        var state = new NativeBackendState(handle, request.Role, request.InitialMessage);
        _active[instanceId] = state;

        if (request.SessionRequest is not null)
        {
            state.Status = AgentBackendRunStatus.Running;
            state.RunTask = RunSessionAsync(instanceId, request.SessionRequest, ct);
        }

        _logger.LogInformation(
            "Spawned Libr4Native backend {InstanceId} for run {RunId} role={Role}",
            instanceId,
            request.RunId,
            request.Role);

        return Task.FromResult(handle);
    }

    public Task SendMessageAsync(string backendInstanceId, string message, CancellationToken ct = default)
    {
        if (!_active.TryGetValue(backendInstanceId, out var state))
            throw new KeyNotFoundException($"backend_not_found:{backendInstanceId}");

        state.PendingMessages.Enqueue(message);
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

        yield return AgentBackendEventMapper.CreateStatusEvent(
            state.Handle.RunId,
            backendInstanceId,
            "spawned");

        var lastRolloutCount = 0;
        while (!ct.IsCancellationRequested)
        {
            while (state.Events.TryDequeue(out var queued))
                yield return queued;

            var rollout = await _rollout.GetRolloutAsync(state.Handle.RunId, ct).ConfigureAwait(false);
            if (rollout.Count > lastRolloutCount)
            {
                foreach (var entry in rollout.Skip(lastRolloutCount))
                {
                    yield return MapRolloutEntry(state.Handle.RunId, backendInstanceId, entry);
                }

                lastRolloutCount = rollout.Count;
            }

            if (state.RunTask is { IsCompleted: true })
            {
                var summary = state.Result?.Summary ?? "completed";
                yield return AgentBackendEventMapper.CreateMessageEvent(
                    state.Handle.RunId,
                    backendInstanceId,
                    "assistant",
                    summary);
                break;
            }

            await Task.Delay(250, ct).ConfigureAwait(false);
        }
    }

    public Task CancelAsync(string backendInstanceId, CancellationToken ct = default)
    {
        if (!_active.TryGetValue(backendInstanceId, out var state))
            return Task.CompletedTask;

        state.Cts.Cancel();
        state.Status = AgentBackendRunStatus.Cancelled;
        _runControl?.CancelRun(state.Handle.RunId, "agent-backend", "backend_cancel");
        return Task.CompletedTask;
    }

    public Task<AgentBackendStatus> GetStatusAsync(string backendInstanceId, CancellationToken ct = default)
    {
        if (!_active.TryGetValue(backendInstanceId, out var state))
            throw new KeyNotFoundException($"backend_not_found:{backendInstanceId}");

        if (state.RunTask is { IsCompleted: true, IsFaulted: false, IsCanceled: false })
        {
            state.Status = state.Result?.Succeeded == true
                ? AgentBackendRunStatus.Completed
                : AgentBackendRunStatus.Failed;
        }
        else if (state.RunTask is { IsFaulted: true })
        {
            state.Status = AgentBackendRunStatus.Failed;
            state.Error = state.RunTask.Exception?.GetBaseException().Message;
        }
        else if (state.RunTask is { IsCanceled: true })
        {
            state.Status = AgentBackendRunStatus.Cancelled;
        }

        return Task.FromResult(new AgentBackendStatus(
            backendInstanceId,
            state.Status,
            state.Stage,
            state.StepNumber,
            state.CostUsd,
            state.Error));
    }

    public async Task<AgentSessionResult> WaitForCompletionAsync(string backendInstanceId, CancellationToken ct)
    {
        if (!_active.TryGetValue(backendInstanceId, out var state))
            throw new KeyNotFoundException($"backend_not_found:{backendInstanceId}");

        if (state.RunTask is not null)
            await state.RunTask.ConfigureAwait(false);

        if (state.Result is not null)
            return state.Result;

        return new AgentSessionResult(
            state.Status == AgentBackendRunStatus.Completed,
            state.Error ?? state.Stage ?? state.Status.ToString(),
            Array.Empty<Libr4.IDE.Domain.AutonomousAppGeneration.GeneratedFile>(),
            state.StepNumber ?? 0,
            Array.Empty<string>());
    }

    private async Task RunSessionAsync(
        string instanceId,
        AgentSessionRunRequest request,
        CancellationToken ct)
    {
        if (!_active.TryGetValue(instanceId, out var state))
            return;

        try
        {
            using var scope = _scopeFactory.CreateScope();
            var session = scope.ServiceProvider.GetRequiredService<IAgentSession>();
            state.Status = AgentBackendRunStatus.Running;
            state.Stage = "running";
            state.Result = await session.RunAsync(request, ct).ConfigureAwait(false);
            state.Status = state.Result.Succeeded
                ? AgentBackendRunStatus.Completed
                : AgentBackendRunStatus.Failed;
            state.Stage = state.Status.ToString().ToLowerInvariant();
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            state.Status = AgentBackendRunStatus.Failed;
            state.Error = ex.Message;
            state.Events.Enqueue(new AgentBackendEvent(
                AgentBackendEventKind.Error,
                state.Handle.RunId,
                instanceId,
                DateTime.UtcNow,
                JsonSerializer.Serialize(new { error = ex.Message })));
            _logger.LogError(ex, "Libr4Native backend {InstanceId} failed", instanceId);
        }
    }

    private static AgentBackendEvent MapRolloutEntry(Guid runId, string instanceId, RolloutEntry entry)
    {
        var kind = entry.Type.Contains("tool", StringComparison.OrdinalIgnoreCase)
            ? AgentBackendEventKind.ToolUse
            : AgentBackendEventKind.Message;

        return new AgentBackendEvent(
            kind,
            runId,
            instanceId,
            entry.TimestampUtc,
            JsonSerializer.Serialize(new
            {
                entry.Type,
                entry.StepNumber,
                entry.PayloadJson
            }));
    }

    private sealed class NativeBackendState
    {
        public NativeBackendState(AgentBackendHandle handle, string role, string? initialMessage)
        {
            Handle = handle;
            Role = role;
            if (!string.IsNullOrWhiteSpace(initialMessage))
                PendingMessages.Enqueue(initialMessage);
        }

        public AgentBackendHandle Handle { get; }
        public string Role { get; }
        public AgentBackendRunStatus Status { get; set; } = AgentBackendRunStatus.Queued;
        public string? Stage { get; set; }
        public int? StepNumber { get; set; }
        public decimal? CostUsd { get; set; }
        public string? Error { get; set; }
        public Task? RunTask { get; set; }
        public AgentSessionResult? Result { get; set; }
        public ConcurrentQueue<string> PendingMessages { get; } = new();
        public ConcurrentQueue<AgentBackendEvent> Events { get; } = new();
        public CancellationTokenSource Cts { get; } = new();
    }
}
