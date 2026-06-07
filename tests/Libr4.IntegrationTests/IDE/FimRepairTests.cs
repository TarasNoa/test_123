using FluentAssertions;
using Libr4.AI.Application.Abstractions;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentIntegration;
using Libr4.IDE.Application.AutonomousAppGeneration.Infrastructure;
using Libr4.IDE.Application.AutonomousAppGeneration.Infrastructure.Fim;
using Libr4.IDE.Application.AutonomousAppGeneration.Services;
using Libr4.IDE.Domain.AutonomousAppGeneration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Libr4.IntegrationTests.IDE;

public sealed class FimRepairTests
{
    private readonly FimPromptBuilder _builder = new();

    [Fact]
    public void FimPromptBuilder_FormatsHoleMarkerBetweenPrefixAndSuffix()
    {
        var content = string.Join('\n', Enumerable.Range(1, 220).Select(i =>
            i == 150 ? "from meals import Meal  # bad import" : $"# line {i}"));

        _builder.TryBuild("backend/meals/views.py", content, 150, 4, out var prompt).Should().BeTrue();
        var formatted = _builder.FormatLlmPrompt(prompt);
        formatted.Should().Contain(FimPromptBuilder.HoleMarker);
        formatted.Should().Contain("# backend/meals/views.py");
        formatted.IndexOf("# line 145").Should().BeLessThan(formatted.IndexOf(FimPromptBuilder.HoleMarker));
        formatted.IndexOf(FimPromptBuilder.HoleMarker).Should().BeLessThan(formatted.IndexOf("# line 155"));
    }

    [Fact]
    public void FimApplyFill_FixesImportInLargeViewsPy()
    {
        var lines = Enumerable.Range(1, 220).Select(i =>
            i == 150 ? "from meals import Meal" : $"# line {i}").ToList();
        var content = string.Join('\n', lines);

        _builder.TryBuild("backend/meals/views.py", content, 150, 4, out var prompt).Should().BeTrue();
        const string fill = "from .models import Meal";
        _builder.TryApplyFill(content, prompt, fill, out var patched).Should().BeTrue();
        patched.Should().Contain("from .models import Meal");
        patched.Should().NotContain("from meals import Meal");
        patched.Should().Contain("# line 145");
        patched.Should().Contain("# line 155");
    }

    [Fact]
    public void FimOutputApplier_FallsBackToSurgicalPatch_WhenDirectApplyFails()
    {
        var content = string.Join('\n', Enumerable.Range(1, 220).Select(i =>
            i == 150 ? "BAD_IMPORT" : $"# line {i}"));
        _builder.TryBuild("backend/meals/views.py", content, 150, 2, out var prompt).Should().BeTrue();

        var files = new[] { new GeneratedFile("backend/meals/views.py", "python", content) };
        var patches = FimOutputApplier.ApplyOrFallback(files, prompt, "FIXED_IMPORT", _builder);
        patches.Should().HaveCount(1);
        patches[0].Content.Should().Contain("FIXED_IMPORT");
        patches[0].Content.Should().NotContain("BAD_IMPORT");
    }

    [Fact]
    public async Task ClaudeCodeStyleRepair_UsesFimForLargeFileWithErrorLine()
    {
        var content = string.Join('\n', Enumerable.Range(1, 220).Select(i =>
            i == 150 ? "from meals import Meal" : $"# line {i}"));
        var files = new List<GeneratedFile>
        {
            new("backend/meals/views.py", "python", content)
        };
        var root = new ErrorReport(
            "ImportError",
            "cannot import name 'Meal' from 'meals'",
            "Use relative import from .models",
            "backend/meals/views.py",
            150);
        var repairPlan = new CompileRepairPlanner.RepairPlan(
            new[] { root },
            root,
            1,
            1,
            "ImportError at backend/meals/views.py:150",
            "import_error");

        var ai = new StubFimAiService("from .models import Meal");
        var service = new ClaudeCodeStyleRepairService(
            ai,
            new DefaultProviderCapabilityMatrix(
                NullLogger<DefaultProviderCapabilityMatrix>.Instance,
                Options.Create(new ProviderMatrixOptions())),
            _builder,
            NullLogger<ClaudeCodeStyleRepairService>.Instance,
            Options.Create(new AutonomousGenerationOptions()),
            Options.Create(new AutonomousLoopGuardOptions
            {
                UseClaudeCodeStyleRepair = true,
                UseFimRepair = true,
                FimMinFileLines = 200
            }));

        var patches = await service.TryRepairAsync(
            CreatePlan(),
            files,
            repairPlan,
            "ImportError at backend/meals/views.py:150");

        patches.Should().HaveCount(1);
        patches[0].RelativePath.Should().Be("backend/meals/views.py");
        patches[0].Content.Should().Contain("from .models import Meal");
        ai.LastSystemPrompt.Should().Contain("infilling");
    }

    private static GenerationPlan CreatePlan() =>
        new(
            applicationName: "CalorieVision",
            applicationDescription: "Calorie tracker",
            techStack: new TechStack(
                new[] { "Python" },
                new[] { "Django" },
                Array.Empty<string>(),
                Array.Empty<string>(),
                "django"),
            phases: Array.Empty<GenerationPhase>(),
            requiredAgents: Array.Empty<string>(),
            runtimeImage: "python:3.12-slim",
            buildCommands: new[] { "cd backend && pip install -r requirements.txt" },
            testCommands: Array.Empty<string>());

    private sealed class StubFimAiService : IAIService
    {
        private readonly string _fill;

        public StubFimAiService(string fill) => _fill = fill;

        public string? LastSystemPrompt { get; private set; }

        public Task<string> GenerateCompletionAsync(string prompt, string? systemPrompt = null, string? model = null)
        {
            LastSystemPrompt = systemPrompt;
            return Task.FromResult(_fill);
        }

        public Task<string> GenerateEmbeddingAsync(string text, string? model = null) =>
            Task.FromResult(string.Empty);

        public Task<string> AnalyzeTextAsync(string text, string analysisType, string? model = null) =>
            Task.FromResult(string.Empty);

        public Task<string> ChatAsync(string message, string? systemPrompt = null, string? model = null) =>
            Task.FromResult(_fill);
    }
}
