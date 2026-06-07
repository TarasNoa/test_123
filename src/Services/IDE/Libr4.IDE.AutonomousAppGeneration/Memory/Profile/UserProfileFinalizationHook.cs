using Libr4.IDE.Application.AutonomousAppGeneration.Services;
using Libr4.IDE.Domain.AutonomousAppGeneration;

namespace Libr4.IDE.Application.AutonomousAppGeneration.Memory.Profile;

public sealed class UserProfileFinalizationHook : IAutonomousFinalizationHook
{
    private readonly IUserProfileService _profiles;

    public UserProfileFinalizationHook(IUserProfileService profiles) => _profiles = profiles;

    public int Order => 85;

    public string Name => "user_profile_update";

    public Task ExecuteAsync(AppGenerationOrchestrator orchestrator, CancellationToken ct) =>
        _profiles.UpdateFromRunAsync(orchestrator, ct);
}
