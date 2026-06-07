namespace Libr4.IDE.Application.AutonomousAppGeneration.Scheduling;

public sealed class AgentSchedulingOptions
{
    public const string SectionName = "AutonomousAppGeneration:AgentScheduling";

    public bool Enabled { get; set; }

    public bool UseMassTransit { get; set; }

    public string DbPath { get; set; } = ".logs/agent-schedules.db";

    public int PollIntervalSeconds { get; set; } = 60;

    public string TriggerSource { get; set; } = "scheduled";

    public List<FlowScheduleConfigEntry> Flows { get; set; } = new();
}

public sealed class FlowScheduleConfigEntry
{
    public string FlowName { get; set; } = string.Empty;

    public string CronExpression { get; set; } = "0 2 * * *";

    public string? Prompt { get; set; }

    public int MaxIterations { get; set; } = 8;

    public bool Enabled { get; set; } = true;

    public string? TenantId { get; set; }
}

public sealed record ScheduledAgentRunDefinition(
    string ScheduleId,
    string FlowName,
    string CronExpression,
    string UserRequest,
    int MaxIterations,
    bool Enabled,
    string? TenantId = null,
    DateTime? LastRunAtUtc = null,
    Guid? LastRunId = null);

public sealed record ScheduledAgentRunResult(
    string ScheduleId,
    Guid? RunId,
    bool Started,
    string Message);
