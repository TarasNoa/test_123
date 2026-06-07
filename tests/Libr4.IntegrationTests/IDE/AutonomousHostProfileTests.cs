using FluentAssertions;
using Libr4.IDE.Application.AutonomousAppGeneration.BatchCi;
using Libr4.IDE.Application.AutonomousAppGeneration.HostProfiles;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Libr4.IntegrationTests.IDE;

public sealed class AutonomousHostProfileTests
{
    [Theory]
    [InlineData("OpenRouter", AutonomousHostProfile.OpenRouter, "OpenRouter", "openrouter")]
    [InlineData("DockerModelRunner", AutonomousHostProfile.DockerModelRunner, "DockerModelRunner", "dockermodelrunner")]
    [InlineData("BatchCi", AutonomousHostProfile.BatchCi, "OpenRouter", "openrouter")]
    [InlineData("Benchmark", AutonomousHostProfile.Benchmark, "DockerModelRunner", "dockermodelrunner")]
    public void DescribeActive_ReflectsProfileOverlay(
        string profileName,
        AutonomousHostProfile expectedProfile,
        string expectedAiProvider,
        string expectedMatrix)
    {
        var config = BuildConfig(profileName);
        var sut = new AutonomousHostProfileService(
            Options.Create(new AutonomousHostProfileOptions { ActiveProfile = expectedProfile }),
            config);

        var descriptor = sut.DescribeActive();

        descriptor.Profile.Should().Be(expectedProfile);
        descriptor.AiDefaultProvider.Should().Be(expectedAiProvider);
        descriptor.ProviderMatrixDefault.Should().Be(expectedMatrix);
    }

    [Fact]
    public void BatchLlmProfileScope_BatchCiProfile_AlwaysUsesBatch()
    {
        var scope = new AutonomousBatchLlmProfileScope(
            Options.Create(new AutonomousBatchLlmProfileOptions { UseBatchLlmProfile = false }),
            Options.Create(new AutonomousHostProfileOptions { ActiveProfile = AutonomousHostProfile.BatchCi }),
            NullLogger<AutonomousBatchLlmProfileScope>.Instance);

        scope.ShouldUseBatchProfile("manual").Should().BeTrue();
    }

    [Fact]
    public void BatchLlmProfileScope_DmrProfile_UsesBatchOnlyForCiTrigger()
    {
        var scope = new AutonomousBatchLlmProfileScope(
            Options.Create(new AutonomousBatchLlmProfileOptions { UseBatchLlmProfile = false }),
            Options.Create(new AutonomousHostProfileOptions { ActiveProfile = AutonomousHostProfile.DockerModelRunner }),
            NullLogger<AutonomousBatchLlmProfileScope>.Instance);

        scope.ShouldUseBatchProfile("nightly-ci").Should().BeTrue();
        scope.ShouldUseBatchProfile("manual").Should().BeFalse();
    }

    private static IConfiguration BuildConfig(string profileName)
    {
        var hostDir = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..", "..", "..", "..", "..",
            "src", "Services", "IDE", "Libr4.IDE.AutonomousAppGeneration.Host"));

        return new ConfigurationBuilder()
            .AddJsonFile(Path.Combine(hostDir, "appsettings.json"), optional: true)
            .AddJsonFile(Path.Combine(hostDir, $"appsettings.Profile.{profileName}.json"), optional: false)
            .Build();
    }
}
