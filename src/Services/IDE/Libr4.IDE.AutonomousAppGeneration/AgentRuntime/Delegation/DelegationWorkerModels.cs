namespace Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Delegation;

public sealed record DelegationWorkerRequest(
    Guid RunId,
    string DelegationId,
    string Task,
    string RunsRoot,
    string OutputPath,
    string RecordPath);

public sealed record DelegationWorkerResult(
    bool Succeeded,
    string Output,
    string? Error = null,
    bool TimedOut = false);
