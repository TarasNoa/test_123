namespace Libr4.IDE.Application.AutonomousAppGeneration.Services.PlatformUtilization;

public sealed record PlatformJitPlaybookMatch(
    string PlaybookId,
    string ErrorSignature,
    string InjectionText);

public sealed record PlatformJitInjectionResult(
    bool Injected,
    string? InjectionId,
    string? PlaybookId,
    string? ErrorSignature,
    string? InjectionText);

public sealed record PlatformJitInjectedEvent(
    string Event,
    string InjectionId,
    Guid RunId,
    int RepairAttempt,
    int Iteration,
    bool JitInjected,
    string PlaybookId,
    string ErrorSignature,
    DateTime TimestampUtc);

public sealed record PlatformJitResolvedEvent(
    string Event,
    string InjectionId,
    Guid RunId,
    int ResolvedAtIteration,
    bool ResolvedWithinNextIteration,
    DateTime TimestampUtc);
