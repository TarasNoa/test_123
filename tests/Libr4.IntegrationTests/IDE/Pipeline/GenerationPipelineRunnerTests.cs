using FluentAssertions;
using Libr4.IDE.Application.AutonomousAppGeneration.Services.Pipeline;
using Libr4.IDE.Domain.AutonomousAppGeneration;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Libr4.IntegrationTests.IDE.Pipeline;

public sealed class GenerationPipelineRunnerTests
{
    [Fact]
    public async Task RunAsync_NoStages_ReturnsSuccess()
    {
        var runner = new DefaultGenerationPipelineRunner(
            Array.Empty<IGenerationStage>(),
            NullLogger<DefaultGenerationPipelineRunner>.Instance);
        var ctx = MakeContext();

        var outcome = await runner.RunAsync(ctx, CancellationToken.None);

        outcome.Succeeded.Should().BeTrue();
        outcome.ShortCircuited.Should().BeFalse();
        outcome.ExecutedStageNames.Should().BeEmpty();
    }

    [Fact]
    public async Task RunAsync_StagesRunInOrderAscending()
    {
        var executed = new List<string>();
        var runner = new DefaultGenerationPipelineRunner(
            new IGenerationStage[]
            {
                new RecordingStage("c", 30, executed),
                new RecordingStage("a", 10, executed),
                new RecordingStage("b", 20, executed),
            },
            NullLogger<DefaultGenerationPipelineRunner>.Instance);
        var ctx = MakeContext();

        var outcome = await runner.RunAsync(ctx, CancellationToken.None);

        outcome.Succeeded.Should().BeTrue();
        executed.Should().Equal("a", "b", "c");
        outcome.ExecutedStageNames.Should().Equal("a", "b", "c");
    }

    [Fact]
    public async Task RunAsync_StageStops_HaltsPipelineWithReason()
    {
        var executed = new List<string>();
        var runner = new DefaultGenerationPipelineRunner(
            new IGenerationStage[]
            {
                new RecordingStage("a", 10, executed),
                new StoppingStage("b", 20, "synthetic_failure", executed),
                new RecordingStage("c", 30, executed),
            },
            NullLogger<DefaultGenerationPipelineRunner>.Instance);
        var ctx = MakeContext();

        var outcome = await runner.RunAsync(ctx, CancellationToken.None);

        outcome.Succeeded.Should().BeFalse();
        outcome.FailureReason.Should().Be("synthetic_failure");
        outcome.FailedStageName.Should().Be("b");
        executed.Should().Equal("a", "b");
        ctx.FailureReason.Should().Be("synthetic_failure");
    }

    [Fact]
    public async Task RunAsync_StageShortCircuits_HaltsAsSuccess()
    {
        var executed = new List<string>();
        var runner = new DefaultGenerationPipelineRunner(
            new IGenerationStage[]
            {
                new RecordingStage("a", 10, executed),
                new ShortCircuitStage("b", 20, executed),
                new RecordingStage("c", 30, executed),
            },
            NullLogger<DefaultGenerationPipelineRunner>.Instance);
        var ctx = MakeContext();

        var outcome = await runner.RunAsync(ctx, CancellationToken.None);

        outcome.Succeeded.Should().BeTrue();
        outcome.ShortCircuited.Should().BeTrue();
        outcome.FailedStageName.Should().BeNull();
        executed.Should().Equal("a", "b");
    }

    [Fact]
    public async Task RunAsync_StageThrows_HaltsWithStageExceptionFailureReason()
    {
        var runner = new DefaultGenerationPipelineRunner(
            new IGenerationStage[] { new ThrowingStage("a", 10) },
            NullLogger<DefaultGenerationPipelineRunner>.Instance);
        var ctx = MakeContext();

        var outcome = await runner.RunAsync(ctx, CancellationToken.None);

        outcome.Succeeded.Should().BeFalse();
        outcome.FailedStageName.Should().Be("a");
        outcome.FailureReason.Should().StartWith("stage_exception:a:InvalidOperationException");
        ctx.FailureReason.Should().StartWith("stage_exception:");
    }

    [Fact]
    public async Task RunAsync_Cancellation_PropagatesOperationCancelledException()
    {
        var runner = new DefaultGenerationPipelineRunner(
            new IGenerationStage[] { new RecordingStage("a", 10, new List<string>()) },
            NullLogger<DefaultGenerationPipelineRunner>.Instance);
        var ctx = MakeContext();
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        Func<Task> act = () => runner.RunAsync(ctx, cts.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    private static GenerationContext MakeContext()
    {
        var orch = AppGenerationOrchestrator.Create("test request", "fp-test");
        return new GenerationContext
        {
            Orchestrator = orch,
            UserRequest = "test request",
            RequestedMaxIterations = 5,
            Fingerprint = "fp-test"
        };
    }

    private sealed class RecordingStage : IGenerationStage
    {
        private readonly List<string> _bag;
        public RecordingStage(string name, int order, List<string> bag) { Name = name; Order = order; _bag = bag; }
        public string Name { get; }
        public int Order { get; }
        public Task<StageOutcome> ExecuteAsync(GenerationContext context, CancellationToken ct)
        {
            _bag.Add(Name);
            return Task.FromResult(StageOutcome.Continue);
        }
    }

    private sealed class StoppingStage : IGenerationStage
    {
        private readonly List<string> _bag;
        private readonly string _reason;
        public StoppingStage(string name, int order, string reason, List<string> bag)
        { Name = name; Order = order; _reason = reason; _bag = bag; }
        public string Name { get; }
        public int Order { get; }
        public Task<StageOutcome> ExecuteAsync(GenerationContext context, CancellationToken ct)
        {
            _bag.Add(Name);
            return Task.FromResult(StageOutcome.Stop(_reason));
        }
    }

    private sealed class ShortCircuitStage : IGenerationStage
    {
        private readonly List<string> _bag;
        public ShortCircuitStage(string name, int order, List<string> bag) { Name = name; Order = order; _bag = bag; }
        public string Name { get; }
        public int Order { get; }
        public Task<StageOutcome> ExecuteAsync(GenerationContext context, CancellationToken ct)
        {
            _bag.Add(Name);
            return Task.FromResult(StageOutcome.ShortCircuitSuccess);
        }
    }

    private sealed class ThrowingStage : IGenerationStage
    {
        public ThrowingStage(string name, int order) { Name = name; Order = order; }
        public string Name { get; }
        public int Order { get; }
        public Task<StageOutcome> ExecuteAsync(GenerationContext context, CancellationToken ct)
            => throw new InvalidOperationException("simulated stage failure");
    }
}
