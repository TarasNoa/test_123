using Libr4.IDE.Domain.AutonomousAppGeneration;

namespace Libr4.IDE.Application.AutonomousAppGeneration.AgentIntegration;

public interface ISecurityReviewGateService
{
    Task<SecurityReviewAuditEntry> EvaluateArtifactsAsync(
        string stage,
        IReadOnlyList<GeneratedFile> files,
        GenerationPlan plan,
        CancellationToken ct = default);
}
