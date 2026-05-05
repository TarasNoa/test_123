using System.Text.Json;
using Libr4.AI.Infrastructure.AI;
using Libr4.IDE.Application.Cascade.Commands;
using Libr4.IDE.Application.Cascade.DTOs;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Libr4.IDE.Application.Cascade.Handlers;

/// <summary>
/// Handles cascade planning: decomposes a prompt into orchestration phases using the LLM.
/// Falls back to heuristic decomposition when the LLM is unavailable.
/// </summary>
public class RunCascadePlanningCommandHandler : IRequestHandler<RunCascadePlanningCommand, OrchestratorPlanDto>
{
    private readonly IAIService _aiService;
    private readonly ILogger<RunCascadePlanningCommandHandler> _logger;

    private const string SystemPrompt = """
        You are a senior software architect responsible for cascade planning.
        Given a task prompt, decompose it into concrete orchestration phases.
        Return ONLY valid JSON (no markdown fences) matching this schema:
        {
          "rationale": "<why these phases>",
          "phases": [
            {
              "phaseId": "<kebab-id>",
              "phaseName": "<short name>",
              "description": "<what happens>",
              "dependencies": ["<phaseId>"],
              "expectedOutput": "<artifact produced>"
            }
          ]
        }
        """;

    public RunCascadePlanningCommandHandler(
        IAIService aiService,
        ILogger<RunCascadePlanningCommandHandler> logger)
    {
        _aiService = aiService;
        _logger = logger;
    }

    public async Task<OrchestratorPlanDto> Handle(
        RunCascadePlanningCommand request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Running cascade planning for prompt: {Prompt} [complexity={Complexity}]",
            request.Prompt[..Math.Min(80, request.Prompt.Length)],
            request.Complexity);

        var userPrompt = BuildUserPrompt(request);
        var planId = $"cascade-{Guid.NewGuid():N}"[..24];

        List<OrchestratorPhaseDto> phases;
        string rationale;

        try
        {
            var json = await _aiService.GenerateCompletionAsync(userPrompt, SystemPrompt);

            (phases, rationale) = ParseLlmResponse(json, request);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "LLM cascade planning failed, using heuristic fallback");
            (phases, rationale) = HeuristicDecompose(request);
        }

        return new OrchestratorPlanDto
        {
            Id = Guid.NewGuid(),
            PlanId = planId,
            OriginalPrompt = request.Prompt,
            TaskDescription = request.TaskDescription,
            Subtasks = request.Subtasks,
            Complexity = request.Complexity,
            Phases = phases,
            PrefetchContext = new PrefetchContextDto
            {
                PrefetchEnabled = request.PrefetchWeb,
                PrefetchedAt = DateTime.UtcNow
            },
            OrchestratorJson = JsonSerializer.Serialize(phases),
            Rationale = rationale,
            CreatedAt = DateTime.UtcNow
        };
    }

    private static string BuildUserPrompt(RunCascadePlanningCommand request)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"Task: {request.Prompt}");
        if (!string.IsNullOrWhiteSpace(request.TaskDescription))
            sb.AppendLine($"Description: {request.TaskDescription}");
        if (request.Subtasks.Count > 0)
            sb.AppendLine($"Known subtasks: {string.Join(", ", request.Subtasks)}");
        sb.AppendLine($"Complexity: {request.Complexity}");
        return sb.ToString();
    }

    private static (List<OrchestratorPhaseDto> phases, string rationale) ParseLlmResponse(
        string json, RunCascadePlanningCommand request)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        var rationale = root.TryGetProperty("rationale", out var r) ? r.GetString() ?? string.Empty : string.Empty;
        var phases = new List<OrchestratorPhaseDto>();

        if (root.TryGetProperty("phases", out var arr) && arr.ValueKind == JsonValueKind.Array)
        {
            foreach (var el in arr.EnumerateArray())
            {
                var deps = new List<string>();
                if (el.TryGetProperty("dependencies", out var depsEl) && depsEl.ValueKind == JsonValueKind.Array)
                    foreach (var d in depsEl.EnumerateArray())
                        deps.Add(d.GetString() ?? string.Empty);

                phases.Add(new OrchestratorPhaseDto
                {
                    PhaseId = el.TryGetProperty("phaseId", out var pid) ? pid.GetString() ?? Guid.NewGuid().ToString() : Guid.NewGuid().ToString(),
                    PhaseName = el.TryGetProperty("phaseName", out var pn) ? pn.GetString() ?? string.Empty : string.Empty,
                    Description = el.TryGetProperty("description", out var desc) ? desc.GetString() ?? string.Empty : string.Empty,
                    Dependencies = deps,
                    ExpectedOutput = el.TryGetProperty("expectedOutput", out var eo) ? eo.GetString() ?? string.Empty : string.Empty
                });
            }
        }

        return phases.Count > 0
            ? (phases, rationale)
            : HeuristicDecompose(request);
    }

    private static (List<OrchestratorPhaseDto> phases, string rationale) HeuristicDecompose(
        RunCascadePlanningCommand request)
    {
        var phases = new List<OrchestratorPhaseDto>
        {
            new() { PhaseId = "analyse", PhaseName = "Analysis", Description = $"Analyse requirements: {request.Prompt}", Dependencies = new(), ExpectedOutput = "Requirements document" },
            new() { PhaseId = "design", PhaseName = "Design", Description = "Design solution architecture", Dependencies = new() { "analyse" }, ExpectedOutput = "Architecture plan" },
            new() { PhaseId = "implement", PhaseName = "Implementation", Description = "Implement the solution", Dependencies = new() { "design" }, ExpectedOutput = "Working code" },
            new() { PhaseId = "validate", PhaseName = "Validation", Description = "Build, test, security scan", Dependencies = new() { "implement" }, ExpectedOutput = "Passing build + test report" }
        };

        if (request.Complexity is "High" or "Critical")
        {
            phases.Insert(3, new OrchestratorPhaseDto
            {
                PhaseId = "review",
                PhaseName = "Code Review",
                Description = "Multi-agent debate and code review",
                Dependencies = new() { "implement" },
                ExpectedOutput = "Review report"
            });
        }

        return (phases, $"Heuristic decomposition for {request.Complexity} complexity task");
    }
}
