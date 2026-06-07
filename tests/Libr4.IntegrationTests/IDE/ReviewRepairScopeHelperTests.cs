using FluentAssertions;
using Libr4.IDE.Application.AutonomousAppGeneration.DiffReview;
using Libr4.IDE.Domain.AutonomousAppGeneration;
using Xunit;

namespace Libr4.IntegrationTests.IDE;

public sealed class ReviewRepairScopeHelperTests
{
    [Fact]
    public void SelectScopedFiles_ReturnsOnlyRejectedPath()
    {
        var all = new List<GeneratedFile>
        {
            new("frontend/App.tsx", "typescript", "a"),
            new("backend/main.py", "python", "b"),
            new("README.md", "markdown", "c"),
        };

        var scoped = ReviewRepairScopeHelper.SelectScopedFiles(all, ["frontend/App.tsx"]);

        scoped.Should().HaveCount(1);
        scoped[0].RelativePath.Should().Be("frontend/App.tsx");
    }

    [Fact]
    public void BuildRepairTask_ListsOnlyScopedPaths()
    {
        var task = ReviewRepairScopeHelper.BuildRepairTask(["frontend/App.tsx"], "fix hook deps");

        task.Should().Contain("frontend/App.tsx");
        task.Should().NotContain("backend/main.py");
        task.Should().Contain("fix hook deps");
        task.Should().Contain("files ONLY");
    }
}
