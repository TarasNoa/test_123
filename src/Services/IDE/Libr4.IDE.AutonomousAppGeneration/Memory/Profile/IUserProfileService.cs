using Libr4.IDE.Domain.AutonomousAppGeneration;

namespace Libr4.IDE.Application.AutonomousAppGeneration.Memory.Profile;

public interface IUserProfileService
{
    string? ResolveUserId(AppGenerationOrchestrator orchestrator);

    Task<string> AugmentPlanningRequestAsync(
        AppGenerationOrchestrator orchestrator,
        string userRequest,
        CancellationToken ct = default);

    Task UpdateFromRunAsync(AppGenerationOrchestrator orchestrator, CancellationToken ct = default);
}
