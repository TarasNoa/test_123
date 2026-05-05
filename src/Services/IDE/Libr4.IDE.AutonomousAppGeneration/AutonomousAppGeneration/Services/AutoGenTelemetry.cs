using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Libr4.IDE.Application.AutonomousAppGeneration.Services;

/// <summary>
/// P1-4 of audit roadmap. First-class observability primitives based on the
/// BCL <see cref="Meter"/> / <see cref="ActivitySource"/> APIs so the host can
/// plug in OpenTelemetry / Prometheus / Application Insights without changing
/// any service code.
///
/// Naming follows the audit roadmap (`autogen.runs.*`, `autogen.gate.*`,
/// `autogen.iterations`, `autogen.fallback.used`, `autogen.consolidation.*`).
/// All instruments are static so they can be referenced from any layer.
/// </summary>
public static class AutoGenTelemetry
{
    public const string MeterName = "Libr4.AutoGen";
    public const string ActivitySourceName = "Libr4.AutoGen";
    public const string Version = "1.0";

    private static readonly Meter _meter = new(MeterName, Version);

    /// <summary>Activity source for distributed tracing (one span per stage / LLM call).</summary>
    public static readonly ActivitySource Source = new(ActivitySourceName, Version);

    // -------- counters --------

    /// <summary>Increments when a run is started (after fingerprint dedup).</summary>
    public static readonly Counter<long> RunsStarted =
        _meter.CreateCounter<long>("autogen.runs.started", description: "Number of runs that began execution.");

    /// <summary>Increments when a run reaches a terminal state. Tag: status=Completed|Failed|Cancelled.</summary>
    public static readonly Counter<long> RunsCompleted =
        _meter.CreateCounter<long>("autogen.runs.completed", description: "Runs that reached a terminal state.");

    /// <summary>Increments whenever a deterministic fallback artefact is injected. Tag: stack=python|node|dotnet, kind=readme|env|...</summary>
    public static readonly Counter<long> FallbackUsed =
        _meter.CreateCounter<long>("autogen.fallback.used", description: "Deterministic fallback artefacts injected.");

    /// <summary>Increments when the per-phase build gate aborts a run. Tag: phase=contracts|models|...</summary>
    public static readonly Counter<long> BuildGateAborted =
        _meter.CreateCounter<long>("autogen.build_gate.aborted", description: "Runs aborted by StrictPerPhase build gate.");

    /// <summary>Memory consolidation queue: enqueued / dropped / processed.</summary>
    public static readonly Counter<long> ConsolidationEnqueued =
        _meter.CreateCounter<long>("autogen.consolidation.enqueued");
    public static readonly Counter<long> ConsolidationDropped =
        _meter.CreateCounter<long>("autogen.consolidation.dropped");
    public static readonly Counter<long> ConsolidationProcessed =
        _meter.CreateCounter<long>("autogen.consolidation.processed");

    // -------- histograms --------

    /// <summary>Distribution of quality-gate scores (0-10). Tag: stage=plan|generation|build|review2|...</summary>
    public static readonly Histogram<int> GateScore =
        _meter.CreateHistogram<int>("autogen.gate.score", description: "Quality-gate scores (0-10).");

    /// <summary>Distribution of iteration counts per completed run.</summary>
    public static readonly Histogram<int> IterationsPerRun =
        _meter.CreateHistogram<int>("autogen.iterations", description: "Iteration count per run.");

    /// <summary>LLM step latency in milliseconds. Tag: stage=planning|generation|fixing.</summary>
    public static readonly Histogram<long> LlmStepDurationMs =
        _meter.CreateHistogram<long>("autogen.llm.step_ms", unit: "ms", description: "LLM call wall-clock latency.");

    // -------- helpers --------

    /// <summary>Convenience: start a span tied to <see cref="Source"/>.</summary>
    public static Activity? StartActivity(string name, ActivityKind kind = ActivityKind.Internal) =>
        Source.StartActivity(name, kind);

    /// <summary>Convenience: record a gate score with the supplied stage tag.</summary>
    public static void RecordGateScore(int score, string stage)
    {
        GateScore.Record(score, new KeyValuePair<string, object?>("stage", stage));
    }

    /// <summary>Convenience: record fallback usage for a stack/kind.</summary>
    public static void RecordFallback(string stack, string kind)
    {
        FallbackUsed.Add(1,
            new KeyValuePair<string, object?>("stack", stack),
            new KeyValuePair<string, object?>("kind", kind));
    }
}
