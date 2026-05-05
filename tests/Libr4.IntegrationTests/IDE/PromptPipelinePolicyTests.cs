using FluentAssertions;
using Libr4.IDE.Application.AutonomousAppGeneration.Infrastructure;
using Xunit;

namespace Libr4.IntegrationTests.IDE;

public sealed class PromptPipelinePolicyTests
{
    [Fact]
    public void ApplyInputBudget_ShouldTruncateLongPrompt_WithDeterministicMarker()
    {
        var prompt = new string('a', 80_000);

        var result = PromptPipelinePolicy.ApplyInputBudget("planning", prompt);

        result.Length.Should().BeLessThan(prompt.Length);
        result.Should().Contain("[truncated_by_prompt_budget_policy=true]");
    }

    [Fact]
    public void ValidateOutputContract_Planning_ShouldRejectMalformedPayload()
    {
        const string malformed = "{\"applicationName\":\"App\",\"techStack\":{}}";

        var ok = PromptPipelinePolicy.ValidateOutputContract("planning", malformed, out var reason);

        ok.Should().BeFalse();
        reason.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void ValidateOutputContract_Generation_ShouldAcceptFilesEnvelope()
    {
        const string payload = "{\"files\":[{\"relativePath\":\"src/a.cs\",\"content\":\"class A {}\"}]}";

        var ok = PromptPipelinePolicy.ValidateOutputContract("generation", payload, out var reason);

        ok.Should().BeTrue();
        reason.Should().BeEmpty();
    }
}

