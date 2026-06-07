namespace Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Delegation;

public sealed class DelegationRuntimeOptions
{
    public const string SectionName = "AutonomousAppGeneration:AgentRuntime:Delegation";

    public int DelegationTimeoutMinutes { get; set; } = 15;

    public int MaxConcurrentDelegationsPerRun { get; set; } = 3;

    /// <summary>Global cap across all runs/tenants (7.7.3 fleet scheduler).</summary>
    public int MaxGlobalConcurrentDelegations { get; set; } = 6;

    /// <summary>Fair-share cap per tenant user id.</summary>
    public int MaxConcurrentDelegationsPerTenant { get; set; } = 2;

    /// <summary>Route delegation workers through <see cref="IBackgroundFleetScheduler"/>.</summary>
    public bool EnableFleetScheduler { get; set; } = true;

    public bool EnableTimeoutRateAlerts { get; set; } = true;

    public double TimeoutAlertRateThreshold { get; set; } = 0.10;

    public int TimeoutAlertMinSamples { get; set; } = 5;

    public int TimeoutAlertCooldownMinutes { get; set; } = 60;

    /// <summary>Auto-restart count after worker crash (default 1 retry).</summary>
    public int MaxRestartAttempts { get; set; } = 1;

    /// <summary>When true, spawn out-of-process worker via libr4-run delegation-run.</summary>
    public bool UseOutOfProcessWorkers { get; set; } = true;

    /// <summary>Optional absolute path to libr4-run.dll. When empty, managed in-process worker is used.</summary>
    public string? WorkerCliPath { get; set; }

    /// <summary>Soft working-set cap for out-of-process workers (MB). 0 disables.</summary>
    public int WorkerMemoryLimitMb { get; set; } = 512;
}

public static class DelegationStatuses
{
    public const string Queued = "queued";
    public const string Running = "running";
    public const string Completed = "completed";
    public const string Failed = "failed";
    public const string TimedOut = "timed_out";
}
