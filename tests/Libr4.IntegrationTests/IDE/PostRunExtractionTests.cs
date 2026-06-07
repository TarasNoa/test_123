using FluentAssertions;
using Libr4.AI.Application.Abstractions;
using Libr4.IDE.Application.AutonomousAppGeneration.Memory.Extraction;
using Libr4.IDE.Application.AutonomousAppGeneration.Memory.Hermes;
using Libr4.IDE.Application.AutonomousAppGeneration.Services;
using Libr4.IDE.Domain.AutonomousAppGeneration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Libr4.IntegrationTests.IDE;

public sealed class PostRunExtractionTests : IDisposable
{
    private readonly string _dbPath;
    private readonly SqliteHermesMemoryStore _memoryStore;

    public PostRunExtractionTests()
    {
        _dbPath = Path.Combine(Path.GetTempPath(), $"post-run-{Guid.NewGuid():N}.db");
        _memoryStore = new SqliteHermesMemoryStore(
            Options.Create(new HermesMemoryOptions { DbPath = _dbPath }),
            NullLogger<SqliteHermesMemoryStore>.Instance);
    }

    public void Dispose()
    {
        try
        {
            if (File.Exists(_dbPath))
                File.Delete(_dbPath);
        }
        catch
        {
            // best-effort cleanup
        }
    }

    [Fact]
    public void HeuristicExtractor_FailedRun_ProducesFailureLessons()
    {
        var extractor = new HeuristicPostRunExtractor();
        var request = new PostRunExtractionRequest(
            Guid.NewGuid(),
            GenerationStatus.Failed,
            "fp-fail",
            "build_failed: django import error",
            "DjangoApp",
            "djangoapp|python|django",
            ["[tool_use] step=3 tool=apply_patch success=True output=patched settings"],
            new[]
            {
                new ErrorReport("ImportError", "cannot import settings", "fix imports", "backend/settings.py", 4)
            },
            2);

        var result = extractor.Extract(request);

        result.Source.Should().Be("heuristic");
        result.Lessons.Should().Contain(l => l.Kind == MemoryKind.Meta);
        result.Lessons.Should().Contain(l => l.Kind == MemoryKind.Episodic);
        result.Lessons.Should().Contain(l => l.Kind == MemoryKind.Procedural);
    }

    [Fact]
    public async Task ExtractAndIngest_FailedRun_PersistsLessonsToHermes()
    {
        var plan = SamplePlan();
        var orchestrator = AppGenerationOrchestrator.Create("calorie app", "fp-post-run-fail");
        orchestrator.AttachPlan(plan);
        orchestrator.BeginGeneration();
        var iteration = orchestrator.BeginIteration();
        orchestrator.CompleteIteration(
            iteration.Id,
            new ExecutionResult(false, 1, TimeSpan.FromSeconds(3), Array.Empty<ConsoleLogEntry>()),
            new[] { new ErrorReport("SyntaxError", "invalid syntax", "fix syntax", "manage.py", 2) });
        orchestrator.MarkFailed("tests_failed");

        var extractor = CreateExtractor(useLlm: false);
        var result = await extractor.ExtractAndIngestAsync(orchestrator);

        result.Lessons.Should().NotBeEmpty();
        var stored = await _memoryStore.RetrieveAsync(new HermesMemoryQuery("fp-post-run-fail", TopK: 20));
        stored.Should().NotBeEmpty();
        stored.Should().Contain(r => r.Entry.Stage == "post_run");
    }

    [Fact]
    public async Task ExtractAndIngest_CompletedRun_IngestsStrategicLesson()
    {
        var plan = SamplePlan();
        var orchestrator = AppGenerationOrchestrator.Create("calorie app", "fp-post-run-ok");
        orchestrator.AttachPlan(plan);
        orchestrator.BeginGeneration();
        orchestrator.MarkCompleted();

        var extractor = CreateExtractor(useLlm: false);
        var result = await extractor.ExtractAndIngestAsync(orchestrator);

        result.Lessons.Should().Contain(l => l.Kind == MemoryKind.Strategic);
        var stored = await _memoryStore.RetrieveAsync(new HermesMemoryQuery("fp-post-run-ok", TopK: 10));
        stored.Select(r => r.Entry.Kind).Should().Contain(MemoryKind.Strategic);
    }

    [Fact]
    public async Task FinalizationHook_AcceptsCompletedAndFailedRuns()
    {
        var queue = new BoundedPostRunExtractionQueue(new PostRunExtractionOptions { QueueCapacity = 8 });
        var hook = new PostRunExtractionFinalizationHook(
            queue,
            Options.Create(new PostRunExtractionOptions { Enabled = true }));

        var completed = AppGenerationOrchestrator.Create("ok", "fp-ok");
        completed.MarkCompleted();
        var actCompleted = () => hook.ExecuteAsync(completed, CancellationToken.None);

        var failed = AppGenerationOrchestrator.Create("bad", "fp-bad");
        failed.MarkFailed("boom");
        var actFailed = () => hook.ExecuteAsync(failed, CancellationToken.None);

        await actCompleted.Should().NotThrowAsync();
        await actFailed.Should().NotThrowAsync();
    }

