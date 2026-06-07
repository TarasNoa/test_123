using FluentAssertions;
using Libr4.IDE.Application.AutonomousAppGeneration.Spaces;
using Xunit;

namespace Libr4.IntegrationTests.IDE;

public sealed class SpaceMergeConflictPolicyTests
{
    [Fact]
    public void RequiresHumanResolution_WhenConflictsPresent()
    {
        SpaceMergeConflictPolicy.RequiresHumanResolution(false, ["src/a.cs"])
            .Should().BeTrue();
    }

    [Fact]
    public void DoesNotRequireHumanResolution_WhenMergeSucceeded()
    {
        SpaceMergeConflictPolicy.RequiresHumanResolution(true, [])
            .Should().BeFalse();
    }

    [Fact]
    public void FormatHumanReport_ListsConflictPaths()
    {
        var report = SpaceMergeConflictPolicy.FormatHumanReport(["src/a.cs", "src/b.cs"]);
        report.Should().Contain("src/a.cs");
        report.Should().Contain("resolve conflicts manually");
    }
}
