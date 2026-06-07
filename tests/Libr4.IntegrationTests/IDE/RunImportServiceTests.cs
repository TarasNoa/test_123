using FluentAssertions;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Permissions;
using Libr4.IDE.Application.AutonomousAppGeneration.Infrastructure;
using Libr4.IDE.Application.AutonomousAppGeneration.RunHandoff;
using Libr4.IDE.Application.Obscura;
using Libr4.IDE.Domain.AutonomousAppGeneration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Libr4.IntegrationTests.IDE;

public sealed class RunImportServiceTests : IDisposable
{
    private readonly string _runsRoot;
    private readonly string _exportRoot;
    private readonly string _idempotencyRoot;
    private readonly InMemoryAppGenerationRepository _repository = new();

    public RunImportServiceTests()
    {
        var suffix = Guid.NewGuid().ToString("N");
        _runsRoot = Path.Combine(Path.GetTempPath(), $"run-import-{suffix}");
        _exportRoot = Path.Combine(Path.GetTempPath(), $"run-import-export-{suffix}");
        _idempotencyRoot = Path.Combine(Path.GetTempPath(), $"run-import-idem-{suffix}");
        Directory.CreateDirectory(_runsRoot);
        Directory.CreateDirectory(_exportRoot);
        Directory.CreateDirectory(_idempotencyRoot);
    }

    public void Dispose()
    {
        TryDelete(_runsRoot);
        TryDelete(_exportRoot);
        TryDelete(_idempotencyRoot);
    }

    [Fact]
    public async Task ImportAsync_RoundTripFromExport_ReturnsNewRunWithLineage()
    {
        var sourceRunId = await SeedAndExportAsync();
        var export = CreateExportService();
        var exported = await export.ExportAsync(sourceRunId);
        exported.Should().NotBeNull();

        var import = CreateImportService();
        var first = await import.ImportBundleAsync(exported!.ArtifactPath);
        var second = await import.ImportBundleAsync(exported.ArtifactPath);

        first.IdempotentReplay.Should().BeFalse();
        second.IdempotentReplay.Should().BeTrue();
        second.RunId.Should().Be(first.RunId);
        first.SourceRunId.Should().Be(sourceRunId);
        first.LastStepNumber.Should().BeGreaterThan(0);

        var orchestrator = await _repository.GetAsync(first.RunId);
        orchestrator.Should().NotBeNull();
        orchestrator!.Files.Should().Contain(f => f.RelativePath == "app/main.py");

        var lineagePath = Path.Combine(_runsRoot, first.RunId.ToString("D"), "handoff", "lineage.json");
        File.Exists(lineagePath).Should().BeTrue();
    }

    [Fact]
    public async Task ImportAsync_CorruptBundle_ThrowsStructuredError()
    {
        var corruptPath = Path.Combine(Path.GetTempPath(), $"corrupt-{Guid.NewGuid():N}.tar.gz");
        await File.WriteAllTextAsync(corruptPath, "not-a-tarball");

        var import = CreateImportService();
        var act = async () => await import.ImportBundleAsync(corruptPath);
        var ex = await act.Should().ThrowAsync<RunImportException>();
        ex.Which.ErrorCode.Should().Be("bundle_corrupt");

        TryDeleteFile(corruptPath);
    }

    [Fact]
    public async Task UrlRemapper_RewritesLocalhostInArtifacts()
    {
        var runId = Guid.NewGuid();
        var runDir = Path.Combine(_runsRoot, runId.ToString("D"), "verify");
        Directory.CreateDirectory(runDir);
        await File.WriteAllTextAsync(
            Path.Combine(runDir, "verify-report.json"),
            """{"url":"http://localhost:5173/app"}""");

        var remapper = new RunEnvironmentUrlRemapper(new ObscuraNetworkRouter(
            Options.Create(new ObscuraNetworkRouterOptions
            {
                DockerBrowserHost = "host.docker.internal",
                UseDockerHostMapping = true
            })));

        await remapper.RemapRunArtifactsAsync(runId, Path.Combine(_runsRoot, runId.ToString("D")));

        var text = await File.ReadAllTextAsync(Path.Combine(runDir, "verify-report.json"));
        text.Should().Contain("host.docker.internal:5173");
        text.Should().NotContain("localhost");
    }

    private async Task<Guid> SeedAndExportAsync()
    {
        var orchestrator = AppGenerationOrchestrator.Create("import source", "fp-import-source");
        orchestrator.AttachPlan(new GenerationPlan(
            "import-app",
            "desc",
            new TechStack(new[] { "python" }, [], [], [], "import"),
            Array.Empty<GenerationPhase>(),
            Array.Empty<string>(),
            "python:3.12",
            [],
            [],
            5));
        orchestrator.UpsertFile(new GeneratedFile("app/main.py", "python", "print('ok')"));
        await _repository.SaveAsync(orchestrator);

        var runDir = Path.Combine(_runsRoot, orchestrator.Id.ToString("D"));
        Directory.CreateDirectory(runDir);
        await File.WriteAllTextAsync(
            Path.Combine(runDir, "rollout.jsonl"),
            """{"type":"tool_use","stepNumber":5,"toolName":"write_file","success":true}""");

        return orchestrator.Id;
    }

    private RunExportService CreateExportService() =>
        new(
            _repository,
            new RunSessionSnapshotExporter(
                Options.Create(new AgentRuntimeOptions { RunsRoot = _runsRoot, SessionDbPath = Path.Combine(_runsRoot, "sessions.db") }),
                NullLogger<RunSessionSnapshotExporter>.Instance),
            new AgentRunPermissionStore(),
            Options.Create(new AgentRuntimeOptions { RunsRoot = _runsRoot }),
            Options.Create(new RunExportOptions { ExportRootPath = _exportRoot }),
            NullLogger<RunExportService>.Instance);

    private RunImportService CreateImportService() =>
        new(
            _repository,
            new FileRunImportIdempotencyStore(Options.Create(new RunImportOptions { IdempotencyRootPath = _idempotencyRoot })),
            new RunSessionSnapshotImporter(
                Options.Create(new AgentRuntimeOptions { RunsRoot = _runsRoot, SessionDbPath = Path.Combine(_runsRoot, "sessions.db") }),
                NullLogger<RunSessionSnapshotImporter>.Instance),
            new RunEnvironmentUrlRemapper(),
            new AgentRunPermissionStore(),
            Options.Create(new AgentRuntimeOptions { RunsRoot = _runsRoot }),
            Options.Create(new RunImportOptions { IdempotencyRootPath = _idempotencyRoot }),
            NullLogger<RunImportService>.Instance);

    private static void TryDelete(string path)
    {
        try
        {
            if (Directory.Exists(path))
                Directory.Delete(path, recursive: true);
        }
        catch
        {
            // best-effort
        }
    }

    private static void TryDeleteFile(string path)
    {
        try { File.Delete(path); } catch { /* best-effort */ }
    }
}
