using FluentAssertions;
using Libr4.IDE.Application.AutonomousAppGeneration.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Libr4.IntegrationTests.IDE;

public sealed class PromptContractServiceTests
{
    [Fact]
    public void ValidatePromptOutput_ShouldPassValidJson()
    {
        var service = new PromptContractService(NullLogger<PromptContractService>.Instance);

        var contract = new PromptOutputContract(
            "planning",
            "json",
            new[] { "tasks", "phases" },
            5000,
            null);

        var output = @"{ ""tasks"": [""task1""], ""phases"": [""phase1""] }";

        var result = service.ValidatePromptOutput("planning", output, contract);

        result.IsValid.Should().BeTrue();
        result.ValidationErrors.Should().BeEmpty();
        result.MissingFields.Should().BeEmpty();
    }

    [Fact]
    public void ValidatePromptOutput_ShouldFailMissingRequiredFields()
    {
        var service = new PromptContractService(NullLogger<PromptContractService>.Instance);

        var contract = new PromptOutputContract(
            "planning",
            "json",
            new[] { "tasks", "phases", "priority" },
            5000,
            null);

        var output = @"{ ""tasks"": [""task1""], ""phases"": [""phase1""] }";

        var result = service.ValidatePromptOutput("planning", output, contract);

        result.IsValid.Should().BeFalse();
        result.MissingFields.Should().Contain("priority");
        result.ValidationErrors.Should().Contain(e => e.Contains("missing_required_field"));
    }

    [Fact]
    public void ValidatePromptOutput_ShouldFailInvalidJson()
    {
        var service = new PromptContractService(NullLogger<PromptContractService>.Instance);

        var contract = new PromptOutputContract(
            "planning",
            "json",
            new[] { "tasks" },
            5000,
            null);

        var output = "{ invalid json }";

        var result = service.ValidatePromptOutput("planning", output, contract);

        result.IsValid.Should().BeFalse();
        result.ValidationErrors.Should().Contain(e => e.Contains("invalid_json_format"));
    }

    [Fact]
    public void ValidatePromptOutput_ShouldFailEmptyOutput()
    {
        var service = new PromptContractService(NullLogger<PromptContractService>.Instance);

        var contract = new PromptOutputContract(
            "planning",
            "json",
            new[] { "tasks" },
            5000,
            null);

        var result = service.ValidatePromptOutput("planning", "", contract);

        result.IsValid.Should().BeFalse();
        result.ValidationErrors.Should().Contain("output_is_empty");
    }

    [Fact]
    public void ValidatePromptOutput_ShouldFailExceedsTokenLimit()
    {
        var service = new PromptContractService(NullLogger<PromptContractService>.Instance);

        var contract = new PromptOutputContract(
            "planning",
            "json",
            new[] { "tasks" },
            10,
            null);

        var largeOutput = @"{ ""tasks"": [""" + string.Concat(Enumerable.Repeat("x", 1000)) + @"""] }";

        var result = service.ValidatePromptOutput("planning", largeOutput, contract);

        result.IsValid.Should().BeFalse();
        result.ValidationErrors.Should().Contain(e => e.Contains("exceeds_token_limit"));
    }

    [Fact]
    public void IsWithinTokenBudget_ShouldReturnTrueWhenWithinBudget()
    {
        var service = new PromptContractService(NullLogger<PromptContractService>.Instance);

        var result = service.IsWithinTokenBudget("planning", 100, 500);

        result.Should().BeTrue();
    }

    [Fact]
    public void IsWithinTokenBudget_ShouldReturnFalseWhenExceedsBudget()
    {
        var service = new PromptContractService(NullLogger<PromptContractService>.Instance);

        var result = service.IsWithinTokenBudget("planning", 600, 500);

        result.Should().BeFalse();
    }

    [Fact]
    public void GetTokenBudgetAllocation_ShouldAllocateProportionally()
    {
        var service = new PromptContractService(NullLogger<PromptContractService>.Instance);

        var stages = new[] { "planning", "generation", "fixing" };
        var allocation = service.GetTokenBudgetAllocation("generation", 1000, stages);

        allocation.Stage.Should().Be("generation");
        allocation.AllocatedTokens.Should().BeGreaterThan(0);
        allocation.RemainingTokens.Should().Be(allocation.AllocatedTokens);
    }

    [Fact]
    public void GetTokenBudgetAllocation_ShouldHandleEmptyStages()
    {
        var service = new PromptContractService(NullLogger<PromptContractService>.Instance);

        var allocation = service.GetTokenBudgetAllocation("planning", 1000, Array.Empty<string>());

        allocation.AllocatedTokens.Should().Be(1000);
        allocation.RemainingTokens.Should().Be(1000);
    }

    [Fact]
    public void GetOverflowStrategy_ShouldReturnWithinBudgetWhenNoOverflow()
    {
        var service = new PromptContractService(NullLogger<PromptContractService>.Instance);

        var strategy = service.GetOverflowStrategy("planning", 100, 500);

        strategy.Should().Be("within_budget");
    }

    [Fact]
    public void GetOverflowStrategy_ShouldReturnCompressForSmallOverflow()
    {
        var service = new PromptContractService(NullLogger<PromptContractService>.Instance);

        var strategy = service.GetOverflowStrategy("planning", 550, 500);

        strategy.Should().Be("compress_output");
    }

    [Fact]
    public void GetOverflowStrategy_ShouldReturnTruncateForLargeOverflowInPlanning()
    {
        var service = new PromptContractService(NullLogger<PromptContractService>.Instance);

        var strategy = service.GetOverflowStrategy("planning", 800, 500);

        strategy.Should().Be("truncate_lowest_priority");
    }

    [Fact]
    public void GetOverflowStrategy_ShouldReturnSplitForLargeOverflowInGeneration()
    {
        var service = new PromptContractService(NullLogger<PromptContractService>.Instance);

        var strategy = service.GetOverflowStrategy("generation", 700, 500);

        strategy.Should().Be("split_into_phases");
    }

    [Fact]
    public void GetOverflowStrategy_ShouldReturnDeterministicFallbackForExecution()
    {
        var service = new PromptContractService(NullLogger<PromptContractService>.Instance);

        var strategy = service.GetOverflowStrategy("execution", 700, 500);

        strategy.Should().Be("deterministic_fallback");
    }

    [Fact]
    public void ValidatePromptOutput_ShouldRecordTokensUsed()
    {
        var service = new PromptContractService(NullLogger<PromptContractService>.Instance);

        var contract = new PromptOutputContract(
            "planning",
            "json",
            new[] { "tasks" },
            5000,
            null);

        var output = @"{ ""tasks"": [""task1"", ""task2"", ""task3""] }";

        var result = service.ValidatePromptOutput("planning", output, contract);

        result.TokensUsed.Should().BeGreaterThan(0);
    }

    [Fact]
    public void ValidatePromptOutput_ShouldValidateWithoutRequiredFields()
    {
        var service = new PromptContractService(NullLogger<PromptContractService>.Instance);

        var contract = new PromptOutputContract(
            "planning",
            "json",
            Array.Empty<string>(),
            5000,
            null);

        var output = @"{ ""any"": ""content"" }";

        var result = service.ValidatePromptOutput("planning", output, contract);

        result.IsValid.Should().BeTrue();
    }
}
