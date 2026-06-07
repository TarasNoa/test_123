namespace Libr4.IDE.Application.AutonomousAppGeneration.Spaces;

public interface ISpaceContextBus
{
    string BuildHermesScope(Guid spaceId);

    string BuildDMailAddress(Guid spaceId, SpaceMemberRole role);

    Task PublishAsync(
        Guid spaceId,
        string kind,
        string title,
        string? payload,
        string? authorMemberId = null,
        CancellationToken ct = default);

    Task<IReadOnlyList<SpaceContextEvent>> ReadRecentAsync(Guid spaceId, int limit = 32, CancellationToken ct = default);
}
