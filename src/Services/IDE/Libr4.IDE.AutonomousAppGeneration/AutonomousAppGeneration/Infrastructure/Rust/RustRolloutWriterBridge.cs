using Microsoft.Extensions.Logging;

namespace Libr4.IDE.Application.AutonomousAppGeneration.Infrastructure.Rust;

/// <summary>Rust buffered NDJSON append for rollout recorder (Wave 3.5).</summary>
internal static class RustRolloutWriterBridge
{
    private static bool? _available;

    public static bool IsAvailable
    {
        get
        {
            if (_available.HasValue)
                return _available.Value;

            _available = RustNativeLibrary.TryLoad(
                () => RustRolloutWriterNative.rollout_append_line(string.Empty, string.Empty),
                out _,
                out _);
            return _available.Value;
        }
    }

    public static bool TryAppendLine(string path, string line, ILogger? logger = null)
    {
        if (!IsAvailable)
            return false;

        try
        {
            var rc = RustRolloutWriterNative.rollout_append_line(path, line);
            return rc == 0;
        }
        catch (Exception ex) when (ex is DllNotFoundException or BadImageFormatException or EntryPointNotFoundException)
        {
            _available = false;
            logger?.LogDebug(ex, "[RustRolloutWriter] native library unavailable");
            return false;
        }
        catch (Exception ex)
        {
            logger?.LogDebug(ex, "[RustRolloutWriter] append failed, using C# fallback");
            return false;
        }
    }
}
