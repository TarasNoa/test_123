using System.Runtime.InteropServices;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace Libr4.IDE.Application.AutonomousAppGeneration.Infrastructure.Rust;

/// <summary>Wave 3.2: Rust isolated delegation worker spawn via libr4-delegation-worker.</summary>
internal static class RustDelegationWorkerBridge
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private static bool? _available;

    public static bool IsAvailable
    {
        get
        {
            if (_available.HasValue)
                return _available.Value;

            _available = RustNativeLibrary.TryLoad(
                () => RustDelegationWorkerNative.delegation_run_worker_json("{}", out _),
                out _,
                out _);
            return _available.Value;
        }
    }

    public static bool TryRunWorker(
        string jobPath,
        string workerCliPath,
        string workingDirectory,
        int timeoutMinutes,
        int memoryLimitMb,
        int maxRestartAttempts,
        ILogger logger,
        out bool succeeded,
        out string output,
        out string error,
        out bool timedOut)
    {
        succeeded = false;
        output = string.Empty;
        error = string.Empty;
        timedOut = false;

        if (!IsAvailable)
            return false;

        try
        {
            var request = JsonSerializer.Serialize(new
            {
                jobPath,
                workerCliPath,
                workingDirectory,
                timeoutSeconds = Math.Clamp(timeoutMinutes, 1, 120) * 60L,
                memoryLimitMb = Math.Max(0, memoryLimitMb),
                maxRestartAttempts = Math.Max(0, maxRestartAttempts)
            }, JsonOptions);

            var rc = RustDelegationWorkerNative.delegation_run_worker_json(request, out var jsonPtr);
            if (jsonPtr == IntPtr.Zero)
                return false;

            var json = Marshal.PtrToStringUTF8(jsonPtr) ?? "{}";
            RustDelegationWorkerNative.delegation_worker_free_string(jsonPtr);

            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.TryGetProperty("error", out var errEl))
            {
                error = errEl.GetString() ?? "delegation_worker_error";
                return true;
            }

            succeeded = doc.RootElement.TryGetProperty("exitCode", out var exitEl) && exitEl.GetInt32() == 0;
            output = doc.RootElement.TryGetProperty("stdout", out var outEl) ? outEl.GetString() ?? string.Empty : string.Empty;
            error = doc.RootElement.TryGetProperty("stderr", out var errOutEl) ? errOutEl.GetString() ?? string.Empty : string.Empty;
            timedOut = doc.RootElement.TryGetProperty("timedOut", out var toEl) && toEl.GetBoolean();
            return true;
        }
        catch (Exception ex) when (ex is DllNotFoundException or BadImageFormatException or EntryPointNotFoundException)
        {
            _available = false;
            logger.LogDebug(ex, "[RustDelegationWorker] native library unavailable");
            return false;
        }
        catch (Exception ex)
        {
            logger.LogDebug(ex, "[RustDelegationWorker] run failed, using C# fallback");
            return false;
        }
    }
}
