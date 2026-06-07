using System.Text;
using System.Text.Json;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentStack;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Abstractions;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Core;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Infrastructure;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Models;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Subagents;
using Libr4.IDE.Application.AutonomousAppGeneration.Services;
using Libr4.IDE.Application.AutonomousAppGeneration.Services.Pipeline;
using Libr4.IDE.Application.Obscura;
using Libr4.IDE.Domain.AutonomousAppGeneration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Libr4.IDE.Application.AutonomousAppGeneration.Verify;

public sealed class VerifyOrchestrator : IVerifyOrchestrator
{
    private readonly IShadowExecutionService _shadow;
    private readonly IShadowWorkspaceAccessor? _shadowAccessor;
    private readonly IVerifyReadinessProbe _readiness;
    private readonly IAgentSpecRegistry _specs;
    private readonly IServiceScopeFactory _scopes;
    private readonly GenerationWorkspaceStore _workspaceStore;
    private readonly GenerationWorkspaceAccessor _generationAccessor;
    private readonly IObscuraNetworkRouter? _networkRouter;
    private readonly IObscuraVerifySmokeRunner? _obscuraSmoke;
    private readonly IObscuraSecurityScannerClient? _securityScanner;
    private readonly IShadowSyncClient? _shadowSync;
    private readonly ISandboxControllerClient? _sandboxController;
    private readonly ObscuraSessionOptions? _obscuraOptions;
    private readonly VerifySubagentOptions _options;
    private readonly ILogger<VerifyOrchestrator> _logger;

    public VerifyOrchestrator(
        IShadowExecutionService shadow,
        IVerifyReadinessProbe readiness,
        IAgentSpecRegistry specs,
        IServiceScopeFactory scopes,
        GenerationWorkspaceStore workspaceStore,
        GenerationWorkspaceAccessor generationAccessor,
        IOptions<VerifySubagentOptions> options,
        ILogger<VerifyOrchestrator> logger,
        IShadowWorkspaceAccessor? shadowAccessor = null,
        IObscuraNetworkRouter? networkRouter = null,
        IObscuraVerifySmokeRunner? obscuraSmoke = null,
        IObscuraSecurityScannerClient? securityScanner = null,
        IShadowSyncClient? shadowSync = null,
        ISandboxControllerClient? sandboxController = null,
        IOptions<ObscuraSessionOptions>? obscuraOptions = null)
    {
        _shadow = shadow;
        _readiness = readiness;
        _specs = specs;
        _scopes = scopes;
        _workspaceStore = workspaceStore;
        _generationAccessor = generationAccessor;
        _options = options.Value;
        _logger = logger;
        _shadowAccessor = shadowAccessor;
        _networkRouter = networkRouter;
        _obscuraSmoke = obscuraSmoke;
        _securityScanner = securityScanner;
        _shadowSync = shadowSync;
        _sandboxController = sandboxController;
        _obscuraOptions = obscuraOptions?.Value;
    }

    public VerifyRunPlan PrepareVerifyRun(
        GenerationContext context,
        VerifyRecipeDetectionResult recipeDetection,
        string evidenceDir)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(context.Plan);

