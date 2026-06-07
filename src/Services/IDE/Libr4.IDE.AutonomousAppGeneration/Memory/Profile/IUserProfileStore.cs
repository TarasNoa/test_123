namespace Libr4.IDE.Application.AutonomousAppGeneration.Memory.Profile;

public interface IUserProfileStore
{
    Task<UserProfileDocument?> LoadAsync(string userId, CancellationToken ct = default);

    Task SaveAsync(UserProfileDocument document, CancellationToken ct = default);
}
