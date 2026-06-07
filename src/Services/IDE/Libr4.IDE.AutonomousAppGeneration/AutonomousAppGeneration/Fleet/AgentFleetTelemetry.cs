using System.Diagnostics.Metrics;

namespace Libr4.IDE.Application.AutonomousAppGeneration.Fleet;

public static class AgentFleetTelemetry
{
    public const string MeterName = "Libr4.AgentFleet";
    public const string Version = "1.0";

    private static readonly Meter Meter = new(MeterName, Version);

    public static readonly UpDownCounter<long> RunsActive =
        Meter.CreateUpDownCounter<long>(
            "libr4_fleet_runs_active",
            description: "Currently active agent fleet runs.");

    public static readonly Counter<long> StatusTransitions =
        Meter.CreateCounter<long>(
            "libr4_fleet_status_transition_total",
            description: "Fleet status transitions.");

    public static readonly Histogram<double> TimeToVerifySeconds =
        Meter.CreateHistogram<double>(
            "libr4_fleet_time_to_verify_seconds",
            unit: "s",
            description: "Elapsed seconds from run start to first Verifying status.");

    public static void RecordTransition(AgentFleetStatus status, string stage)
    {
        StatusTransitions.Add(1,
            new KeyValuePair<string, object?>("status", status.ToString()),
            new KeyValuePair<string, object?>("stage", stage));
    }

    public static void RecordTimeToVerify(double seconds, Guid runId)
    {
        TimeToVerifySeconds.Record(seconds,
            new KeyValuePair<string, object?>("run_id", runId.ToString("D")));
    }

    private static readonly Counter<long> StuckRepairing =
        Meter.CreateCounter<long>(
            "libr4_fleet_stuck_repairing_total",
            description: "Runs flagged as stuck in Repairing without tool activity.");

    public static void RecordStuckRepairing(Guid runId)
    {
        StuckRepairing.Add(1, new KeyValuePair<string, object?>("run_id", runId.ToString("D")));
    }
}
