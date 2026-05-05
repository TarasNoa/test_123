using Libr4.IDE.Application.AutonomousAppGeneration.Tooling.Security;

namespace Libr4.IDE.Application.AutonomousAppGeneration.Tooling.Teams;

public sealed class InMemoryTeamTemplateRepository : ITeamTemplateRepository
{
    private readonly Dictionary<string, TeamTemplateDefinition> _templates = new(StringComparer.OrdinalIgnoreCase);

    public void Upsert(TeamTemplateDefinition definition)
    {
        if (string.IsNullOrWhiteSpace(definition.Id))
            throw new ArgumentException("Team template id is required.", nameof(definition));
        _templates[definition.Id] = definition;
    }

    public TeamTemplateDefinition? Get(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
            return null;
        return _templates.TryGetValue(id, out var value) ? value : null;
    }

    public IReadOnlyList<TeamTemplateDefinition> List() =>
        _templates.Values.OrderBy(x => x.Id, StringComparer.OrdinalIgnoreCase).ToArray();
}

public sealed class TeamTemplateResolver : ITeamTemplateResolver
{
    private static readonly TeamTemplateDefinition[] BuiltInTemplates =
    {
        new(
            Id: "graphql-api-team",
            Name: "GraphQL API Team",
            Description: "Predefined team for GraphQL-first API delivery.",
            AgentRoles: new[] { "graphql-architect", "backend-developer", "api-designer" },
            SkillPackIds: new[] { "web-development-pack" },
            TriggerKeywords: new[] { "graphql", "resolver", "schema" },
            MinimumRole: UserRole.ExternalUser),
        new(
            Id: "event-driven-backend-team",
            Name: "Event-driven Backend Team",
            Description: "Predefined team for queue/worker based backend stacks.",
            AgentRoles: new[] { "backend-developer", "microservices-architect", "observability-agent" },
            SkillPackIds: new[] { "devops-pack" },
            TriggerKeywords: new[] { "celery", "queue", "worker", "redis" },
            MinimumRole: UserRole.ExternalUser),
        new(
            Id: "secure-ai-platform-team",
            Name: "Secure AI Platform Team",
            Description: "Team for AI platforms with security and governance requirements.",
            AgentRoles: new[] { "security-auditor", "llm-engineer", "ai-safety-reviewer" },
            SkillPackIds: new[] { "devops-pack", "web-development-pack", "security-pack" },
            TriggerKeywords: new[] { "ai platform", "llm", "rag", "security audit", "guardrails" },
            MinimumRole: UserRole.ExternalUser),
        new(
            Id: "dx-automation-team",
            Name: "Developer Experience Automation Team",
            Description: "Team for build acceleration, docs and workflow automation.",
            AgentRoles: new[] { "developer-experience-engineer", "build-systems-engineer", "workflow-automation-engineer" },
            SkillPackIds: new[] { "devops-pack" },
            TriggerKeywords: new[] { "developer experience", "dx", "release automation", "onboarding", "workflow automation" },
            MinimumRole: UserRole.ExternalUser),
        new(
            Id: "research-ops-team",
            Name: "Research Ops Team",
            Description: "Team for research-heavy discovery and experiment design.",
            AgentRoles: new[] { "research-analyst", "experiment-designer", "quant-analyst" },
            SkillPackIds: new[] { "research-pack" },
            TriggerKeywords: new[] { "research", "experiment", "benchmark", "hypothesis", "analysis" },
            MinimumRole: UserRole.ExternalUser),
        new(
            Id: "internal-ops-orchestration-team",
            Name: "Internal Ops Orchestration Team",
            Description: "Restricted internal team for deep operations orchestration.",
            AgentRoles: new[] { "it-ops-orchestrator", "multi-agent-coordinator", "error-coordinator" },
            SkillPackIds: new[] { "internal-ops-pack" },
            TriggerKeywords: new[] { "incident", "operations", "war room", "service outage" },
            MinimumRole: UserRole.InternalDeveloper),
        new(
            Id: "fintech-payments-team",
            Name: "FinTech Payments Team",
            Description: "Team for payment-heavy fintech systems and billing flows.",
            AgentRoles: new[] { "fintech-specialist", "pricing-analyst", "security-auditor" },
            SkillPackIds: new[] { "web-development-pack" },
            TriggerKeywords: new[] { "fintech", "payments", "ledger", "billing", "pci" },
            MinimumRole: UserRole.ExternalUser),
        new(
            Id: "healthcare-compliance-team",
            Name: "Healthcare Compliance Team",
            Description: "Team for healthcare interoperability and privacy-driven systems.",
            AgentRoles: new[] { "healthcare-systems-engineer", "privacy-engineer", "compliance-specialist" },
            SkillPackIds: new[] { "security-pack" },
            TriggerKeywords: new[] { "healthcare", "fhir", "hl7", "patient", "hipaa" },
            MinimumRole: UserRole.ExternalUser),
        new(
            Id: "monetization-growth-team",
            Name: "Monetization Growth Team",
            Description: "Team for pricing, growth experiments and monetization optimization.",
            AgentRoles: new[] { "monetization-specialist", "growth-engineer", "product-manager-agent" },
            SkillPackIds: new[] { "research-pack" },
            TriggerKeywords: new[] { "monetization", "pricing", "conversion", "retention", "growth" },
            MinimumRole: UserRole.ExternalUser),
        new(
            Id: "internal-security-audit-team",
            Name: "Internal Security Audit Team",
            Description: "Restricted internal team for deep security audits and penetration testing.",
            AgentRoles: new[] { "security-auditor", "penetration-tester", "forensic-analyst" },
            SkillPackIds: new[] { "security-pack", "internal-ops-pack" },
            TriggerKeywords: new[] { "security audit", "penetration test", "forensic", "security review", "vulnerability assessment" },
            MinimumRole: UserRole.InternalDeveloper),
        new(
            Id: "internal-data-governance-team",
            Name: "Internal Data Governance Team",
            Description: "Restricted internal team for data privacy, compliance and governance operations.",
            AgentRoles: new[] { "data-governance-specialist", "compliance-auditor", "privacy-engineer" },
            SkillPackIds: new[] { "security-pack", "internal-ops-pack" },
            TriggerKeywords: new[] { "data governance", "compliance audit", "privacy review", "gdpr", "data classification" },
            MinimumRole: UserRole.InternalDeveloper)
    };

