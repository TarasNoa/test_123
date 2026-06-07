using System.Text;
using System.Text.Json;
using Libr4.AI.Application.Abstractions;
using Libr4.AI.Infrastructure.AI;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentIntegration;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Patching;
using Libr4.IDE.Application.AutonomousAppGeneration.FastContext;
using Libr4.IDE.Application.AutonomousAppGeneration.Infrastructure.Fim;
using Libr4.IDE.Application.AutonomousAppGeneration.Services;
using Libr4.IDE.Application.AutonomousAppGeneration.Services.PlatformUtilization;
using Libr4.IDE.Domain.AutonomousAppGeneration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Libr4.IDE.Application.AutonomousAppGeneration.Infrastructure;

/// <summary>
/// Investigates build failures using real console output and applies minimal search/replace edits.
/// </summary>
public sealed class ClaudeCodeStyleRepairService : IClaudeCodeStyleRepairService
{
    private const string SystemPrompt = """
        You are a surgical build-repair agent (Claude Code style). You receive ACTUAL console build output and numbered source files.

        OUTPUT CONTRACT (HARD):
        Return ONLY valid JSON:
        {
          "edits": [
            { "relativePath": "backend/...", "search": "exact text from file", "replace": "fixed text" }
          ],
          "newFiles": [
            { "relativePath": "backend/...", "content": "full file body for NEW missing types only" }
          ]
        }

        RULES:
        1. Read the BUILD CONSOLE LOG first — fix the FIRST root cause it shows.
        2. Prefer "edits" (search/replace) over rewriting whole files. "search" MUST match the file exactly (whitespace included).
        3. Use "newFiles" only for genuinely missing types/files referenced in errors.
        4. Max 6 edits and 3 newFiles per response.
        5. Do NOT guess — if the log shows file:line, fix that location.
        6. For Java "cannot find symbol": add import, method, or create missing type.
        7. For TS "Cannot find module": fix import path or export in client.ts.
        8. For .NET CS0246: add using or PackageReference via csproj edit.
        9. Never return prose or markdown fences.

        JSON escaping: use \n \t \" \\ inside strings.
        """;

    private const string ApplyPatchSystemPrompt = """
        You are a surgical build-repair agent using unified diff patches. You receive ACTUAL console build output and numbered source files.

        OUTPUT CONTRACT (HARD):
        Return ONLY valid JSON:
        {
          "patches": [
            { "relativePath": "backend/...", "patch": "@@ -1,3 +1,4 @@\\n context line\\n-old line\\n+new line\\n" }
          ]
        }

        RULES:
        1. Read the BUILD CONSOLE LOG first — fix the FIRST root cause it shows.
        2. Each patch must be a valid unified diff with @@ hunk headers.
        3. Max 4 patches per response.
        4. Do NOT guess — if the log shows file:line, fix that location.
        5. Never return prose or markdown fences.
        """;

    private const string FimSystemPrompt = """
        You are a surgical code infilling agent. The user prompt contains a file with a <|fim_hole|> marker.

        OUTPUT CONTRACT (HARD):
        Return ONLY the replacement code for the <|fim_hole|> region.
        - No JSON, no markdown fences, no prose, no explanations.
        - The output must compile in context of the prefix and suffix shown.
        - Fix the root build error at the hole location.
        """;

    private readonly IAIService _ai;
    private readonly IProviderCapabilityMatrix _providerMatrix;
    private readonly IFimPromptBuilder _fimBuilder;
    private readonly ILogger<ClaudeCodeStyleRepairService> _logger;
    private readonly AutonomousGenerationOptions _options;
    private readonly AutonomousLoopGuardOptions _loopGuard;
    private readonly IFastContextPrefetcher? _fastContext;

    public ClaudeCodeStyleRepairService(
        IAIService ai,
        IProviderCapabilityMatrix providerMatrix,
        IFimPromptBuilder fimBuilder,
        ILogger<ClaudeCodeStyleRepairService> logger,
        IOptions<AutonomousGenerationOptions> options,
        IOptions<AutonomousLoopGuardOptions> loopGuard,
        IFastContextPrefetcher? fastContext = null)
    {
        _ai = ai;
        _providerMatrix = providerMatrix;
        _fimBuilder = fimBuilder;
        _logger = logger;
        _options = options.Value;
        _loopGuard = loopGuard.Value;
        _fastContext = fastContext;
    }

