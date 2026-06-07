using FluentAssertions;
using Libr4.IDE.Application.AutonomousAppGeneration.Infrastructure.Cpp;
using Libr4.IDE.Application.AutonomousAppGeneration.Services.Analysis;
using Libr4.IDE.Domain.AutonomousAppGeneration;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Libr4.IntegrationTests.IDE;

public sealed class CppTreeSitterBridgeSmokeTests
{
    private const string SamplePython = """
        # TODO: wire auth middleware
        def greet(name: str) -> str:
            if name:
                return f"hello {name}"
            return "hello"
        """;

    [Fact]
    public void IsAvailable_DoesNotThrow()
    {
        var act = () => _ = CppTreeSitterBridge.IsAvailable;
        act.Should().NotThrow();
    }

    [Fact]
    public void TryAnalyzeFile_WhenNativePresent_DetectsTodoAndFunctions()
    {
        if (!CppTreeSitterBridge.IsAvailable)
            return;

        var ok = CppTreeSitterBridge.TryAnalyzeFile(
            "backend/services/auth_service.py",
            SamplePython,
            NullLogger.Instance,
            out var result);

        ok.Should().BeTrue();
        result.Should().NotBeNull();
        result!.Complexity.Should().NotBeNull();
        result.Complexity!.FunctionCount.Should().BeGreaterOrEqualTo(1);
        result.Placeholders.Should().Contain(p =>
            p.Type.Contains("TODO", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task CppTreeSitterAnalysisSidecar_WhenNativePresent_ReturnsStructuredResults()
    {
        if (!CppTreeSitterBridge.IsAvailable)
            return;

        var sidecar = new CppTreeSitterAnalysisSidecar(NullLogger<CppTreeSitterAnalysisSidecar>.Instance);
        (await sidecar.IsHealthyAsync()).Should().BeTrue();

        var result = await sidecar.AnalyzeAsync([
            new GeneratedFile("backend/auth.py", "python", SamplePython)
        ]);

        result.Error.Should().BeNull();
        result.Results.Should().HaveCount(1);
        result.Results[0].Placeholders.Should().NotBeEmpty();
    }
}
