using Libr4.IDE.Domain.AutonomousAppGeneration;

namespace Libr4.IDE.Application.AutonomousAppGeneration.Memory.Extraction;

public interface IPostRunExtractor
{
    Task<PostRunExtractionResult> ExtractAsync(PostRunExtractionRequest request, CancellationToken ct = default);

    Task<PostRunExtractionResult> ExtractAndIngestAsync(
        AppGenerationOrchestrator orchestrator,
        CancellationToken ct = default);
}
