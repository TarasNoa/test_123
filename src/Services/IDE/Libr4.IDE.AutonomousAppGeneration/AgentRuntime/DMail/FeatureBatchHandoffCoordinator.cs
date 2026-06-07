using Libr4.IDE.Domain.AutonomousAppGeneration;
using DomainGeneratedFile = Libr4.IDE.Domain.AutonomousAppGeneration.GeneratedFile;

namespace Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.DMail;

public interface IFeatureBatchHandoffCoordinator
{
    Task SendBackendToFrontendAsync(Guid runId, IReadOnlyList<GeneratedFile> backendFiles, CancellationToken ct = default);
    Task<string?> BuildFrontendHandoffPrefixAsync(Guid runId, CancellationToken ct = default);
}

public sealed class FeatureBatchHandoffCoordinator : IFeatureBatchHandoffCoordinator
{
    private readonly IDMailBus _bus;

    public FeatureBatchHandoffCoordinator(IDMailBus bus) => _bus = bus;

    public async Task SendBackendToFrontendAsync(
        Guid runId,
        IReadOnlyList<DomainGeneratedFile> backendFiles,
        CancellationToken ct = default)
    {
        if (backendFiles.Count == 0)
            return;

        var paths = backendFiles
            .Select(f => f.RelativePath)
            .OrderBy(p => p, StringComparer.OrdinalIgnoreCase)
            .Take(48);
        var payload =
            "Backend handoff context:\n" +
            "- Generated backend files:\n" +
            string.Join("\n", paths.Select(p => $"  - {p}")) +
            "\n- Integrate frontend against these API paths and models.";

        await _bus.SendAsync(runId, "backend", "frontend", payload, ackRequired: true, ct).ConfigureAwait(false);
    }

    public async Task<string?> BuildFrontendHandoffPrefixAsync(Guid runId, CancellationToken ct = default)
    {
        var messages = await _bus.ReadAsync(runId, to: "frontend", from: "backend", unackedOnly: true, ct)
            .ConfigureAwait(false);
        if (messages.Count == 0)
            return null;

        var latest = messages[^1];
        return $"[DMail handoff from {latest.From}]\n{latest.Payload}\n\n";
    }
}