    public async Task<IReadOnlyList<GeneratedFile>> TryRepairAsync(
        GenerationPlan plan,
        IReadOnlyList<GeneratedFile> currentFiles,
        CompileRepairPlanner.RepairPlan repairPlan,
        string buildLog,
        CancellationToken ct = default)
    {
        if (!_loopGuard.UseClaudeCodeStyleRepair || repairPlan.FixerErrors.Count == 0)
            return Array.Empty<GeneratedFile>();

        var contextFiles = BuildInvestigationContext(currentFiles, repairPlan);
        string? fastContextBlock = null;
        if (_fastContext is not null)
        {
            var prefetch = await _fastContext.PrefetchForRepairAsync(
                    new FastContextPrefetchRequest(
                        WorkspaceRoot: null,
                        buildLog,
                        repairPlan.FixerErrors,
                        MemoryFiles: currentFiles),
                    ct)
                .ConfigureAwait(false);
            if (!string.IsNullOrWhiteSpace(prefetch.FormattedText))
                fastContextBlock = prefetch.FormattedText;
        }

        if (_loopGuard.UseFimRepair)
        {
            var fimPatches = await TryFimRepairAsync(
                plan,
                currentFiles,
                repairPlan,
                buildLog,
                contextFiles,
                ct).ConfigureAwait(false);
            if (fimPatches.Count > 0)
                return fimPatches;
        }

        if (_loopGuard.UseApplyPatchRepair)
        {
            var patchResults = await TryApplyPatchRepairAsync(
                plan,
                currentFiles,
                repairPlan,
                buildLog,
                contextFiles,
                fastContextBlock,
                ct).ConfigureAwait(false);
            if (patchResults.Count > 0)
                return patchResults;
        }

        var prompt = BuildPrompt(plan, repairPlan, buildLog, contextFiles, fastContextBlock);

        string raw;
        try
        {
            raw = await GenerateSurgicalCompletionAsync(prompt, ct).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Surgical repair LLM call failed.");
            return Array.Empty<GeneratedFile>();
        }

        var parsed = SurgicalFixerOutputParser.Parse(raw, _logger);
        if (parsed.Edits.Count == 0 && parsed.NewFiles.Count == 0)
        {
            _logger.LogWarning("Surgical repair returned no parseable edits.");
            return Array.Empty<GeneratedFile>();
        }

        var maxEdits = Math.Clamp(_loopGuard.MaxSurgicalEditsPerIteration, 1, 12);
        var edits = parsed.Edits.Take(maxEdits).ToList();
        var newFiles = parsed.NewFiles.Take(3).ToList();

        var result = SurgicalPatchEngine.Apply(currentFiles, edits, newFiles);
        if (result.Warnings.Count > 0)
        {
            _logger.LogInformation(
                "Surgical repair: applied={Applied} skipped={Skipped} warnings={Warnings}",
                result.AppliedEdits,
                result.SkippedEdits,
                string.Join("; ", result.Warnings.Take(6)));
        }

        if (result.Patches.Count == 0)
            return Array.Empty<GeneratedFile>();

        _logger.LogInformation(
            "Surgical repair produced {Count} patch(es) for root={Root} category={Category}",
            result.Patches.Count,
            repairPlan.RootCause.FilePath ?? "(n/a)",
            repairPlan.RootCauseCategory);

        return result.Patches;
    }

    private async Task<IReadOnlyList<GeneratedFile>> TryFimRepairAsync(
        GenerationPlan plan,
        IReadOnlyList<GeneratedFile> currentFiles,
        CompileRepairPlanner.RepairPlan repairPlan,
        string buildLog,
        IReadOnlyList<GeneratedFile> contextFiles,
        CancellationToken ct)
    {
        var root = repairPlan.RootCause;
        var target = ResolveTargetFile(contextFiles, root.FilePath)
                     ?? ResolveTargetFile(currentFiles, root.FilePath);
        if (target is null || !_fimBuilder.ShouldUseFim(target, root, _loopGuard.FimMinFileLines))
            return Array.Empty<GeneratedFile>();

        if (!_fimBuilder.TryBuild(
                target.RelativePath,
                target.Content ?? string.Empty,
                root.LineNumber,
                _loopGuard.FimHoleRadiusLines,
                out var fimPrompt))
            return Array.Empty<GeneratedFile>();

        var userPrompt = BuildFimUserPrompt(plan, repairPlan, buildLog, fimPrompt, _fimBuilder);
        string raw;
        try
        {
            raw = await GenerateFimCompletionAsync(userPrompt, ct).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "FIM repair LLM call failed for {Path}.", target.RelativePath);
            return Array.Empty<GeneratedFile>();
        }

