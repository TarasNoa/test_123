using FluentAssertions;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Infrastructure;
using Libr4.IDE.Domain.AutonomousAppGeneration;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Libr4.IntegrationTests.IDE;

public sealed class GenerationWorkspaceStoreTests
{
    [Fact]
    public void Create_MaterializesFiles_AndDisposeCleansUp()
    {
        var store = new GenerationWorkspaceStore(NullLogger<GenerationWorkspaceStore>.Instance);
        var files = new List<GeneratedFile>
        {
            new("backend/app.py", "python", "print('ok')")
        };

        var id = store.Create(files);
        store.TryGetHostPath(id, out var path).Should().BeTrue();
        File.Exists(Path.Combine(path, "backend", "app.py")).Should().BeTrue();

        store.SyncFromFiles(id, new[]
        {
            new GeneratedFile("backend/app.py", "python", "print('updated')"),
            new GeneratedFile("frontend/index.ts", "typescript", "export {}")
        });

        File.ReadAllText(Path.Combine(path, "backend", "app.py")).Should().Contain("updated");
        File.Exists(Path.Combine(path, "frontend", "index.ts")).Should().BeTrue();

        store.Dispose(id);
        store.TryGetHostPath(id, out _).Should().BeFalse();
        Directory.Exists(path).Should().BeFalse();
    }
}