    [Fact]
    public void ExtractionQueue_AcceptsEnqueue()
    {
        var queue = new BoundedPostRunExtractionQueue(new PostRunExtractionOptions { QueueCapacity = 4 });
        queue.TryEnqueue(Guid.NewGuid()).Should().BeTrue();
    }

    [Fact]
    public async Task BackgroundService_ProcessesQueuedRun_EndToEnd()
    {
        var runId = Guid.NewGuid();
        var dbPath = Path.Combine(Path.GetTempPath(), $"post-run-bg-{Guid.NewGuid():N}.db");
        var memoryStore = new SqliteHermesMemoryStore(
            Options.Create(new HermesMemoryOptions { DbPath = dbPath }),
            NullLogger<SqliteHermesMemoryStore>.Instance);
        var queue = new BoundedPostRunExtractionQueue(new PostRunExtractionOptions { QueueCapacity = 4 });
        var repository = new InMemoryPostRunRepository(runId);
        var extractor = CreateExtractor(useLlm: false, memoryStore);
        queue.TryEnqueue(runId).Should().BeTrue();

        var services = new ServiceCollection();
        services.AddSingleton<IAppGenerationRepository>(repository);
        services.AddSingleton<IPostRunExtractor>(extractor);
        var provider = services.BuildServiceProvider();

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var service = new PostRunExtractionBackgroundService(
            queue,
            provider,
            NullLogger<PostRunExtractionBackgroundService>.Instance);

        await service.StartAsync(cts.Token);
        await Task.Delay(500, cts.Token);
        await service.StopAsync(CancellationToken.None);

        var stored = await memoryStore.RetrieveAsync(new HermesMemoryQuery("fp-bg-fail", TopK: 10));
        stored.Should().NotBeEmpty();
    }

    private PostRunExtractor CreateExtractor(bool useLlm, SqliteHermesMemoryStore memoryStore)
    {
        var options = Options.Create(new PostRunExtractionOptions
        {
            Enabled = true,
            UseLlmExtractor = useLlm
        });
        var aiServices = new ServiceCollection();
        aiServices.AddSingleton<IAIService>(new StubAiService());
        var aiProvider = aiServices.BuildServiceProvider();
        return new PostRunExtractor(
            new PostRunExtractionRequestBuilder(options),
            new LlmPostRunExtractor(
                aiProvider.GetRequiredService<IServiceScopeFactory>(),
                NullLogger<LlmPostRunExtractor>.Instance),
            new PostRunLessonIngestor(memoryStore, options),
            options,
            NullLogger<PostRunExtractor>.Instance);
    }

    private sealed class InMemoryPostRunRepository : IAppGenerationRepository
    {
        private readonly AppGenerationOrchestrator _orchestrator;

        public InMemoryPostRunRepository(Guid runId)
        {
            var plan = SamplePlan();
            _orchestrator = AppGenerationOrchestrator.Create("bg fail", "fp-bg-fail");
            _orchestrator.AttachPlan(plan);
            _orchestrator.BeginGeneration();
            _orchestrator.MarkFailed("tests_failed");
        }

        public Task<AppGenerationOrchestrator?> GetAsync(Guid id, CancellationToken ct = default) =>
            Task.FromResult<AppGenerationOrchestrator?>(_orchestrator);

        public Task SaveAsync(AppGenerationOrchestrator orchestrator, CancellationToken ct = default) => Task.CompletedTask;

        public Task<IReadOnlyList<AppGenerationOrchestrator>> ListAsync(CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<AppGenerationOrchestrator>>([_orchestrator]);

        public Task<AppGenerationOrchestrator?> FindLatestByFingerprintAsync(string requestFingerprint, CancellationToken ct = default) =>
            Task.FromResult<AppGenerationOrchestrator?>(_orchestrator);

        public Task<IReadOnlyList<AppGenerationOrchestrator>> ListByTenantAsync(string? tenantId, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<AppGenerationOrchestrator>>([_orchestrator]);
    }

    private PostRunExtractor CreateExtractor(bool useLlm) =>
        CreateExtractor(useLlm, _memoryStore);

    private static GenerationPlan SamplePlan() =>
        new(
            "DjangoApp",
            "Calorie tracker",
            new TechStack(["Python"], ["Django"], [], [], "django"),
            Array.Empty<GenerationPhase>(),
            Array.Empty<string>(),
            "python:3.12-slim",
            Array.Empty<string>(),
            Array.Empty<string>());

    private sealed class StubAiService : Libr4.AI.Application.Abstractions.IAIService
    {
        public Task<string> GenerateCompletionAsync(string prompt, string? systemPrompt = null, string? model = null) =>
            Task.FromResult("""{"lessons":[{"key":"llm-lesson","summary":"LLM lesson","kind":"semantic","confidence":0.9}]}""");

        public Task<string> GenerateEmbeddingAsync(string text, string? model = null) => Task.FromResult("[]");
        public Task<string> AnalyzeTextAsync(string text, string analysisType, string? model = null) => Task.FromResult("{}");
        public Task<string> ChatAsync(string message, string? systemPrompt = null, string? model = null) => Task.FromResult("ok");
    }
}
