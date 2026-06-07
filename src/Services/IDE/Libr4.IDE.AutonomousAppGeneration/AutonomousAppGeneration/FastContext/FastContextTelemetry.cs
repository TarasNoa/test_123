using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Libr4.IDE.Application.AutonomousAppGeneration.FastContext;

public static class FastContextTelemetry
{
    public const string MeterName = "Libr4.FastContext";
    private const string Version = "1.0";

    private static readonly Meter Meter = new(MeterName, Version);

    public static readonly Histogram<long> QueryDurationMs =
        Meter.CreateHistogram<long>(
            "libr4_fast_context_query_duration_ms",
            unit: "ms",
            description: "Fast context search/index query wall-clock latency.");

    public static readonly Counter<long> CacheHits =
        Meter.CreateCounter<long>(
            "libr4_fast_context_cache_hit",
            description: "Warm index cache hits.");

    public static readonly Counter<long> CacheMisses =
        Meter.CreateCounter<long>(
            "libr4_fast_context_cache_miss",
            description: "Cold index cache misses.");

    public static void RecordQuery(string operation, long durationMs, bool cacheHit)
    {
        QueryDurationMs.Record(
            durationMs,
            new KeyValuePair<string, object?>("operation", operation),
            new KeyValuePair<string, object?>("cache", cacheHit ? "hit" : "miss"));

        if (cacheHit)
            CacheHits.Add(1, new KeyValuePair<string, object?>("operation", operation));
        else
            CacheMisses.Add(1, new KeyValuePair<string, object?>("operation", operation));
    }

    public static long StartTiming() => Stopwatch.GetTimestamp();

    public static long ElapsedMs(long startTimestamp) =>
        (long)Stopwatch.GetElapsedTime(startTimestamp).TotalMilliseconds;
}
