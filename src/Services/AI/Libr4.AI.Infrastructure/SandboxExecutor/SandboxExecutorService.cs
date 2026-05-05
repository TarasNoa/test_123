using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Libr4.AI.Infrastructure.SandboxExecutor;

public class SandboxExecutorOptions
{
    public uint TimeoutMs { get; set; } = 30000; // 30 seconds
    public uint MaxOutputBytes { get; set; } = 100000; // 100KB
    public string ProjectRoot { get; set; } = string.Empty;
}

public class ExecutionResult
{
    public string Stdout { get; set; } = string.Empty;
    public string Stderr { get; set; } = string.Empty;
    public int ExitCode { get; set; }
    public bool TimedOut { get; set; }
}

public class SandboxExecutorService : IDisposable
{
    private readonly string _libraryPath;
    private readonly SandboxExecutorOptions _options;
    private readonly ILogger<SandboxExecutorService> _logger;
    private IntPtr _executorHandle;

    // P/Invoke declarations
    [DllImport("libr4_sandbox_executor", CallingConvention = CallingConvention.Cdecl)]
    private static extern IntPtr executor_create(
        ulong timeout_ms,
        ulong max_output_bytes,
        [MarshalAs(UnmanagedType.LPStr)] string project_root);

    [DllImport("libr4_sandbox_executor", CallingConvention = CallingConvention.Cdecl)]
    private static extern int executor_execute(
        IntPtr executor,
        [MarshalAs(UnmanagedType.LPStr)] string language,
        [MarshalAs(UnmanagedType.LPStr)] string code,
        out IntPtr out_stdout,
        out IntPtr out_stderr,
        out int out_exit_code,
        [MarshalAs(UnmanagedType.I1)] out bool out_timed_out);

    [DllImport("libr4_sandbox_executor", CallingConvention = CallingConvention.Cdecl)]
    private static extern void executor_free_string(IntPtr s);

    [DllImport("libr4_sandbox_executor", CallingConvention = CallingConvention.Cdecl)]
    private static extern void executor_destroy(IntPtr executor);

    public SandboxExecutorService(
        IOptions<SandboxExecutorOptions> options,
        ILogger<SandboxExecutorService> logger)
    {
        _options = options.Value;
        _logger = logger;
        
        // Load library from project directory or system path
        _libraryPath = DetermineLibraryPath();
        
        try
        {
            _executorHandle = executor_create(
                _options.TimeoutMs,
                _options.MaxOutputBytes,
                _options.ProjectRoot);
            
            if (_executorHandle == IntPtr.Zero)
            {
                throw new InvalidOperationException("Failed to create Rust executor");
            }
            
            _logger.LogInformation("Sandbox executor initialized with project root: {ProjectRoot}", _options.ProjectRoot);
        }
        catch (DllNotFoundException ex)
        {
            _logger.LogError(ex, "Rust executor library not found at {LibraryPath}", _libraryPath);
            throw new InvalidOperationException(
                $"Rust executor library not found. Please build the Rust project and place the library at: {_libraryPath}", ex);
        }
    }

    private string DetermineLibraryPath()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return "libr4_sandbox_executor.dll";
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            return "liblibr4_sandbox_executor.so";
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            return "liblibr4_sandbox_executor.dylib";
        }
        else
        {
            throw new PlatformNotSupportedException("Unsupported platform");
        }
    }

    public ExecutionResult Execute(string language, string code)
    {
        if (_executorHandle == IntPtr.Zero)
        {
            throw new ObjectDisposedException(nameof(SandboxExecutorService));
        }

        _logger.LogDebug("Executing {Language} code", language);

        int result = executor_execute(
            _executorHandle,
            language,
            code,
            out IntPtr stdoutPtr,
            out IntPtr stderrPtr,
            out int exitCode,
            out bool timedOut);

        if (result != 0)
        {
            throw new InvalidOperationException("Execution failed in Rust executor");
        }

        string stdout = Marshal.PtrToStringAnsi(stdoutPtr) ?? string.Empty;
        string stderr = Marshal.PtrToStringAnsi(stderrPtr) ?? string.Empty;

        executor_free_string(stdoutPtr);
        executor_free_string(stderrPtr);

        _logger.LogDebug("Execution completed. Exit code: {ExitCode}, Timed out: {TimedOut}", exitCode, timedOut);

        return new ExecutionResult
        {
            Stdout = stdout,
            Stderr = stderr,
            ExitCode = exitCode,
            TimedOut = timedOut
        };
    }

    public void Dispose()
    {
        if (_executorHandle != IntPtr.Zero)
        {
            executor_destroy(_executorHandle);
            _executorHandle = IntPtr.Zero;
            _logger.LogInformation("Sandbox executor destroyed");
        }
    }
}
