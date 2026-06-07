namespace Libr4.IDE.Application.AutonomousAppGeneration.Verify;

public sealed record ObscuraVerifySmokeTargetResult(
    string TargetName,
    string Url,
    bool Passed,
    string Summary,
    IReadOnlyList<string> EvidencePaths);

public sealed record ObscuraVerifySmokeResult(
    bool Passed,
    string Summary,
    IReadOnlyList<ObscuraVerifySmokeTargetResult> Targets);
