using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Models;
using Libr4.IDE.Domain.AutonomousAppGeneration;

namespace Libr4.IDE.Application.AutonomousAppGeneration.Memory.Hermes;

public sealed record HermesTurnContext(
    Guid? RunId,
    string RequestFingerprint,
    string Stage,
    IReadOnlyList<string>? Keywords = null,
    string? UserId = null);

public interface IHermesMemoryManager
{
    string ResolveFingerprint(GenerationPlan plan, string? requestFingerprint = null);

    Task<string?> PrefetchBeforeTurnAsync(HermesTurnContext context, CancellationToken ct = default);

    Task SyncAfterToolAsync(
        HermesTurnContext context,
        string toolName,
        string toolOutput,
        bool success,
        CancellationToken ct = default);

    Task OnPreCompactAsync(HermesTurnContext context, CancellationToken ct = default);
}
