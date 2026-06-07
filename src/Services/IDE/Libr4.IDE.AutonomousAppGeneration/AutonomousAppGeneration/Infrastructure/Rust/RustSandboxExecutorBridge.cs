using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;

namespace Libr4.IDE.Application.AutonomousAppGeneration.Infrastructure.Rust;

/// <summary>Rust polyglot sandbox executor (Wave 3.1). Falls back gracefully when cdylib is unavailable.</summary>
public static class RustSandboxExecutorBridge
{
    private const ulong DefaultMaxOutputBytes = 4 * 1024 * 1024;
    private static bool? _available;

    public static bool IsAvailable
    {
        get
        {
            if (_available.HasValue)
                return _available.Value;

            var probeRoot = Path.GetTempPath();
            _available = RustNativeLibrary.TryLoad(
                () =>
                {
                    var handle = RustSandboxExecutorNative.executor_create(
                        1_000,
                        DefaultMaxOutputBytes,
                        probeRoot);
                    if (handle != IntPtr.Zero)
                        RustSandboxExecutorNative.executor_destroy(handle);
                    return handle != IntPtr.Zero;
                },
                out _,
                out _);
            return _available.Value;
        }
    }

    public static bool TryExecute(
        string projectRoot,
        string language,
        string code,
        TimeSpan timeout,
        ILogger? logger,
        out SandboxExecutorBridgeResult result)
    {
        result = SandboxExecutorBridgeResult.Empty;
        if (!IsAvailable || string.IsNullOrWhiteSpace(projectRoot))
            return false;

        if (!Directory.Exists(projectRoot))
            return false;

        IntPtr handle = IntPtr.Zero;
        IntPtr stdoutPtr = IntPtr.Zero;
        IntPtr stderrPtr = IntPtr.Zero;

        try
        {
            var timeoutMs = (ulong)Math.Clamp(timeout.TotalMilliseconds, 1, long.MaxValue);
            handle = RustSandboxExecutorNative.executor_create(timeoutMs, DefaultMaxOutputBytes, projectRoot);
            if (handle == IntPtr.Zero)
                return false;

            var rc = RustSandboxExecutorNative.executor_execute(
                handle,
                language,
                code,
                out stdoutPtr,
                out stderrPtr,
                out var exitCode,
                out var timedOut);

            if (rc != 0)
                return false;

            var stdout = Marshal.PtrToStringAnsi(stdoutPtr) ?? string.Empty;
            var stderr = Marshal.PtrToStringAnsi(stderrPtr) ?? string.Empty;
            result = new SandboxExecutorBridgeResult(stdout, stderr, exitCode, timedOut);
            return true;
        }
        catch (Exception ex) when (ex is DllNotFoundException or BadImageFormatException or EntryPointNotFoundException)
        {
            _available = false;
            logger?.LogDebug(ex, "[RustSandboxExecutor] native library unavailable");
            return false;
        }
        catch (Exception ex)
        {
            logger?.LogDebug(ex, "[RustSandboxExecutor] execute failed, using C# fallback");
            return false;
        }
        finally
        {
            if (stdoutPtr != IntPtr.Zero)
                RustSandboxExecutorNative.executor_free_string(stdoutPtr);
            if (stderrPtr != IntPtr.Zero)
                RustSandboxExecutorNative.executor_free_string(stderrPtr);
            if (handle != IntPtr.Zero)
                RustSandboxExecutorNative.executor_destroy(handle);
        }
    }

    public static bool TryRunShell(
        string projectRoot,
        string command,
        TimeSpan timeout,
        ILogger? logger,
        out SandboxExecutorBridgeResult result)
    {
        result = SandboxExecutorBridgeResult.Empty;
        if (!IsAvailable || string.IsNullOrWhiteSpace(projectRoot) || string.IsNullOrWhiteSpace(command))
            return false;

        if (!Directory.Exists(projectRoot))
            return false;

        IntPtr stdoutPtr = IntPtr.Zero;
        IntPtr stderrPtr = IntPtr.Zero;

        try
        {
            var timeoutMs = (ulong)Math.Clamp(timeout.TotalMilliseconds, 1, long.MaxValue);
            var rc = RustSandboxExecutorNative.executor_run_shell(
                projectRoot,
                command,
                timeoutMs,
                DefaultMaxOutputBytes,
                out stdoutPtr,
                out stderrPtr,
                out var exitCode,
                out var timedOut);

            if (rc != 0)
                return false;

            var stdout = Marshal.PtrToStringAnsi(stdoutPtr) ?? string.Empty;
            var stderr = Marshal.PtrToStringAnsi(stderrPtr) ?? string.Empty;
            result = new SandboxExecutorBridgeResult(stdout, stderr, exitCode, timedOut);
            return true;
        }
        catch (Exception ex) when (ex is DllNotFoundException or BadImageFormatException or EntryPointNotFoundException)
        {
            _available = false;
            logger?.LogDebug(ex, "[RustSandboxExecutor] native library unavailable");
            return false;
        }
        catch (Exception ex)
        {
            logger?.LogDebug(ex, "[RustSandboxExecutor] shell exec failed, using C# fallback");
            return false;
        }
        finally
        {
            if (stdoutPtr != IntPtr.Zero)
                RustSandboxExecutorNative.executor_free_string(stdoutPtr);
            if (stderrPtr != IntPtr.Zero)
                RustSandboxExecutorNative.executor_free_string(stderrPtr);
        }
    }
}

public readonly record struct SandboxExecutorBridgeResult(
    string Stdout,
    string Stderr,
    int ExitCode,
    bool TimedOut)
{
    public static SandboxExecutorBridgeResult Empty { get; } = new(string.Empty, string.Empty, -1, false);
}
