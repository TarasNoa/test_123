using FluentAssertions;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentIntegration;
using Libr4.IDE.Application.AutonomousAppGeneration.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Libr4.IntegrationTests.IDE;

public sealed class SkillSafetyPolicyServiceTests
{
    [Fact]
    public void EvaluateSkillExecution_ShouldAllowTrustedSkills()
    {
        var service = new SkillSafetyPolicyService(NullLogger<SkillSafetyPolicyService>.Instance);

        var skill = new SkillDefinition(
            "libr4.plan.architect",
            "1.0.0",
            "Architecture planning",
            new[] { "planning" },
            "trusted",
            new[] { "planning" },
            new SkillModelConfig(),
            new SkillRunConfig(),
            new[] { "planner" });

        var decision = service.EvaluateSkillExecution(skill, "planning", "internal");

        decision.IsAllowed.Should().BeTrue();
        decision.RequiredConditions.Should().BeEmpty();
    }

    [Fact]
    public void EvaluateSkillExecution_ShouldAllowReviewRequiredWithConditions()
    {
        var service = new SkillSafetyPolicyService(NullLogger<SkillSafetyPolicyService>.Instance);

        var skill = new SkillDefinition(
            "libr4.generate.phased",
            "1.0.0",
            "Code generation",
            new[] { "generation" },
            "review-required",
            new[] { "generation" },
            new SkillModelConfig(),
            new SkillRunConfig(),
            new[] { "codegen" });

        var decision = service.EvaluateSkillExecution(skill, "generation", "internal");

        decision.IsAllowed.Should().BeTrue();
        decision.RequiredConditions.Should().Contain("human_review_completed");
        decision.RequiredConditions.Should().Contain("execution_logged");
    }

    [Fact]
    public void EvaluateSkillExecution_ShouldAllowSandboxOnlyWithConditions()
    {
        var service = new SkillSafetyPolicyService(NullLogger<SkillSafetyPolicyService>.Instance);

        var skill = new SkillDefinition(
            "libr4.fix.dependency-aware",
            "1.0.0",
            "Fix iteration",
            new[] { "fix" },
            "sandbox-only",
            new[] { "fixing" },
            new SkillModelConfig(),
            new SkillRunConfig(),
            new[] { "codegen" });

        var decision = service.EvaluateSkillExecution(skill, "fixing", "internal");

        decision.IsAllowed.Should().BeTrue();
        decision.RequiredConditions.Should().Contain("sandbox_enabled");
        decision.RequiredConditions.Should().Contain("isolation_enforced");
    }

    [Fact]
    public void EvaluateSkillExecution_ShouldBlockBlockedSkills()
    {
        var service = new SkillSafetyPolicyService(NullLogger<SkillSafetyPolicyService>.Instance);

        var skill = new SkillDefinition(
            "libr4.dangerous.skill",
            "1.0.0",
            "Dangerous skill",
            new[] { "dangerous" },
            "blocked",
            new[] { "never" },
            new SkillModelConfig(),
            new SkillRunConfig(),
            new[] { "dangerous_tool" });

        var decision = service.EvaluateSkillExecution(skill, "planning", "internal");

        decision.IsAllowed.Should().BeFalse();
        decision.Reason.Should().Contain("blocked");
    }

    [Fact]
    public void RequiresReview_ShouldReturnTrueForReviewRequired()
    {
        var service = new SkillSafetyPolicyService(NullLogger<SkillSafetyPolicyService>.Instance);

        var skill = new SkillDefinition(
            "libr4.generate.phased",
            "1.0.0",
            "Code generation",
            new[] { "generation" },
            "review-required",
            new[] { "generation" },
            new SkillModelConfig(),
            new SkillRunConfig(),
            new[] { "codegen" });

        var requires = service.RequiresReview(skill);

        requires.Should().BeTrue();
    }

    [Fact]
    public void RequiresReview_ShouldReturnFalseForTrusted()
    {
        var service = new SkillSafetyPolicyService(NullLogger<SkillSafetyPolicyService>.Instance);

        var skill = new SkillDefinition(
            "libr4.plan.architect",
            "1.0.0",
            "Architecture planning",
            new[] { "planning" },
            "trusted",
            new[] { "planning" },
            new SkillModelConfig(),
            new SkillRunConfig(),
            new[] { "planner" });

        var requires = service.RequiresReview(skill);

        requires.Should().BeFalse();
    }

    [Fact]
    public void RequiresSandbox_ShouldReturnTrueForSandboxOnly()
    {
        var service = new SkillSafetyPolicyService(NullLogger<SkillSafetyPolicyService>.Instance);

        var skill = new SkillDefinition(
            "libr4.fix.dependency-aware",
            "1.0.0",
            "Fix iteration",
            new[] { "fix" },
            "sandbox-only",
            new[] { "fixing" },
            new SkillModelConfig(),
            new SkillRunConfig(RequiresSandbox: true),
            new[] { "codegen" });

        var requires = service.RequiresSandbox(skill);

        requires.Should().BeTrue();
    }

    [Fact]
    public void RequiresSandbox_ShouldReturnTrueWhenRunConfigRequiresSandbox()
    {
        var service = new SkillSafetyPolicyService(NullLogger<SkillSafetyPolicyService>.Instance);

        var skill = new SkillDefinition(
            "libr4.generate.phased",
            "1.0.0",
            "Code generation",
            new[] { "generation" },
            "review-required",
            new[] { "generation" },
            new SkillModelConfig(),
            new SkillRunConfig(RequiresSandbox: true),
            new[] { "codegen" });

        var requires = service.RequiresSandbox(skill);

        requires.Should().BeTrue();
    }

