using Microsoft.Extensions.Logging;

namespace Libr4.AI.Infrastructure.Subagent;

/// <summary>
/// Implementation of persona system with 32 specialized personas
/// Based on Claude Octopus personas
/// </summary>
public class PersonaSystem : IPersonaSystem
{
    private readonly ILogger<PersonaSystem> _logger;
    private readonly List<SubagentPersona> _personas = new();
    private readonly Dictionary<string, string> _activePersonas = new();

    public PersonaSystem(ILogger<PersonaSystem> logger)
    {
        _logger = logger;
        InitializeDefaultPersonas();
    }

    public async Task<SubagentPersona?> GetPersonaAsync(string taskDescription, Dictionary<string, object>? context = null)
    {
        var lowerDesc = taskDescription.ToLowerInvariant();
        
        // Score each persona based on keyword matches
        var scored = _personas.Select(p => new
        {
            Persona = p,
            Score = p.Keywords.Count(k => lowerDesc.Contains(k.ToLowerInvariant()))
        }).Where(s => s.Score > 0).OrderByDescending(s => s.Score).ToList();

        if (scored.Any())
        {
            var best = scored.First();
            _logger.LogDebug("Persona selected: {Persona} with score {Score}", 
                best.Persona.Name, best.Score);
            return best.Persona;
        }

        return null;
    }

    public async Task<List<SubagentPersona>> GetAllPersonasAsync()
    {
        return _personas.ToList();
    }

    public async Task AddPersonaAsync(SubagentPersona persona)
    {
        _personas.Add(persona);
        _logger.LogInformation("Added custom persona: {Name}", persona.Name);
    }

    public async Task ActivatePersonaAsync(string sessionId, string personaId)
    {
        _activePersonas[sessionId] = personaId;
        _logger.LogDebug("Activated persona {PersonaId} for session {SessionId}", 
            personaId, sessionId);
    }

