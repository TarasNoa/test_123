using System.Text;
using System.Text.Json;
using Libr4.AI.Application.Abstractions;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentIntegration;
using Libr4.IDE.Application.AutonomousAppGeneration.Services;
using Libr4.IDE.Domain.AutonomousAppGeneration;
using Microsoft.Extensions.Logging;

namespace Libr4.IDE.Application.AutonomousAppGeneration.Infrastructure;

/// <summary>
/// Transforms raw shadow-workspace console output into structured
/// <see cref="ErrorReport"/>s. Uses the project-wide LLM with a light
/// heuristic fallback so the orchestrator never deadlocks.
/// </summary>
public sealed class LlmErrorAnalysisService : IErrorAnalysisService
{
    private readonly IAIService _ai;
    private readonly ILogger<LlmErrorAnalysisService> _logger;
    private readonly IProviderCapabilityMatrix _providerMatrix;

    private const string SystemPrompt = @"
You are SemanticBlame. Read failing build/test logs plus the current source files and output a
structured error list for the Fixer agent.

====================== OUTPUT ======================
Return ONLY valid JSON, no prose, no markdown fences:
{
  ""errors"": [
    {
      ""errorType"": string,            // e.g. CompileError, MissingPackage, MissingType, TestFailure, ConfigError, RuntimeError, ManifestError
      ""message"": string,              // short actionable summary of the failure
      ""filePath"": string | null,      // relativePath of the file the Fixer must edit (match manifest paths)
      ""lineNumber"": int | null,       // 1-based, if locatable
      ""suggestedFix"": string          // concrete fix: add package X, implement method Y, correct namespace Z
    }
  ]
}

====================== RULES ======================
- One entry per distinct root cause. Don't repeat the same error across many lines.
- Prefer the FILE THAT MUST CHANGE in filePath (not the file that reported the error - e.g. missing package -> put the .csproj in filePath).
- If the log shows CS0246 / CS0103 / 'module not found' -> errorType=MissingType or MissingPackage, suggestedFix names the package/namespace.
- If tests fail due to wrong production behaviour -> errorType=TestFailure, suggestedFix describes the business-logic fix.
- If a log line is harmless warning noise, do NOT emit it.
- suggestedFix is for a machine Fixer: imperative, <=200 chars.
- Output only the JSON object. No commentary.
";

    public LlmErrorAnalysisService(IAIService ai, ILogger<LlmErrorAnalysisService> logger, IProviderCapabilityMatrix providerMatrix)
    {
        _ai = ai;
        _logger = logger;
        _providerMatrix = providerMatrix;
    }

