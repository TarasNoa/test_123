namespace Libr4.IDE.Application.AutonomousAppGeneration.Scheduling.Messaging;

public sealed record ExecuteScheduledAgentRunMessage(string ScheduleId, DateTime EnqueuedAtUtc);
