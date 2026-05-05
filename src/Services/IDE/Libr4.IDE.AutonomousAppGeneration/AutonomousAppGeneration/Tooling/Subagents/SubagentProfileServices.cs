namespace Libr4.IDE.Application.AutonomousAppGeneration.Tooling.Subagents;

public sealed class InMemorySubagentProfileRepository : ISubagentProfileRepository
{
    private static readonly SubagentProfile[] BuiltInProfiles =
    {
        // Category 1: Core Development
        new("api-designer", "API Designer", "core-development", "api-designer", new[] { "rest", "contract-first", "openapi" }, new[] { "spec", "lint" }),
        new("backend-developer", "Backend Developer", "core-development", "backend-developer", new[] { "backend", "services", "persistence" }, new[] { "build", "test", "migrate" }),
        new("frontend-developer", "Frontend Developer", "core-development", "frontend-developer", new[] { "frontend", "ui", "state" }, new[] { "build", "test" }),
        new("fullstack-developer", "Fullstack Developer", "core-development", "fullstack-developer", new[] { "fullstack", "integration", "delivery" }, new[] { "build", "test", "smoke" }),
        new("code-mapper", "Code Mapper", "core-development", "code-mapper", new[] { "codebase-map", "dependency-map" }, new[] { "search", "index" }),
        new("graphql-architect", "GraphQL Architect", "core-development", "graphql-architect", new[] { "graphql", "schema", "resolver" }, new[] { "schema-validate", "test" }),
        new("microservices-architect", "Microservices Architect", "core-development", "microservices-architect", new[] { "microservices", "eventing", "scalability" }, new[] { "design", "compose" }),
        new("ui-designer", "UI Designer", "core-development", "ui-designer", new[] { "ux", "layout", "accessibility" }, new[] { "design-artifact", "review" }),
        new("ui-fixer", "UI Fixer", "core-development", "ui-fixer", new[] { "ui-fix", "regression", "visual" }, new[] { "screenshot", "test" }),
        new("websocket-engineer", "WebSocket Engineer", "core-development", "websocket-engineer", new[] { "realtime", "websocket", "events" }, new[] { "test", "load" }),
        new("electron-pro", "Electron Pro", "core-development", "electron-pro", new[] { "desktop", "electron", "packaging" }, new[] { "build", "sign" }),
        new("mobile-developer", "Mobile Developer", "core-development", "mobile-developer", new[] { "mobile", "ios", "android" }, new[] { "build", "test", "bundle" }),

        // Category 2: Language Specialists
        new("python-specialist", "Python Specialist", "language-specialists", "python-specialist", new[] { "python", "asyncio", "fastapi" }, new[] { "build", "test", "lint" }),
        new("csharp-specialist", "C# Specialist", "language-specialists", "csharp-specialist", new[] { "csharp", "dotnet", "aspnet" }, new[] { "build", "test", "analyze" }),
        new("typescript-specialist", "TypeScript Specialist", "language-specialists", "typescript-specialist", new[] { "typescript", "node", "frontend" }, new[] { "build", "test", "lint" }),
        new("go-specialist", "Go Specialist", "language-specialists", "go-specialist", new[] { "go", "concurrency", "services" }, new[] { "build", "test" }),
        new("rust-specialist", "Rust Specialist", "language-specialists", "rust-specialist", new[] { "rust", "performance", "safety" }, new[] { "build", "test", "bench" }),
        new("java-specialist", "Java Specialist", "language-specialists", "java-specialist", new[] { "java", "spring", "jvm" }, new[] { "build", "test" }),
        new("kotlin-specialist", "Kotlin Specialist", "language-specialists", "kotlin-specialist", new[] { "kotlin", "jvm", "android" }, new[] { "build", "test" }),
        new("swift-specialist", "Swift Specialist", "language-specialists", "swift-specialist", new[] { "swift", "ios", "mobile" }, new[] { "build", "test" }),
        new("php-specialist", "PHP Specialist", "language-specialists", "php-specialist", new[] { "php", "laravel", "symfony" }, new[] { "build", "test" }),
        new("ruby-specialist", "Ruby Specialist", "language-specialists", "ruby-specialist", new[] { "ruby", "rails", "backend" }, new[] { "build", "test" }),

        // Category 3: Infrastructure
        new("devops-engineer", "DevOps Engineer", "infrastructure", "devops-engineer", new[] { "ci-cd", "pipelines", "release" }, new[] { "deploy", "rollout", "rollback" }),
        new("platform-engineer", "Platform Engineer", "infrastructure", "platform-engineer", new[] { "platform", "golden-path", "runtime" }, new[] { "provision", "validate" }),
        new("cloud-architect", "Cloud Architect", "infrastructure", "cloud-architect", new[] { "cloud", "aws", "azure" }, new[] { "plan", "cost-check" }),
        new("kubernetes-operator", "Kubernetes Operator", "infrastructure", "kubernetes-operator", new[] { "kubernetes", "helm", "operators" }, new[] { "deploy", "health-check" }),
        new("terraform-specialist", "Terraform Specialist", "infrastructure", "terraform-specialist", new[] { "terraform", "iac", "state" }, new[] { "plan", "apply" }),
        new("sre-engineer", "SRE Engineer", "infrastructure", "sre-engineer", new[] { "sre", "reliability", "slis" }, new[] { "observe", "incident" }),
        new("observability-agent", "Observability Agent", "infrastructure", "observability-agent", new[] { "metrics", "tracing", "logging" }, new[] { "dashboard", "alert-check" }),
        new("security-platform-engineer", "Security Platform Engineer", "infrastructure", "security-platform-engineer", new[] { "platform-security", "policy", "hardening" }, new[] { "scan", "policy-check" }),
        new("database-operator", "Database Operator", "infrastructure", "database-operator", new[] { "database", "replication", "backup" }, new[] { "migrate", "backup", "restore-test" }),
        new("network-engineer", "Network Engineer", "infrastructure", "network-engineer", new[] { "networking", "service-mesh", "ingress" }, new[] { "verify", "load-check" }),

        // Categories 4-6: Quality & Security, Data & AI, Developer Experience
        new("test-automation-engineer", "Test Automation Engineer", "quality-security", "test-automation-engineer", new[] { "testing", "e2e", "regression" }, new[] { "test", "coverage" }),
        new("security-auditor", "Security Auditor", "quality-security", "security-auditor", new[] { "security", "threat-model", "audit" }, new[] { "scan", "policy-check" }),
        new("performance-engineer", "Performance Engineer", "quality-security", "performance-engineer", new[] { "performance", "latency", "throughput" }, new[] { "bench", "profile" }),
        new("compliance-specialist", "Compliance Specialist", "quality-security", "compliance-specialist", new[] { "compliance", "controls", "governance" }, new[] { "audit-report", "policy-check" }),
        new("chaos-engineer", "Chaos Engineer", "quality-security", "chaos-engineer", new[] { "resilience", "chaos", "fault-injection" }, new[] { "inject", "verify-recovery" }),
        new("incident-responder", "Incident Responder", "quality-security", "incident-responder", new[] { "incident", "triage", "forensics" }, new[] { "triage", "timeline" }),
        new("vulnerability-researcher", "Vulnerability Researcher", "quality-security", "vulnerability-researcher", new[] { "vuln-research", "exploit-analysis" }, new[] { "scan", "report" }),
        new("privacy-engineer", "Privacy Engineer", "quality-security", "privacy-engineer", new[] { "privacy", "pii", "data-minimization" }, new[] { "classify", "redact-check" }),
        new("supply-chain-security", "Supply Chain Security", "quality-security", "supply-chain-security", new[] { "sbom", "dependencies", "provenance" }, new[] { "sbom", "attest" }),
        new("release-quality-gatekeeper", "Release Quality Gatekeeper", "quality-security", "release-quality-gatekeeper", new[] { "quality-gates", "release-readiness" }, new[] { "gate-check", "release-report" }),

        new("ml-engineer", "ML Engineer", "data-ai", "ml-engineer", new[] { "ml", "training", "inference" }, new[] { "train", "evaluate" }),
        new("data-engineer", "Data Engineer", "data-ai", "data-engineer", new[] { "etl", "pipelines", "warehouse" }, new[] { "transform", "validate" }),
        new("analytics-engineer", "Analytics Engineer", "data-ai", "analytics-engineer", new[] { "analytics", "metrics", "semantic-layer" }, new[] { "model", "validate" }),
        new("llm-engineer", "LLM Engineer", "data-ai", "llm-engineer", new[] { "llm", "prompting", "rag" }, new[] { "eval", "prompt-test" }),
        new("rag-architect", "RAG Architect", "data-ai", "rag-architect", new[] { "retrieval", "embeddings", "indexing" }, new[] { "index", "retrieval-test" }),
        new("data-scientist", "Data Scientist", "data-ai", "data-scientist", new[] { "statistics", "experiments", "modeling" }, new[] { "analyze", "report" }),
        new("feature-store-operator", "Feature Store Operator", "data-ai", "feature-store-operator", new[] { "feature-store", "online-serving" }, new[] { "sync", "consistency-check" }),
        new("model-ops-engineer", "Model Ops Engineer", "data-ai", "model-ops-engineer", new[] { "mlops", "deployment", "monitoring" }, new[] { "deploy-model", "drift-check" }),
        new("data-governance-agent", "Data Governance Agent", "data-ai", "data-governance-agent", new[] { "governance", "lineage", "catalog" }, new[] { "lineage", "catalog-check" }),
        new("ai-safety-reviewer", "AI Safety Reviewer", "data-ai", "ai-safety-reviewer", new[] { "ai-safety", "guardrails", "red-team" }, new[] { "safety-eval", "policy-check" }),

        new("developer-experience-engineer", "Developer Experience Engineer", "developer-experience", "developer-experience-engineer", new[] { "dx", "feedback-loop", "tooling" }, new[] { "measure", "improve" }),
        new("build-systems-engineer", "Build Systems Engineer", "developer-experience", "build-systems-engineer", new[] { "build", "caching", "toolchains" }, new[] { "build", "cache-check" }),
        new("documentation-engineer", "Documentation Engineer", "developer-experience", "documentation-engineer", new[] { "docs", "architecture-docs", "runbooks" }, new[] { "generate-doc", "lint-doc" }),
        new("onboarding-specialist", "Onboarding Specialist", "developer-experience", "onboarding-specialist", new[] { "onboarding", "starter-guides", "templates" }, new[] { "scaffold", "verify-quickstart" }),
        new("cli-tooling-engineer", "CLI Tooling Engineer", "developer-experience", "cli-tooling-engineer", new[] { "cli", "automation", "developer-tools" }, new[] { "build-cli", "smoke" }),
        new("ide-integration-engineer", "IDE Integration Engineer", "developer-experience", "ide-integration-engineer", new[] { "ide", "plugins", "editor-workflows" }, new[] { "integration-test", "compat-check" }),
        new("template-maintainer", "Template Maintainer", "developer-experience", "template-maintainer", new[] { "templates", "scaffolding", "starter-kits" }, new[] { "validate-template", "snapshot-test" }),
        new("api-sdk-generator", "API SDK Generator", "developer-experience", "api-sdk-generator", new[] { "sdk", "api-clients", "codegen" }, new[] { "generate-sdk", "sdk-test" }),
        new("release-notes-writer", "Release Notes Writer", "developer-experience", "release-notes-writer", new[] { "release-notes", "changelog", "communication" }, new[] { "draft-notes", "diff-scan" }),
        new("workflow-automation-engineer", "Workflow Automation Engineer", "developer-experience", "workflow-automation-engineer", new[] { "automation", "workflows", "n8n" }, new[] { "workflow-test", "trigger-check" }),

        // Categories 7-10: Specialized Domains, Business & Product, Meta & Orchestration, Research & Analysis
        new("fintech-specialist", "FinTech Specialist", "specialized-domains", "fintech-specialist", new[] { "payments", "ledger", "compliance" }, new[] { "validate-flows", "risk-check" }),
        new("healthcare-systems-engineer", "Healthcare Systems Engineer", "specialized-domains", "healthcare-systems-engineer", new[] { "healthcare", "hl7", "fhir" }, new[] { "interop-check", "privacy-check" }),
        new("iot-engineer", "IoT Engineer", "specialized-domains", "iot-engineer", new[] { "iot", "edge", "telemetry" }, new[] { "device-sim", "telemetry-check" }),
        new("gameplay-engineer", "Gameplay Engineer", "specialized-domains", "gameplay-engineer", new[] { "gameplay", "rendering", "latency" }, new[] { "perf-test", "playtest-check" }),
        new("embedded-systems-engineer", "Embedded Systems Engineer", "specialized-domains", "embedded-systems-engineer", new[] { "embedded", "firmware", "rtos" }, new[] { "flash-test", "hw-sim" }),
        new("blockchain-engineer", "Blockchain Engineer", "specialized-domains", "blockchain-engineer", new[] { "blockchain", "smart-contracts", "consensus" }, new[] { "contract-test", "security-audit" }),
        new("erp-specialist", "ERP Specialist", "specialized-domains", "erp-specialist", new[] { "erp", "workflows", "integrations" }, new[] { "workflow-validate", "integration-test" }),
        new("crm-specialist", "CRM Specialist", "specialized-domains", "crm-specialist", new[] { "crm", "sales", "customer-data" }, new[] { "schema-check", "automation-test" }),
        new("edtech-specialist", "EdTech Specialist", "specialized-domains", "edtech-specialist", new[] { "edtech", "lms", "learning-analytics" }, new[] { "content-check", "progression-test" }),
        new("legaltech-specialist", "LegalTech Specialist", "specialized-domains", "legaltech-specialist", new[] { "legaltech", "documents", "compliance" }, new[] { "policy-check", "document-validate" }),

        new("product-manager-agent", "Product Manager Agent", "business-product", "product-manager-agent", new[] { "product", "roadmap", "prioritization" }, new[] { "plan", "scope-check" }),
        new("business-analyst-agent", "Business Analyst Agent", "business-product", "business-analyst-agent", new[] { "business-analysis", "requirements", "flows" }, new[] { "analyze", "traceability-check" }),
        new("growth-engineer", "Growth Engineer", "business-product", "growth-engineer", new[] { "growth", "experiments", "conversion" }, new[] { "ab-test-plan", "metric-check" }),
        new("monetization-specialist", "Monetization Specialist", "business-product", "monetization-specialist", new[] { "pricing", "monetization", "billing" }, new[] { "pricing-sim", "billing-check" }),
        new("customer-success-analyst", "Customer Success Analyst", "business-product", "customer-success-analyst", new[] { "customer-success", "retention", "nps" }, new[] { "cohort-check", "retention-report" }),
        new("marketing-automation-specialist", "Marketing Automation Specialist", "business-product", "marketing-automation-specialist", new[] { "marketing", "automation", "campaigns" }, new[] { "campaign-test", "funnel-check" }),
        new("sales-ops-engineer", "Sales Ops Engineer", "business-product", "sales-ops-engineer", new[] { "sales-ops", "crm-ops", "forecasting" }, new[] { "pipeline-check", "forecast-validate" }),
        new("operations-optimizer", "Operations Optimizer", "business-product", "operations-optimizer", new[] { "operations", "efficiency", "sop" }, new[] { "process-map", "kpi-check" }),
        new("strategy-analyst", "Strategy Analyst", "business-product", "strategy-analyst", new[] { "strategy", "market-analysis", "positioning" }, new[] { "scenario-model", "risk-review" }),
        new("pricing-analyst", "Pricing Analyst", "business-product", "pricing-analyst", new[] { "pricing", "unit-economics", "elasticity" }, new[] { "price-model", "margin-check" }),

        new("multi-agent-coordinator", "Multi Agent Coordinator", "meta-orchestration", "multi-agent-coordinator", new[] { "multi-agent", "coordination", "handoff" }, new[] { "route", "synchronize" }),
        new("task-distributor", "Task Distributor", "meta-orchestration", "task-distributor", new[] { "distribution", "queueing", "parallelism" }, new[] { "queue", "rebalance" }),
        new("workflow-orchestrator", "Workflow Orchestrator", "meta-orchestration", "workflow-orchestrator", new[] { "workflow", "dag", "orchestration" }, new[] { "orchestrate", "recover" }),
        new("agent-organizer", "Agent Organizer", "meta-orchestration", "agent-organizer", new[] { "agent-topology", "roles", "lifecycles" }, new[] { "allocate", "retire" }),
        new("context-manager", "Context Manager", "meta-orchestration", "context-manager", new[] { "context", "memory", "budget" }, new[] { "trim", "hydrate" }),
        new("error-coordinator", "Error Coordinator", "meta-orchestration", "error-coordinator", new[] { "errors", "recovery", "fallbacks" }, new[] { "triage", "fallback" }),
        new("knowledge-synthesizer", "Knowledge Synthesizer", "meta-orchestration", "knowledge-synthesizer", new[] { "knowledge", "synthesis", "summaries" }, new[] { "synthesize", "crosslink" }),
        new("performance-monitor", "Performance Monitor", "meta-orchestration", "performance-monitor", new[] { "monitoring", "latency", "efficiency" }, new[] { "profile", "alert-check" }),
        new("agent-installer", "Agent Installer", "meta-orchestration", "agent-installer", new[] { "agent-setup", "bootstrap", "install" }, new[] { "install", "verify" }),
        new("it-ops-orchestrator", "IT Ops Orchestrator", "meta-orchestration", "it-ops-orchestrator", new[] { "it-ops", "service-ops", "incident" }, new[] { "operate", "stabilize" }),
        new("pied-piper", "Pied Piper", "meta-orchestration", "pied-piper", new[] { "meta-control", "agent-guidance", "coordination" }, new[] { "guide", "align" }),

        new("research-analyst", "Research Analyst", "research-analysis", "research-analyst", new[] { "research", "synthesis", "evidence" }, new[] { "research", "summarize" }),
        new("competitive-intelligence-agent", "Competitive Intelligence Agent", "research-analysis", "competitive-intelligence-agent", new[] { "competition", "benchmark", "positioning" }, new[] { "benchmark", "compare" }),
        new("trend-spotter", "Trend Spotter", "research-analysis", "trend-spotter", new[] { "trends", "signals", "forecast" }, new[] { "scan", "trend-report" }),
        new("experiment-designer", "Experiment Designer", "research-analysis", "experiment-designer", new[] { "experiments", "hypotheses", "methodology" }, new[] { "design-experiment", "analyze-result" }),
        new("quant-analyst", "Quant Analyst", "research-analysis", "quant-analyst", new[] { "quant", "statistics", "modeling" }, new[] { "model", "backtest" }),
        new("qualitative-researcher", "Qualitative Researcher", "research-analysis", "qualitative-researcher", new[] { "qualitative", "interviews", "themes" }, new[] { "code-feedback", "insight-report" }),
        new("market-research-specialist", "Market Research Specialist", "research-analysis", "market-research-specialist", new[] { "market-research", "tam-sam-som", "segmentation" }, new[] { "market-map", "segment-check" }),
        new("risk-analyst", "Risk Analyst", "research-analysis", "risk-analyst", new[] { "risk", "controls", "mitigation" }, new[] { "risk-register", "mitigation-check" }),
        new("ops-research-specialist", "Operations Research Specialist", "research-analysis", "ops-research-specialist", new[] { "optimization", "or", "constraints" }, new[] { "optimize", "simulate" }),
        new("signal-detection-agent", "Signal Detection Agent", "research-analysis", "signal-detection-agent", new[] { "signals", "anomaly", "detection" }, new[] { "detect", "validate-signals" })
    };

