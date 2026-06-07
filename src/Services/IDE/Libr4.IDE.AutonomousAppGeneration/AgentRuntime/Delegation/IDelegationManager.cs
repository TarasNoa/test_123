using System.Collections.Concurrent;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Delegation;

public interface IDelegationManager
{
    bool IsBackgroundChild();
    Task<DelegationRecord> StartExploreAsync(
        Guid runId,
        string task,
        Func<CancellationToken, Task<string>> worker,
        DelegationFleetPriority priority = DelegationFleetPriority.UserInitiated,
        string? tenantUserId = null,
        CancellationToken ct = default);
    Task<IReadOnlyList<DelegationRecord>> ListAsync(Guid runId, CancellationToken ct = default);
    Task<DelegationRecord?> GetAsync(Guid runId, string delegationId, CancellationToken ct = default);
    Task<DelegationNotification?> TryDequeueNotificationAsync(Guid runId, CancellationToken ct = default);
    Task<string?> ReadOutputAsync(Guid runId, string delegationId, CancellationToken ct = default);
}

public sealed class FileDelegationManager : IDelegationManager
{
    private readonly AgentRuntime.AgentRuntimeOptions _options;
    private readonly DelegationRuntimeOptions _delegationOptions;
    private readonly IDelegationWorkerHost _workerHost;
    private readonly IBackgroundFleetScheduler? _fleetScheduler;
    private readonly ILogger<FileDelegationManager> _logger;
    private readonly object _lock = new();
    private readonly Dictionary<Guid, Queue<DelegationNotification>> _notifications = new();
    private readonly ConcurrentDictionary<Guid, SemaphoreSlim> _runSemaphores = new();

    public FileDelegationManager(
        IOptions<AgentRuntime.AgentRuntimeOptions> options,
        IOptions<DelegationRuntimeOptions> delegationOptions,
        IDelegationWorkerHost workerHost,
        ILogger<FileDelegationManager> logger,
        IBackgroundFleetScheduler? fleetScheduler = null)
    {
        _options = options.Value;
        _delegationOptions = delegationOptions.Value;
        _workerHost = workerHost;
        _logger = logger;
        _fleetScheduler = fleetScheduler;
    }

    public bool IsBackgroundChild() => DelegationBackgroundContext.IsBackgroundChild;

    public async Task<DelegationRecord> StartExploreAsync(
        Guid runId,
        string task,
        Func<CancellationToken, Task<string>> worker,
        DelegationFleetPriority priority = DelegationFleetPriority.UserInitiated,
        string? tenantUserId = null,
        CancellationToken ct = default)
    {
        if (IsBackgroundChild())
            throw new InvalidOperationException("nested background delegation denied");

        var id = HumanReadableIdGenerator.Create();
        var now = DateTime.UtcNow;
        var record = new DelegationRecord(id, runId, task, DelegationStatuses.Queued, now, now, null, null, false);
        await WriteRecordAsync(runId, record, ct).ConfigureAwait(false);
        WriteRollout(runId, id, "queued", task);

        if (_fleetScheduler is not null && _delegationOptions.EnableFleetScheduler)
        {
            var request = new BackgroundDelegationRequest(runId, id, task, priority, tenantUserId);
            await _fleetScheduler.ScheduleAsync(
                request,
                workerCt => RunWorkerAsync(runId, id, task, worker, workerCt),
                ct).ConfigureAwait(false);
        }
        else
        {
            _ = RunWorkerAsync(runId, id, task, worker, CancellationToken.None);
        }

        return record;
    }

    public async Task<IReadOnlyList<DelegationRecord>> ListAsync(Guid runId, CancellationToken ct = default)
    {
        var root = DelegationRoot(runId);
        string[] files;
        lock (_lock)
        {
            if (!Directory.Exists(root))
                return Array.Empty<DelegationRecord>();

            files = Directory.EnumerateFiles(root, "*.json")
                .Where(f => !f.EndsWith(".worker.json", StringComparison.OrdinalIgnoreCase))
                .ToArray();
        }

        var records = new List<DelegationRecord>();
        foreach (var file in files)
        {
            var json = ReadRecordJson(file);
            var record = JsonSerializer.Deserialize<DelegationRecord>(json);
            if (record is not null)
                records.Add(record);
        }

        records.Sort((a, b) => b.CreatedAtUtc.CompareTo(a.CreatedAtUtc));
        return records;
    }

