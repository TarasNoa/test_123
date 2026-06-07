using System.Runtime.InteropServices;

namespace Libr4.IDE.Application.AutonomousAppGeneration.Infrastructure.Rust;

internal static class RustNativeLibrary
{
    public const string FastContextLibraryName = "libr4_fast_context";
    public const string RolloutWriterLibraryName = "libr4_rollout_writer";
    public const string SandboxExecutorLibraryName = "libr4_sandbox_executor";

    public static bool TryLoad<T>(Func<T> factory, out T? instance, out Exception? error)
    {
        try
        {
            instance = factory();
            error = null;
            return true;
        }
        catch (DllNotFoundException ex)
        {
            instance = default;
            error = ex;
            return false;
        }
        catch (BadImageFormatException ex)
        {
            instance = default;
            error = ex;
            return false;
        }
        catch (EntryPointNotFoundException ex)
        {
            instance = default;
            error = ex;
            return false;
        }
    }
}

internal static class RustFastContextNative
{
    [DllImport(RustNativeLibrary.FastContextLibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern int fast_context_search_json(
        [MarshalAs(UnmanagedType.LPStr)] string requestJson,
        out IntPtr outJson);

    [DllImport(RustNativeLibrary.FastContextLibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern int fast_context_build_manifest_json(
        [MarshalAs(UnmanagedType.LPStr)] string workspaceRoot,
        out IntPtr outJson);

    [DllImport(RustNativeLibrary.FastContextLibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void fast_context_free_string(IntPtr s);
}

internal static class RustRolloutWriterNative
{
    [DllImport(RustNativeLibrary.RolloutWriterLibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern int rollout_append_line(
        [MarshalAs(UnmanagedType.LPStr)] string path,
        [MarshalAs(UnmanagedType.LPStr)] string line);
}

internal static class RustSandboxExecutorNative
{
    [DllImport(RustNativeLibrary.SandboxExecutorLibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr executor_create(
        ulong timeoutMs,
        ulong maxOutputBytes,
        [MarshalAs(UnmanagedType.LPStr)] string projectRoot);

    [DllImport(RustNativeLibrary.SandboxExecutorLibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern int executor_execute(
        IntPtr executor,
        [MarshalAs(UnmanagedType.LPStr)] string language,
        [MarshalAs(UnmanagedType.LPStr)] string code,
        out IntPtr outStdout,
        out IntPtr outStderr,
        out int outExitCode,
        [MarshalAs(UnmanagedType.I1)] out bool outTimedOut);

    [DllImport(RustNativeLibrary.SandboxExecutorLibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void executor_free_string(IntPtr s);

    [DllImport(RustNativeLibrary.SandboxExecutorLibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void executor_destroy(IntPtr executor);

    [DllImport(RustNativeLibrary.SandboxExecutorLibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern int executor_run_shell(
        [MarshalAs(UnmanagedType.LPStr)] string projectRoot,
        [MarshalAs(UnmanagedType.LPStr)] string command,
        ulong timeoutMs,
        ulong maxOutputBytes,
        out IntPtr outStdout,
        out IntPtr outStderr,
        out int outExitCode,
        [MarshalAs(UnmanagedType.I1)] out bool outTimedOut);
}

internal static class RustDelegationWorkerNative
{
    public const string LibraryName = "libr4_delegation_worker";

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern int delegation_run_worker_json(
        [MarshalAs(UnmanagedType.LPStr)] string requestJson,
        out IntPtr outJson);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void delegation_worker_free_string(IntPtr s);
}
