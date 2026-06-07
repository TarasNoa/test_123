using System.Text.Json;
using Libr4.AI.Application.Abstractions;
using Libr4.IDE.Application.AutonomousAppGeneration.Infrastructure;
using Libr4.IDE.Application.AutonomousAppGeneration.Services;
using Libr4.IDE.Application.AutonomousAppGeneration.Services.PlatformUtilization;
using Libr4.IDE.Domain.AutonomousAppGeneration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Libr4.IDE.Application.AutonomousAppGeneration.AgentIntegration;

/// <summary>
/// Deterministic cascade-style orchestrator pass for autonomous generation.
/// Produces explicit phase dependencies and instructions so agent execution is not a simple linear chain.
/// </summary>
public sealed class AutonomousCascadePlanner : IAutonomousCascadePlanner
{
    private const string CascadeSystemPrompt = """
You are a cascade orchestrator planner.
Return ONLY strict JSON:
{
  "rationale": "string",
  "phases": [
    {
      "phase_name": "existing phase name from input",
      "dependencies": ["phase name", "..."],
      "expected_output": "string",
      "instructions": { "key": "value" }
    }
  ]
}
Rules:
- Use only provided phase names.
- Keep DAG (no self dependency).
- If uncertain, keep dependencies minimal and valid.
- No markdown, no extra prose.
""";

    private readonly IServiceScopeFactory? _scopeFactory;
    private readonly ILogger<AutonomousCascadePlanner>? _logger;
    private readonly CascadePlannerOptions _options;
    private readonly AutonomousBenchmarkModeOptions _benchmarkModeOptions;
    private readonly AutonomousPlatformUtilizationOptions _platformUtilizationOptions;

    public AutonomousCascadePlanner()
    {
        _options = new CascadePlannerOptions();
        _benchmarkModeOptions = new AutonomousBenchmarkModeOptions();
        _platformUtilizationOptions = new AutonomousPlatformUtilizationOptions();
    }