    private readonly Dictionary<string, SubagentProfile> _profiles = new(StringComparer.OrdinalIgnoreCase);

    public InMemorySubagentProfileRepository()
    {
        foreach (var p in BuiltInProfiles)
            _profiles[p.Id] = p;
    }

    public void Upsert(SubagentProfile profile)
    {
        if (string.IsNullOrWhiteSpace(profile.Id))
            throw new ArgumentException("Subagent profile id is required.", nameof(profile));
        _profiles[profile.Id] = profile;
    }

    public SubagentProfile? Get(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
            return null;
        return _profiles.TryGetValue(id, out var value) ? value : null;
    }

    public IReadOnlyList<SubagentProfile> List() =>
        _profiles.Values.OrderBy(x => x.Id, StringComparer.OrdinalIgnoreCase).ToArray();

    public IReadOnlyList<SubagentProfile> ListByRole(string role)
    {
        if (string.IsNullOrWhiteSpace(role))
            return Array.Empty<SubagentProfile>();
        return _profiles.Values
            .Where(x => string.Equals(x.Role, role, StringComparison.OrdinalIgnoreCase))
            .OrderBy(x => x.Id, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }
}

public sealed class SubagentSelector : ISubagentSelector
{
    private readonly ISubagentProfileRepository _repository;

    public SubagentSelector(ISubagentProfileRepository repository)
    {
        _repository = repository;
    }

    public IReadOnlyList<SubagentProfile> SelectByRoles(IReadOnlyList<string> roles)
    {
        if (roles is null || roles.Count == 0)
            return Array.Empty<SubagentProfile>();

        return roles
            .Where(r => !string.IsNullOrWhiteSpace(r))
            .SelectMany(r => _repository.ListByRole(r))
            .GroupBy(x => x.Id, StringComparer.OrdinalIgnoreCase)
            .Select(g => g.First())
            .OrderBy(x => x.Id, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }
}