    private readonly ITeamTemplateRepository _repository;
    private readonly IUserRoleProvider _roleProvider;

    public TeamTemplateResolver(
        ITeamTemplateRepository repository,
        IUserRoleProvider roleProvider)
    {
        _repository = repository;
        _roleProvider = roleProvider;
    }

    public TeamTemplateResolution Resolve(string userRequest)
    {
        if (string.IsNullOrWhiteSpace(userRequest))
            return new TeamTemplateResolution(false, null, "empty_request");

        var role = _roleProvider.GetCurrentRole();
        var normalized = userRequest.ToLowerInvariant();

        var candidateTemplates = _repository.List()
            .Concat(BuiltInTemplates)
            .GroupBy(t => t.Id, StringComparer.OrdinalIgnoreCase)
            .Select(g => g.First())
            .ToArray();

        var bestMatch = candidateTemplates
            .Where(t => role >= t.MinimumRole)
            .Select(t => new
            {
                Template = t,
                Score = t.TriggerKeywords.Count(k =>
                    !string.IsNullOrWhiteSpace(k) &&
                    normalized.Contains(k.Trim().ToLowerInvariant(), StringComparison.Ordinal))
            })
            .Where(x => x.Score > 0)
            .OrderByDescending(x => x.Score)
            .ThenBy(x => x.Template.Id, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault();

        if (bestMatch is null)
            return new TeamTemplateResolution(false, null, "no_matching_template");

        return new TeamTemplateResolution(
            Matched: true,
            Template: bestMatch.Template,
            Reason: $"keyword_match_score:{bestMatch.Score}");
    }
}
