namespace Libr4.IDE.Application.AutonomousAppGeneration.Services.PlatformUtilization;

/// <summary>Pipeline stage for scoped capability injection (not every stage needs every tool).</summary>
public enum PlatformCapabilityBriefingStage
{
    Planning,
    Generation,
    Repair,
    CascadePlanning,
    SecurityReview,
    Verify,
    ErrorAnalysis
}