    public Task<DelegationRecord?> GetAsync(Guid runId, string delegationId, CancellationToken ct = default)
    {
        var path = RecordPath(runId, delegationId);
        lock (_lock)
        {
            if (!File.Exists(path))
                return Task.FromResult<DelegationRecord?>(null);

            var json = ReadRecordJson(path);
            return Task.FromResult(JsonSerializer.Deserialize<DelegationRecord>(json));
        }
    }

    public Task<DelegationNotification?> TryDequeueNotificationAsync(Guid runId, CancellationToken ct = default)
    {
        lock (_lock)
        {
            if (_notifications.TryGetValue(runId, out var queue) && queue.Count > 0)
                return Task.FromResult<DelegationNotification?>(queue.Dequeue());
        }

        return Task.FromResult<DelegationNotification?>(null);
    }

    public async Task<string?> ReadOutputAsync(Guid runId, string delegationId, CancellationToken ct = default)
    {
        var path = OutputPath(runId, delegationId);
        if (!File.Exists(path))
            return null;
        return await File.ReadAllTextAsync(path, ct).ConfigureAwait(false);
    }

    private async Task RunWorkerAsync(
        Guid runId,
        string id,
        string task,
        Func<CancellationToken, Task<string>> worker,
        CancellationToken ct)
    {
        var semaphore = _runSemaphores.GetOrAdd(
            runId,
            _ => new SemaphoreSlim(Math.Max(1, _delegationOptions.MaxConcurrentDelegationsPerRun)));

        await semaphore.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            var running = await GetAsync(runId, id).ConfigureAwait(false);
            if (running is null)
                return;

            await WriteRecordAsync(
                runId,
                running with { Status = DelegationStatuses.Running, UpdatedAtUtc = DateTime.UtcNow },
                ct).ConfigureAwait(false);
            WriteRollout(runId, id, "running", task);

            var request = new DelegationWorkerRequest(
                runId,
                id,
                task,
                _options.RunsRoot,
                OutputPath(runId, id),
                RecordPath(runId, id));

            var result = await _workerHost.ExecuteAsync(request, worker, ct).ConfigureAwait(false);

            if (result.Succeeded)
            {
                await File.WriteAllTextAsync(request.OutputPath, result.Output, ct).ConfigureAwait(false);
                var completed = running with
                {
                    Status = DelegationStatuses.Completed,
                    UpdatedAtUtc = DateTime.UtcNow,
                    OutputPreview = Preview(result.Output),
                    NotificationPending = true
                };
                await WriteRecordAsync(runId, completed, ct).ConfigureAwait(false);
                WriteRollout(runId, id, "completed", Preview(result.Output) ?? "completed");
                EnqueueNotification(runId, new DelegationNotification
                {
                    DelegationId = id,
                    Summary = Preview(result.Output) ?? "completed",
                    CompletedAtUtc = DateTime.UtcNow,
                    OutputRelativePath = RelativeOutputPath(runId, id)
                });
                return;
            }

            var status = result.TimedOut ? DelegationStatuses.TimedOut : DelegationStatuses.Failed;
            var failed = running with
            {
                Status = status,
                UpdatedAtUtc = DateTime.UtcNow,
                Error = result.Error,
                OutputPreview = Preview(result.Error)
            };
            await WriteRecordAsync(runId, failed, ct).ConfigureAwait(false);
            WriteRollout(runId, id, status, result.Error ?? status);

            if (result.TimedOut)
            {
                EnqueueNotification(runId, new DelegationNotification
                {
                    DelegationId = id,
                    Summary = $"timed out: {result.Error}",
                    CompletedAtUtc = DateTime.UtcNow,
                    OutputRelativePath = RelativeOutputPath(runId, id)
                });
            }
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            _logger.LogInformation("Delegation worker preempted for {DelegationId}", id);
            var preempted = new DelegationRecord(
                id,
                runId,
                task,
                DelegationStatuses.Failed,
                DateTime.UtcNow,
                DateTime.UtcNow,
                null,
                "preempted_for_implementer_budget",
                false);
            await WriteRecordAsync(runId, preempted, CancellationToken.None).ConfigureAwait(false);
            WriteRollout(runId, id, DelegationStatuses.Failed, "preempted_for_implementer_budget");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Delegation worker crashed for {DelegationId}", id);
            var failed = new DelegationRecord(
                id, runId, task, DelegationStatuses.Failed, DateTime.UtcNow, DateTime.UtcNow, null, ex.Message, false);
            await WriteRecordAsync(runId, failed, CancellationToken.None).ConfigureAwait(false);
            WriteRollout(runId, id, DelegationStatuses.Failed, ex.Message);
        }
        finally
        {
            semaphore.Release();
        }
    }

    private void EnqueueNotification(Guid runId, DelegationNotification notification)
    {
        lock (_lock)
        {
            if (!_notifications.TryGetValue(runId, out var queue))
            {
                queue = new Queue<DelegationNotification>();
                _notifications[runId] = queue;
            }

            queue.Enqueue(notification);
        }
    }

    private async Task WriteRecordAsync(Guid runId, DelegationRecord record, CancellationToken ct)
    {
        var dir = DelegationRoot(runId);
        var path = RecordPath(runId, record.Id);
        var json = JsonSerializer.Serialize(record);
        lock (_lock)
        {
            Directory.CreateDirectory(dir);
            WriteRecordJson(path, json);
        }

        await Task.CompletedTask;
    }

    private static string ReadRecordJson(string path)
    {
        IOException? last = null;
        for (var attempt = 0; attempt < 3; attempt++)
        {
            try
            {
                return File.ReadAllText(path);
            }
            catch (IOException ex)
            {
                last = ex;
                Thread.Sleep(10 * (attempt + 1));
            }
        }

        throw last ?? new IOException($"failed to read delegation record: {path}");
    }

    private static void WriteRecordJson(string path, string json)
    {
        IOException? last = null;
        for (var attempt = 0; attempt < 3; attempt++)
        {
            try
            {
                using var stream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None);
                using var writer = new StreamWriter(stream);
                writer.Write(json);
                return;
            }
            catch (IOException ex)
            {
                last = ex;
                Thread.Sleep(10 * (attempt + 1));
            }
        }

        throw last ?? new IOException($"failed to write delegation record: {path}");
    }

    private void WriteRollout(Guid runId, string delegationId, string stage, string? message)
    {
        try
        {
            var dir = Path.Combine(_options.RunsRoot, runId.ToString("D"), "delegations", delegationId);
            Directory.CreateDirectory(dir);
            var line = JsonSerializer.Serialize(new
            {
                type = "status",
                stage,
                message,
                timestampUtc = DateTime.UtcNow
            });
            File.AppendAllText(Path.Combine(dir, "rollout.jsonl"), line + Environment.NewLine);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to write delegation rollout for {DelegationId}", delegationId);
        }
    }

    private string DelegationRoot(Guid runId) =>
        Path.Combine(_options.RunsRoot, runId.ToString("D"), "delegations");

    private string RecordPath(Guid runId, string delegationId) =>
        Path.Combine(DelegationRoot(runId), $"{delegationId}.json");

    private string OutputPath(Guid runId, string delegationId) =>
        Path.Combine(DelegationRoot(runId), $"{delegationId}.md");

    private static string RelativeOutputPath(Guid runId, string delegationId) =>
        $"delegations/{delegationId}.md";

    private static string? Preview(string? text) =>
        string.IsNullOrWhiteSpace(text) ? null : text.Length <= 240 ? text : text[..240] + "...";
}
