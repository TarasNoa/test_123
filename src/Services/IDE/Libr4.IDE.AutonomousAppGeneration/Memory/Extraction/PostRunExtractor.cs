using Libr4.IDE.Domain.AutonomousAppGeneration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Libr4.IDE.Application.AutonomousAppGeneration.Memory.Extraction;

public sealed class PostRunExtractor : IPostRunExtractor
{
    private readonly PostRunExtractionRequestBuilder _builder;
    private readonly LlmPostRunExtractor _llm;
    private readonly HeuristicPostRunExtractor _heuristic;
    private readonly PostRunLessonIngestor _ingestor;
    private readonly PostRunExtractionOptions _options;
    private readonly ILogger<PostRunExtractor> _logger;

    public PostRunExtractor(
        PostRunExtractionRequestBuilder builder,
        LlmPostRunExtractor llm,
        PostRunLessonIngestor ingestor,
        IOptions<PostRunExtractionOptions> options,
        ILogger<PostRunExtractor> logger,
        HeuristicPostRunExtractor? heuristic = null)
    {
        _builder = builder;
        _llm = llm;
        _heuristic = heuristic ?? new HeuristicPostRunExtractor();
        _ingestor = ingestor;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<PostRunExtractionResult> ExtractAsync(PostRunExtractionRequest request, CancellationToken ct = default)
    {
        if (!_options.Enabled)
            return new PostRunExtractionResult(request.RunId, request.Status.ToString(), Array.Empty<PostRunLesson>(), "disabled");

        if (_options.UseLlmExtractor)
            return await _llm.ExtractAsync(request, ct).ConfigureAwait(false);

        return _heuristic.Extract(request);
    }

    public async Task<PostRunExtractionResult> ExtractAndIngestAsync(
        AppGenerationOrchestrator orchestrator,
        CancellationToken ct = default)
    {
        if (!_options.Enabled)
            return new PostRunExtractionResult(orchestrator.Id, orchestrator.Status.ToString(), Array.Empty<PostRunLesson>(), "disabled");

        if (orchestrator.Status != GenerationStatus.Completed && orchestrator.Status != GenerationStatus.Failed)
        {
            _logger.LogDebug("Skipping post-run extraction for non-terminal run {RunId}: {Status}", orchestrator.Id, orchestrator.Status);
            return new PostRunExtractionResult(orchestrator.Id, orchestrator.Status.ToString(), Array.Empty<PostRunLesson>(), "skipped");
        }

        var request = await _builder.BuildAsync(orchestrator, ct).ConfigureAwait(false);
        var result = await ExtractAsync(request, ct).ConfigureAwait(false);
        var ingested = await _ingestor.IngestAsync(result, orchestrator.RequestFingerprint, ct).ConfigureAwait(false);

        _logger.LogInformation(
            "Post-run extraction for {RunId} status={Status} source={Source} lessons={Lessons} ingested={Ingested}",
            orchestrator.Id,
            orchestrator.Status,
            result.Source,
            result.Lessons.Count,
            ingested);

        return result;
    }
}
