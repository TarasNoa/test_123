using Libr4.IDE.Domain.AutonomousAppGeneration;

namespace Libr4.IDE.Application.AutonomousAppGeneration.AgentIntegration.LspBridge;

public interface ILspBridge
{
    Task<LspWorkspaceContext> GetWorkspaceContextAsync(
        IReadOnlyList<GeneratedFile> files,
        GenerationPlan? plan,
        IReadOnlyList<ErrorReport>? compilerErrors,
        IReadOnlyList<string>? focusPaths,
        CancellationToken ct = default);
}
