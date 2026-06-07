using System.Diagnostics;
using System.Text.Json;
using Libr4.IDE.Application.AutonomousAppGeneration.Infrastructure.Rust;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Delegation;

/// <summary>
/// Out-of-process delegation worker via <c>dotnet libr4-run.dll delegation-run</c>.
/// Falls back to <see cref="ManagedDelegationWorkerHost"/> when CLI path is unavailable.
/// </summary>
public sealed class ProcessDelegationWorkerHost : IDelegationWorkerHost
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly DelegationRuntimeOptions _options;
    private readonly ManagedDelegationWorkerHost _fallback;
    private readonly ILogger<ProcessDelegationWorkerHost> _logger;

    public ProcessDelegationWorkerHost(
        IOptions<DelegationRuntimeOptions> options,
        ManagedDelegationWorkerHost fallback,
        ILogger<ProcessDelegationWorkerHost> logger)
    {
        _options = options.Value;
        _fallback = fallback;
        _logger = logger;
    }

    public async Task<DelegationWorkerResult> ExecuteAsync(
        DelegationWorkerRequest request,
        Func<CancellationToken, Task<string>> worker,
        CancellationToken ct = default)
    {
        if (!_options.UseOutOfProcessWorkers || string.IsNullOrWhiteSpace(_options.WorkerCliPath) || !File.Exists(_options.WorkerCliPath))
            return await _fallback.ExecuteAsync(request, worker, ct).ConfigureAwait(false);

        var jobPath = Path.Combine(Path.GetDirectoryName(request.RecordPath)!, $"{request.DelegationId}.worker.json");
        await File.WriteAllTextAsync(
            jobPath,
            JsonSerializer.Serialize(new
            {
                request.RunId,
                request.DelegationId,
                request.Task,
                request.RunsRoot,
                request.OutputPath
            }, JsonOptions),
            ct).ConfigureAwait(false);

        if (RustDelegationWorkerBridge.TryRunWorker(
                jobPath,
                _options.WorkerCliPath!,
                Directory.GetCurrentDirectory(),
                _options.DelegationTimeoutMinutes,
                _options.WorkerMemoryLimitMb,
                _options.MaxRestartAttempts,
                _logger,
                out var rustSucceeded,
                out var rustStdout,
                out var rustStderr,
                out var rustTimedOut)
            && (rustSucceeded || rustTimedOut || !string.IsNullOrEmpty(rustStderr)))
        {
            if (rustSucceeded && File.Exists(request.OutputPath))
            {
                var output = await File.ReadAllTextAsync(request.OutputPath, ct).ConfigureAwait(false);
                DelegationTelemetry.RecordCompletion(request.RunId, succeeded: true, timedOut: false);
                return new DelegationWorkerResult(true, output);
            }

            DelegationTelemetry.RecordCompletion(request.RunId, succeeded: false, timedOut: rustTimedOut);
            return new DelegationWorkerResult(
                false,
                rustStdout,
                rustStderr,
                TimedOut: rustTimedOut);
        }

        var timeout = TimeSpan.FromMinutes(Math.Clamp(_options.DelegationTimeoutMinutes, 1, 120));
        var attempts = Math.Max(0, _options.MaxRestartAttempts) + 1;

        for (var attempt = 1; attempt <= attempts; attempt++)
        {
            var psi = new ProcessStartInfo
            {
                FileName = "dotnet",
                WorkingDirectory = Directory.GetCurrentDirectory(),
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            psi.ArgumentList.Add(_options.WorkerCliPath);
            psi.ArgumentList.Add("delegation-run");
            psi.ArgumentList.Add("--request");
            psi.ArgumentList.Add(jobPath);
            psi.Environment["DELEGATE_BACKGROUND_CHILD"] = "1";

            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            timeoutCts.CancelAfter(timeout);

            try
            {
                var sw = Stopwatch.StartNew();
                using var process = Process.Start(psi) ?? throw new InvalidOperationException("failed_to_start_delegation_worker");
                ApplyMemoryLimit(process);
                var stdout = await process.StandardOutput.ReadToEndAsync(timeoutCts.Token).ConfigureAwait(false);
                var stderr = await process.StandardError.ReadToEndAsync(timeoutCts.Token).ConfigureAwait(false);
                await process.WaitForExitAsync(timeoutCts.Token).ConfigureAwait(false);
                sw.Stop();

                DelegationTelemetry.RecordDuration(request.RunId, sw.Elapsed.TotalSeconds, request.DelegationId);

                if (process.ExitCode == 0 && File.Exists(request.OutputPath))
                {
                    var output = await File.ReadAllTextAsync(request.OutputPath, ct).ConfigureAwait(false);
                    DelegationTelemetry.RecordCompletion(request.RunId, succeeded: true, timedOut: false);
                    return new DelegationWorkerResult(true, output);
                }

                _logger.LogWarning(
                    "Delegation worker process exit={ExitCode} attempt={Attempt} stderr={Stderr}",
                    process.ExitCode,
                    attempt,
                    stderr);

                if (attempt >= attempts)
                {
                    DelegationTelemetry.RecordCompletion(request.RunId, succeeded: false, timedOut: false);
                    return new DelegationWorkerResult(false, stdout, stderr.Trim().Length > 0 ? stderr : $"exit_{process.ExitCode}");
                }
            }
            catch (OperationCanceledException) when (timeoutCts.IsCancellationRequested && !ct.IsCancellationRequested)
            {
                DelegationTelemetry.RecordCompletion(request.RunId, succeeded: false, timedOut: true);
                return new DelegationWorkerResult(false, string.Empty, $"delegation_timeout:{_options.DelegationTimeoutMinutes}m", TimedOut: true);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Delegation worker process failed attempt={Attempt}", attempt);
                if (attempt >= attempts)
                {
                    DelegationTelemetry.RecordCompletion(request.RunId, succeeded: false, timedOut: false);
                    return new DelegationWorkerResult(false, string.Empty, ex.Message);
                }
            }
        }

        return await _fallback.ExecuteAsync(request, worker, ct).ConfigureAwait(false);
    }

    private void ApplyMemoryLimit(Process process)
    {
        if (_options.WorkerMemoryLimitMb <= 0 || !OperatingSystem.IsWindows())
            return;

        try
        {
            var bytes = (long)_options.WorkerMemoryLimitMb * 1024 * 1024;
            process.MaxWorkingSet = (IntPtr)Math.Min(bytes, int.MaxValue);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Could not apply delegation worker memory limit {Mb}MB", _options.WorkerMemoryLimitMb);
        }
    }
}