    [Fact]
    public void GetBlockedSkills_ShouldReturnOnlyBlockedSkills()
    {
        var service = new SkillSafetyPolicyService(NullLogger<SkillSafetyPolicyService>.Instance);

        var skills = new[]
        {
            new SkillDefinition("skill1", "1.0.0", "Skill 1", new[] { "tag1" }, "trusted", new[] { "planning" }, new SkillModelConfig(), new SkillRunConfig(), new[] { "tool1" }),
            new SkillDefinition("skill2", "1.0.0", "Skill 2", new[] { "tag2" }, "blocked", new[] { "planning" }, new SkillModelConfig(), new SkillRunConfig(), new[] { "tool2" }),
            new SkillDefinition("skill3", "1.0.0", "Skill 3", new[] { "tag3" }, "review-required", new[] { "planning" }, new SkillModelConfig(), new SkillRunConfig(), new[] { "tool3" }),
            new SkillDefinition("skill4", "1.0.0", "Skill 4", new[] { "tag4" }, "blocked", new[] { "planning" }, new SkillModelConfig(), new SkillRunConfig(), new[] { "tool4" }),
        };

        var blocked = service.GetBlockedSkills(skills);

        blocked.Should().HaveCount(2);
        blocked.Should().Contain("skill2");
        blocked.Should().Contain("skill4");
    }

    [Fact]
    public void ValidateExecutionCompliance_ShouldPassWhenAllConditionsFulfilled()
    {
        var service = new SkillSafetyPolicyService(NullLogger<SkillSafetyPolicyService>.Instance);

        var skill = new SkillDefinition(
            "libr4.generate.phased",
            "1.0.0",
            "Code generation",
            new[] { "generation" },
            "review-required",
            new[] { "generation" },
            new SkillModelConfig(),
            new SkillRunConfig(),
            new[] { "codegen" });

        var fulfilledConditions = new[] { "human_review_completed", "execution_logged" };

        var isCompliant = service.ValidateExecutionCompliance(skill, "generation", "internal", fulfilledConditions);

        isCompliant.Should().BeTrue();
    }

    [Fact]
    public void ValidateExecutionCompliance_ShouldFailWhenConditionsMissing()
    {
        var service = new SkillSafetyPolicyService(NullLogger<SkillSafetyPolicyService>.Instance);

        var skill = new SkillDefinition(
            "libr4.generate.phased",
            "1.0.0",
            "Code generation",
            new[] { "generation" },
            "review-required",
            new[] { "generation" },
            new SkillModelConfig(),
            new SkillRunConfig(),
            new[] { "codegen" });

        var fulfilledConditions = new[] { "human_review_completed" }; // Missing execution_logged

        var isCompliant = service.ValidateExecutionCompliance(skill, "generation", "internal", fulfilledConditions);

        isCompliant.Should().BeFalse();
    }

    [Fact]
    public void ValidateExecutionCompliance_ShouldFailForBlockedSkills()
    {
        var service = new SkillSafetyPolicyService(NullLogger<SkillSafetyPolicyService>.Instance);

        var skill = new SkillDefinition(
            "libr4.dangerous.skill",
            "1.0.0",
            "Dangerous skill",
            new[] { "dangerous" },
            "blocked",
            new[] { "never" },
            new SkillModelConfig(),
            new SkillRunConfig(),
            new[] { "dangerous_tool" });

        var fulfilledConditions = Array.Empty<string>();

        var isCompliant = service.ValidateExecutionCompliance(skill, "planning", "internal", fulfilledConditions);

        isCompliant.Should().BeFalse();
    }

    [Fact]
    public void ValidateExecutionCompliance_ShouldPassForTrustedSkillsWithoutConditions()
    {
        var service = new SkillSafetyPolicyService(NullLogger<SkillSafetyPolicyService>.Instance);

        var skill = new SkillDefinition(
            "libr4.plan.architect",
            "1.0.0",
            "Architecture planning",
            new[] { "planning" },
            "trusted",
            new[] { "planning" },
            new SkillModelConfig(),
            new SkillRunConfig(),
            new[] { "planner" });

        var fulfilledConditions = Array.Empty<string>();

        var isCompliant = service.ValidateExecutionCompliance(skill, "planning", "internal", fulfilledConditions);

        isCompliant.Should().BeTrue();
    }

    [Fact]
    public void ValidateExecutionCompliance_ShouldBeCaseInsensitiveForConditions()
    {
        var service = new SkillSafetyPolicyService(NullLogger<SkillSafetyPolicyService>.Instance);

        var skill = new SkillDefinition(
            "libr4.generate.phased",
            "1.0.0",
            "Code generation",
            new[] { "generation" },
            "review-required",
            new[] { "generation" },
            new SkillModelConfig(),
            new SkillRunConfig(),
            new[] { "codegen" });

        var fulfilledConditions = new[] { "HUMAN_REVIEW_COMPLETED", "EXECUTION_LOGGED" };

        var isCompliant = service.ValidateExecutionCompliance(skill, "generation", "internal", fulfilledConditions);

        isCompliant.Should().BeTrue();
    }
}
