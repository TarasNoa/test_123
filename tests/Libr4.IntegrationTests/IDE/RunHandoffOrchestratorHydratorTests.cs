using FluentAssertions;
using Libr4.IDE.Application.AutonomousAppGeneration.RunHandoff;
using Libr4.IDE.Domain.AutonomousAppGeneration;
using Xunit;

namespace Libr4.IntegrationTests.IDE;

public sealed class RunHandoffOrchestratorHydratorTests
{
    [Fact]
    public void TryHydrate_LoadsWorkspaceFilesFromRunDirectory()
    {
        var root = Path.Combine(Path.GetTempPath(), $"hydrator-{Guid.NewGuid():N}");
        var runId = Guid.NewGuid();
        var workspaceDir = Path.Combine(root, runId.ToString("D"), "workspace", "app");
        Directory.CreateDirectory(workspaceDir);
        File.WriteAllText(Path.Combine(workspaceDir, "main.py"), "print('ok')");

        try
        {
            var orchestrator = RunHandoffOrchestratorHydrator.TryHydrate(runId, root);
            orchestrator.Should().NotBeNull();
            orchestrator!.Id.Should().Be(runId);
            orchestrator.Files.Should().ContainSingle(f => f.RelativePath == "app/main.py");
        }
        finally
        {
            if (Directory.Exists(root))
                Directory.Delete(root, recursive: true);
        }
    }
}
