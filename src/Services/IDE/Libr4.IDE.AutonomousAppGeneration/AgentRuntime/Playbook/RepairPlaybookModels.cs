namespace Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Playbook;

public sealed record RepairPlaybookEntry(
    string ErrorSignature,
    string StackPattern,
    string FixPattern,
    int SuccessCount,
    int FailCount,
    double Score,
    DateTime LastUsedAtUtc);

public sealed record RepairPlaybookSignatureResult(
    string Signature,
    string StackPattern);
