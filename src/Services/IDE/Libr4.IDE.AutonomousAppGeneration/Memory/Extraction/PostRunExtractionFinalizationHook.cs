using Libr4.IDE.Application.AutonomousAppGeneration.Services;
using Libr4.IDE.Domain.AutonomousAppGeneration;
using Microsoft.Extensions.Options;

namespace Libr4.IDE.Application.AutonomousAppGeneration.Memory.Extraction;

public sealed class PostRunExtractionFinalizationHook : IAutonomousFinalizationHook
{
    private readonly IPostRunExtractionQueue _queue;
    private readonly PostRunExtractionOptions _options;

    public PostRunExtractionFinalizationHook(
        IPostRunExtractionQueue queue,
        IOptions<PostRunExtractionOptions> options)
    {
        _queue = queue;
        _options = options.Value;
    }

    public int Order => 80;

    public string Name => "post_run_extraction";

    public Task ExecuteAsync(AppGenerationOrchestrator orchestrator, CancellationToken ct)
    {
        if (!_options.Enabled)
            return Task.CompletedTask;

        if (orchestrator.Status != GenerationStatus.Completed
            && orchestrator.Status != GenerationStatus.Failed)
        {
            return Task.CompletedTask;
        }

        _queue.TryEnqueue(orchestrator.Id);
        return Task.CompletedTask;
    }
}
