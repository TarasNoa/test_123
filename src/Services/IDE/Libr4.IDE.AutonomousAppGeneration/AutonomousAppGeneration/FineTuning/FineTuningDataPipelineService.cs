using Libr4.IDE.Application.AutonomousAppGeneration.Services;
using Libr4.IDE.Domain.AutonomousAppGeneration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Libr4.IDE.Application.AutonomousAppGeneration.FineTuning;

public sealed class FineTuningDataPipelineService : IFineTuningDataPipelineService
{
    private readonly FineTuningDataPipelineOptions _options;
    private readonly FineTuningQualityFilter _filter;
    private readonly FineTuningDatasetWriter _writer;
    private readonly ILogger<FineTuningDataPipelineService> _logger;

    public FineTuningDataPipelineService(
        IOptions<FineTuningDataPipelineOptions> options,
        FineTuningQualityFilter filter,
        FineTuningDatasetWriter writer,
        ILogger<FineTuningDataPipelineService> logger)
    {
        _options = options.Value;
        _filter = filter;
        _writer = writer;
        _logger = logger;
    }

    public async Task<FineTuningExportResult> ExportRunAsync(
        AppGenerationOrchestrator orchestrator,
        CancellationToken ct = default)
    {
        if (!_options.Enabled)
        {
            return new FineTuningExportResult(
                orchestrator.Id,
                "unknown",
                false,
                null,
                new FineTuningQualityReport(false, 0, false, false, "disabled"));
        }

        var stack = FineTuningStackClassifier.Classify(orchestrator);
        if (stack is "unknown")
        {
            return new FineTuningExportResult(
                orchestrator.Id,
                stack,
                false,
                null,
                new FineTuningQualityReport(false, 0, false, false, "unknown_stack"));
        }

        var example = FineTuningRunExtractor.TryExtract(orchestrator, stack);
        if (example is null)
        {
            return new FineTuningExportResult(
                orchestrator.Id,
                stack,
                false,
                null,
                new FineTuningQualityReport(false, 0, false, false, "not_exportable"));
        }

        var files = orchestrator.Files.Select(f => (f.RelativePath, f.Content));
        var quality = _filter.Evaluate(example, files);
        if (!quality.Passed)
        {
            _logger.LogInformation(
                "Fine-tuning export rejected for run {RunId}: {Reason}",
                orchestrator.Id,
                quality.RejectionReason);
            return new FineTuningExportResult(orchestrator.Id, stack, false, null, quality);
        }

        var path = await _writer.AppendAsync(example, ct).ConfigureAwait(false);
        _logger.LogInformation("Fine-tuning example exported for run {RunId} -> {Path}", orchestrator.Id, path);
        return new FineTuningExportResult(orchestrator.Id, stack, true, path, quality);
    }

    public async Task<FineTuningDatasetBuildResult> BuildDatasetAsync(
        IEnumerable<AppGenerationOrchestrator> runs,
        CancellationToken ct = default)
    {
        var perStack = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var accepted = 0;
        var rejected = 0;
        var processed = 0;

        foreach (var run in runs)
        {
            processed++;
            var result = await ExportRunAsync(run, ct).ConfigureAwait(false);
            if (result.Accepted)
            {
                accepted++;
                perStack[result.Stack] = perStack.GetValueOrDefault(result.Stack) + 1;
            }
            else
            {
                rejected++;
            }
        }

        return new FineTuningDatasetBuildResult(processed, accepted, rejected, perStack);
    }
}

public sealed class FineTuningFinalizationHook : IAutonomousFinalizationHook
{
    private readonly IFineTuningDataPipelineService _pipeline;
    private readonly FineTuningDataPipelineOptions _options;

    public FineTuningFinalizationHook(
        IFineTuningDataPipelineService pipeline,
        IOptions<FineTuningDataPipelineOptions> options)
    {
        _pipeline = pipeline;
        _options = options.Value;
    }

    public int Order => 88;

    public string Name => "fine_tuning_export";

    public Task ExecuteAsync(AppGenerationOrchestrator orchestrator, CancellationToken ct)
    {
        if (!_options.Enabled || !_options.AutoExtractCompletedRuns)
            return Task.CompletedTask;

        if (orchestrator.Status != GenerationStatus.Completed)
            return Task.CompletedTask;

        return _pipeline.ExportRunAsync(orchestrator, ct);
    }
}
