namespace Libr4.IDE.Domain.AutonomousAppGeneration;

/// <summary>Who applied the fix attempt (recovery efficiency metric).</summary>
public enum RecoveryMechanism
{
    None,
    DeterministicStructural,
    DeterministicRuntime,
    DeterministicCompile,
    PatternRecovery,
    DeepStackHandler,
    Llm,
    SurgicalLlm,
    AgentToolLoop,
    RootCauseEscalation
}
