namespace Libr4.AI.Infrastructure.AI;

/// <summary>
/// Backpressure before local LLM calls to avoid saturating GPU VRAM/utilization (thermal throttling).
/// </summary>
public sealed class GpuThrottleOptions
{
    public const string SectionName = "AI:GpuThrottle";

    public bool Enabled { get; set; } = true;

    /// <summary>Wait while GPU compute utilization is at or above this percent.</summary>
    public int MaxGpuUtilizationPercent { get; set; } = 80;

    /// <summary>
    /// When true, also wait while used VRAM / total VRAM is at or above <see cref="MaxVramUtilizationPercent"/>.
    /// Off by default: Docker Model Runner keeps models resident (~90%+ VRAM idle).
    /// </summary>
    public bool ThrottleOnVramUtilization { get; set; }

    /// <summary>VRAM cap when <see cref="ThrottleOnVramUtilization"/> is enabled.</summary>
    public int MaxVramUtilizationPercent { get; set; } = 80;

    public int PollIntervalMs { get; set; } = 2000;

    /// <summary>Max seconds to wait for headroom before proceeding anyway (avoid deadlock).</summary>
    public int MaxWaitSeconds { get; set; } = 600;
}
