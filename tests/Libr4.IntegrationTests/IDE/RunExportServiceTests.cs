using System.Formats.Tar;
using System.IO.Compression;
using System.Text.Json;
using FluentAssertions;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Permissions;
using Libr4.IDE.Application.AutonomousAppGeneration.Infrastructure;
using Libr4.IDE.Application.AutonomousAppGeneration.RunHandoff;
using Libr4.IDE.Application.AutonomousAppGeneration.Tooling.Flow;
using Libr4.IDE.Domain.AutonomousAppGeneration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Libr4.IntegrationTests.IDE;

public sealed class RunExportServiceTests : IDisposable
{
    private readonly string _runsRoot;
    private readonly string _exportRoot;
    private readonly InMemoryAppGenerationRepository _repository = new();

    public RunExportServiceTests()
    {
        _runsRoot = Path.Combine(Path.GetTempPath(), $"run-export-{Guid.NewGuid():N}");
        _exportRoot = Path.Combine(Path.GetTempPath(), $"run-export-out-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_runsRoot);
        Directory.CreateDirectory(_exportRoot);
    }

    public void Dispose()
    {
        TryDelete(_runsRoot);
        TryDelete(_exportRoot);
    }

    [Fact]
    public async Task ExportAsync_CreatesContentAddressedBundleWithManifestAndWorkspace()
    {
        var runId = await SeedRunAsync();

        var service = CreateService();
        var result = await service.ExportAsync(runId);

        result.Should().NotBeNull();
        result!.RunId.Should().Be(runId);
        result.ContentSha256.Should().HaveLength(64);
        File.Exists(result.ArtifactPath).Should().BeTrue();
        Path.GetFileName(result.ArtifactPath).Should().Be($"{result.ContentSha256}.tar.gz");
        result.DownloadPath.Should().Contain("/export/");
        result.BundleBytes.Should().BeGreaterThan(0);

        var download = await service.TryResolveDownloadAsync(runId, result.ExportId);
        download.Should().NotBeNull();
        download!.Value.Path.Should().Be(result.ArtifactPath);

        await using var bundle = File.OpenRead(result.ArtifactPath);
        await using var gzip = new GZipStream(bundle, CompressionMode.Decompress);
        using var reader = new TarReader(gzip);

        var entries = new List<string>();
        while (reader.GetNextEntry() is { } entry)
            entries.Add(entry.Name);

        entries.Should().Contain(e => e.EndsWith("run-manifest.json", StringComparison.Ordinal));
        entries.Should().Contain(e => e.EndsWith("workspace.tar.gz", StringComparison.Ordinal));
        entries.Should().Contain(e => e.Contains("run-artifacts/rollout.jsonl", StringComparison.Ordinal));
        entries.Should().Contain(e => e.EndsWith("handoff/permissions.json", StringComparison.Ordinal));
    }

    [Fact]
    public async Task ExportAsync_ExcludesNodeModulesFromWorkspaceTarball()
    {
        var runId = await SeedRunAsync(includeNodeModules: true);
        var service = CreateService();

        var result = await service.ExportAsync(runId);
        result.Should().NotBeNull();

        await using var bundle = File.OpenRead(result!.ArtifactPath);
        await using var gzip = new GZipStream(bundle, CompressionMode.Decompress);
        using var reader = new TarReader(gzip);

        while (reader.GetNextEntry() is { } entry)
        {
            if (!entry.Name.EndsWith("workspace.tar.gz", StringComparison.Ordinal))
                continue;

            await using var wsStream = entry.DataStream!;
            await using var wsGzip = new GZipStream(wsStream, CompressionMode.Decompress);
            using var wsReader = new TarReader(wsGzip);
            while (wsReader.GetNextEntry() is { } wsEntry)
                wsEntry.Name.Should().NotContain("node_modules");
            return;
        }

        throw new InvalidOperationException("workspace.tar.gz not found in bundle");
    }

    [Fact]
    public async Task ExportAsync_ReturnsNull_WhenRunMissing()
    {
        var service = CreateService();
        var result = await service.ExportAsync(Guid.NewGuid());
        result.Should().BeNull();
    }

    [Fact]
    public async Task TryResolveDownloadAsync_RejectsExpiredExport()
    {
        var runId = await SeedRunAsync();
        var service = CreateService();
        var result = await service.ExportAsync(runId);
        result.Should().NotBeNull();

        var sidecar = Path.Combine(_exportRoot, $"{result!.ExportId}.manifest.json");
        using (var doc = JsonDocument.Parse(await File.ReadAllTextAsync(sidecar)))
        {
            using var stream = new MemoryStream();
            using (var writer = new Utf8JsonWriter(stream))
            {
                writer.WriteStartObject();
                foreach (var prop in doc.RootElement.EnumerateObject())
                {
                    if (prop.NameEquals("ExportedAtUtc") || prop.NameEquals("exportedAtUtc"))
                        writer.WriteString(prop.Name, DateTime.UtcNow.AddDays(-8));
                    else
                        prop.WriteTo(writer);
                }
                writer.WriteEndObject();
            }

            await File.WriteAllTextAsync(sidecar, System.Text.Encoding.UTF8.GetString(stream.ToArray()));
        }

        var download = await service.TryResolveDownloadAsync(runId, result.ExportId);
        download.Should().BeNull();
    }

    [Fact]
    public async Task PruneExpiredExports_RemovesOldBundlesAndSidecars()
    {
        var runId = await SeedRunAsync();
        var service = CreateService();
        var result = await service.ExportAsync(runId);
        result.Should().NotBeNull();

        var sidecar = Path.Combine(_exportRoot, $"{result!.ExportId}.manifest.json");
        using (var doc = JsonDocument.Parse(await File.ReadAllTextAsync(sidecar)))
        {
            using var stream = new MemoryStream();
            using (var writer = new Utf8JsonWriter(stream))
            {
                writer.WriteStartObject();
                foreach (var prop in doc.RootElement.EnumerateObject())
                {
                    if (prop.NameEquals("ExportedAtUtc") || prop.NameEquals("exportedAtUtc"))
                        writer.WriteString(prop.Name, DateTime.UtcNow.AddDays(-10));
                    else
                        prop.WriteTo(writer);
                }
                writer.WriteEndObject();
            }

            await File.WriteAllTextAsync(sidecar, System.Text.Encoding.UTF8.GetString(stream.ToArray()));
        }

        var removed = service.PruneExpiredExports();
        removed.Should().BeGreaterThan(0);
        File.Exists(result.ArtifactPath).Should().BeFalse();
        File.Exists(sidecar).Should().BeFalse();
    }

    private RunExportService CreateService() =>
        new(
            _repository,
            new RunSessionSnapshotExporter(
                Options.Create(new AgentRuntimeOptions { RunsRoot = _runsRoot, SessionDbPath = Path.Combine(_runsRoot, "sessions.db") }),
                NullLogger<RunSessionSnapshotExporter>.Instance),
            new AgentRunPermissionStore(),
            Options.Create(new AgentRuntimeOptions { RunsRoot = _runsRoot }),
            Options.Create(new RunExportOptions
            {
                ExportRootPath = _exportRoot,
                MaxBundleBytes = 512 * 1024 * 1024,
                RetentionDays = 7
            }),
            NullLogger<RunExportService>.Instance,
            new FileFlowProgressStore(Options.Create(new FlowEngineOptions { RunsRoot = _runsRoot })));

    private async Task<Guid> SeedRunAsync(bool includeNodeModules = false)
    {
        var orchestrator = AppGenerationOrchestrator.Create("export test", "fp-export");
        orchestrator.AttachPlan(new GenerationPlan(
            applicationName: "export-app",
            applicationDescription: "desc",
            techStack: new TechStack(["python"], ["fastapi"], [], [], "fastapi"),
            phases: Array.Empty<GenerationPhase>(),
            requiredAgents: Array.Empty<string>(),
            runtimeImage: "python:3.12",
            buildCommands: ["pip install -r requirements.txt"],
            testCommands: ["pytest"],
            maxIterations: 2));
        orchestrator.UpsertFile(new GeneratedFile("app/main.py", "python", "print('ok')"));
        await _repository.SaveAsync(orchestrator);

        var runDir = Path.Combine(_runsRoot, orchestrator.Id.ToString("D"));
        Directory.CreateDirectory(runDir);
        await File.WriteAllTextAsync(
            Path.Combine(runDir, "rollout.jsonl"),
            """{"type":"tool_use","stepNumber":1,"toolName":"write_file","success":true}""");

        if (includeNodeModules)
            orchestrator.UpsertFile(new GeneratedFile("node_modules/pkg/index.js", "javascript", "export {}"));

        var flowDir = Path.Combine(runDir, "flow");
        Directory.CreateDirectory(flowDir);
        await File.WriteAllTextAsync(
            Path.Combine(flowDir, "flow-state.json"),
            $$"""{"RunId":"{{orchestrator.Id:D}}","FlowName":"default","CurrentNodeId":"verify","Status":"running","Nodes":[],"UpdatedAtUtc":"2026-01-01T00:00:00Z"}""");

        return orchestrator.Id;
    }

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
}
