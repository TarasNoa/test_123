using System.Text.Json;
using Libr4.AI.Application.Abstractions;
using Libr4.IDE.Application.AutonomousAppGeneration.Infrastructure;
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

    public AutonomousCascadePlanner()
    {
        _options = new CascadePlannerOptions();
    }

    public AutonomousCascadePlanner(
        IServiceScopeFactory scopeFactory,
        ILogger<AutonomousCascadePlanner> logger,
        IOptions<CascadePlannerOptions>? options = null)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _options = options?.Value ?? new CascadePlannerOptions();
    }

    public CascadeExecutionPlan Build(GenerationPlan plan, string userRequest)
    {
        if (_options.EnableLlmAssistedPass && TryBuildLlmAssisted(plan, userRequest, out var llmPlan))
            return llmPlan;

        return BuildDeterministic(plan, userRequest);
    }

    private CascadeExecutionPlan BuildDeterministic(GenerationPlan plan, string userRequest)
    {
        var ordered = plan.Phases
            .OrderBy(p => p.Order)
            .ToList();

        if (ordered.Count == 0)
        {
            var fallback = BuildFallback();
            return new CascadeExecutionPlan(
                fallback,
                "Fallback cascade plan generated due to empty phase list.",
                Serialize(fallback, "fallback"));
        }

        var phases = new List<CascadeExecutionPhase>(ordered.Count);
        for (var i = 0; i < ordered.Count; i++)
        {
            var src = ordered[i];
            var phaseId = BuildPhaseId(src.Name, i);
            var deps = InferDependencies(ordered, i);
            var instructions = BuildInstructions(src, plan, userRequest);
            var expected = BuildExpectedOutput(src);

            phases.Add(new CascadeExecutionPhase(
                phaseId,
                src.Name,
                src.Description,
                deps,
                expected,
                instructions));
        }

        var rationale =
            "Cascade planning enabled: dependencies are inferred by phase semantics and preserved as a DAG, " +
            "so build/test loops receive structured upstream context instead of purely sequential steps.";

        return new CascadeExecutionPlan(
            phases,
            rationale,
            Serialize(phases, rationale),
            RoutingProfile: "deterministic",
            ModelHint: null,
            PlannerMode: "deterministic");
    }

    private bool TryBuildLlmAssisted(GenerationPlan plan, string userRequest, out CascadeExecutionPlan cascadePlan)
    {
        cascadePlan = default!;
        if (_scopeFactory is null)
            return false;

        try
        {
            using var scope = _scopeFactory.CreateScope();
            var ai = scope.ServiceProvider.GetService<IAIService>();
            if (ai is null)
                return false;

            var (routingProfile, modelHint) = ResolveModelRoute();
            var webPrefetch = TryBuildWebPrefetchContext(scope, userRequest);
            var prompt = BuildLlmOrchestratorPrompt(plan, userRequest, webPrefetch);
            var raw = ai.GenerateCompletionAsync(prompt, CascadeSystemPrompt, modelHint).GetAwaiter().GetResult();
            using var doc = LlmJsonHelpers.ExtractJson(raw ?? string.Empty);
            if (doc is null)
                return false;

            if (!TryMapLlmPlan(plan, userRequest, doc.RootElement, routingProfile, modelHint, out cascadePlan))
                return false;

            return true;
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "LLM-assisted cascade planning failed, fallback to deterministic DAG.");
            return false;
        }
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
        string userRequest)
    {
        var d = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["stack_languages"] = string.Join(",", plan.TechStack.Languages),
            ["stack_frameworks"] = string.Join(",", plan.TechStack.Frameworks),
            ["build_commands"] = string.Join(" && ", plan.BuildCommands),
            ["test_commands"] = string.Join(" && ", plan.TestCommands),
            ["focus"] = InferFocus(phase.Name),
            ["request_fingerprint_hint"] = Truncate(userRequest, 180)
        };
        return d;
    }

    private static string InferFocus(string phaseName)
    {
        var n = phaseName.ToLowerInvariant();
        if (n.Contains("plan")) return "architecture_and_scope";
        if (n.Contains("model") || n.Contains("database")) return "data_modeling";
        if (n.Contains("api") || n.Contains("backend") || n.Contains("service")) return "backend_contracts_and_logic";
        if (n.Contains("front")) return "ui_and_integration";
        if (n.Contains("test")) return "coverage_and_regression";
        if (n.Contains("security")) return "threat_model_and_guardrails";
        if (n.Contains("valid")) return "quality_and_acceptance";
        return "implementation";
    }

    private static string BuildExpectedOutput(GenerationPhase phase)
    {
        var n = phase.Name.ToLowerInvariant();
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
        if (!_options.EnableWebPrefetchContext)
            return null;

        try
        {
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

            var max = Math.Clamp(_options.MaxPrefetchContextChars, 120, 4000);
            return Truncate(outcome.ResultSummary, max);
        }
        catch (Exception ex)
        {
            _logger?.LogDebug(ex, "Cascade web-prefetch unavailable; continuing without it.");
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
            sb.AppendLine("Optional web-prefetch context (safe lane):");
            sb.AppendLine(webPrefetchContext);
        }
        return sb.ToString();
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
                    BuildExpectedOutput(source),
                    BuildInstructions(source, plan, userRequest)));
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

            var expected = LlmJsonHelpers.GetString(llmPhase, "expected_output", BuildExpectedOutput(source));
            var instructions = BuildInstructions(source, plan, userRequest);
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
            "api" => ("api", NormalizeModel(_options.ApiModel)),
            _ => ResolveAutoModelRoute()
        };
    }

    private (string RoutingProfile, string? ModelHint) ResolveAutoModelRoute()
    {
        var api = NormalizeModel(_options.ApiModel);
        if (!string.IsNullOrWhiteSpace(api))
            return ("auto->api", api);
        return ("auto->local", NormalizeModel(_options.LocalModel));
    }

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

