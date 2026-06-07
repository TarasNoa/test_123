using FluentAssertions;
using Libr4.IDE.Application.AutonomousAppGeneration.Context.RepoGraph;
using Libr4.IDE.Application.AutonomousAppGeneration.Infrastructure.Cpp;
using Xunit;

namespace Libr4.IntegrationTests.IDE;

public sealed class CppLibClangBridgeSmokeTests
{
    private const string SampleCpp = """
        #include "utils/helper.h"
        #include <vector>

        int add(int a, int b) {
            return a + b;
        }

        class Widget {
        public:
            void run() {}
        };
        """;

    [Fact]
    public void IsAvailable_DoesNotThrow()
    {
        var act = () => _ = CppLibClangBridge.IsAvailable;
        act.Should().NotThrow();
    }

    [Fact]
    public void TryParseIncludes_WhenNativePresent_DetectsLocalAndSystemIncludes()
    {
        if (!CppLibClangBridge.IsAvailable)
            return;

        var ok = CppLibClangBridge.TryParseIncludes(
            "src/main.cpp",
            SampleCpp,
            Microsoft.Extensions.Logging.Abstractions.NullLogger.Instance,
            out var includes,
            out var functionCount,
            out var lines);

        ok.Should().BeTrue();
        includes.Should().NotBeEmpty();
        includes.Should().Contain(i => i.Contains("helper", StringComparison.OrdinalIgnoreCase));
        functionCount.Should().BeGreaterOrEqualTo(1);
        lines.Should().BeGreaterThan(5);
    }

    [Fact]
    public void RepoGraphBuilder_WhenNativePresent_LinksCppIncludes()
    {
        if (!CppLibClangBridge.IsAvailable)
            return;

        var builder = new RepoGraphBuilder();
        var paths = new[] { "src/main.cpp", "src/utils/helper.h" };
        var contents = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["src/main.cpp"] = SampleCpp,
            ["src/utils/helper.h"] = "#pragma once\nvoid helper();"
        };

        var graph = builder.Build(paths, contents);
        graph.Edges.Should().Contain(e =>
            e.FromPath.Equals("src/main.cpp", StringComparison.OrdinalIgnoreCase)
            && e.ToPath.Contains("helper", StringComparison.OrdinalIgnoreCase));
    }
}
