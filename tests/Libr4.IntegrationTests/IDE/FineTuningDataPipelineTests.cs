using FluentAssertions;
using Libr4.IDE.Application.AutonomousAppGeneration.FineTuning;
using Libr4.IDE.Domain.AutonomousAppGeneration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Libr4.IntegrationTests.IDE;

public sealed class FineTuningDataPipelineTests : IDisposable
{
    private readonly string _root;
    private readonly FineTuningDataPipelineService _service;

    public FineTuningDataPipelineTests()
    {
        _root = Path.Combine(Path.GetTempPath(), $"ft-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_root);

        var options = Options.Create(new FineTuningDataPipelineOptions
        {
            Enabled = true,
            AutoExtractCompletedRuns = true,
            DatasetsRoot = Path.Combine(_root, "datasets"),
            SignaturesIndexPath = Path.Combine(_root, "signatures.jsonl"),
            MinReadabilityScore = 0.1,
            MinOutputChars = 100
        });

        _service = new FineTuningDataPipelineService(
            options,
            new FineTuningQualityFilter(options),
            new FineTuningDatasetWriter(options),
            NullLogger<FineTuningDataPipelineService>.Instance);
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_root))
                Directory.Delete(_root, recursive: true);
        }
        catch
        {
            // best effort
        }
    }

    [Fact]
    public void StackClassifier_DetectsDjangoFromFiles()
    {
        var run = CompletedRun("django");
        FineTuningStackClassifier.Classify(run).Should().Be("django");
    }

    [Fact]
    public async Task ExportRun_CompletedDjangoRun_WritesJsonlDataset()
    {
        var run = CompletedRun("django");
        var result = await _service.ExportRunAsync(run);

        result.Accepted.Should().BeTrue();
        result.DatasetPath.Should().NotBeNull();
        File.Exists(result.DatasetPath!).Should().BeTrue();

        var line = await File.ReadAllTextAsync(result.DatasetPath!);
        line.Should().Contain("\"instruction\"");
        line.Should().Contain("\"output\"");
        line.Should().Contain("manage.py");
    }

    [Fact]
    public async Task ExportRun_DuplicateRun_IsRejectedByMinHash()
    {
        var run = CompletedRun("django");
        var first = await _service.ExportRunAsync(run);
        first.Accepted.Should().BeTrue();

        var duplicate = CompletedRun("django");
        var second = await _service.ExportRunAsync(duplicate);
        second.Accepted.Should().BeFalse();
        second.Quality.Duplicate.Should().BeTrue();
    }

    [Fact]
    public void SyntaxValidator_RejectsUnbalancedCSharp()
    {
        FineTuningSyntaxValidator.ValidateFile("Program.cs", "class A { void M() {").Should().BeFalse();
    }

    private static AppGenerationOrchestrator CompletedRun(string stack)
    {
        var run = AppGenerationOrchestrator.Create("build calorie tracker api", "fp-ft");
        run.AttachPlan(SamplePlan(stack));
        run.BeginGeneration();

        if (stack == "django")
        {
            run.UpsertFile(new GeneratedFile(
                "manage.py",
                "python",
                """
                #!/usr/bin/env python
                import os

                def main():
                    print("ok")

                if __name__ == "__main__":
                    main()
                """));
            run.UpsertFile(new GeneratedFile(
                "app/views.py",
                "python",
                """
                from django.http import JsonResponse

                def health(_request):
                    # health endpoint
                    return JsonResponse({"status": "ok"})
                """));
        }

        run.MarkCompleted();
        return run;
    }

    private static GenerationPlan SamplePlan(string stack) =>
        stack switch
        {
            "django" => new GenerationPlan(
                "CalorieApp",
                "django api",
                new TechStack(["Python"], ["Django"], ["PostgreSQL"], ["Docker"], "django stack"),
                Array.Empty<GenerationPhase>(),
                ["CodeGenerationAgent"],
                "python:3.12-slim",
                ["pip install -r requirements.txt"],
                ["pytest"],
                8),
            _ => new GenerationPlan(
                "App",
                "app",
                new TechStack(["C#"], [".NET"], [], [], "dotnet"),
                Array.Empty<GenerationPhase>(),
                ["CodeGenerationAgent"],
                "mcr.microsoft.com/dotnet/sdk:8.0",
                ["dotnet build"],
                ["dotnet test"],
                8)
        };
}
