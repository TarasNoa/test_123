using System.Collections.Concurrent;
using System.Diagnostics.Metrics;

namespace Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Delegation;

public static class DelegationTelemetry
{
    public const string MeterName = "Libr4.Delegation";
    public const string Version = "1.0";

    private static readonly Meter Meter = new(MeterName, Version);

    public static readonly Histogram<double> DurationSeconds =
        Meter.CreateHistogram<double>(
            "libr4_delegation_duration_seconds",
            unit: "s",
            description: "Background delegation worker duration.");

    public static readonly Counter<long> TimeoutTotal =
        Meter.CreateCounter<long>(
            "libr4_delegation_timeout_total",
            description: "Delegation workers that timed out.");

    public static readonly Counter<long> CompletedTotal =
        Meter.CreateCounter<long>(
            "libr4_delegation_completed_total",
            description: "Delegation workers that completed successfully.");

    public static readonly Counter<long> FailedTotal =
        Meter.CreateCounter<long>(
            "libr4_delegation_failed_total",
            description: "Delegation workers that failed.");

    public static readonly Counter<long> TimeoutRateAlerts =
        Meter.CreateCounter<long>(
            "libr4_delegation_timeout_rate_alert_total",
            description: "Alerts when hourly delegation timeout rate exceeds threshold.");

    private static long _timeoutTotal;
    private static long _completedTotal;
    private static long _failedTotal;
    private static readonly ConcurrentQueue<DurationSample> _durations = new();
    private static readonly ConcurrentQueue<CompletionEvent> _recentCompletions = new();

    public static void RecordCompletion(Guid runId, bool succeeded, bool timedOut)
    {
        var now = DateTime.UtcNow;
        _recentCompletions.Enqueue(new CompletionEvent(now, succeeded, timedOut));
        TrimRecentCompletions(TimeSpan.FromHours(1));

        if (timedOut)
        {
            Interlocked.Increment(ref _timeoutTotal);
            TimeoutTotal.Add(1, TagRun(runId));
        }
        else if (succeeded)
        {
            Interlocked.Increment(ref _completedTotal);
            CompletedTotal.Add(1, TagRun(runId));
        }
        else
        {
            Interlocked.Increment(ref _failedTotal);
            FailedTotal.Add(1, TagRun(runId));
        }
    }

    public static void RecordDuration(Guid runId, double seconds, string? delegationId = null)
    {
        _durations.Enqueue(new DurationSample(seconds, DateTime.UtcNow));
        while (_durations.Count > 500 && _durations.TryDequeue(out _))
        {
        }

        DurationSeconds.Record(
            seconds,
            TagRun(runId),
            new KeyValuePair<string, object?>("delegation_id", delegationId));
    }

    public static DelegationTelemetrySnapshot Snapshot()
    {
        var samples = _durations.ToArray();
        return new DelegationTelemetrySnapshot(
            Interlocked.Read(ref _completedTotal),
            Interlocked.Read(ref _failedTotal),
            Interlocked.Read(ref _timeoutTotal),
            samples.Select(s => new DurationSamplePublic(s.Seconds, s.TimestampUtc)).ToArray(),
            GetHourlyStats());
    }

    public static DelegationHourlyStats GetHourlyStats()
    {
        var cutoff = DateTime.UtcNow.AddHours(-1);
        var events = _recentCompletions.Where(e => e.AtUtc >= cutoff).ToArray();
        var total = events.Length;
        var timeouts = events.Count(e => e.TimedOut);
        var rate = total > 0 ? (double)timeouts / total : 0d;
        return new DelegationHourlyStats(total, timeouts, rate);
    }

    internal static void RecordTimeoutRateAlert(double rate, int sampleCount)
    {
        TimeoutRateAlerts.Add(
            1,
            new KeyValuePair<string, object?>("timeout_rate", rate),
            new KeyValuePair<string, object?>("sample_count", sampleCount));
    }

    private static void TrimRecentCompletions(TimeSpan window)
    {
        var cutoff = DateTime.UtcNow - window;
        while (_recentCompletions.TryPeek(out var head) && head.AtUtc < cutoff)
            _recentCompletions.TryDequeue(out _);
    }

    private static KeyValuePair<string, object?> TagRun(Guid runId) =>
        new("run_id", runId.ToString("D"));

    private readonly record struct DurationSample(double Seconds, DateTime TimestampUtc);

    private readonly record struct CompletionEvent(DateTime AtUtc, bool Succeeded, bool TimedOut);
}

public sealed record DurationSamplePublic(double Seconds, DateTime TimestampUtc);

public sealed record DelegationHourlyStats(int SampleCount, int TimeoutCount, double TimeoutRate);

public sealed record DelegationTelemetrySnapshot(
    long CompletedTotal,
    long FailedTotal,
    long TimeoutTotal,
    IReadOnlyList<DurationSamplePublic> RecentDurations,
    DelegationHourlyStats Hourly);
