using FluentAssertions;
using Libr4.IDE.Application.AutonomousAppGeneration.Tooling.Security;
using Libr4.IDE.Application.AutonomousAppGeneration.Tooling.Teams;
using Xunit;

namespace Libr4.IntegrationTests.IDE;

public sealed class TeamTemplateExtendedRoutingTests
{
    [Fact]
    public void Resolver_ShouldMatchSecureAiPlatformTemplate()
    {
        var resolver = new TeamTemplateResolver(
            new InMemoryTeamTemplateRepository(),
            new StaticUserRoleProvider(UserRole.ExternalUser));

        var resolution = resolver.Resolve("Need LLM + RAG ai platform with security audit and guardrails.");

        resolution.Matched.Should().BeTrue();
        resolution.Template.Should().NotBeNull();
        resolution.Template!.Id.Should().Be("secure-ai-platform-team");
        resolution.Template.AgentRoles.Should().Contain("security-auditor");
        resolution.Template.AgentRoles.Should().Contain("llm-engineer");
    }

    [Fact]
    public void Resolver_ShouldMatchDeveloperExperienceAutomationTemplate()
    {
        var resolver = new TeamTemplateResolver(
            new InMemoryTeamTemplateRepository(),
            new StaticUserRoleProvider(UserRole.ExternalUser));

        var resolution = resolver.Resolve("Improve developer experience with release automation and onboarding workflows.");

        resolution.Matched.Should().BeTrue();
        resolution.Template.Should().NotBeNull();
        resolution.Template!.Id.Should().Be("dx-automation-team");
        resolution.Template.AgentRoles.Should().Contain("workflow-automation-engineer");
    }

    [Fact]
    public void Resolver_ShouldRestrictInternalOpsOrchestrationTemplate_ForExternalUsers()
    {
        var externalResolver = new TeamTemplateResolver(
            new InMemoryTeamTemplateRepository(),
            new StaticUserRoleProvider(UserRole.ExternalUser));
        var internalResolver = new TeamTemplateResolver(
            new InMemoryTeamTemplateRepository(),
            new StaticUserRoleProvider(UserRole.InternalDeveloper));

        externalResolver.Resolve("Operations war room for service outage incident").Matched.Should().BeFalse();
        internalResolver.Resolve("Operations war room for service outage incident").Matched.Should().BeTrue();
    }

    [Fact]
    public void Resolver_ShouldMatchFintechPaymentsTemplate()
    {
        var resolver = new TeamTemplateResolver(
            new InMemoryTeamTemplateRepository(),
            new StaticUserRoleProvider(UserRole.ExternalUser));

        var resolution = resolver.Resolve("Build fintech payments ledger with secure billing and PCI controls.");

        resolution.Matched.Should().BeTrue();
        resolution.Template.Should().NotBeNull();
        resolution.Template!.Id.Should().Be("fintech-payments-team");
        resolution.Template.AgentRoles.Should().Contain("fintech-specialist");
    }

    [Fact]
    public void Resolver_ShouldMatchHealthcareComplianceTemplate()
    {
        var resolver = new TeamTemplateResolver(
            new InMemoryTeamTemplateRepository(),
            new StaticUserRoleProvider(UserRole.ExternalUser));

        var resolution = resolver.Resolve("Design healthcare patient platform with FHIR interoperability and HIPAA controls.");

        resolution.Matched.Should().BeTrue();
        resolution.Template.Should().NotBeNull();
        resolution.Template!.Id.Should().Be("healthcare-compliance-team");
        resolution.Template.AgentRoles.Should().Contain("healthcare-systems-engineer");
    }

    [Fact]
    public void Resolver_ShouldMatchMonetizationGrowthTemplate()
    {
        var resolver = new TeamTemplateResolver(
            new InMemoryTeamTemplateRepository(),
            new StaticUserRoleProvider(UserRole.ExternalUser));

        var resolution = resolver.Resolve("Improve monetization with pricing experiments for conversion and retention growth.");

        resolution.Matched.Should().BeTrue();
        resolution.Template.Should().NotBeNull();
        resolution.Template!.Id.Should().Be("monetization-growth-team");
        resolution.Template.AgentRoles.Should().Contain("monetization-specialist");
    }

    [Fact]
    public void Resolver_ShouldRestrictInternalSecurityAuditTemplate_ForExternalUsers()
    {
        var externalResolver = new TeamTemplateResolver(
            new InMemoryTeamTemplateRepository(),
            new StaticUserRoleProvider(UserRole.ExternalUser));
        var internalResolver = new TeamTemplateResolver(
            new InMemoryTeamTemplateRepository(),
            new StaticUserRoleProvider(UserRole.InternalDeveloper));

        externalResolver.Resolve("Conduct vulnerability assessment for security review").Matched.Should().BeFalse();
        internalResolver.Resolve("Conduct vulnerability assessment for security review").Matched.Should().BeTrue();
        internalResolver.Resolve("Conduct vulnerability assessment for security review").Template!.Id.Should().Be("internal-security-audit-team");
    }

    [Fact]
    public void Resolver_ShouldRestrictInternalDataGovernanceTemplate_ForExternalUsers()
    {
        var externalResolver = new TeamTemplateResolver(
            new InMemoryTeamTemplateRepository(),
            new StaticUserRoleProvider(UserRole.ExternalUser));
        var internalResolver = new TeamTemplateResolver(
            new InMemoryTeamTemplateRepository(),
            new StaticUserRoleProvider(UserRole.InternalDeveloper));

        externalResolver.Resolve("Conduct data governance audit with GDPR compliance review").Matched.Should().BeFalse();
        internalResolver.Resolve("Conduct data governance audit with GDPR compliance review").Matched.Should().BeTrue();
        internalResolver.Resolve("Conduct data governance audit with GDPR compliance review").Template!.Id.Should().Be("internal-data-governance-team");
    }
}
