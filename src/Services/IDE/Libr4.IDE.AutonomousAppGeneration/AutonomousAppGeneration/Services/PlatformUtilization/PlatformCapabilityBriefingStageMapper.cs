namespace Libr4.IDE.Application.AutonomousAppGeneration.Services.PlatformUtilization;

public static class PlatformCapabilityBriefingStageMapper
{
    public static PlatformCapabilityBriefingStage FromLlmStage(string stage) =>
        stage.ToLowerInvariant() switch
        {
            "planning" => PlatformCapabilityBriefingStage.Planning,
            "generation" or "generating" => PlatformCapabilityBriefingStage.Generation,
            "fixing" or "repair" or "repairing" => PlatformCapabilityBriefingStage.Repair,
            "cascade_planning" or "cascade" => PlatformCapabilityBriefingStage.CascadePlanning,
            "security_review" => PlatformCapabilityBriefingStage.SecurityReview,
            "verify" => PlatformCapabilityBriefingStage.Verify,
            "error_analysis" => PlatformCapabilityBriefingStage.ErrorAnalysis,
            _ => PlatformCapabilityBriefingStage.Generation
        };
}
