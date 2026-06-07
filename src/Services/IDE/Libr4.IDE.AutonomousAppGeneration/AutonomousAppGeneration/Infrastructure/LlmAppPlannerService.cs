using System.Text.Json;
using Libr4.AI.Application.Abstractions;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentIntegration;
using Libr4.IDE.Application.AutonomousAppGeneration.Services;
using Libr4.IDE.Application.AutonomousAppGeneration.Services.PlatformUtilization;
using Libr4.IDE.AutonomousAppGeneration.AutonomousAppGeneration.Recovery;
using Libr4.IDE.Domain.AutonomousAppGeneration;
using Microsoft.Extensions.Logging;

namespace Libr4.IDE.Application.AutonomousAppGeneration.Infrastructure;

/// <summary>
/// Planner that turns a natural-language request into a <see cref="GenerationPlan"/>
/// by asking an LLM (OpenRouter free model by default).
/// Fail-fast: invalid or missing LLM planner output throws
/// <see cref="AutonomousGenerationFailedException"/> (no synthetic plans).
/// </summary>
public sealed class LlmAppPlannerService : IAppPlannerService
{
    private readonly IAIService _ai;
    private readonly ILogger<LlmAppPlannerService> _logger;
    private readonly IProviderCapabilityMatrix _providerMatrix;
#if INTERNAL
    private readonly RecoveryCascadeService? _recoveryCascade;
#endif

    /// <summary>Every agent name the orchestrator may assign to a phase.</summary>
    private static readonly string[] KnownAgents =
    {
        "TaskDecompositionAgent",
        "CodeGenerationAgent",
        "ArchitecturalGuardrailsAgent",
        "CodeReviewAgent",
        "SecurityTestingAgent",
        "SemanticBlameAgent",
        "WebSearchAgent",
        "HackerAgent",
        "AIWorkflowAutomationAgent",
        "DatabaseDesignAgent",
        "CICDPipelineAgent",
        "PerformanceProfilingAgent",
        "TechDebtTrackingAgent",
        "ObservabilityAgent"
    };

