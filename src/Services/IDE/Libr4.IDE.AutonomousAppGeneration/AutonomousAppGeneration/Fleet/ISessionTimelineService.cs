namespace Libr4.IDE.Application.AutonomousAppGeneration.Fleet;

public interface ISessionTimelineService
{
    Task<SessionTimelineResponse> GetTimelineAsync(Guid runId, CancellationToken ct = default);
}