    private void InitializeDefaultPersonas()
    {
        // Software Engineering (11)
        _personas.AddRange(new[]
        {
            new SubagentPersona
            {
                Id = "architect",
                Name = "Architect",
                Category = PersonaCategory.SoftwareEngineering,
                Keywords = new List<string> { "architecture", "design", "structure", "system", "pattern" },
                SystemPrompt = "You are a software architect. Focus on system design, patterns, and architectural decisions.",
                PreferredModel = "claude-opus-4.7",
                Capabilities = new List<string> { "system-design", "architecture-review", "pattern-selection" }
            },
            new SubagentPersona
            {
                Id = "strategist",
                Name = "Strategist",
                Category = PersonaCategory.SoftwareEngineering,
                Keywords = new List<string> { "strategy", "planning", "roadmap", "technical-debt", "refactor" },
                SystemPrompt = "You are a technical strategist. Focus on long-term planning and technical decisions.",
                PreferredModel = "claude-opus-4.7",
                Capabilities = new List<string> { "strategic-planning", "technical-debt-analysis", "roadmap-design" }
            },
            new SubagentPersona
            {
                Id = "security-reviewer",
                Name = "Security Reviewer",
                Category = PersonaCategory.SoftwareEngineering,
                Keywords = new List<string> { "security", "vulnerability", "audit", "penetration", "auth" },
                SystemPrompt = "You are a security reviewer. Focus on identifying vulnerabilities and security issues.",
                PreferredModel = "claude-opus-4.7",
                Capabilities = new List<string> { "security-audit", "vulnerability-scan", "auth-review" }
            },
            new SubagentPersona
            {
                Id = "code-reviewer",
                Name = "Code Reviewer",
                Category = PersonaCategory.SoftwareEngineering,
                Keywords = new List<string> { "review", "pr", "pull-request", "code-quality", "best-practices" },
                SystemPrompt = "You are a code reviewer. Focus on code quality, best practices, and maintainability.",
                PreferredModel = "gpt-5.4",
                Capabilities = new List<string> { "code-review", "quality-check", "style-enforcement" }
            },
            new SubagentPersona
            {
                Id = "implementer",
                Name = "Implementer",
                Category = PersonaCategory.SoftwareEngineering,
                Keywords = new List<string> { "implement", "code", "write", "develop", "build" },
                SystemPrompt = "You are a code implementer. Focus on writing clean, efficient, and correct code.",
                PreferredModel = "gpt-5.4",
                Capabilities = new List<string> { "implementation", "code-generation", "refactoring" }
            },
            new SubagentPersona
            {
                Id = "tester",
                Name = "Tester",
                Category = PersonaCategory.SoftwareEngineering,
                Keywords = new List<string> { "test", "testing", "unit-test", "integration-test", "e2e" },
                SystemPrompt = "You are a QA engineer. Focus on comprehensive testing and test coverage.",
                PreferredModel = "gpt-5.4",
                Capabilities = new List<string> { "test-generation", "test-coverage", "quality-assurance" }
            },
            new SubagentPersona
            {
                Id = "debugger",
                Name = "Debugger",
                Category = PersonaCategory.SoftwareEngineering,
                Keywords = new List<string> { "debug", "bug", "error", "fix", "troubleshoot" },
                SystemPrompt = "You are a debugger. Focus on identifying and fixing bugs efficiently.",
                PreferredModel = "gpt-5.4",
                Capabilities = new List<string> { "debugging", "bug-fixing", "error-analysis" }
            },
            new SubagentPersona
            {
                Id = "performance-engineer",
                Name = "Performance Engineer",
                Category = PersonaCategory.SoftwareEngineering,
                Keywords = new List<string> { "performance", "optimization", "speed", "latency", "throughput" },
                SystemPrompt = "You are a performance engineer. Focus on optimization and performance improvements.",
                PreferredModel = "gpt-5.4",
                Capabilities = new List<string> { "performance-analysis", "optimization", "profiling" }
            },
            new SubagentPersona
            {
                Id = "devops-engineer",
                Name = "DevOps Engineer",
                Category = PersonaCategory.SoftwareEngineering,
                Keywords = new List<string> { "devops", "ci/cd", "deployment", "infrastructure", "pipeline" },
                SystemPrompt = "You are a DevOps engineer. Focus on CI/CD, deployment, and infrastructure.",
                PreferredModel = "gpt-5.4",
                Capabilities = new List<string> { "ci-cd", "deployment", "infrastructure" }
            },
            new SubagentPersona
            {
                Id = "database-engineer",
                Name = "Database Engineer",
                Category = PersonaCategory.SoftwareEngineering,
                Keywords = new List<string> { "database", "sql", "query", "schema", "migration" },
                SystemPrompt = "You are a database engineer. Focus on database design, queries, and optimization.",
                PreferredModel = "gpt-5.4",
                Capabilities = new List<string> { "database-design", "query-optimization", "schema-migration" }
            },
            new SubagentPersona
            {
                Id = "api-designer",
                Name = "API Designer",
                Category = PersonaCategory.SoftwareEngineering,
                Keywords = new List<string> { "api", "rest", "graphql", "endpoint", "contract" },
                SystemPrompt = "You are an API designer. Focus on clean, well-documented API design.",
                PreferredModel = "gpt-5.4",
                Capabilities = new List<string> { "api-design", "rest", "graphql", "api-documentation" }
            }
        });

        // Specialized Development (6)
        _personas.AddRange(new[]
        {
            new SubagentPersona
            {
                Id = "frontend-developer",
                Name = "Frontend Developer",
                Category = PersonaCategory.SpecializedDevelopment,
                Keywords = new List<string> { "frontend", "ui", "react", "vue", "angular", "css" },
                SystemPrompt = "You are a frontend developer. Focus on UI/UX and frontend frameworks.",
                PreferredModel = "gpt-5.4",
                Capabilities = new List<string> { "frontend", "react", "vue", "css", "ui" }
            },
            new SubagentPersona
            {
                Id = "backend-developer",
                Name = "Backend Developer",
                Category = PersonaCategory.SpecializedDevelopment,
                Keywords = new List<string> { "backend", "server", "api", "microservice", "service" },
                SystemPrompt = "You are a backend developer. Focus on server-side logic and APIs.",
                PreferredModel = "gpt-5.4",
                Capabilities = new List<string> { "backend", "api", "microservices", "server" }
            },
            new SubagentPersona
            {
                Id = "mobile-developer",
                Name = "Mobile Developer",
                Category = PersonaCategory.SpecializedDevelopment,
                Keywords = new List<string> { "mobile", "ios", "android", "flutter", "react-native" },
                SystemPrompt = "You are a mobile developer. Focus on iOS and Android development.",
                PreferredModel = "gpt-5.4",
                Capabilities = new List<string> { "mobile", "ios", "android", "flutter" }
            },
            new SubagentPersona
            {
                Id = "data-engineer",
                Name = "Data Engineer",
                Category = PersonaCategory.SpecializedDevelopment,
                Keywords = new List<string> { "data", "etl", "pipeline", "warehouse", "analytics" },
                SystemPrompt = "You are a data engineer. Focus on data pipelines and analytics infrastructure.",
                PreferredModel = "gpt-5.4",
                Capabilities = new List<string> { "data-engineering", "etl", "data-warehouse" }
            },
            new SubagentPersona
            {
                Id = "ml-engineer",
                Name = "ML Engineer",
                Category = PersonaCategory.SpecializedDevelopment,
                Keywords = new List<string> { "ml", "machine-learning", "ai", "model", "training" },
                SystemPrompt = "You are an ML engineer. Focus on machine learning models and pipelines.",
                PreferredModel = "gpt-5.4",
                Capabilities = new List<string> { "ml", "machine-learning", "model-training" }
            },
            new SubagentPersona
            {
                Id = "blockchain-developer",
                Name = "Blockchain Developer",
                Category = PersonaCategory.SpecializedDevelopment,
                Keywords = new List<string> { "blockchain", "smart-contract", "web3", "crypto", "defi" },
                SystemPrompt = "You are a blockchain developer. Focus on smart contracts and Web3.",
                PreferredModel = "gpt-5.4",
                Capabilities = new List<string> { "blockchain", "smart-contracts", "web3" }
            }
        });

        // Documentation & Communication (5)
        _personas.AddRange(new[]
        {
            new SubagentPersona
            {
                Id = "technical-writer",
                Name = "Technical Writer",
                Category = PersonaCategory.DocumentationAndCommunication,
                Keywords = new List<string> { "documentation", "docs", "readme", "guide", "manual" },
                SystemPrompt = "You are a technical writer. Focus on clear, comprehensive documentation.",
                PreferredModel = "gpt-5.4",
                Capabilities = new List<string> { "documentation", "technical-writing", "readme" }
            },
            new SubagentPersona
            {
                Id = "api-documenter",
                Name = "API Documenter",
                Category = PersonaCategory.DocumentationAndCommunication,
                Keywords = new List<string> { "api-doc", "swagger", "openapi", "api-specification" },
                SystemPrompt = "You are an API documenter. Focus on API specifications and documentation.",
                PreferredModel = "gpt-5.4",
                Capabilities = new List<string> { "api-documentation", "swagger", "openapi" }
            },
            new SubagentPersona
            {
                Id = "communicator",
                Name = "Communicator",
                Category = PersonaCategory.DocumentationAndCommunication,
                Keywords = new List<string> { "explain", "clarify", "summarize", "communicate" },
                SystemPrompt = "You are a technical communicator. Focus on clear explanations.",
                PreferredModel = "gpt-5.4",
                Capabilities = new List<string> { "communication", "explanation", "summarization" }
            },
            new SubagentPersona
            {
                Id = "presentation-designer",
                Name = "Presentation Designer",
                Category = PersonaCategory.DocumentationAndCommunication,
                Keywords = new List<string> { "presentation", "slide", "deck", "pitch" },
                SystemPrompt = "You are a presentation designer. Focus on clear, impactful presentations.",
                PreferredModel = "gpt-5.4",
                Capabilities = new List<string> { "presentation", "slides", "pitch-deck" }
            },
            new SubagentPersona
            {
                Id = "tutorial-creator",
                Name = "Tutorial Creator",
                Category = PersonaCategory.DocumentationAndCommunication,
                Keywords = new List<string> { "tutorial", "guide", "how-to", "learning" },
                SystemPrompt = "You are a tutorial creator. Focus on step-by-step learning materials.",
                PreferredModel = "gpt-5.4",
                Capabilities = new List<string> { "tutorial", "guide", "how-to" }
            }
        });

        // Research & Strategy (3)
        _personas.AddRange(new[]
        {
            new SubagentPersona
            {
                Id = "researcher",
                Name = "Researcher",
                Category = PersonaCategory.ResearchAndStrategy,
                Keywords = new List<string> { "research", "investigate", "explore", "study", "analyze" },
                SystemPrompt = "You are a researcher. Focus on thorough investigation and analysis.",
                PreferredModel = "claude-opus-4.7",
                Capabilities = new List<string> { "research", "investigation", "analysis" }
            },
            new SubagentPersona
            {
                Id = "analyst",
                Name = "Analyst",
                Category = PersonaCategory.ResearchAndStrategy,
                Keywords = new List<string> { "analyze", "metrics", "data-analysis", "insights" },
                SystemPrompt = "You are an analyst. Focus on data analysis and insights.",
                PreferredModel = "claude-opus-4.7",
                Capabilities = new List<string> { "data-analysis", "metrics", "insights" }
            },
            new SubagentPersona
            {
                Id = "consultant",
                Name = "Consultant",
                Category = PersonaCategory.ResearchAndStrategy,
                Keywords = new List<string> { "consult", "advise", "recommend", "suggest" },
                SystemPrompt = "You are a consultant. Focus on strategic recommendations.",
                PreferredModel = "claude-opus-4.7",
                Capabilities = new List<string> { "consulting", "recommendations", "advisory" }
            }
        });

        // Business & Compliance (3)
        _personas.AddRange(new[]
        {
            new SubagentPersona
            {
                Id = "business-analyst",
                Name = "Business Analyst",
                Category = PersonaCategory.BusinessAndCompliance,
                Keywords = new List<string> { "business", "requirements", "user-story", "acceptance-criteria" },
                SystemPrompt = "You are a business analyst. Focus on requirements and user stories.",
                PreferredModel = "gpt-5.4",
                Capabilities = new List<string> { "business-analysis", "requirements", "user-stories" }
            },
            new SubagentPersona
            {
                Id = "compliance-officer",
                Name = "Compliance Officer",
                Category = PersonaCategory.BusinessAndCompliance,
                Keywords = new List<string> { "compliance", "legal", "regulation", "audit", "policy" },
                SystemPrompt = "You are a compliance officer. Focus on regulatory compliance.",
                PreferredModel = "gpt-5.4",
                Capabilities = new List<string> { "compliance", "legal", "regulations" }
            },
            new SubagentPersona
            {
                Id = "project-manager",
                Name = "Project Manager",
                Category = PersonaCategory.BusinessAndCompliance,
                Keywords = new List<string> { "project", "timeline", "milestone", "schedule", "plan" },
                SystemPrompt = "You are a project manager. Focus on planning and coordination.",
                PreferredModel = "gpt-5.4",
                Capabilities = new List<string> { "project-management", "planning", "coordination" }
            }
        });

        // Creative & Design (4)
        _personas.AddRange(new[]
        {
            new SubagentPersona
            {
                Id = "ui-ux-designer",
                Name = "UI/UX Designer",
                Category = PersonaCategory.CreativeAndDesign,
                Keywords = new List<string> { "ui", "ux", "design", "interface", "user-experience" },
                SystemPrompt = "You are a UI/UX designer. Focus on user interface and experience.",
                PreferredModel = "gpt-5.4",
                Capabilities = new List<string> { "ui-design", "ux-design", "user-experience" }
            },
            new SubagentPersona
            {
                Id = "graphic-designer",
                Name = "Graphic Designer",
                Category = PersonaCategory.CreativeAndDesign,
                Keywords = new List<string> { "graphic", "visual", "design", "brand", "logo" },
                SystemPrompt = "You are a graphic designer. Focus on visual design and branding.",
                PreferredModel = "gpt-5.4",
                Capabilities = new List<string> { "graphic-design", "branding", "visual-design" }
            },
            new SubagentPersona
            {
                Id = "content-creator",
                Name = "Content Creator",
                Category = PersonaCategory.CreativeAndDesign,
                Keywords = new List<string> { "content", "copy", "writing", "blog", "article" },
                SystemPrompt = "You are a content creator. Focus on engaging written content.",
                PreferredModel = "gpt-5.4",
                Capabilities = new List<string> { "content-creation", "copywriting", "blogging" }
            },
            new SubagentPersona
            {
                Id = "creative-director",
                Name = "Creative Director",
                Category = PersonaCategory.CreativeAndDesign,
                Keywords = new List<string> { "creative", "direction", "vision", "concept" },
                SystemPrompt = "You are a creative director. Focus on creative vision and direction.",
                PreferredModel = "gpt-5.4",
                Capabilities = new List<string> { "creative-direction", "vision", "concept" }
            }
        });

        _logger.LogInformation("Initialized {Count} default personas", _personas.Count);
    }
}
