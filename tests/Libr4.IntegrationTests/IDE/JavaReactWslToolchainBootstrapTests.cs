using FluentAssertions;
using Libr4.IDE.Application.AutonomousAppGeneration.Infrastructure;
using Libr4.IDE.Domain.AutonomousAppGeneration;
using Xunit;

namespace Libr4.IntegrationTests.IDE;

public sealed class JavaReactWslToolchainBootstrapTests
{
    [Fact]
    public void ShouldPrepend_ForWslJavaReactPlan()
    {
        var plan = new GenerationPlan(
            "MobileBankingApp",
            "Java Spring Boot backend + React frontend banking app",
            new TechStack(
                ["Java", "TypeScript"],
                ["Spring Boot", "React"],
                ["PostgreSQL"],
                [],
                "banking"),
            [],
            [],
            "eclipse-temurin:21-jdk",
            ["cd backend && mvn -q package"],
            [],
            6);

        JavaReactWslToolchainBootstrap.ShouldPrepend("wsl", plan).Should().BeTrue();
        JavaReactWslToolchainBootstrap.ShouldPrepend("docker", plan).Should().BeFalse();

        var dotnetPlan = new GenerationPlan(
            "Api",
            "dotnet minimal api",
            new TechStack(["C#"], ["ASP.NET"], [], [], "api"),
            [],
            [],
            "mcr.microsoft.com/dotnet/sdk:8.0",
            ["dotnet build"],
            [],
            4);
        JavaReactWslToolchainBootstrap.ShouldPrepend("wsl", dotnetPlan).Should().BeFalse();
    }

    [Fact]
    public void Command_InstallsMavenAndNpmIdempotently()
    {
        JavaReactWslToolchainBootstrap.Command.Should().Contain("command -v mvn");
        JavaReactWslToolchainBootstrap.Command.Should().Contain("command -v npm");
        JavaReactWslToolchainBootstrap.Command.Should().Contain("apt-get install");
        JavaReactWslToolchainBootstrap.Command.Should().Contain("apk add");
    }

    [Fact]
    public void WindowsBootstrap_DownloadsPortableMaven()
    {
        JavaReactWindowsToolchainBootstrap.Command.Should().Contain("apache-maven");
        JavaReactWindowsToolchainBootstrap.Command.Should().Contain("archive.apache.org");
        JavaReactWindowsToolchainBootstrap.Command.Should().Contain("LIBR4_MAVEN_BOOTSTRAP_FAILED");
        JavaReactWindowsToolchainBootstrap.MavenPathExports.Should().Contain("apache-maven\\bin");
        JavaReactWindowsToolchainBootstrap.MavenPathExports.Should().Contain("JAVA_HOME");
    }

    [Fact]
    public void QualifyMavenExecutable_ReplacesBareMvnToken()
    {
        var cmd = "cd backend && mvn -q -DskipTests package";
        JavaReactWindowsToolchainBootstrap.QualifyMavenExecutable(cmd)
            .Should().Contain("mvn.cmd")
            .And.NotContain("&& mvn ");
    }

    [Fact]
    public void IsMavenInvocation_MatchesPackageCommand_NotBootstrap()
    {
        JavaReactWindowsToolchainBootstrap.IsMavenInvocation("cd backend && mvn -q package").Should().BeTrue();
        JavaReactWindowsToolchainBootstrap.IsMavenInvocation(JavaReactWindowsToolchainBootstrap.Command).Should().BeFalse();
    }
}
