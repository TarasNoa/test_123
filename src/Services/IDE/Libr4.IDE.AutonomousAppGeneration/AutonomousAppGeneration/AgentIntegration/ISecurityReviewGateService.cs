using Libr4.IDE.Domain.AutonomousAppGeneration;

namespace Libr4.IDE.Application.AutonomousAppGeneration.AgentIntegration;

public interface ISecurityReviewGateService
{
    SecurityReviewAuditEntry EvaluateArtifacts(
        string stage,
        IReadOnlyList<GeneratedFile> files,
        GenerationPlan plan);
}