    private const string PlannerSystemPrompt = @"
You are the Orchestrator. Plan a PRODUCTION-READY app based on the user's requested tech stack.

====================== REASONING OUTPUT ======================
Before providing your final JSON output, include a <thinking> section
with your step-by-step reasoning process. This helps users understand
your decision-making and see how you approach the problem.

Example:
<thinking>
1. User wants a web app with React and Node.js
2. Need to plan frontend (React) and backend (Node.js/Express)
3. PostgreSQL for database
4. Docker for deployment
5. Frontend needs build step, backend needs npm install
6. Testing: Jest for frontend, Mocha for backend
</thinking>

The <thinking> section will be extracted and shown to the user separately.

====================== TECH STACK RULES ======================
- languages: Extract from user request (e.g., Python, JavaScript/TypeScript, C#, etc.)
- frameworks: Extract from user request (e.g., FastAPI, Next.js, ASP.NET Core, etc.)
- databases: PostgreSQL by default unless user specifies otherwise
- infrastructure: Docker, Docker Compose by default
- runtimeImage: Choose appropriate image based on tech stack:
  - Python backend: ""python:3.12-slim""
  - Node.js/Next.js frontend: ""node:22-alpine""
  - C#/.NET: ""mcr.microsoft.com/dotnet/sdk:8.0""
  - Go: ""golang:1.23-alpine""
  - Rust: ""rust:1.80""
  - Java/Kotlin: ""eclipse-temurin:21-jdk""
- buildCommands: Choose appropriate commands based on tech stack:
  - Python: [""pip install -r requirements.txt""]
  - Node.js: [""npm ci"", ""npm run build""]
  - C#/.NET: [""dotnet restore"", ""dotnet build --configuration Release""]
  - Go: [""go mod download"", ""go build""]
  - Rust: [""cargo build --release""]
- testCommands: Choose appropriate commands based on tech stack:
  - Python: [""pytest""]
  - Node.js: [""npm test""]
  - C#/.NET: [""dotnet test --configuration Release""]
  - Go: [""go test""]
  - Rust: [""cargo test""]

====================== OUTPUT CONTRACT (HARD) ======================
After the optional <thinking> block, output one JSON object (no markdown fences).
{
  ""applicationName"": string,
  ""description"": string,
  ""techStack"": {
    ""languages"": [string, ...],
    ""frameworks"": [string, ...],
    ""databases"": [string, ...],
    ""infrastructure"": [string, ...],
    ""rationale"": string
  },
  ""runtimeImage"": string,
  ""buildCommands"": [string, ...],
  ""testCommands"":  [string, ...],
  ""requiredAgents"": [string, ...], // subset of: TaskDecompositionAgent, CodeGenerationAgent,
      // ArchitecturalGuardrailsAgent, CodeReviewAgent, SecurityTestingAgent,
      // SemanticBlameAgent, WebSearchAgent, HackerAgent, AIWorkflowAutomationAgent,
      // DatabaseDesignAgent, CICDPipelineAgent, PerformanceProfilingAgent,
      // TechDebtTrackingAgent, ObservabilityAgent
  ""phases"": [
    { ""order"": int, ""name"": string, ""description"": string,
      ""assignments"": [ { ""agentName"": string, ""role"": string, ""taskDescription"": string }, ... ] }
  ],
  ""maxIterations"": int     // 15..30, higher for complex apps
}

====================== PLANNING RULES ======================
- applicationName: PascalCase, 3-30 chars, derived from the request (e.g. ""FinSecureBank"").
- description: one paragraph listing every user-facing feature in the request.
- Phases MUST include: ""Scaffold"", ""Implement core"", ""Tests"", ""Security & review"", ""Documentation"".
- Every phase has >=1 assignment. Every assignment names a real agent from the allowed list.
- requiredAgents MUST include CodeGenerationAgent, CodeReviewAgent, SecurityTestingAgent.
- Build/test commands MUST run in the runtime image without extra installs.
- CRITICAL: If the user names a language or framework (e.g. ""Python and Flask"", ""FastAPI"", ""Node Express""), techStack.languages and techStack.frameworks MUST match. Never substitute C# / ASP.NET Core when the user asked for Python or Node.
- If request contains [REPO_BOOTSTRAP_CONTEXT] or mentions GitHub/Obscura repository adaptation:
  - Include a dedicated phase ""Repo bootstrap & adaptation"".
  - Description MUST explicitly require upstream adaptation evidence and forbid generic template output.
  - Phases MUST include concrete auth + kanban implementation deliverables and business test deliverables.
- Prefer explicit failure-oriented planning over fake/degraded output when constraints are unsatisfied.
- Output only the JSON object described above.
";

    public LlmAppPlannerService(
        IAIService ai,
        ILogger<LlmAppPlannerService> logger,
        IProviderCapabilityMatrix providerMatrix
#if INTERNAL
        , RecoveryCascadeService? recoveryCascade = null
#endif
        )
    {
        _ai = ai;
        _logger = logger;
        _providerMatrix = providerMatrix;
#if INTERNAL
        _recoveryCascade = recoveryCascade;
#endif
    }

    public async Task<GenerationPlan> PlanAsync(string userRequest, CancellationToken ct = default)
    {
        
        // Use provider capability matrix for model routing
        var stageRequirement = _providerMatrix.GetStageRequirements("planning") 
            ?? new StageModelRequirement(
                Stage: "planning",
                RequiresFunctionCalling: false,
                RequiresStreaming: false,
                RequiresJsonMode: true,
                MinContextTokens: 32000,
                MinOutputTokens: 4096,
                MaxCostPer1kTokens: 0.01);
        var routingDecision = _providerMatrix.RouteStage("planning", stageRequirement);
        _logger.LogInformation("Model routing for planning stage: {Provider}/{Model} (reason: {Reason})",
            routingDecision.ProviderId, routingDecision.ModelId, routingDecision.RoutingReason);
        
        string raw;
        try
        {
            _logger.LogInformation("Calling LLM planner with request: {Request}", userRequest);
            var budgetedPrompt = PromptPipelinePolicy.ApplyInputBudget(
                "planning",
                PlatformCapabilityBriefingScope.AppendToPrompt(
                    userRequest,
                    PlatformCapabilityBriefingStage.Planning));

#if INTERNAL
            // Try recovery cascade if available
            if (_recoveryCascade != null)
            {
                var recoveryContext = new RecoveryContext
                {
                    CurrentPrompt = budgetedPrompt,
                    MessageHistory = new List<string> { budgetedPrompt },
                    CurrentTokenCount = budgetedPrompt.Length / 4,
                    MaxTokenLimit = 32000
                };

                try
                {
                    raw = await _ai.GenerateCompletionAsync(budgetedPrompt, PlannerSystemPrompt, routingDecision.ModelId);
                }
                catch (Exception llmEx)
                {
                    _logger.LogWarning(llmEx, "LLM call failed, attempting recovery cascade");
                    var recoveryResult = await _recoveryCascade.AttemptRecoveryAsync(llmEx, recoveryContext, ct);
                    
                    if (recoveryResult.Success)
                    {
                        _logger.LogInformation("Recovery succeeded with strategy: {Strategy}", recoveryResult.StrategyUsed);
                        budgetedPrompt = recoveryResult.ContextAfterRecovery.CurrentPrompt;
                        raw = await _ai.GenerateCompletionAsync(budgetedPrompt, PlannerSystemPrompt, routingDecision.ModelId);
                    }
                    else
                    {
                        throw; // Recovery failed, re-throw original exception
                    }
                }
            }
            else
            {
                raw = await _ai.GenerateCompletionAsync(budgetedPrompt, PlannerSystemPrompt, routingDecision.ModelId);
            }
#else
            raw = await _ai.GenerateCompletionAsync(budgetedPrompt, PlannerSystemPrompt, routingDecision.ModelId);
#endif

            _logger.LogInformation("LLM planner response received. Length: {Length}", raw?.Length ?? 0);
            _logger.LogDebug("LLM planner response: {Response}", raw);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Planner LLM call failed");
            throw new AutonomousGenerationFailedException(
                "planning",
                $"Planner LLM call failed: {ex.Message}",
                ex);
        }

        if (string.IsNullOrWhiteSpace(raw))
        {
            throw new AutonomousGenerationFailedException(
                "planning",
                "Planner LLM returned an empty response.");
        }

        if (!PromptPipelinePolicy.ValidateOutputContract("planning", raw, out var contractReason))
        {
            var parseHint = LlmJsonHelpers.LastParseError;
            var detail = string.IsNullOrWhiteSpace(parseHint)
                ? contractReason
                : $"{contractReason}; parse={parseHint}";
            throw new AutonomousGenerationFailedException(
                "planning",
                $"Planner output failed contract validation: {detail}");
        }

        using var doc = LlmJsonHelpers.ExtractJson(raw);
        if (doc is null)
        {
            var snippet = raw.Length <= 500 ? raw : raw[..500];
            throw new AutonomousGenerationFailedException(
                "planning",
                $"Planner returned unparseable JSON. parse={LlmJsonHelpers.LastParseError ?? "unknown"} snippet={snippet}");
        }

        try
        {
            var plan = BuildPlan(doc.RootElement, userRequest);
            _logger.LogInformation("LLM planner successfully generated plan: {AppName}", plan.ApplicationName);
            plan = ReconcilePlanWithUserRequest(plan, userRequest);
            plan = AlignRuntimeAndCommandsWithTechStack(plan);
            return StrictStackContractEnforcer.Enforce(plan, userRequest);
        }
        catch (Exception ex) when (ex is not AutonomousGenerationFailedException)
        {
            _logger.LogError(ex, "Failed to map planner JSON into GenerationPlan");
            throw new AutonomousGenerationFailedException(
                "planning",
                $"Failed to map planner JSON: {ex.Message}",
                ex);
        }
    }

    private static GenerationPlan BuildPlan(JsonElement root, string userRequest)
    {
        var appName = LlmJsonHelpers.GetString(root, "applicationName", "GeneratedApp");
        var description = LlmJsonHelpers.GetString(root, "description", userRequest);
        var maxIter = Math.Clamp(LlmJsonHelpers.GetInt(root, "maxIterations", 20), 15, 30);

        TechStack stack;
        if (root.TryGetProperty("techStack", out var ts) && ts.ValueKind == JsonValueKind.Object)
        {
            stack = new TechStack(
                languages: LlmJsonHelpers.GetStringArray(ts, "languages"),
                frameworks: LlmJsonHelpers.GetStringArray(ts, "frameworks"),
                databases: LlmJsonHelpers.GetStringArray(ts, "databases"),
                infrastructure: LlmJsonHelpers.GetStringArray(ts, "infrastructure"),
                rationale: LlmJsonHelpers.GetString(ts, "rationale", ""));
        }
        else
        {
            stack = DefaultTechStack();
        }

        var requiredAgents = LlmJsonHelpers.GetStringArray(root, "requiredAgents")
            .Where(a => KnownAgents.Contains(a))
            .Distinct()
            .ToList();
        var repoBootstrap = IsRepoBootstrapRequest(userRequest);
        var requiresAuth = MentionsAuth(userRequest);
        var requiresKanban = MentionsKanban(userRequest);
        if (requiredAgents.Count == 0) requiredAgents = DefaultAgents(repoBootstrap);

        var phases = new List<GenerationPhase>();
        if (root.TryGetProperty("phases", out var phasesEl) && phasesEl.ValueKind == JsonValueKind.Array)
        {
            foreach (var p in phasesEl.EnumerateArray())
            {
                var order = LlmJsonHelpers.GetInt(p, "order", phases.Count + 1);
                var name = LlmJsonHelpers.GetString(p, "name", $"Phase {order}");
                var phaseDesc = LlmJsonHelpers.GetString(p, "description", string.Empty);
                var assignments = new List<AgentAssignment>();
                if (p.TryGetProperty("assignments", out var assEl) && assEl.ValueKind == JsonValueKind.Array)
                {
                    foreach (var a in assEl.EnumerateArray())
                    {
                        var agentName = LlmJsonHelpers.GetString(a, "agentName", "CodeGenerationAgent");
                        var role = LlmJsonHelpers.GetString(a, "role", "Worker");
                        var taskDesc = LlmJsonHelpers.GetString(a, "taskDescription", string.Empty);
                        if (!KnownAgents.Contains(agentName)) continue;
                        assignments.Add(new AgentAssignment(agentName, role, taskDesc));
                    }
                }
                phases.Add(new GenerationPhase(order, name, phaseDesc, assignments));
            }
        }
        if (phases.Count == 0) phases = DefaultPhases(repoBootstrap, requiresAuth, requiresKanban);

        var runtimeImage = LlmJsonHelpers.GetString(root, "runtimeImage",
            GuessImage(stack.Languages));
        var buildCommands = LlmJsonHelpers.GetStringArray(root, "buildCommands");
        var testCommands = LlmJsonHelpers.GetStringArray(root, "testCommands");

        return new GenerationPlan(
            appName, description, stack, phases, requiredAgents,
            runtimeImage, buildCommands, testCommands, maxIter);
    }

    private enum UserPreferredStackKind
    {
        None,
        Python,
        Node,
        DotNet
    }

    /// <summary>
    /// When the LLM fails, returns a .NET default, or the model ignores explicit stack in the user message,
    /// infer Python/Node/.NET from <paramref name="userRequest"/> and fix the plan before command alignment.
    /// </summary>
    private static GenerationPlan ReconcilePlanWithUserRequest(GenerationPlan plan, string userRequest)
    {
        var preferred = InferUserPreferredStackKind(userRequest);
        if (preferred == UserPreferredStackKind.None)
            return plan;

        var requestedPythonFramework = ExtractRequestedPythonFramework(userRequest);
        var requestedNodeFramework = ExtractRequestedNodeFramework(userRequest);

        if (preferred == UserPreferredStackKind.Python
            && requestedPythonFramework is not null
            && !plan.TechStack.Frameworks.Any(f => f.Equals(requestedPythonFramework, StringComparison.OrdinalIgnoreCase)))
        {
            return new GenerationPlan(
                applicationName: ImproveFallbackAppName(plan.ApplicationName, userRequest),
                applicationDescription: plan.ApplicationDescription,
                techStack: BuildPythonTechStackFromUserRequest(userRequest, plan.TechStack),
                phases: plan.Phases,
                requiredAgents: plan.RequiredAgents,
                runtimeImage: plan.RuntimeImage,
                buildCommands: plan.BuildCommands,
                testCommands: plan.TestCommands,
                maxIterations: plan.MaxIterations);
        }

        if (preferred == UserPreferredStackKind.Python && !IsPythonPrimary(plan.TechStack))
        {
            return new GenerationPlan(
                applicationName: ImproveFallbackAppName(plan.ApplicationName, userRequest),
                applicationDescription: plan.ApplicationDescription,
                techStack: BuildPythonTechStackFromUserRequest(userRequest, plan.TechStack),
                phases: plan.Phases,
                requiredAgents: plan.RequiredAgents,
                runtimeImage: plan.RuntimeImage,
                buildCommands: plan.BuildCommands,
                testCommands: plan.TestCommands,
                maxIterations: plan.MaxIterations);
        }

        if (preferred == UserPreferredStackKind.Node
            && requestedNodeFramework is not null
            && !plan.TechStack.Frameworks.Any(f => f.Equals(requestedNodeFramework, StringComparison.OrdinalIgnoreCase)))
        {
            return new GenerationPlan(
                applicationName: ImproveFallbackAppName(plan.ApplicationName, userRequest),
                applicationDescription: plan.ApplicationDescription,
                techStack: BuildNodeTechStackFromUserRequest(userRequest, plan.TechStack),
                phases: plan.Phases,
                requiredAgents: plan.RequiredAgents,
                runtimeImage: plan.RuntimeImage,
                buildCommands: plan.BuildCommands,
                testCommands: plan.TestCommands,
                maxIterations: plan.MaxIterations);
        }

        if (preferred == UserPreferredStackKind.Node && !IsNodePrimary(plan.TechStack))
        {
            return new GenerationPlan(
                applicationName: ImproveFallbackAppName(plan.ApplicationName, userRequest),
                applicationDescription: plan.ApplicationDescription,
                techStack: BuildNodeTechStackFromUserRequest(userRequest, plan.TechStack),
                phases: plan.Phases,
                requiredAgents: plan.RequiredAgents,
                runtimeImage: plan.RuntimeImage,
                buildCommands: plan.BuildCommands,
                testCommands: plan.TestCommands,
                maxIterations: plan.MaxIterations);
        }

        if (preferred == UserPreferredStackKind.DotNet && !IsDotNetPrimary(plan.TechStack))
        {
            return new GenerationPlan(
                applicationName: ImproveFallbackAppName(plan.ApplicationName, userRequest),
                applicationDescription: plan.ApplicationDescription,
                techStack: BuildDotNetTechStack(plan.TechStack),
                phases: plan.Phases,
                requiredAgents: plan.RequiredAgents,
                runtimeImage: plan.RuntimeImage,
                buildCommands: plan.BuildCommands,
                testCommands: plan.TestCommands,
                maxIterations: plan.MaxIterations);
        }

        return plan;
    }

    private static string? ExtractRequestedPythonFramework(string userRequest)
    {
        if (string.IsNullOrWhiteSpace(userRequest)) return null;
        var s = userRequest.ToLowerInvariant();
        if (s.Contains("django", StringComparison.OrdinalIgnoreCase)) return "Django";
        if (s.Contains("fastapi", StringComparison.OrdinalIgnoreCase)) return "FastAPI";
        if (s.Contains("flask", StringComparison.OrdinalIgnoreCase)) return "Flask";
        return null;
    }

    private static string? ExtractRequestedNodeFramework(string userRequest)
    {
        if (string.IsNullOrWhiteSpace(userRequest)) return null;
        var s = userRequest.ToLowerInvariant();
        if (s.Contains("next.js", StringComparison.OrdinalIgnoreCase) || s.Contains("nextjs", StringComparison.OrdinalIgnoreCase)) return "Next.js";
        if (s.Contains("nestjs", StringComparison.OrdinalIgnoreCase) || s.Contains("nest.js", StringComparison.OrdinalIgnoreCase)) return "NestJS";
        if (s.Contains("express", StringComparison.OrdinalIgnoreCase)) return "Express";
        return null;
    }

    private static UserPreferredStackKind InferUserPreferredStackKind(string userRequest)
    {
        if (string.IsNullOrWhiteSpace(userRequest)) return UserPreferredStackKind.None;
        var u = userRequest.AsSpan();
        var s = userRequest.ToLowerInvariant();

        static bool Has(string hay, string needle) => hay.Contains(needle, StringComparison.OrdinalIgnoreCase);

        var wantsDotNet = Has(s, "asp.net") || Has(s, "aspnet") || Has(s, " blazor")
                          || Has(s, "c#") || Has(s, "csharp") || Has(s, ".net core")
                          || Has(s, "ef core") || Has(s, "entity framework");

        var wantsPython = Has(s, "python") || Has(s, "flask") || Has(s, "django")
                          || Has(s, "fastapi") || Has(s, "uvicorn") || Has(s, "gunicorn");

        var wantsNode = (Has(s, "node.js") || Has(s, "nodejs") || Has(s, "express")
                         || Has(s, "nestjs") || Has(s, "next.js") || Has(s, "nextjs")
                         || (Has(s, "typescript") && Has(s, "api") && !Has(s, "django"))
                         || (Has(s, "javascript") && Has(s, "api") && !Has(s, "django")))
                        && !Has(s, "django");

        // If user explicitly names Python or Node, prioritize that over .NET
        // This handles cases where the LLM falls back to .NET default but user requested Python/Node
        if (wantsPython) return UserPreferredStackKind.Python;
        if (wantsNode) return UserPreferredStackKind.Node;
        if (wantsDotNet) return UserPreferredStackKind.DotNet;

        return UserPreferredStackKind.None;
    }

    private static TechStack BuildPythonTechStackFromUserRequest(string userRequest, TechStack? preserve = null)
    {
        var s = userRequest.ToLowerInvariant();
        var languages = new List<string> { "Python" };
        if (s.Contains("typescript", StringComparison.OrdinalIgnoreCase))
            languages.Add("TypeScript");

        List<string> frameworks;
        if (s.Contains("fastapi", StringComparison.OrdinalIgnoreCase)) frameworks = new List<string> { "FastAPI" };
        else if (s.Contains("django", StringComparison.OrdinalIgnoreCase)) frameworks = new List<string> { "Django" };
        else if (s.Contains("flask", StringComparison.OrdinalIgnoreCase)) frameworks = new List<string> { "Flask" };
        else frameworks = new List<string> { "Flask" };

        if (s.Contains("django rest", StringComparison.OrdinalIgnoreCase)
            || s.Contains("drf", StringComparison.OrdinalIgnoreCase)
            || s.Contains("rest framework", StringComparison.OrdinalIgnoreCase))
            frameworks.Add("Django REST Framework");
        if (s.Contains("solidjs", StringComparison.OrdinalIgnoreCase) || s.Contains("solid js", StringComparison.OrdinalIgnoreCase))
            frameworks.Add("SolidJS");
        if (s.Contains("vite", StringComparison.OrdinalIgnoreCase))
            frameworks.Add("Vite");

        return new TechStack(
            languages: languages,
            frameworks: frameworks,
            databases: preserve is not null && preserve.Databases.Count > 0
                ? preserve.Databases.ToList()
                : new List<string> { "PostgreSQL" },
            infrastructure: preserve is not null && preserve.Infrastructure.Count > 0
                ? preserve.Infrastructure.ToList()
                : new List<string> { "Docker" },
            rationale: "Inferred from user request (Python stack).");
    }

    private static TechStack BuildNodeTechStackFromUserRequest(string userRequest, TechStack? preserve = null)
    {
        var s = userRequest.ToLowerInvariant();
        List<string> frameworks;
        if (s.Contains("nest", StringComparison.OrdinalIgnoreCase)) frameworks = new List<string> { "NestJS" };
        else if (s.Contains("next", StringComparison.OrdinalIgnoreCase)) frameworks = new List<string> { "Next.js" };
        else if (s.Contains("express", StringComparison.OrdinalIgnoreCase)) frameworks = new List<string> { "Express" };
        else frameworks = new List<string> { "Express" };

        var langs = s.Contains("typescript", StringComparison.OrdinalIgnoreCase)
            ? new List<string> { "TypeScript" }
            : new List<string> { "JavaScript" };

        return new TechStack(
            languages: langs,
            frameworks: frameworks,
            databases: preserve is not null && preserve.Databases.Count > 0
                ? preserve.Databases.ToList()
                : new List<string> { "PostgreSQL" },
            infrastructure: preserve is not null && preserve.Infrastructure.Count > 0
                ? preserve.Infrastructure.ToList()
                : new List<string> { "Docker" },
            rationale: "Inferred from user request (Node stack).");
    }

    private static TechStack BuildDotNetTechStack(TechStack? preserve) =>
        new(
            languages: new List<string> { "C#" },
            frameworks: new List<string> { "ASP.NET Core", "EF Core" },
            databases: preserve is not null && preserve.Databases.Count > 0
                ? preserve.Databases.ToList()
                : new List<string> { "PostgreSQL" },
            infrastructure: preserve is not null && preserve.Infrastructure.Count > 0
                ? preserve.Infrastructure.ToList()
                : new List<string> { "Docker" },
            rationale: "Inferred from user request (.NET stack).");

    private static string ImproveFallbackAppName(string current, string userRequest)
    {
        if (!string.Equals(current, "GeneratedApp", StringComparison.OrdinalIgnoreCase))
            return current;
        var derived = DeriveApplicationNameFromRequest(userRequest);
        return string.IsNullOrWhiteSpace(derived) ? current : derived;
    }

    /// <summary>PascalCase slug from words (same spirit as codegen sanitizers).</summary>
    private static string DeriveApplicationNameFromRequest(string userRequest)
    {
        if (string.IsNullOrWhiteSpace(userRequest)) return "GeneratedApp";
        var words = userRequest.Split(new[] { ' ', '\t', '\r', '\n', ',', '.', ';', ':', '-', '_' },
            StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var skip = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "a", "an", "the", "with", "and", "or", "for", "to", "in", "on", "using", "use", "generate", "create", "build",
            "app", "application", "api", "service", "web", "rest", "http", "system",
            "python", "flask", "django", "fastapi", "node", "nodejs", "express", "nestjs", "typescript", "javascript",
            "sql", "postgres", "postgresql"
        };
        var parts = words
            .Where(w => w.Length > 1 && !skip.Contains(w))
            .Take(4)
            .Select(w =>
            {
                var cleaned = new string(w.Where(char.IsLetterOrDigit).ToArray());
                if (string.IsNullOrEmpty(cleaned)) return null;
                if (cleaned.Length > 1)
                    return char.ToUpperInvariant(cleaned[0]) + cleaned[1..].ToLowerInvariant();
                return cleaned.ToUpperInvariant();
            })
            .Where(p => p is not null)
            .Cast<string>()
            .ToList();
        if (parts.Count == 0) return "GeneratedApp";
        var name = string.Concat(parts);
        if (char.IsDigit(name[0])) name = "App" + name;
        return name.Length > 48 ? name[..48] : name;
    }

    private static bool IsDotNetPrimary(TechStack ts) =>
        ts.Languages.Any(l =>
            l.Contains("c#", StringComparison.OrdinalIgnoreCase) ||
            l.Contains("csharp", StringComparison.OrdinalIgnoreCase) ||
            l.Contains(".net", StringComparison.OrdinalIgnoreCase))
        || ts.Frameworks.Any(f =>
            f.Contains("asp.net", StringComparison.OrdinalIgnoreCase) ||
            f.Contains("aspnet", StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// Fixes common planner mistakes: Python/Node stack with a dotnet runtime image or dotnet build/test commands.
    /// </summary>
    private static GenerationPlan AlignRuntimeAndCommandsWithTechStack(GenerationPlan plan)
    {
        if (IsPythonPrimary(plan.TechStack))
        {
            var runtime = plan.RuntimeImage;
            if (string.IsNullOrWhiteSpace(runtime) || runtime.Contains("dotnet", StringComparison.OrdinalIgnoreCase))
                runtime = "python:3.12-slim";

            var build = plan.BuildCommands.ToList();
            if (build.Count == 0 || build.Any(c => c.Contains("dotnet", StringComparison.OrdinalIgnoreCase)))
                build = new List<string> { "python -m pip install --no-cache-dir -r requirements.txt" };

            var test = plan.TestCommands.ToList();
            if (test.Count == 0 || test.Any(c => c.Contains("dotnet", StringComparison.OrdinalIgnoreCase)))
                test = new List<string> { "python -m pytest -q" };

            return new GenerationPlan(
                plan.ApplicationName,
                plan.ApplicationDescription,
                plan.TechStack,
                plan.Phases,
                plan.RequiredAgents,
                runtime,
                build,
                test,
                plan.MaxIterations);
        }

        if (IsNodePrimary(plan.TechStack))
        {
            var runtime = plan.RuntimeImage;
            if (string.IsNullOrWhiteSpace(runtime) || runtime.Contains("dotnet", StringComparison.OrdinalIgnoreCase))
                runtime = "node:22-alpine";

            var build = plan.BuildCommands.ToList();
            if (build.Count == 0 || build.Any(c => c.Contains("dotnet", StringComparison.OrdinalIgnoreCase)))
                build = new List<string> { "npm ci" };

            var test = plan.TestCommands.ToList();
            if (test.Count == 0 || test.Any(c => c.Contains("dotnet", StringComparison.OrdinalIgnoreCase)))
                test = new List<string> { "npm test --silent" };

            return new GenerationPlan(
                plan.ApplicationName,
                plan.ApplicationDescription,
                plan.TechStack,
                plan.Phases,
                plan.RequiredAgents,
                runtime,
                build,
                test,
                plan.MaxIterations);
        }

        return plan;
    }

    private static bool IsPythonPrimary(TechStack ts)
    {
        var hasPythonLang = ts.Languages.Any(l =>
            l.Contains("python", StringComparison.OrdinalIgnoreCase) ||
            l.Equals("py", StringComparison.OrdinalIgnoreCase));
        var hasPythonFw = ts.Frameworks.Any(f =>
            f.Contains("flask", StringComparison.OrdinalIgnoreCase) ||
            f.Contains("django", StringComparison.OrdinalIgnoreCase) ||
            f.Contains("fastapi", StringComparison.OrdinalIgnoreCase));
        if (!hasPythonLang && !hasPythonFw) return false;
        var hasCsharp = ts.Languages.Any(l =>
            l.Contains("c#", StringComparison.OrdinalIgnoreCase) ||
            l.Contains("csharp", StringComparison.OrdinalIgnoreCase));
        return !hasCsharp;
    }

    private static bool IsNodePrimary(TechStack ts) =>
        !IsPythonPrimary(ts)
        && (ts.Languages.Any(l =>
                l.Contains("javascript", StringComparison.OrdinalIgnoreCase) ||
                l.Contains("typescript", StringComparison.OrdinalIgnoreCase) ||
                l.Equals("node", StringComparison.OrdinalIgnoreCase))
            || ts.Frameworks.Any(f =>
                f.Contains("express", StringComparison.OrdinalIgnoreCase) ||
                f.Contains("next", StringComparison.OrdinalIgnoreCase)))
        && !ts.Languages.Any(l => l.Contains("c#", StringComparison.OrdinalIgnoreCase) || l.Contains("csharp", StringComparison.OrdinalIgnoreCase));

    private static GenerationPlan FallbackPlan(string userRequest)
    {
        var repoBootstrap = IsRepoBootstrapRequest(userRequest);
        var requiresAuth = MentionsAuth(userRequest);
        var requiresKanban = MentionsKanban(userRequest);

        var description = userRequest;
        if (repoBootstrap)
        {
            description +=
                "\n[[REPO_BOOTSTRAP_REQUIRED]] " +
                "Use discovered upstream repository adaptation with explicit evidence; reject generic template output.";
        }

        if (requiresAuth)
            description += "\n[[AUTH_REQUIRED]] Implement JWT auth with token issuance and protected endpoints.";
        if (requiresKanban)
            description += "\n[[KANBAN_REQUIRED]] Implement board/columns/tasks with transition operations.";

        return new GenerationPlan(
            applicationName: "GeneratedApp",
            applicationDescription: description,
            techStack: DefaultTechStack(),
            phases: DefaultPhases(repoBootstrap, requiresAuth, requiresKanban),
            requiredAgents: DefaultAgents(repoBootstrap),
            runtimeImage: "mcr.microsoft.com/dotnet/sdk:8.0",
            buildCommands: new[] { "dotnet restore", "dotnet build" },
            testCommands: new[] { "dotnet test" },
            maxIterations: 8);
    }

    private static TechStack DefaultTechStack() =>
        new(
            languages: new List<string> { "C#" },
            frameworks: new List<string> { "ASP.NET Core", "EF Core" },
            databases: new List<string> { "PostgreSQL" },
            infrastructure: new List<string> { "Docker" },
            rationale: "Neutral default - the generated app can be in any stack; this is only used when the planner returns nothing usable.");

    /// <summary>Rough language-to-image mapping used when the planner didn't pick one.</summary>
    private static string GuessImage(IReadOnlyList<string> languages)
    {
        if (languages.Count == 0) return "mcr.microsoft.com/dotnet/sdk:8.0";
        var first = languages[0].ToLowerInvariant();
        return first switch
        {
            "python" or "py" => "python:3.12-slim",
            "javascript" or "typescript" or "node" or "js" or "ts" => "node:22-alpine",
            "go" or "golang" => "golang:1.23-alpine",
            "rust" or "rs" => "rust:1.80",
            "java" or "kotlin" => "eclipse-temurin:21-jdk",
            "ruby" => "ruby:3.3-slim",
            "php" => "php:8.3-cli",
            _ => "mcr.microsoft.com/dotnet/sdk:8.0"
        };
    }

    private static List<string> DefaultAgents(bool repoBootstrap) =>
        new List<string>
        {
            "TaskDecompositionAgent",
            "CodeGenerationAgent",
            "CodeReviewAgent",
            "SecurityTestingAgent",
            "SemanticBlameAgent",
            "DatabaseDesignAgent",
            "CICDPipelineAgent",
            "ObservabilityAgent"
        }.Concat(repoBootstrap ? new[] { "WebSearchAgent" } : Array.Empty<string>()).Distinct().ToList();

    private static List<GenerationPhase> DefaultPhases(bool repoBootstrap, bool requiresAuth, bool requiresKanban)
    {
        var phases = new List<GenerationPhase>
        {
            new GenerationPhase(1, "Scaffold",
                "Create project structure, csproj, entry point.",
                new List<AgentAssignment>
                {
                    new("TaskDecompositionAgent", "Planner", "Break down the request into concrete subtasks"),
                    new("CodeGenerationAgent", "Generator", "Produce the initial scaffold"),
                    new("DatabaseDesignAgent", "Designer", "Design database schema and relationships")
                }),
            new GenerationPhase(2, "Implement core",
                "Domain model + main features.",
                new List<AgentAssignment>
                {
                    new("CodeGenerationAgent", "Generator", "Implement domain and use cases"),
                    new("ArchitecturalGuardrailsAgent", "Reviewer", "Enforce DDD layering")
                }),
            new GenerationPhase(3, "Tests",
                "Unit + integration tests runnable in the shadow workspace.",
                new List<AgentAssignment>
                {
                    new("CodeGenerationAgent", "Generator", "Write unit and integration tests")
                }),
            new GenerationPhase(4, "Security & review",
                "Static security checks and code review.",
                new List<AgentAssignment>
                {
                    new("SecurityTestingAgent", "Tester", "Static security scan + common vulnerability checks"),
                    new("CodeReviewAgent", "Reviewer", "General code review"),
                    new("SemanticBlameAgent", "Fixer", "Diagnose any runtime errors from shadow execution"),
                    new("CICDPipelineAgent", "DevOps", "Generate CI/CD pipeline configuration"),
                    new("ObservabilityAgent", "SRE", "Design monitoring and alerting strategy")
                })
        };

        if (repoBootstrap)
        {
            phases.Insert(0, new GenerationPhase(
                1,
                "Repo bootstrap & adaptation",
                "Discover permissive-license upstream repository and adapt code with evidence.",
                new List<AgentAssignment>
                {
                    new("WebSearchAgent", "Discovery", "Discover upstream GitHub repository and capture license evidence"),
                    new("CodeGenerationAgent", "Adaptation", "Adapt upstream code instead of creating template-only scaffold"),
                    new("CodeReviewAgent", "Verification", "Verify adaptation evidence and remove generic placeholder output")
                }));
        }

        if (requiresAuth || requiresKanban)
        {
            phases.Add(new GenerationPhase(
                phases.Count + 1,
                "Business hardening",
                "Implement explicit auth/kanban business workflows and corresponding tests.",
                new List<AgentAssignment>
                {
                    new("CodeGenerationAgent", "Generator", "Implement requested business features and tests"),
                    new("CodeReviewAgent", "Reviewer", "Check feature completeness against request"),
                    new("SecurityTestingAgent", "Tester", "Validate auth boundaries and failure modes")
                }));
        }

        for (var i = 0; i < phases.Count; i++)
        {
            var phase = phases[i];
            phases[i] = new GenerationPhase(i + 1, phase.Name, phase.Description, phase.Assignments);
        }

        return phases;
    }

    private static bool IsRepoBootstrapRequest(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return false;

        return text.Contains("repo_bootstrap_context", StringComparison.OrdinalIgnoreCase)
               || text.Contains("github", StringComparison.OrdinalIgnoreCase)
               || text.Contains("repository", StringComparison.OrdinalIgnoreCase)
               || text.Contains("obscura", StringComparison.OrdinalIgnoreCase)
               || text.Contains("open-source", StringComparison.OrdinalIgnoreCase)
               || text.Contains("opensource", StringComparison.OrdinalIgnoreCase)
               || text.Contains("лиценз", StringComparison.OrdinalIgnoreCase)
               || text.Contains("репозитор", StringComparison.OrdinalIgnoreCase);
    }

    private static bool MentionsAuth(string text) =>
        !string.IsNullOrWhiteSpace(text) &&
        (text.Contains("auth", StringComparison.OrdinalIgnoreCase)
         || text.Contains("jwt", StringComparison.OrdinalIgnoreCase)
         || text.Contains("login", StringComparison.OrdinalIgnoreCase)
         || text.Contains("token", StringComparison.OrdinalIgnoreCase)
         || text.Contains("авториза", StringComparison.OrdinalIgnoreCase));

    private static bool MentionsKanban(string text) =>
        !string.IsNullOrWhiteSpace(text) &&
        (text.Contains("kanban", StringComparison.OrdinalIgnoreCase)
         || text.Contains("board", StringComparison.OrdinalIgnoreCase)
         || text.Contains("backlog", StringComparison.OrdinalIgnoreCase)
         || text.Contains("column", StringComparison.OrdinalIgnoreCase)
         || text.Contains("доска", StringComparison.OrdinalIgnoreCase)
         || text.Contains("канбан", StringComparison.OrdinalIgnoreCase));
}