    public async Task<IReadOnlyList<ErrorReport>> AnalyzeAsync(
        ExecutionResult execution,
        IReadOnlyList<GeneratedFile> files,
        CancellationToken ct = default)
    {
        if (execution.Succeeded) return Array.Empty<ErrorReport>();

        var prompt = BuildPrompt(execution, files);
        prompt = PromptPipelinePolicy.ApplyInputBudget("error_analysis", prompt);
        string raw;
        try
        {
            // Use provider capability matrix for model routing
            var stageRequirement = _providerMatrix.GetStageRequirements("fixing") 
                ?? new StageModelRequirement(
                    Stage: "fixing",
                    RequiresFunctionCalling: false,
                    RequiresStreaming: false,
                    RequiresJsonMode: true,
                    MinContextTokens: 64000,
                    MinOutputTokens: 8192,
                    MaxCostPer1kTokens: 0.01);
            var routingDecision = _providerMatrix.RouteStage("fixing", stageRequirement);
            _logger.LogInformation("Model routing for error analysis: {Provider}/{Model} (reason: {Reason})",
                routingDecision.ProviderId, routingDecision.ModelId, routingDecision.RoutingReason);
            
            raw = await _ai.GenerateCompletionAsync(prompt, SystemPrompt, routingDecision.ModelId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analysis LLM call failed");
            throw new AutonomousGenerationFailedException(
                "error_analysis",
                $"Error analysis LLM call failed: {ex.Message}",
                ex);
        }

        if (!PromptPipelinePolicy.ValidateOutputContract("error_analysis", raw, out var contractReason))
        {
            throw new AutonomousGenerationFailedException(
                "error_analysis",
                $"Error-analysis output failed contract validation: {contractReason}");
        }

        using var doc = LlmJsonHelpers.ExtractJson(raw);
        if (doc is null || !doc.RootElement.TryGetProperty("errors", out var arr)
            || arr.ValueKind != JsonValueKind.Array)
        {
            throw new AutonomousGenerationFailedException(
                "error_analysis",
                $"Error-analysis response is not a JSON object with an 'errors' array. parse={LlmJsonHelpers.LastParseError ?? "unknown"}");
        }

        var list = new List<ErrorReport>();
        foreach (var item in arr.EnumerateArray())
        {
            var type = LlmJsonHelpers.GetString(item, "errorType", "Unknown");
            var msg = LlmJsonHelpers.GetString(item, "message", string.Empty);
            var fix = LlmJsonHelpers.GetString(item, "suggestedFix", string.Empty);
            string? file = null;
            if (item.TryGetProperty("filePath", out var fp) && fp.ValueKind == JsonValueKind.String)
                file = fp.GetString();
            int? line = null;
            if (item.TryGetProperty("lineNumber", out var ln) && ln.ValueKind == JsonValueKind.Number
                && ln.TryGetInt32(out var v))
                line = v;
            list.Add(new ErrorReport(type, msg, fix, file, line, "SemanticBlameAgent"));
        }

        if (list.Count == 0)
        {
            throw new AutonomousGenerationFailedException(
                "error_analysis",
                "Error-analysis LLM returned an empty errors list.");
        }

        return list;
    }

    [Obsolete("Heuristic error-analysis fallback removed.")]
    private static IReadOnlyList<ErrorReport> HeuristicAnalysis(ExecutionResult execution)
    {
        var logs = execution.Logs.Select(l => l.Message).ToList();
        var stderr = execution.ErrorLogs.Select(l => l.Message).ToList();
        if (stderr.Count == 0 && logs.Count == 0) return Array.Empty<ErrorReport>();

        var joinedAll = string.Join('\n', logs);
        if (joinedAll.Contains("No module named 'httpx'", StringComparison.OrdinalIgnoreCase) ||
            joinedAll.Contains("requires the httpx package", StringComparison.OrdinalIgnoreCase))
        {
            return new[]
            {
                new ErrorReport(
                    errorType: "MissingPackage",
                    message: "Tests require httpx for fastapi/starlette TestClient but package is missing.",
                    suggestedFix: "Add pinned dependency httpx to requirements.txt and src/requirements.txt.",
                    filePath: "requirements.txt",
                    diagnosingAgent: "HeuristicFallback")
            };
        }

        var joined = string.Join('\n', stderr);
        var snippet = joined.Length > 500 ? joined[..500] : joined;
        return new[]
        {
            new ErrorReport(
                errorType: "BuildOrRuntimeError",
                message: snippet,
                suggestedFix: "Inspect the offending file(s) and fix compilation/runtime errors reported above.",
                diagnosingAgent: "HeuristicFallback")
        };
    }

    private static string BuildPrompt(ExecutionResult execution, IReadOnlyList<GeneratedFile> files)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Exit code: {execution.ExitCode}");
        sb.AppendLine("Console output (most recent last):");
        foreach (var log in execution.Logs.TakeLast(200))
        {
            sb.Append('[').Append(log.Stream).Append("] ").AppendLine(log.Message);
        }
        sb.AppendLine();
        sb.AppendLine("Project files (truncated):");
        foreach (var f in files)
        {
            sb.AppendLine($"--- {f.RelativePath} ---");
            sb.AppendLine(f.Content.Length > 4000 ? f.Content[..4000] + "\n... (truncated)" : f.Content);
        }
        return sb.ToString();
    }
}