        var testsGreen = context.Items.TryGetValue("tests_passed", out var testsFlag) && testsFlag is true;
        return new VerifyRunPlan(
            context.Orchestrator.Id,
            recipeDetection.Recipe,
            evidenceDir,
            context.Orchestrator.ShadowWorkspaceId,
            context.Plan.RuntimeImage,
            recipeDetection.ManifestPath,
            testsGreen,
            recipeDetection.DetectionMethod);
    }

    public async Task<VerifyOrchestrationResult> RunVerifyOrchestrationAsync(
        GenerationContext context,
        VerifyRunPlan plan,
        CancellationToken ct = default)
    {
        var stackReady = await EnsureVerifyStackReadyAsync(plan, ct).ConfigureAwait(false);
        if (!stackReady.Ready)
        {
            return new VerifyOrchestrationResult(
                ShadowPassed: false,
                ReadinessPassed: false,
                AgentPassed: false,
                AgentSummary: stackReady.Summary,
                ReadinessResults: Array.Empty<VerifyReadinessResult>(),
                ReadinessEvidencePath: Path.Combine(plan.EvidenceDir, "readiness.json"),
                FailureEvidencePath: Path.Combine(plan.EvidenceDir, "verify-failure-evidence.json"));
        }

        var shadowPassed = await RunShadowVerificationAsync(context, plan, ct).ConfigureAwait(false);
        var readinessResults = new List<VerifyReadinessResult>();
        var readinessPassed = true;

        if (_options.EnableReadinessProbe && plan.Recipe.SmokeTargets.Count > 0)
        {
            RegisterSmokeTargets(plan);
            await StartRecipeServicesAsync(plan, ct).ConfigureAwait(false);
            foreach (var target in plan.Recipe.SmokeTargets)
            {
                var result = await _readiness.ProbeAsync(
                        target,
                        plan.ShadowWorkspaceId,
                        plan.EvidenceDir,
                        plan.RunId,
                        ct)
                    .ConfigureAwait(false);
                readinessResults.Add(result);
                if (!result.Ready)
                    readinessPassed = false;
            }
        }

        var agentPassed = true;
        var summaryParts = new List<string>();
        var hasBrowserTargets = plan.Recipe.SmokeTargets.Any(t => t.Kind == VerifySmokeKind.Browser);

        if (_options.EnableObscuraSmokeRunner && hasBrowserTargets)
        {
            if (_obscuraSmoke is null)
            {
                agentPassed = false;
                summaryParts.Add("obscura_smoke=fail: runner not configured");
            }
            else
            {
                var smoke = await _obscuraSmoke.RunBrowserTargetsAsync(
                        plan.RunId,
                        plan.Recipe.SmokeTargets,
                        ct)
                    .ConfigureAwait(false);
                if (!smoke.Passed)
                    agentPassed = false;

                summaryParts.Add($"obscura_smoke={(smoke.Passed ? "pass" : "fail")}: {smoke.Summary}");
                await PersistObscuraSmokeReportAsync(plan, smoke, ct).ConfigureAwait(false);
            }
        }

        if (_options.EnableAgentSubagent)
        {
            var (subPassed, subSummary) = await TryRunAgentVerifyAsync(context, plan, ct).ConfigureAwait(false);
            if (!subPassed)
                agentPassed = false;
            summaryParts.Add($"agent_subagent={(subPassed ? "pass" : "fail")}: {subSummary}");
        }
        else if (summaryParts.Count == 0)
        {
            summaryParts.Add("agent verify skipped");
        }

        var agentSummary = string.Join("; ", summaryParts);

        if (shadowPassed && readinessPassed && agentPassed)
        {
            var (scanPassed, scanSummary) = await RunPostVerifySecurityScanAsync(context, plan, ct)
                .ConfigureAwait(false);
            if (!scanPassed)
            {
                agentPassed = false;
                summaryParts.Add(scanSummary);
            }
            else if (!string.IsNullOrWhiteSpace(scanSummary))
            {
                summaryParts.Add(scanSummary);
            }
        }

        agentSummary = string.Join("; ", summaryParts);

        var readinessEvidencePath = Path.Combine(plan.EvidenceDir, "readiness.json");
        await PersistReadinessSummaryAsync(plan, readinessResults, readinessPassed, readinessEvidencePath, ct)
            .ConfigureAwait(false);

        string? failureEvidencePath = null;
        if (!shadowPassed || !readinessPassed || !agentPassed)
        {
            failureEvidencePath = Path.Combine(plan.EvidenceDir, "verify-failure-evidence.json");
            await PersistFailureEvidenceAsync(
                plan,
                shadowPassed,
                readinessPassed,
                agentPassed,
                agentSummary,
                readinessResults,
                failureEvidencePath,
                ct).ConfigureAwait(false);
        }

        return new VerifyOrchestrationResult(
            shadowPassed,
            readinessPassed,
            agentPassed,
            agentSummary,
            readinessResults,
            readinessEvidencePath,
            failureEvidencePath);
    }

    private async Task<(bool Ready, string Summary)> EnsureVerifyStackReadyAsync(
        VerifyRunPlan plan,
        CancellationToken ct)
    {
        var parts = new List<string>();

        if (_shadowSync is not null)
        {
            var workspaceId = plan.ShadowWorkspaceId?.ToString("D") ?? plan.RunId.ToString("D");
            if (!await _shadowSync.EnsureHealthyAsync(ct).ConfigureAwait(false))
                return (false, "verify_stack=fail:shadow_sync_unhealthy");

            if (!await _shadowSync.TriggerSyncAsync(workspaceId, ct).ConfigureAwait(false))
                return (false, "verify_stack=fail:shadow_sync_trigger_failed");

            parts.Add("shadow_sync=ok");
        }

        if (_sandboxController is not null)
        {
            if (!await _sandboxController.EnsureHealthyAsync(ct).ConfigureAwait(false))
                return (false, "verify_stack=fail:sandbox_controller_unhealthy");
            parts.Add("sandbox_controller=ok");
        }

        if (_securityScanner is not null && _obscuraOptions is { EnablePostVerifySecurityScan: true })
        {
            parts.Add("security_scanner=configured");
        }

        return parts.Count == 0
            ? (true, string.Empty)
            : (true, string.Join("; ", parts));
    }

    private async Task<bool> RunShadowVerificationAsync(
        GenerationContext context,
        VerifyRunPlan plan,
        CancellationToken ct)
    {
        if (plan.ShadowWorkspaceId is not Guid workspaceId)
            return true;

        try
        {
            await _shadow.UpdateWorkspaceAsync(workspaceId, context.Orchestrator.Files, ct).ConfigureAwait(false);
            var execution = await _shadow.RunAsync(workspaceId, context.Plan!, ct).ConfigureAwait(false);
            if (!execution.Succeeded)
            {
                _logger.LogWarning(
                    "[VerifyOrchestrator {RunId}] Shadow verification failed with {LogCount} log entries",
                    plan.RunId,
                    execution.Logs.Count);
            }

            return execution.Succeeded;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[VerifyOrchestrator {RunId}] Shadow verification error", plan.RunId);
            return false;
        }
    }

    private async Task StartRecipeServicesAsync(VerifyRunPlan plan, CancellationToken ct)
    {
        if (plan.ShadowWorkspaceId is not Guid workspaceId || _shadowAccessor is null)
            return;

        if (plan.Recipe.InstallCommands.Count > 0)
        {
            foreach (var cmd in plan.Recipe.InstallCommands)
            {
                var install = await _shadowAccessor.ExecAsync(workspaceId, cmd, ct).ConfigureAwait(false);
                if (!install.Succeeded)
                {
                    _logger.LogWarning(
                        "[VerifyOrchestrator {RunId}] Install command failed: {Cmd}",
                        plan.RunId,
                        cmd);
                }
            }
        }

        var logPath = Path.Combine(plan.EvidenceDir, "app.log").Replace('\\', '/');
        foreach (var cmd in plan.Recipe.StartCommands)
        {
            var background = $"nohup bash -lc {ShellQuote(cmd)} >> {ShellQuote(logPath)} 2>&1 &";
            await _shadowAccessor.ExecAsync(workspaceId, background, ct).ConfigureAwait(false);
        }

        if (plan.Recipe.StartCommands.Count > 0)
            await Task.Delay(_options.ReadinessStartupDelayMs, ct).ConfigureAwait(false);
    }

    private async Task<(bool Success, string Summary)> TryRunAgentVerifyAsync(
        GenerationContext context,
        VerifyRunPlan plan,
        CancellationToken ct)
    {
        if (!_specs.TryGet("verify", out var spec))
            return (false, "verify agent spec not found");

        var workspaceId = _workspaceStore.Create(context.Orchestrator.Files);
        try
        {
            if (!_generationAccessor.TryGetWorkspace(workspaceId, out var workspace))
                return (false, "generation workspace unavailable");

            var toolContext = new ToolContext
            {
                Workspace = workspace,
                Accessor = _generationAccessor,
                WorkingFiles = context.Orchestrator.Files.ToList(),
                FileState = new FileStateCache(),
                Plan = context.Plan,
                BuildLog = context.Orchestrator.Iterations.LastOrDefault()?.Execution?.Logs is { Count: > 0 } logs
                    ? string.Join('\n', logs.Select(l => l.Message).Take(40))
                    : null,
                Mode = AgentSessionMode.Repair,
                Session = new AgentSessionState { RunId = context.Orchestrator.Id }
            };

            var task = BuildVerifyTask(context, plan);
            using var scope = _scopes.CreateScope();
            var runner = scope.ServiceProvider.GetRequiredService<IAgentSpecSubagentRunner>();
            var result = await runner.RunAsync(spec, task, toolContext, ct).ConfigureAwait(false);
            return (result.Succeeded, result.Summary ?? "verify subagent finished");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[VerifyOrchestrator {RunId}] Agent verify failed", plan.RunId);
            return (false, $"verify subagent error: {ex.Message}");
        }
        finally
        {
            _workspaceStore.Dispose(workspaceId);
        }
    }

    private void RegisterSmokeTargets(VerifyRunPlan plan)
    {
        if (_networkRouter is null || plan.Recipe.SmokeTargets.Count == 0)
            return;

        if (plan.ShadowWorkspaceId is Guid workspaceId)
            _networkRouter.BindRun(plan.RunId, workspaceId);

        var registrations = plan.Recipe.SmokeTargets
            .Select(t => new ObscuraServiceRegistration(
                t.Name,
                t.Port,
                ExtractPath(t.Url)))
            .ToList();
        _networkRouter.RegisterServices(plan.RunId, registrations);
    }

    private static string ExtractPath(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            return "/";
        return string.IsNullOrEmpty(uri.PathAndQuery) ? "/" : uri.PathAndQuery;
    }

    private string BuildVerifyTask(GenerationContext context, VerifyRunPlan plan)
    {
        var generationPlan = context.Plan!;
        var sb = new StringBuilder();
        sb.AppendLine("Validate the generated application after testing.");
        sb.AppendLine($"Application: {generationPlan.ApplicationName}");
        sb.AppendLine($"Recipe: {plan.Recipe.Id}");
        sb.AppendLine($"Stack: {string.Join(',', generationPlan.TechStack.Languages)} / {string.Join(',', generationPlan.TechStack.Frameworks)}");
        if (plan.Recipe.SmokeTargets.Count > 0)
        {
            var targets = plan.Recipe.SmokeTargets
                .Select(t => _networkRouter?.ResolveForBrowser(plan.RunId, t.Url) ?? t.Url);
            sb.AppendLine($"Smoke targets: {string.Join(" ; ", targets)}");
        }
        sb.AppendLine(
            "Run build/tests. For UI apps use Obscura browser_* smoke flow: " +
            "browser_record_start → browser_navigate → browser_wait → browser_snapshot → browser_click → " +
            "browser_screenshot → browser_console → browser_get_content → browser_record_stop → browser_close. " +
            "Return PASS or FAIL with evidence paths (screenshot-final.png, smoke.webm, console-errors.json, dom-snapshot.md).");
        return sb.ToString();
    }

    private static async Task PersistObscuraSmokeReportAsync(
        VerifyRunPlan plan,
        ObscuraVerifySmokeResult smoke,
        CancellationToken ct)
    {
        var path = Path.Combine(plan.EvidenceDir, "obscura-smoke-report.json");
        var payload = new
        {
            runId = plan.RunId,
            recipeId = plan.Recipe.Id,
            passed = smoke.Passed,
            summary = smoke.Summary,
            targets = smoke.Targets.Select(t => new
            {
                t.TargetName,
                t.Url,
                t.Passed,
                t.Summary,
                evidence = t.EvidencePaths
            }),
            completedAtUtc = DateTime.UtcNow
        };

        await File.WriteAllTextAsync(
            path,
            JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = true }),
            ct).ConfigureAwait(false);
    }

    private static async Task PersistReadinessSummaryAsync(
        VerifyRunPlan plan,
        IReadOnlyList<VerifyReadinessResult> readinessResults,
        bool readinessPassed,
        string path,
        CancellationToken ct)
    {
        var payload = new
        {
            runId = plan.RunId,
            recipeId = plan.Recipe.Id,
            passed = readinessPassed,
            targets = readinessResults.Select(r => new
            {
                r.TargetName,
                r.Url,
                r.Ready,
                attempts = r.Attempts.Count,
                elapsedMs = (int)r.TotalElapsed.TotalMilliseconds
            }),
            completedAtUtc = DateTime.UtcNow
        };

        await File.WriteAllTextAsync(
            path,
            JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = true }),
            ct).ConfigureAwait(false);
    }

    private static async Task PersistFailureEvidenceAsync(
        VerifyRunPlan plan,
        bool shadowPassed,
        bool readinessPassed,
        bool agentPassed,
        string agentSummary,
        IReadOnlyList<VerifyReadinessResult> readinessResults,
        string path,
        CancellationToken ct)
    {
        var payload = new
        {
            runId = plan.RunId,
            recipeId = plan.Recipe.Id,
            shadowPassed,
            readinessPassed,
            agentPassed,
            agentSummary,
            readiness = readinessResults.Select(r => new
            {
                r.TargetName,
                r.Url,
                r.Ready,
                lastError = r.Attempts.LastOrDefault()?.Error
            }),
            repairHint = BuildRepairHint(shadowPassed, readinessPassed, agentPassed, readinessResults),
            capturedAtUtc = DateTime.UtcNow
        };

        await File.WriteAllTextAsync(
            path,
            JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = true }),
            ct).ConfigureAwait(false);
    }

    private static string BuildRepairHint(
        bool shadowPassed,
        bool readinessPassed,
        bool agentPassed,
        IReadOnlyList<VerifyReadinessResult> readinessResults)
    {
        var sb = new StringBuilder();
        sb.AppendLine("VERIFY_FAILURE_EVIDENCE");
        if (!shadowPassed)
            sb.AppendLine("- shadow build/test execution failed before readiness");
        if (!readinessPassed)
        {
            foreach (var target in readinessResults.Where(r => !r.Ready))
                sb.AppendLine($"- readiness probe failed for {target.TargetName} ({target.Url})");
        }
        if (!agentPassed)
            sb.AppendLine("- verify subagent reported failure");
        return sb.ToString().TrimEnd();
    }

    private static string ShellQuote(string value) => $"'{value.Replace("'", "'\\''", StringComparison.Ordinal)}'";

    private async Task<(bool Passed, string Summary)> RunPostVerifySecurityScanAsync(
        GenerationContext context,
        VerifyRunPlan plan,
        CancellationToken ct)
    {
        if (_securityScanner is null || _obscuraOptions is not { EnablePostVerifySecurityScan: true })
            return (true, string.Empty);

        var sample = BuildSecurityScanSample(context);
        if (string.IsNullOrWhiteSpace(sample.Code))
            return (true, "security_scan=skipped:no_source");

        var language = sample.Language;
        var result = await _securityScanner.QuickScanAsync(sample.Code, language, ct).ConfigureAwait(false);
        if (result is null)
            return (true, "security_scan=skipped:unavailable");

        var path = Path.Combine(plan.EvidenceDir, "security-scan.json");
        await File.WriteAllTextAsync(
            path,
            JsonSerializer.Serialize(new
            {
                runId = plan.RunId,
                result.ScanId,
                result.IsSafe,
                result.RiskLevel,
                result.IssueCount,
                result.CriticalCount,
                language,
                scannedBytes = sample.Code.Length,
                completedAtUtc = DateTime.UtcNow
            }, new JsonSerializerOptions { WriteIndented = true }),
            ct).ConfigureAwait(false);

        if (!result.IsSafe && result.CriticalCount > 0)
        {
            return (false, $"security_scan=fail: {result.CriticalCount} critical ({result.RiskLevel})");
        }

        return (true, $"security_scan=pass: issues={result.IssueCount} risk={result.RiskLevel}");
    }

    private static (string Code, string Language) BuildSecurityScanSample(GenerationContext context)
    {
        var files = context.Orchestrator.Files
            .Where(f => !string.IsNullOrWhiteSpace(f.Content))
            .OrderByDescending(f => f.Content!.Length)
            .Take(5)
            .ToList();

        if (files.Count == 0)
            return (string.Empty, "unknown");

        var sb = new StringBuilder();
        foreach (var file in files)
            sb.AppendLine(file.Content);

        var code = sb.ToString();
        if (code.Length > 32_000)
            code = code[..32_000];

        var language = InferLanguage(files[0].RelativePath);
        return (code, language);
    }

    private static string InferLanguage(string fileName)
    {
        var ext = Path.GetExtension(fileName).ToLowerInvariant();
        return ext switch
        {
            ".cs" => "csharp",
            ".ts" or ".tsx" => "typescript",
            ".js" or ".jsx" => "javascript",
            ".py" => "python",
            ".go" => "go",
            ".rs" => "rust",
            ".java" => "java",
            _ => "unknown"
        };
    }
}
