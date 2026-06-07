using FluentAssertions;
using Libr4.IDE.Application.AutonomousAppGeneration.Infrastructure;
using Libr4.IDE.Application.AutonomousAppGeneration.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Libr4.IntegrationTests.IDE;

public sealed class ShadowToolchainWarmCacheTests
{
    [Fact]
    public void BuildMavenEnvironmentExports_IncludesSharedLocalRepo_WhenEnabled()
    {
        var cache = new ShadowToolchainWarmCache(
            Options.Create(new ShadowToolchainWarmCacheOptions { Enabled = true }),
            NullLogger<ShadowToolchainWarmCache>.Instance);

        cache.BuildMavenEnvironmentExports()
            .Should().Contain("maven.repo.local")
            .And.Contain(cache.MavenLocalRepositoryPath.Replace('/', '\\'));
    }

    [Fact]
    public void EnrichMavenInvocation_AddsSharedRepoAndBatchFlags()
    {
        var cache = new ShadowToolchainWarmCache(
            Options.Create(new ShadowToolchainWarmCacheOptions { Enabled = true }),
            NullLogger<ShadowToolchainWarmCache>.Instance);

        cache.EnrichMavenInvocation("cd backend && mvn -q -DskipTests package")
            .Should().Contain("maven.repo.local")
            .And.Contain("-B -ntp")
            .And.NotContain(" mvn -q");

        var qualified = cache.EnrichMavenInvocation(
            $"cd backend && \"{cache.ResolveMavenExecutablePath()}\" -B -ntp -DskipTests package");
        qualified.Should().Contain("maven.repo.local");
        qualified.Should().MatchRegex(@"""[^""]*mvn\.cmd"" -Dmaven\.repo\.local=");
    }

    [Fact]
    public void BuildNpmCacheExports_SetsNpmConfigCache()
    {
        var cache = new ShadowToolchainWarmCache(
            Options.Create(new ShadowToolchainWarmCacheOptions { Enabled = true }),
            NullLogger<ShadowToolchainWarmCache>.Instance);

        cache.BuildNpmCacheExports()
            .Should().Contain("npm_config_cache")
            .And.Contain(cache.NpmCachePath.Replace('/', '\\'));
    }
}
