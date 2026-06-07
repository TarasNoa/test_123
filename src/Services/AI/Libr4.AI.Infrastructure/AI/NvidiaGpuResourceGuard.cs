using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Libr4.AI.Infrastructure.AI;

/// <summary>
/// Polls <c>nvidia-smi</c> and delays LLM calls while GPU load or VRAM exceed configured caps.
/// </summary>
public sealed class NvidiaGpuResourceGuard : IGpuResourceGuard
{
    private readonly GpuThrottleOptions _options;
    private readonly ILogger<NvidiaGpuResourceGuard> _logger;
    private readonly bool _nvidiaSmiAvailable;

    public NvidiaGpuResourceGuard(
        IOptions<GpuThrottleOptions> options,
        ILogger<NvidiaGpuResourceGuard> logger)
    {
        _options = options.Value;
        _logger = logger;
        _nvidiaSmiAvailable = ProbeNvidiaSmi();
        if (_options.Enabled && !_nvidiaSmiAvailable)
        {
            _logger.LogWarning(
                "GPU throttle enabled but nvidia-smi is unavailable; throttling will be skipped.");
        }
    }

    public async Task WaitForHeadroomAsync(CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled || !_nvidiaSmiAvailable)
            return;

        var gpuCap = Math.Clamp(_options.MaxGpuUtilizationPercent, 50, 99);
        var vramCap = Math.Clamp(_options.MaxVramUtilizationPercent, 50, 99);
        var pollMs = Math.Clamp(_options.PollIntervalMs, 500, 30_000);
        var deadline = DateTime.UtcNow.AddSeconds(Math.Clamp(_options.MaxWaitSeconds, 30, 3600));

        while (!cancellationToken.IsCancellationRequested)
        {
            if (!TryReadGpuStats(out var gpuUtil, out var vramUsedMb, out var vramTotalMb))
            {
                await Task.Delay(pollMs, cancellationToken).ConfigureAwait(false);
                continue;
            }

            var vramPct = vramTotalMb > 0
                ? (int)Math.Round(vramUsedMb * 100.0 / vramTotalMb)
                : 0;

            var vramOverCap = _options.ThrottleOnVramUtilization && vramPct >= vramCap;
            if (gpuUtil < gpuCap && !vramOverCap)
                return;

            if (DateTime.UtcNow >= deadline)
            {
                _logger.LogWarning(
                    "GPU throttle timeout: proceeding with gpu={Gpu}% vram={Vram}% (gpu_cap<{GpuCap}%, vram_throttle={VramThrottle})",
                    gpuUtil,
                    vramPct,
                    gpuCap,
                    _options.ThrottleOnVramUtilization);
                return;
            }

            _logger.LogInformation(
                "GPU throttle: waiting (gpu={Gpu}% vram={Vram}%/{TotalMb}MB resident, gpu_cap<{GpuCap}%, vram_throttle={VramThrottle})",
                gpuUtil,
                vramPct,
                vramTotalMb,
                gpuCap,
                _options.ThrottleOnVramUtilization);

            await Task.Delay(pollMs, cancellationToken).ConfigureAwait(false);
        }
    }

    private static bool ProbeNvidiaSmi()
    {
        try
        {
            using var process = Process.Start(new ProcessStartInfo
            {
                FileName = "nvidia-smi",
                Arguments = "--query-gpu=utilization.gpu --format=csv,noheader,nounits",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            });
            if (process is null)
                return false;

            process.WaitForExit(5000);
            return process.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }

    private static bool TryReadGpuStats(out int gpuUtil, out long vramUsedMb, out long vramTotalMb)
    {
        gpuUtil = 0;
        vramUsedMb = 0;
        vramTotalMb = 0;

        try
        {
            using var process = Process.Start(new ProcessStartInfo
            {
                FileName = "nvidia-smi",
                Arguments = "--query-gpu=utilization.gpu,memory.used,memory.total --format=csv,noheader,nounits",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            });
            if (process is null)
                return false;

            if (!process.WaitForExit(5000) || process.ExitCode != 0)
                return false;

            var line = process.StandardOutput.ReadLine();
            if (string.IsNullOrWhiteSpace(line))
                return false;

            var parts = line.Split(',', StringSplitOptions.TrimEntries);
            if (parts.Length < 3)
                return false;

            if (!int.TryParse(parts[0], out gpuUtil))
                return false;
            if (!long.TryParse(parts[1], out vramUsedMb))
                return false;
            if (!long.TryParse(parts[2], out vramTotalMb))
                return false;

            return true;
        }
        catch
        {
            return false;
        }
    }
}