        if (!_fimBuilder.TryParseFill(raw, out var fill))
        {
            _logger.LogWarning("FIM repair returned unparsable fill for {Path}.", target.RelativePath);
            return Array.Empty<GeneratedFile>();
        }

        var patches = FimOutputApplier.ApplyOrFallback(currentFiles, fimPrompt, fill, _fimBuilder);
        if (patches.Count == 0)
        {
            _logger.LogWarning("FIM repair produced no applicable patch for {Path}.", target.RelativePath);
            return Array.Empty<GeneratedFile>();
        }

        _logger.LogInformation(
            "FIM repair patched {Path} at lines {Start}-{End}",
            target.RelativePath,
            fimPrompt.HoleStartLine,
            fimPrompt.HoleEndLine);
        return patches;
    }

    private async Task<IReadOnlyList<GeneratedFile>> TryApplyPatchRepairAsync(
        GenerationPlan plan,
        IReadOnlyList<GeneratedFile> currentFiles,
        CompileRepairPlanner.RepairPlan repairPlan,
        string buildLog,
        IReadOnlyList<GeneratedFile> contextFiles,
        string? fastContextBlock,
        CancellationToken ct)
    {
        var prompt = BuildApplyPatchPrompt(plan, repairPlan, buildLog, contextFiles, fastContextBlock);
        string raw;
        try
        {
            raw = await GenerateApplyPatchCompletionAsync(prompt, ct).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Apply-patch repair LLM call failed.");
            return Array.Empty<GeneratedFile>();
        }

        if (!TryParseApplyPatchResponse(raw, out var patchEntries) || patchEntries.Count == 0)
        {
            _logger.LogDebug("Apply-patch repair returned no parseable patches.");
            return Array.Empty<GeneratedFile>();
        }

        var patches = new List<GeneratedFile>();
        foreach (var entry in patchEntries.Take(4))
        {
            if (string.IsNullOrWhiteSpace(entry.RelativePath) || string.IsNullOrWhiteSpace(entry.Patch))
                continue;

            var existing = currentFiles.FirstOrDefault(f =>
                f.RelativePath.Equals(entry.RelativePath, StringComparison.OrdinalIgnoreCase)
                || f.RelativePath.Replace('\\', '/').EndsWith(
                    entry.RelativePath.Replace('\\', '/'),
                    StringComparison.OrdinalIgnoreCase));
            var original = existing?.Content ?? string.Empty;
            var diff = UnifiedDiffParser.Parse(entry.Patch, entry.RelativePath);
            PatchApplyResult applied;
            try
            {
                applied = PatchApplicator.ApplyFuzzy(original, diff);
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Apply-patch repair threw for {Path}", entry.RelativePath);
                continue;
            }

            if (!applied.Success || applied.PatchedContent is null)
            {
                _logger.LogDebug("Apply-patch repair failed for {Path}: {Reason}", entry.RelativePath, applied.ConflictReport);
                continue;
            }

            var file = PatchApplicator.ToGeneratedFile(entry.RelativePath, applied, existing)
                       ?? new GeneratedFile(entry.RelativePath, existing?.Language, applied.PatchedContent);
            patches.Add(file);
        }

        if (patches.Count == 0)
            return Array.Empty<GeneratedFile>();

        _logger.LogInformation(
            "Apply-patch repair produced {Count} patch(es) for root={Root}",
            patches.Count,
            repairPlan.RootCause.FilePath ?? "(n/a)");
        return patches;
    }

    private static string BuildApplyPatchPrompt(
        GenerationPlan plan,
        CompileRepairPlanner.RepairPlan repairPlan,
        string buildLog,
        IReadOnlyList<GeneratedFile> contextFiles,
        string? fastContextBlock)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Application: {plan.ApplicationName}");
        sb.AppendLine($"Root cause: {repairPlan.RootCause.Message}");
        sb.AppendLine();
        sb.AppendLine("=== BUILD LOG EXCERPT ===");
        sb.AppendLine(TruncateBuildLog(buildLog, repairPlan.BuildLogExcerpt));
        if (!string.IsNullOrWhiteSpace(fastContextBlock))
        {
            sb.AppendLine();
            sb.AppendLine(fastContextBlock);
        }

        sb.AppendLine();
        sb.AppendLine("=== FILES ===");
        for (var i = 0; i < contextFiles.Count; i++)
        {
            var file = contextFiles[i];
            sb.AppendLine($"--- FILE {i + 1}: {file.RelativePath} ---");
            sb.AppendLine(file.Content ?? string.Empty);
        }

        return sb.ToString();
    }

    private Task<string> GenerateApplyPatchCompletionAsync(string prompt, CancellationToken ct) =>
        GenerateStageCompletionAsync(prompt, ApplyPatchSystemPrompt, ct);

    private async Task<string> GenerateStageCompletionAsync(string prompt, string systemPrompt, CancellationToken ct)
    {
        var stageRequirement = _providerMatrix.GetStageRequirements("fixing")
                              ?? new StageModelRequirement(
                                  Stage: "fixing",
                                  RequiresFunctionCalling: false,
                                  RequiresStreaming: false,
                                  RequiresJsonMode: true,
                                  MinContextTokens: 32_000,
                                  MinOutputTokens: 4096,
                                  MaxCostPer1kTokens: 0.01);
        var routing = _providerMatrix.RouteStage("fixing", stageRequirement);
        var timeoutSeconds = Math.Clamp(_options.LlmStepTimeoutSeconds, 30, 600);
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        var budgetedPrompt = PromptPipelinePolicy.ApplyInputBudget(
            "fixing",
            PlatformCapabilityBriefingScope.AppendToPrompt(
                prompt,
                PlatformCapabilityBriefingStage.Repair));
        var completionTask = Task.Run(async () =>
        {
            using var _ = AICallCancellationScope.Push(linkedCts.Token);
            return await _ai.GenerateCompletionAsync(budgetedPrompt, systemPrompt, routing.ModelId)
                .ConfigureAwait(false);
        }, linkedCts.Token);
        var timeoutTask = Task.Delay(TimeSpan.FromSeconds(timeoutSeconds), linkedCts.Token);
        var finished = await Task.WhenAny(completionTask, timeoutTask).ConfigureAwait(false);
        if (finished != completionTask)
            throw new TimeoutException($"Surgical repair exceeded timeout of {timeoutSeconds}s.");
        linkedCts.Cancel();
        return await completionTask.ConfigureAwait(false);
    }

    private static bool TryParseApplyPatchResponse(string raw, out List<ApplyPatchEntry> patches)
    {
        patches = new List<ApplyPatchEntry>();
        try
        {
            using var doc = JsonDocument.Parse(raw);
            if (!doc.RootElement.TryGetProperty("patches", out var arr) || arr.ValueKind != JsonValueKind.Array)
                return false;

            foreach (var item in arr.EnumerateArray())
            {
                var path = item.TryGetProperty("relativePath", out var pathEl) ? pathEl.GetString() : null;
                var patch = item.TryGetProperty("patch", out var patchEl) ? patchEl.GetString() : null;
                if (!string.IsNullOrWhiteSpace(path) && !string.IsNullOrWhiteSpace(patch))
                    patches.Add(new ApplyPatchEntry(path!, patch!));
            }

            return patches.Count > 0;
        }
        catch
        {
            return false;
        }
    }

    private sealed record ApplyPatchEntry(string RelativePath, string Patch);

    private static GeneratedFile? ResolveTargetFile(IReadOnlyList<GeneratedFile> files, string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return null;

        return files.FirstOrDefault(f =>
            f.RelativePath.Equals(path, StringComparison.OrdinalIgnoreCase)
            || f.RelativePath.Replace('\\', '/').EndsWith(
                path.Replace('\\', '/'),
                StringComparison.OrdinalIgnoreCase));
    }

    private static string BuildFimUserPrompt(
        GenerationPlan plan,
        CompileRepairPlanner.RepairPlan repairPlan,
        string buildLog,
        FimPrompt fimPrompt,
        IFimPromptBuilder fimBuilder)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Application: {plan.ApplicationName}");
        sb.AppendLine($"Root cause: {repairPlan.RootCause.Message}");
        if (repairPlan.RootCause.LineNumber.HasValue)
            sb.AppendLine($"Error line: {repairPlan.RootCause.LineNumber}");
        sb.AppendLine();
        sb.AppendLine("=== BUILD LOG EXCERPT ===");
        sb.AppendLine(TruncateBuildLog(buildLog, repairPlan.BuildLogExcerpt));
        sb.AppendLine();
        sb.AppendLine("=== FIM TARGET ===");
        sb.AppendLine(fimBuilder.FormatLlmPrompt(fimPrompt));
        return sb.ToString();
    }

    private async Task<string> GenerateFimCompletionAsync(string prompt, CancellationToken ct)
    {
        var stageRequirement = _providerMatrix.GetStageRequirements("fixing")
                              ?? new StageModelRequirement(
                                  Stage: "fixing",
                                  RequiresFunctionCalling: false,
                                  RequiresStreaming: false,
                                  RequiresJsonMode: false,
                                  MinContextTokens: 32_000,
                                  MinOutputTokens: 4096,
                                  MaxCostPer1kTokens: 0.01);
        var routing = _providerMatrix.RouteStage("fixing", stageRequirement);
        var timeoutSeconds = Math.Clamp(_options.LlmStepTimeoutSeconds, 30, 600);
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        var budgetedPrompt = PromptPipelinePolicy.ApplyInputBudget(
            "fixing",
            PlatformCapabilityBriefingScope.AppendToPrompt(
                prompt,
                PlatformCapabilityBriefingStage.Repair));
        var completionTask = Task.Run(async () =>
        {
            using var _ = AICallCancellationScope.Push(linkedCts.Token);
            return await _ai.GenerateCompletionAsync(budgetedPrompt, FimSystemPrompt, routing.ModelId)
                .ConfigureAwait(false);
        }, linkedCts.Token);
        var timeoutTask = Task.Delay(TimeSpan.FromSeconds(timeoutSeconds), linkedCts.Token);
        var finished = await Task.WhenAny(completionTask, timeoutTask).ConfigureAwait(false);
        if (finished != completionTask)
            throw new TimeoutException($"FIM repair exceeded timeout of {timeoutSeconds}s.");
        linkedCts.Cancel();
        return await completionTask.ConfigureAwait(false);
    }

    internal static IReadOnlyList<GeneratedFile> BuildInvestigationContext(
        IReadOnlyList<GeneratedFile> currentFiles,
        CompileRepairPlanner.RepairPlan repairPlan)
    {
        var selected = new Dictionary<string, GeneratedFile>(StringComparer.OrdinalIgnoreCase);

        foreach (var err in repairPlan.FixerErrors)
        {
            if (string.IsNullOrWhiteSpace(err.FilePath))
                continue;

            var match = currentFiles.FirstOrDefault(f =>
                f.RelativePath.Equals(err.FilePath, StringComparison.OrdinalIgnoreCase)
                || f.RelativePath.Replace('\\', '/').EndsWith(
                    err.FilePath.Replace('\\', '/'),
                    StringComparison.OrdinalIgnoreCase));
            if (match is not null)
                selected[match.RelativePath] = match;
        }

        foreach (var file in currentFiles.Where(f => IsManifest(f.RelativePath)))
            selected[file.RelativePath] = file;

        if (repairPlan.SymbolAnalysis?.TargetFilePath is { } target)
        {
            var targetFile = currentFiles.FirstOrDefault(f =>
                f.RelativePath.Equals(target, StringComparison.OrdinalIgnoreCase));
            if (targetFile is not null)
                selected[targetFile.RelativePath] = targetFile;
        }

        foreach (var err in repairPlan.FixerErrors)
        {
            var dir = Path.GetDirectoryName(err.FilePath?.Replace('\\', '/'));
            if (string.IsNullOrWhiteSpace(dir))
                continue;
            foreach (var sibling in currentFiles.Where(f =>
                         f.RelativePath.Replace('\\', '/').StartsWith(dir + "/", StringComparison.OrdinalIgnoreCase)))
                selected[sibling.RelativePath] = sibling;
        }

        return selected.Values
            .OrderBy(f => f.RelativePath, StringComparer.OrdinalIgnoreCase)
            .Take(8)
            .ToList();
    }

    private static string BuildPrompt(
        GenerationPlan plan,
        CompileRepairPlanner.RepairPlan repairPlan,
        string buildLog,
        IReadOnlyList<GeneratedFile> contextFiles,
        string? fastContextBlock = null)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Application: {plan.ApplicationName}");
        sb.AppendLine($"Stack: {StackPlanHeuristics.Classify(plan)}");
        sb.AppendLine($"Root cause category: {repairPlan.RootCauseCategory}");
        sb.AppendLine();

        if (!string.IsNullOrWhiteSpace(fastContextBlock))
        {
            sb.AppendLine("=== FAST CONTEXT (prefetched codebase hits) ===");
            sb.AppendLine(fastContextBlock);
            sb.AppendLine("=== END FAST CONTEXT ===");
            sb.AppendLine();
        }

        sb.AppendLine("=== BUILD CONSOLE LOG (authoritative — do not guess beyond this) ===");
        sb.AppendLine(TruncateBuildLog(buildLog, repairPlan.BuildLogExcerpt));
        sb.AppendLine("=== END BUILD LOG ===");
        sb.AppendLine();

        sb.AppendLine("=== STRUCTURED ERRORS (fix root first) ===");
        foreach (var err in repairPlan.FixerErrors)
        {
            sb.Append($"- [{err.ErrorType}] {err.Message}");
            if (!string.IsNullOrEmpty(err.FilePath))
                sb.Append($" @ {err.FilePath}");
            if (err.LineNumber.HasValue)
                sb.Append($":{err.LineNumber}");
            sb.AppendLine();
            if (!string.IsNullOrEmpty(err.SuggestedFix))
                sb.AppendLine($"  hint: {err.SuggestedFix}");
        }

        if (repairPlan.SymbolAnalysis is not null)
        {
            var a = repairPlan.SymbolAnalysis;
            sb.AppendLine($"  symbol: {a.SymbolName} ({a.Kind}) evidence={a.Evidence}");
            if (!string.IsNullOrEmpty(a.SuggestedFix))
                sb.AppendLine($"  analysis: {a.SuggestedFix}");
        }

        sb.AppendLine();
        sb.AppendLine("=== SOURCE FILES (line-numbered — use exact search strings from here) ===");
        foreach (var file in contextFiles)
        {
            sb.AppendLine($"--- {file.RelativePath} ---");
            AppendNumberedContent(sb, file.Content ?? string.Empty);
            sb.AppendLine();
        }

        sb.AppendLine("Return surgical JSON only (edits + newFiles).");
        return sb.ToString();
    }

    private static void AppendNumberedContent(StringBuilder sb, string content)
    {
        var lines = content.Replace("\r\n", "\n").Split('\n');
        var maxLines = 220;
        for (var i = 0; i < Math.Min(lines.Length, maxLines); i++)
            sb.AppendLine($"{i + 1,4}| {lines[i]}");

        if (lines.Length > maxLines)
            sb.AppendLine($"... [{lines.Length - maxLines} more lines truncated] ...");
    }

    private static string TruncateBuildLog(string buildLog, string plannerExcerpt)
    {
        var merged = string.IsNullOrWhiteSpace(buildLog) ? plannerExcerpt : buildLog;
        if (string.IsNullOrWhiteSpace(merged))
            return "(empty build log)";

        const int maxChars = 16_000;
        if (merged.Length <= maxChars)
            return merged;

        var head = merged[..6_000];
        var tail = merged[^8_000..];
        return head + "\n... [build log truncated] ...\n" + tail;
    }

    private Task<string> GenerateSurgicalCompletionAsync(string prompt, CancellationToken ct) =>
        GenerateStageCompletionAsync(prompt, SystemPrompt, ct);

    private static bool IsManifest(string path) =>
        path.EndsWith("pom.xml", StringComparison.OrdinalIgnoreCase)
        || path.EndsWith("package.json", StringComparison.OrdinalIgnoreCase)
        || path.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase)
        || path.EndsWith("requirements.txt", StringComparison.OrdinalIgnoreCase)
        || path.EndsWith("build.gradle", StringComparison.OrdinalIgnoreCase);
}
