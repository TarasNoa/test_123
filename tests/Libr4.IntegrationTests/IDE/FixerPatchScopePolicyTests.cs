using FluentAssertions;
using Libr4.IDE.Application.AutonomousAppGeneration.Infrastructure;
using Libr4.IDE.Domain.AutonomousAppGeneration;
using Xunit;

namespace Libr4.IntegrationTests.IDE;

public sealed class FixerPatchScopePolicyTests
{
    [Theory]
    [InlineData("./backend/pom.xml", "backend/pom.xml")]
    [InlineData("repo/backend/src/Main.java", "backend/src/Main.java")]
    [InlineData("frontend\\src\\App.tsx", "frontend/src/App.tsx")]
    public void NormalizePatchRelativePath_ShouldStripNoisePrefixes(string input, string expected)
    {
        FixerPatchScopePolicy.NormalizePatchRelativePath(input).Should().Be(expected);
    }

    [Fact]
    public void FilterPatches_ShouldAcceptBackendPaths_WhenStrictScopeEmpty()
    {
        var current = new[]
        {
            new GeneratedFile("backend/pom.xml", "xml", "<project/>"),
            new GeneratedFile("frontend/package.json", "json", "{}")
        };
        var allowed = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "frontend/package.json" };
        var parsed = new[]
        {
            new GeneratedFile("backend/src/main/java/com/app/App.java", "java", "public class App {}")
        };

        var filtered = FixerPatchScopePolicy.FilterPatches(parsed, allowed, current, allowProductTreeFallback: true);

        filtered.Should().ContainSingle();
        filtered[0].RelativePath.Should().StartWith("backend/");
    }
}
