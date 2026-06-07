using Libr4.IDE.Application.AutonomousAppGeneration.Memory.Hermes;
using Microsoft.Extensions.Options;

namespace Libr4.IDE.Application.AutonomousAppGeneration.Memory.Extraction;

public sealed class PostRunLessonIngestor
{
    private readonly IHermesMemoryStore _store;
    private readonly PostRunExtractionOptions _options;

    public PostRunLessonIngestor(
        IHermesMemoryStore store,
        IOptions<PostRunExtractionOptions> options)
    {
        _store = store;
        _options = options.Value;
    }

    public async Task<int> IngestAsync(PostRunExtractionResult result, string requestFingerprint, CancellationToken ct = default)
    {
        var ingested = 0;
        foreach (var lesson in result.Lessons.Take(_options.MaxLessonsPerRun))
        {
            await _store.UpsertAsync(
                new HermesMemoryEntry(
                    Guid.NewGuid(),
                    result.RunId,
                    UserId: null,
                    requestFingerprint,
                    lesson.Kind,
                    Stage: "post_run",
                    lesson.Key,
                    lesson.Summary,
                    PayloadJson: $"{{\"source\":\"{result.Source}\",\"status\":\"{result.Status}\"}}",
                    Tokens: Math.Max(1, lesson.Summary.Length / 4),
                    Score: lesson.Confidence,
                    CreatedAtUtc: DateTime.UtcNow),
                ct).ConfigureAwait(false);
            ingested++;
        }

        return ingested;
    }
}
