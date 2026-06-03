using FluentAssertions;
using Libr4.IDE.Application.AutonomousAppGeneration.Infrastructure;
using Xunit;

namespace Libr4.IntegrationTests.IDE;

public sealed class RepoBootstrapDetailsParserTests
{
    [Fact]
    public void TryParse_ExtractsCloneUrl_FromJsonProbePayload()
    {
        const string details =
            """
            probe ok {"repo_url":"https://github.com/roovo/obsidian-card-board","clone_url":"https://github.com/roovo/obsidian-card-board.git","repository":"roovo/obsidian-card-board","license":"MIT","git_clone":"git clone https://github.com/roovo/obsidian-card-board.git"}
            """;

        var ok = RepoBootstrapDetailsParser.TryParse(details, out var probe);

        ok.Should().BeTrue();
        probe.CloneUrl.Should().Be("https://github.com/roovo/obsidian-card-board.git");
        probe.Repository.Should().Be("roovo/obsidian-card-board");
        probe.License.Should().Be("MIT");
    }

    [Fact]
    public void TryParse_FallsBackToRepoUrl_WhenCloneUrlMissing()
    {
        const string details = """{"repo_url":"https://github.com/org/demo","license":"apache-2.0"}""";

        var ok = RepoBootstrapDetailsParser.TryParse(details, out var probe);

        ok.Should().BeTrue();
        probe.CloneUrl.Should().Be("https://github.com/org/demo.git");
    }
}