    public AutonomousCascadePlanner(
        IServiceScopeFactory scopeFactory,
        ILogger<AutonomousCascadePlanner> logger,
        IOptions<CascadePlannerOptions>? options = null,
        IOptions<AutonomousBenchmarkModeOptions>? benchmarkModeOptions = null,
        IOptions<AutonomousPlatformUtilizationOptions>? platformUtilizationOptions = null)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _options = options?.Value ?? new CascadePlannerOptions();
        _benchmarkModeOptions = benchmarkModeOptions?.Value ?? new AutonomousBenchmarkModeOptions();
        _platformUtilizationOptions = platformUtilizationOptions?.Value ?? new AutonomousPlatformUtilizationOptions();
    }

    public CascadeExecutionPlan Build(GenerationPlan plan, string userRequest)
    {
        if (BenchmarkExecutionPathPolicy.UseDeterministicCascadeOnly(
                _benchmarkModeOptions,
                _platformUtilizationOptions))
        {
            _logger?.LogInformation(
                "Benchmark execution path: cascade planning uses deterministic planner (LLM skipped).");
            return BuildDeterministic(plan, userRequest);
        }

        if (!_options.EnableLlmAssistedPass || _scopeFactory is null)
            return BuildDeterministic(plan, userRequest);

        try
        {
            return BuildLlmAssistedOrThrow(plan, userRequest);
        }
        catch (AutonomousGenerationFailedException ex)
        {
            _logger?.LogWarning(
                ex,
                "Cascade LLM failed ({Stage}); falling back to deterministic cascade.",
                ex.Stage);
            return BuildDeterministic(plan, userRequest);
        }
        catch (Exception ex) when (BenchmarkExecutionPathPolicy.ShouldFallbackOnLlmInfrastructureFailure(
            _benchmarkModeOptions,
            BenchmarkExecutionPathPolicy.Stages.CascadePlanning,
            ex,
            _platformUtilizationOptions))
        {
            _logger?.LogWarning(
                ex,
                "Cascade LLM failed ({Message}); benchmark optional stage — falling back to deterministic cascade.",
                ex.Message);
            return BuildDeterministic(plan, userRequest);
        }
    }

    private CascadeExecutionPlan BuildLlmAssistedOrThrow(GenerationPlan plan, string userRequest)
    {
        if (_scopeFactory is null)
        {
            throw new AutonomousGenerationFailedException(
                "cascade_planning",
                "LLM-assisted cascade planning is enabled but no service scope factory is configured.");
        }

        using var scope = _scopeFactory.CreateScope();
        var ai = scope.ServiceProvider.GetService<IAIService>()
                 ?? throw new AutonomousGenerationFailedException(
                     "cascade_planning",
                     "LLM-assisted cascade planning requires IAIService.");

        try
        {
            var (routingProfile, modelHint) = ResolveModelRoute();
            var webPrefetch = TryBuildWebPrefetchContext(scope, userRequest);
            var prompt = PlatformCapabilityBriefingScope.AppendToPrompt(
                BuildLlmOrchestratorPrompt(plan, userRequest, webPrefetch),
                PlatformCapabilityBriefingStage.CascadePlanning);
            var raw = ai.GenerateCompletionAsync(prompt, CascadeSystemPrompt, modelHint).GetAwaiter().GetResult();
            using var doc = LlmJsonHelpers.ExtractJson(raw ?? string.Empty);
            if (doc is null)
            {
                throw new AutonomousGenerationFailedException(
                    "cascade_planning",
                    $"Cascade planner returned unparseable JSON. parse={LlmJsonHelpers.LastParseError ?? "unknown"}");
            }

            if (!TryMapLlmPlan(plan, userRequest, doc.RootElement, routingProfile, modelHint, out var cascadePlan))
            {
                throw new AutonomousGenerationFailedException(
                    "cascade_planning",
                    "Cascade planner JSON failed strict mapping validation.");
            }

            return cascadePlan;
        }
        catch (AutonomousGenerationFailedException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "LLM-assisted cascade planning failed");
            throw new AutonomousGenerationFailedException(
                "cascade_planning",
                $"LLM-assisted cascade planning failed: {ex.Message}",
                ex);
        }
    }

    private CascadeExecutionPlan BuildDeterministic(GenerationPlan plan, string userRequest)
    {
        var repoBootstrap = RequiresRepoBootstrap(plan, userRequest);
        var ordered = plan.Phases
            .OrderBy(p => p.Order)
            .ToList();

        if (ordered.Count == 0)
        {
            throw new AutonomousGenerationFailedException(
                "cascade_planning",
                "Cannot build cascade plan: generation plan has no phases.");
        }

        var phases = new List<CascadeExecutionPhase>(ordered.Count);
        for (var i = 0; i < ordered.Count; i++)
        {
            var src = ordered[i];
            var phaseId = BuildPhaseId(src.Name, i);
            var deps = InferDependencies(ordered, i);
            var instructions = BuildInstructions(src, plan, userRequest, repoBootstrap);
            var expected = BuildExpectedOutput(src, repoBootstrap);

            phases.Add(new CascadeExecutionPhase(
                phaseId,
                src.Name,
                src.Description,
                deps,
                expected,
                instructions));
        }

        var rationale = repoBootstrap
            ? "Repo-bootstrap cascade: phases enforce upstream adaptation, JWT auth, kanban domain, and business-test evidence (no generic scaffold-only output)."
            : "Cascade planning enabled: dependencies are inferred by phase semantics and preserved as a DAG, " +
              "so build/test loops receive structured upstream context instead of purely sequential steps.";

        return new CascadeExecutionPlan(
            phases,
            rationale,
            Serialize(phases, rationale),
            RoutingProfile: "deterministic",
            ModelHint: null,
            PlannerMode: "deterministic");
    }

    private static IReadOnlyList<string> InferDependencies(IReadOnlyList<GenerationPhase> phases, int idx)
    {
        if (idx == 0)
            return Array.Empty<string>();

        var current = phases[idx].Name;
        var deps = new List<string>();
        for (var i = 0; i < idx; i++)
        {
            var prev = phases[i].Name;
            if (ShouldDependOn(current, prev))
                deps.Add(BuildPhaseId(prev, i));
        }

        if (deps.Count == 0)
            deps.Add(BuildPhaseId(phases[idx - 1].Name, idx - 1));

        return deps;
    }

    private static bool ShouldDependOn(string current, string previous)
    {
        var c = current.ToLowerInvariant();
        var p = previous.ToLowerInvariant();

        if ((c.Contains("scaffold") || c.Contains("implement") || c.Contains("core"))
            && (p.Contains("bootstrap") || p.Contains("adapt")))
            return true;
        if (c.Contains("test") || c.Contains("qa") || c.Contains("validation"))
            return true;
        if (c.Contains("frontend") && (p.Contains("api") || p.Contains("backend")))
            return true;
        if ((c.Contains("service") || c.Contains("api") || c.Contains("backend")) &&
            (p.Contains("model") || p.Contains("data") || p.Contains("database") || p.Contains("schema")))
            return true;
        if (c.Contains("security") && (p.Contains("test") || p.Contains("validation")))
            return true;
        if (c.Contains("deploy") || c.Contains("release"))
            return p.Contains("test") || p.Contains("validation") || p.Contains("security");

        return false;
    }

    private static Dictionary<string, string> BuildInstructions(
        GenerationPhase phase,
        GenerationPlan plan,
        string userRequest,
        bool repoBootstrap)
    {
        var d = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["stack_languages"] = string.Join(",", plan.TechStack.Languages),
            ["stack_frameworks"] = string.Join(",", plan.TechStack.Frameworks),
            ["build_commands"] = string.Join(" && ", plan.BuildCommands),
            ["test_commands"] = string.Join(" && ", plan.TestCommands),
            ["focus"] = InferFocus(phase.Name, repoBootstrap),
            ["request_fingerprint_hint"] = Truncate(userRequest, 180)
        };
        if (repoBootstrap)
            AppendRepoBootstrapInstructions(d, phase);
        return d;
    }

    private static void AppendRepoBootstrapInstructions(Dictionary<string, string> instructions, GenerationPhase phase)
    {
        instructions["repo_bootstrap_mode"] = "required";
        instructions["reject_generic_template"] = "true";
        instructions["require_bootstrap_evidence"] = "BOOTSTRAP_EVIDENCE.md";
        instructions["require_jwt_auth"] = "true";
        instructions["require_kanban_domain"] = "true";
        instructions["require_business_tests"] = "auth+kanban";

        var phaseName = phase.Name.ToLowerInvariant();
        if (phaseName.Contains("bootstrap") || phaseName.Contains("adapt"))
        {
            instructions["deliverable"] =
                "Clone/adapt upstream permissive-license repository; preserve license and source evidence.";
        }
        else if (phaseName.Contains("test"))
        {
            instructions["deliverable"] =
                "Business tests for JWT auth and kanban transitions (not health-only smoke tests).";
        }
        else if (phaseName.Contains("implement") || phaseName.Contains("core"))
        {
            instructions["deliverable"] =
                "AuthController + KanbanController + task/board domain wired to adapted upstream code.";
        }
    }

    private static string InferFocus(string phaseName, bool repoBootstrap)
    {
        var n = phaseName.ToLowerInvariant();
        if (repoBootstrap && (n.Contains("bootstrap") || n.Contains("adapt")))
            return "repo_adaptation";
        if (n.Contains("plan")) return "architecture_and_scope";
        if (n.Contains("model") || n.Contains("database")) return "data_modeling";
        if (n.Contains("api") || n.Contains("backend") || n.Contains("service")) return "backend_contracts_and_logic";
        if (n.Contains("front")) return "ui_and_integration";
        if (n.Contains("test")) return "coverage_and_regression";
        if (n.Contains("security")) return "threat_model_and_guardrails";
        if (n.Contains("valid")) return "quality_and_acceptance";
        return "implementation";
    }

    private static string BuildExpectedOutput(GenerationPhase phase, bool repoBootstrap)
    {
        var n = phase.Name.ToLowerInvariant();
        if (repoBootstrap && (n.Contains("bootstrap") || n.Contains("adapt")))
            return "BOOTSTRAP_EVIDENCE.md plus adapted upstream repository integration artifacts.";
        if (repoBootstrap && n.Contains("scaffold"))
            return "Scaffold aligned to adapted upstream repository layout (not blank template output).";
        if (repoBootstrap && (n.Contains("implement") || n.Contains("core")))
            return "JWT auth endpoints, kanban board/columns/tasks APIs, and persistence wiring.";
        if (repoBootstrap && (n.Contains("test") || n.Contains("qa")))
            return "Runnable business tests covering auth token flow and kanban column transitions.";
        if (n.Contains("plan")) return "Structured implementation plan with explicit constraints and acceptance criteria.";
        if (n.Contains("model") || n.Contains("database")) return "Data layer artifacts and schema-consistent models.";
        if (n.Contains("api") || n.Contains("backend")) return "Executable service/API code mapped to domain requirements.";
        if (n.Contains("front")) return "UI components wired to backend contracts.";
        if (n.Contains("test")) return "Runnable tests aligned with planned commands and stack.";
        if (n.Contains("security")) return "Security review findings and remediation-ready hardening updates.";
        if (n.Contains("valid")) return "Final quality gate evidence and release-readiness summary.";
        return "Complete phase artifacts with traceable evidence.";
    }

    private static string BuildPhaseId(string name, int idx)
    {
        var slug = new string(name
            .ToLowerInvariant()
            .Select(ch => char.IsLetterOrDigit(ch) ? ch : '_')
            .ToArray())
            .Trim('_');
        if (string.IsNullOrWhiteSpace(slug))
            slug = $"phase_{idx + 1}";
        return $"phase_{idx + 1}_{slug}";
    }

    private static List<CascadeExecutionPhase> BuildFallback() =>
        new()
        {
            new("phase_1_planning", "Planning", "Plan implementation", Array.Empty<string>(),
                "Actionable plan", new Dictionary<string, string> { ["focus"] = "architecture_and_scope" }),
            new("phase_2_implementation", "Implementation", "Generate core code", new[] { "phase_1_planning" },
                "Core artifacts", new Dictionary<string, string> { ["focus"] = "implementation" }),
            new("phase_3_testing", "Testing", "Validate with tests", new[] { "phase_2_implementation" },
                "Test evidence", new Dictionary<string, string> { ["focus"] = "coverage_and_regression" }),
        };

    private static string Serialize(IReadOnlyList<CascadeExecutionPhase> phases, string rationale) =>
        JsonSerializer.Serialize(
            new
            {
                phases = phases.Select(p => new
                {
                    phase_id = p.PhaseId,
                    phase_name = p.PhaseName,
                    description = p.Description,
                    dependencies = p.Dependencies,
                    expected_output = p.ExpectedOutput,
                    instructions = p.Instructions
                }),
                rationale
            },
            new JsonSerializerOptions { WriteIndented = true });

    private string? TryBuildWebPrefetchContext(IServiceScope scope, string userRequest)
    {
        if (!_platformUtilizationOptions.EnableCascadePrefetch)
            return null;

        if (!_options.EnableWebPrefetchContext && !_options.EnableCodebasePrefetchContext)
            return null;

        try
        {
            var max = Math.Clamp(_options.MaxPrefetchContextChars, 120, 4000);
            var sections = new List<string>();

            if (_options.EnableWebPrefetchContext)
            {
                var web = TryBuildBrowserPrefetchContext(scope, userRequest, max);
                if (!string.IsNullOrWhiteSpace(web))
                    sections.Add(web);
            }

            if (_options.EnableCodebasePrefetchContext)
            {
                var codebase = scope.ServiceProvider.GetService<ICascadeCodebasePrefetchService>();
                if (codebase is not null)
                {
                    var codebaseContext = codebase.BuildPrefetchContextAsync(userRequest, max, CancellationToken.None)
                        .GetAwaiter()
                        .GetResult();
                    if (!string.IsNullOrWhiteSpace(codebaseContext))
                        sections.Add(codebaseContext);
                }
            }

            if (sections.Count == 0)
                return null;

            return Truncate(string.Join("\n\n", sections), max);
        }
        catch (Exception ex)
        {
            _logger?.LogDebug(ex, "Cascade prefetch unavailable; continuing without it.");
            return null;
        }
    }

    private string? TryBuildBrowserPrefetchContext(IServiceScope scope, string userRequest, int max)
    {
        try
        {
            if (string.Equals(_options.PrefetchToolName, "browser_research", StringComparison.OrdinalIgnoreCase))
            {
                var native = scope.ServiceProvider.GetService<ICascadeWebPrefetchService>();
                if (native is not null)
                {
                    var nativeContext = native.BuildPrefetchContextAsync(userRequest, max, CancellationToken.None)
                        .GetAwaiter()
                        .GetResult();
                    if (!string.IsNullOrWhiteSpace(nativeContext))
                        return nativeContext;
                }
            }

            var mcp = scope.ServiceProvider.GetService<IMcpToolInvocationService>();
            if (mcp is null)
                return null;

            var args = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
            {
                ["query"] = Truncate(userRequest, 180),
                ["mode"] = "cascade_prefetch",
                ["limit"] = 3
            };
            var outcome = mcp.InvokeStandaloneAsync(
                userRequestContext: userRequest,
                toolName: _options.PrefetchToolName,
                arguments: args,
                ct: CancellationToken.None).GetAwaiter().GetResult();
            if (!outcome.Succeeded || string.IsNullOrWhiteSpace(outcome.ResultSummary))
                return null;

            return Truncate(outcome.ResultSummary, max);
        }
        catch (Exception ex)
        {
            _logger?.LogDebug(ex, "Cascade browser-prefetch unavailable; continuing without it.");
            return null;
        }
    }

    private static string BuildLlmOrchestratorPrompt(GenerationPlan plan, string userRequest, string? webPrefetchContext)
    {
        var ordered = plan.Phases.OrderBy(p => p.Order).ToList();
        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"User request: {Truncate(userRequest, 300)}");
        sb.AppendLine($"Stack languages: {string.Join(",", plan.TechStack.Languages)}");
        sb.AppendLine($"Stack frameworks: {string.Join(",", plan.TechStack.Frameworks)}");
        sb.AppendLine($"Build commands: {string.Join(" && ", plan.BuildCommands)}");
        sb.AppendLine($"Test commands: {string.Join(" && ", plan.TestCommands)}");
        sb.AppendLine("Existing ordered phases (must reuse names exactly):");
        foreach (var phase in ordered)
            sb.AppendLine($"- {phase.Name}: {phase.Description}");
        if (!string.IsNullOrWhiteSpace(webPrefetchContext))
        {
            sb.AppendLine("Optional cascade prefetch context (browser_research + search_codebase):");
            sb.AppendLine(webPrefetchContext);
        }
        if (RequiresRepoBootstrap(plan, userRequest))
        {
            sb.AppendLine("REPO BOOTSTRAP HARD REQUIREMENTS:");
            sb.AppendLine("- Adapt upstream repository; do not emit generic template-only scaffold.");
            sb.AppendLine("- Include BOOTSTRAP_EVIDENCE.md with repository_url, license, adaptation_summary.");
            sb.AppendLine("- Implement JWT auth + kanban board/columns/tasks with business tests.");
        }
        return sb.ToString();
    }

    private static bool RequiresRepoBootstrap(GenerationPlan plan, string userRequest)
    {
        var blob = $"{plan.ApplicationDescription}\n{userRequest}";
        return blob.Contains("repo_bootstrap_context", StringComparison.OrdinalIgnoreCase)
               || blob.Contains("[[REPO_BOOTSTRAP_REQUIRED]]", StringComparison.OrdinalIgnoreCase)
               || blob.Contains("github", StringComparison.OrdinalIgnoreCase)
               || blob.Contains("obscura", StringComparison.OrdinalIgnoreCase)
               || blob.Contains("open-source", StringComparison.OrdinalIgnoreCase)
               || blob.Contains("opensource", StringComparison.OrdinalIgnoreCase)
               || blob.Contains("репозитор", StringComparison.OrdinalIgnoreCase);
    }

    private static bool TryMapLlmPlan(
        GenerationPlan plan,
        string userRequest,
        JsonElement root,
        string routingProfile,
        string? modelHint,
        out CascadeExecutionPlan result)
    {
        result = default!;
        if (!root.TryGetProperty("phases", out var phasesEl) || phasesEl.ValueKind != JsonValueKind.Array)
            return false;

        var ordered = plan.Phases.OrderBy(p => p.Order).ToList();
        if (ordered.Count == 0)
            return false;

        var known = ordered
            .Select((p, idx) => new { Key = NormalizeKey(p.Name), Index = idx, Phase = p, Id = BuildPhaseId(p.Name, idx) })
            .ToDictionary(x => x.Key, x => x, StringComparer.Ordinal);

        var llmByKey = new Dictionary<string, JsonElement>(StringComparer.Ordinal);
        foreach (var item in phasesEl.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.Object)
                continue;
            var phaseName = LlmJsonHelpers.GetString(item, "phase_name", string.Empty);
            var key = NormalizeKey(phaseName);
            if (key.Length == 0 || !known.ContainsKey(key))
                continue;
            llmByKey[key] = item;
        }

        if (llmByKey.Count == 0)
            return false;

        var phases = new List<CascadeExecutionPhase>(ordered.Count);
        for (var i = 0; i < ordered.Count; i++)
        {
            var source = ordered[i];
            var key = NormalizeKey(source.Name);
            var phaseId = BuildPhaseId(source.Name, i);

            if (!llmByKey.TryGetValue(key, out var llmPhase))
            {
                var inferredDeps = InferDependencies(ordered, i);
                phases.Add(new CascadeExecutionPhase(
                    phaseId,
                    source.Name,
                    source.Description,
                    inferredDeps,
                    BuildExpectedOutput(source, RequiresRepoBootstrap(plan, userRequest)),
                    BuildInstructions(source, plan, userRequest, RequiresRepoBootstrap(plan, userRequest))));
                continue;
            }

            var deps = new List<string>();
            if (llmPhase.TryGetProperty("dependencies", out var depsEl) && depsEl.ValueKind == JsonValueKind.Array)
            {
                foreach (var dep in depsEl.EnumerateArray())
                {
                    if (dep.ValueKind != JsonValueKind.String)
                        continue;
                    var depKey = NormalizeKey(dep.GetString() ?? string.Empty);
                    if (depKey.Length == 0 || depKey == key)
                        continue;
                    if (!known.TryGetValue(depKey, out var depMeta))
                        continue;
                    if (depMeta.Index > i)
                        continue; // enforce acyclic topological dependency.
                    deps.Add(depMeta.Id);
                }
            }

            deps = deps
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            if (deps.Count == 0 && i > 0)
                deps.Add(BuildPhaseId(ordered[i - 1].Name, i - 1));

            var repoBootstrap = RequiresRepoBootstrap(plan, userRequest);
            var expected = LlmJsonHelpers.GetString(llmPhase, "expected_output", BuildExpectedOutput(source, repoBootstrap));
            var instructions = BuildInstructions(source, plan, userRequest, repoBootstrap);
            if (llmPhase.TryGetProperty("instructions", out var instrEl) && instrEl.ValueKind == JsonValueKind.Object)
            {
                foreach (var prop in instrEl.EnumerateObject())
                {
                    if (prop.Value.ValueKind != JsonValueKind.String)
                        continue;
                    instructions[prop.Name] = prop.Value.GetString() ?? string.Empty;
                }
            }

            phases.Add(new CascadeExecutionPhase(
                phaseId,
                source.Name,
                source.Description,
                deps,
                expected,
                instructions));
        }

        var rationale = LlmJsonHelpers.GetString(
            root,
            "rationale",
            "LLM-assisted cascade DAG validated by strict parser with deterministic fallback.");
        result = new CascadeExecutionPlan(
            phases,
            rationale,
            Serialize(phases, rationale),
            RoutingProfile: routingProfile,
            ModelHint: modelHint,
            PlannerMode: "llm_assisted");
        return true;
    }

    private (string RoutingProfile, string? ModelHint) ResolveModelRoute()
    {
        var mode = (_options.ModelRoutingProfile ?? "auto").Trim().ToLowerInvariant();
        return mode switch
        {
            "local" => ("local", NormalizeModel(_options.LocalModel)),
            "api" or "openrouter" or "alibabacloud" => ("api", NormalizeModel(_options.ApiModel)),
            _ => ResolveAutoModelRoute()
        };
    }

    private (string RoutingProfile, string? ModelHint) ResolveAutoModelRoute()
    {
        var api = NormalizeModel(_options.ApiModel);
        if (!string.IsNullOrWhiteSpace(api) && !LooksLikeLocalRunnerModel(api))
            return ("auto->api", api);
        return ("auto->local", NormalizeModel(_options.LocalModel));
    }

    private static bool LooksLikeLocalRunnerModel(string model) =>
        model.Contains("huggingface.co/", StringComparison.OrdinalIgnoreCase)
        || model.Contains(":Q4_K_M", StringComparison.OrdinalIgnoreCase)
        || model.Contains(":Q8_", StringComparison.OrdinalIgnoreCase);

    private static string? NormalizeModel(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string NormalizeKey(string value) =>
        new string(value
            .ToLowerInvariant()
            .Where(char.IsLetterOrDigit)
            .ToArray());

    private static string Truncate(string text, int max)
    {
        if (string.IsNullOrWhiteSpace(text)) return string.Empty;
        var s = text.Replace('\r', ' ').Replace('\n', ' ');
        return s.Length <= max ? s : s[..max] + "…";
    }
}

