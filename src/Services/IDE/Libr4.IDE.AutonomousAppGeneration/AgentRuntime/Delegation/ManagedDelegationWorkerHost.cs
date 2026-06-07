using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Delegation;

/// <summary>
/// In-process worker host with timeout enforcement and DELEGATE_BACKGROUND_CHILD isolation per thread.
/// </summary>
public sealed class ManagedDelegationWorkerHost : IDelegationWorkerHost
{
    private readonly DelegationRuntimeOptions _options;
    private readonly ILogger<ManagedDelegationWorkerHost> _logger;

    public ManagedDelegationWorkerHost(
        IOptions<DelegationRuntimeOptions> options,
        ILogger<ManagedDelegationWorkerHost> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public async Task<DelegationWorkerResult> ExecuteAsync(
        DelegationWorkerRequest request,
        Func<CancellationToken, Task<string>> worker,
        CancellationToken ct = default)
    {
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeoutCts.CancelAfter(TimeSpan.FromMinutes(Math.Clamp(_options.DelegationTimeoutMinutes, 1, 120)));

        var attempts = Math.Max(0, _options.MaxRestartAttempts) + 1;
        Exception? lastError = null;
        var sw = Stopwatch.StartNew();

        for (var attempt = 1; attempt <= attempts; attempt++)
        {
            try
            {
                var output = await RunIsolatedAsync(worker, timeoutCts.Token).ConfigureAwait(false);
                sw.Stop();
                DelegationTelemetry.RecordDuration(request.RunId, sw.Elapsed.TotalSeconds, request.DelegationId);
                DelegationTelemetry.RecordCompletion(request.RunId, succeeded: true, timedOut: false);
                return new DelegationWorkerResult(true, output);
            }
            catch (OperationCanceledException) when (timeoutCts.IsCancellationRequested && !ct.IsCancellationRequested)
            {
                sw.Stop();
                DelegationTelemetry.RecordDuration(request.RunId, sw.Elapsed.TotalSeconds, request.DelegationId);
                DelegationTelemetry.RecordCompletion(request.RunId, succeeded: false, timedOut: true);
                return new DelegationWorkerResult(false, string.Empty, $"delegation_timeout:{_options.DelegationTimeoutMinutes}m", TimedOut: true);
            }
            catch (Exception ex)
            {
                lastError = ex;
                _logger.LogWarning(
                    ex,
                    "Delegation worker attempt {Attempt}/{Attempts} failed for {DelegationId}",
                    attempt,
                    attempts,
                    request.DelegationId);

                if (attempt >= attempts)
                    break;
            }
        }

        sw.Stop();
        DelegationTelemetry.RecordDuration(request.RunId, sw.Elapsed.TotalSeconds, request.DelegationId);
        DelegationTelemetry.RecordCompletion(request.RunId, succeeded: false, timedOut: false);
        return new DelegationWorkerResult(
            false,
            string.Empty,
            lastError?.Message ?? "delegation_worker_failed");
    }

    private static async Task<string> RunIsolatedAsync(
        Func<CancellationToken, Task<string>> worker,
        CancellationToken ct)
    {
        using var scope = DelegationBackgroundContext.EnterChildScope();
        return await worker(ct).ConfigureAwait(false);
    }
}
