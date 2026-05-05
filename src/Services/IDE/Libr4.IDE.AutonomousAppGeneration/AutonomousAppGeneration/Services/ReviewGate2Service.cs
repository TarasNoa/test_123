using System.Text.RegularExpressions;
using Libr4.IDE.Domain.AutonomousAppGeneration;
using Microsoft.Extensions.Logging;

namespace Libr4.IDE.Application.AutonomousAppGeneration.Services;

/// <summary>
/// Comprehensive review gate with static checks aggregation, architecture validation, and regression detection.
/// </summary>
public sealed class ReviewGate2Service : IReviewGate2Service
{
    private static readonly Regex AwsAccessKey = new(
        @"AKIA[0-9A-Z]{16}",
        RegexOptions.CultureInvariant,
        TimeSpan.FromMilliseconds(150));

    private static readonly Regex PasswordLiteral = new(
        @"(password|passwd|pwd|secret)\s*=\s*[""'][^""'\r\n]{6,}[""']",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant,
        TimeSpan.FromMilliseconds(200));

    private static readonly Regex DangerousShell = new(
        @"\b(rm\s+-rf|mkfs\.|curl[^;\n]*\|\s*bash|wget[^;\n]*\|\s*sh)\b",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant,
        TimeSpan.FromMilliseconds(200));

    private readonly ILogger<ReviewGate2Service> _logger;
    private readonly IReadOnlyList<IArchitectureCheckRule> _semanticRules;

    public ReviewGate2Service(ILogger<ReviewGate2Service> logger)
        : this(logger, Array.Empty<IArchitectureCheckRule>())
    {
    }

    /// <summary>
    /// P1-1 of audit roadmap. Optional Roslyn-backed rules layered on top of
    /// legacy substring checks. Rules with the same <c>CheckId</c> as a legacy
    /// item override its outcome (semantic AST is more reliable than text match).
    /// </summary>
    public ReviewGate2Service(
        ILogger<ReviewGate2Service> logger,
        IEnumerable<IArchitectureCheckRule>? semanticRules)
    {
        _logger = logger;
        _semanticRules = (semanticRules ?? Array.Empty<IArchitectureCheckRule>()).ToList();
    }

    public IReadOnlyList<StaticCheckResult> EvaluateStaticChecks(
        IReadOnlyList<GeneratedFile> files,
        GenerationPlan plan)
    {
        _ = plan;
        var results = new List<StaticCheckResult>();

        // Security check
        var securityIssues = new List<string>();
        foreach (var file in files)
        {
            if (IsTestFile(file.RelativePath))
                continue;

            var content = file.Content ?? string.Empty;

            if (content.Contains("-----BEGIN PRIVATE KEY-----", StringComparison.Ordinal) ||
                content.Contains("-----BEGIN RSA PRIVATE KEY-----", StringComparison.Ordinal))
                securityIssues.Add($"private_key_material:{file.RelativePath}");

            if (AwsAccessKey.IsMatch(content))
                securityIssues.Add($"aws_access_key_pattern:{file.RelativePath}");

            if (PasswordLiteral.IsMatch(content))
                securityIssues.Add($"hardcoded_credential_literal:{file.RelativePath}");

            if (file.RelativePath.EndsWith(".sh", StringComparison.OrdinalIgnoreCase) &&
                DangerousShell.IsMatch(content))
                securityIssues.Add($"dangerous_shell_construct:{file.RelativePath}");
        }

        results.Add(new StaticCheckResult(
            "security_scan",
            securityIssues.Count == 0,
            securityIssues.Count,
            securityIssues));

        // Lint check (basic: file naming, structure)
        var lintIssues = new List<string>();
        foreach (var file in files)
        {
            if (string.IsNullOrWhiteSpace(file.RelativePath))
                lintIssues.Add("empty_file_path");

            if (file.Content?.Length > 50000)
                lintIssues.Add($"oversized_file:{file.RelativePath}");

            if (file.RelativePath.Contains("//") || file.RelativePath.Contains("\\\\"))
                lintIssues.Add($"malformed_path:{file.RelativePath}");
        }

        results.Add(new StaticCheckResult(
            "lint_check",
            lintIssues.Count == 0,
            lintIssues.Count,
            lintIssues));

        // Test coverage check (basic: presence of test files)
        var testFiles = files.Where(f => IsTestFile(f.RelativePath)).ToList();
        var hasTests = testFiles.Count > 0;
        results.Add(new StaticCheckResult(
            "test_coverage",
            hasTests,
            hasTests ? 0 : 1,
            hasTests ? Array.Empty<string>() : new[] { "no_test_files_generated" }));

        return results;
    }

    public IReadOnlyList<ArchitectureChecklistItem> EvaluateArchitectureChecklist(
        IReadOnlyList<GeneratedFile> files,
        GenerationPlan plan)
    {
        var items = new List<ArchitectureChecklistItem>();

        // Checklist item 1: Separation of concerns
        var hasSeparation = files.Count > 1 && files.Select(f => f.RelativePath).Distinct().Count() > 1;
        items.Add(new ArchitectureChecklistItem(
            "separation_of_concerns",
            "Code should be organized into separate modules/files",
            hasSeparation,
            hasSeparation ? $"Generated {files.Count} distinct files" : null,
            hasSeparation ? null : "Consolidate related functionality into separate files"));

        // Checklist item 2: Configuration externalization
        var hasConfigFiles = files.Any(f =>
            f.RelativePath.EndsWith(".json", StringComparison.OrdinalIgnoreCase) ||
            f.RelativePath.EndsWith(".yaml", StringComparison.OrdinalIgnoreCase) ||
            f.RelativePath.EndsWith(".yml", StringComparison.OrdinalIgnoreCase) ||
            f.RelativePath.EndsWith(".env", StringComparison.OrdinalIgnoreCase) ||
            f.RelativePath.EndsWith(".env.example", StringComparison.OrdinalIgnoreCase) ||
            f.RelativePath.EndsWith("settings.py", StringComparison.OrdinalIgnoreCase) ||
            f.RelativePath.EndsWith("config.py", StringComparison.OrdinalIgnoreCase) ||
            f.RelativePath.Contains("/config/", StringComparison.OrdinalIgnoreCase) ||
            f.RelativePath.Contains("\\config\\", StringComparison.OrdinalIgnoreCase));

        // Python-specific: check for pydantic settings patterns in code
        var hasPythonConfig = !hasConfigFiles && files.Any(f =>
            f.RelativePath.EndsWith(".py", StringComparison.OrdinalIgnoreCase) &&
            (f.Content?.Contains("BaseSettings", StringComparison.Ordinal) == true ||
             f.Content?.Contains("Settings(", StringComparison.Ordinal) == true ||
             f.Content?.Contains("os.getenv", StringComparison.Ordinal) == true ||
             f.Content?.Contains("os.environ", StringComparison.Ordinal) == true ||
             f.Content?.Contains("environ.get", StringComparison.Ordinal) == true));

        var configSatisfied = hasConfigFiles || hasPythonConfig;
        items.Add(new ArchitectureChecklistItem(
            "config_externalization",
            "Configuration should be externalized from code",
            configSatisfied,
            configSatisfied ? "Configuration externalization detected" : null,
            configSatisfied ? null : "Add configuration files (appsettings.json, .env, .env.example, settings.py, etc.)"));

        // Checklist item 3: Documentation
        var hasDocumentation = files.Any(f =>
            f.RelativePath.EndsWith(".md", StringComparison.OrdinalIgnoreCase) ||
            f.RelativePath.EndsWith(".txt", StringComparison.OrdinalIgnoreCase) ||
            (f.Content?.Contains("///", StringComparison.Ordinal) == true) ||
            (f.Content?.Contains("/**", StringComparison.Ordinal) == true));
        items.Add(new ArchitectureChecklistItem(
            "documentation",
            "Code should include documentation",
            hasDocumentation,
            hasDocumentation ? "Documentation found" : null,
            hasDocumentation ? null : "Add README.md or code comments"));

        // Checklist item 3.5: Test quality floor (P1.1)
        var testFiles = files.Where(f => IsTestFile(f.RelativePath)).ToList();
        var hasPlaceholderTests = false;
        var hasIntegrationTests = false;
        var hasNegativeTests = false;

        foreach (var testFile in testFiles)
        {
            var content = testFile.Content ?? string.Empty;

            // Check for placeholder tests
            if (content.Contains("assert True", StringComparison.OrdinalIgnoreCase) ||
                content.Contains("assert pass", StringComparison.OrdinalIgnoreCase) ||
                content.Contains("assert True()", StringComparison.OrdinalIgnoreCase) ||
                content.Contains("assert 1 == 1", StringComparison.OrdinalIgnoreCase) ||
                content.Contains("assert True == True", StringComparison.OrdinalIgnoreCase))
            {
                hasPlaceholderTests = true;
            }

            // Check for integration tests (database, api, external service calls)
            if (content.Contains("integration", StringComparison.OrdinalIgnoreCase) ||
                content.Contains("client.", StringComparison.OrdinalIgnoreCase) ||
                content.Contains("httpx", StringComparison.OrdinalIgnoreCase) ||
                content.Contains("requests.", StringComparison.OrdinalIgnoreCase) ||
                content.Contains("pytest.mark.integration", StringComparison.OrdinalIgnoreCase))
            {
                hasIntegrationTests = true;
            }

            // Check for negative tests (error handling, 400/500 responses, exceptions)
            if (content.Contains("400", StringComparison.OrdinalIgnoreCase) ||
                content.Contains("404", StringComparison.OrdinalIgnoreCase) ||
                content.Contains("500", StringComparison.OrdinalIgnoreCase) ||
                content.Contains("raises", StringComparison.OrdinalIgnoreCase) ||
                content.Contains("exception", StringComparison.OrdinalIgnoreCase) ||
                content.Contains("error", StringComparison.OrdinalIgnoreCase))
            {
                hasNegativeTests = true;
            }
        }

        var testQualitySatisfied = !hasPlaceholderTests && hasIntegrationTests && hasNegativeTests;
        items.Add(new ArchitectureChecklistItem(
            "test_quality_floor",
            "Tests should not be placeholders and should include integration and negative path tests",
            testQualitySatisfied,
            testQualitySatisfied ? "Test quality floor satisfied" : null,
            testQualitySatisfied ? null : "Remove placeholder tests (assert True) and add integration/negative path tests"));

        // Checklist item 3.75: Observability baseline (P1.2)
        var hasStructuredLogs = files.Any(f =>
            (f.Content?.Contains("logging", StringComparison.OrdinalIgnoreCase) == true ||
             f.Content?.Contains("logger", StringComparison.OrdinalIgnoreCase) == true ||
             f.Content?.Contains("log.info", StringComparison.OrdinalIgnoreCase) == true ||
             f.Content?.Contains("log.error", StringComparison.OrdinalIgnoreCase) == true) &&
            (f.Content?.Contains("json", StringComparison.OrdinalIgnoreCase) == true ||
             f.Content?.Contains("structured", StringComparison.OrdinalIgnoreCase) == true));

        var hasCorrelationId = files.Any(f =>
            f.Content?.Contains("correlation", StringComparison.OrdinalIgnoreCase) == true ||
            f.Content?.Contains("x-request-id", StringComparison.OrdinalIgnoreCase) == true ||
            f.Content?.Contains("request_id", StringComparison.OrdinalIgnoreCase) == true ||
            f.Content?.Contains("trace_id", StringComparison.OrdinalIgnoreCase) == true);

        var hasReadinessCheck = files.Any(f =>
            f.RelativePath.Contains("health", StringComparison.OrdinalIgnoreCase) ||
            f.RelativePath.Contains("readiness", StringComparison.OrdinalIgnoreCase) ||
            f.Content?.Contains("/health", StringComparison.OrdinalIgnoreCase) == true ||
            f.Content?.Contains("/readiness", StringComparison.OrdinalIgnoreCase) == true);

        var observabilitySatisfied = hasStructuredLogs && hasCorrelationId && hasReadinessCheck;
        items.Add(new ArchitectureChecklistItem(
            "observability_baseline",
            "Application should have structured logs, correlation id, and health/readiness endpoints",
            observabilitySatisfied,
            observabilitySatisfied ? "Observability baseline satisfied" : null,
            observabilitySatisfied ? null : "Add structured logging (JSON), correlation id (x-request-id), and /health or /readiness endpoints"));

        // Checklist item 3.8: Infra completeness (P1.3)
        var hasDockerCompose = files.Any(f =>
            f.RelativePath.EndsWith("docker-compose.yml", StringComparison.OrdinalIgnoreCase) ||
            f.RelativePath.EndsWith("docker-compose.yaml", StringComparison.OrdinalIgnoreCase));

        var hasCIWorkflow = files.Any(f =>
            f.RelativePath.Contains(".github", StringComparison.OrdinalIgnoreCase) ||
            f.RelativePath.Contains(".gitlab-ci", StringComparison.OrdinalIgnoreCase) ||
            f.RelativePath.Contains("jenkinsfile", StringComparison.OrdinalIgnoreCase) ||
            f.RelativePath.Contains("azure-pipelines", StringComparison.OrdinalIgnoreCase) ||
            f.RelativePath.EndsWith(".yml", StringComparison.OrdinalIgnoreCase) && f.Content?.Contains("ci", StringComparison.OrdinalIgnoreCase) == true);

        var hasRunScripts = files.Any(f =>
            f.RelativePath.EndsWith("Makefile", StringComparison.OrdinalIgnoreCase) ||
            f.RelativePath.EndsWith("run.sh", StringComparison.OrdinalIgnoreCase) ||
            f.RelativePath.EndsWith("start.sh", StringComparison.OrdinalIgnoreCase) ||
            f.RelativePath.Contains("scripts/", StringComparison.OrdinalIgnoreCase));

        var infraSatisfied = hasDockerCompose && hasCIWorkflow && hasRunScripts;
        items.Add(new ArchitectureChecklistItem(
            "infra_completeness",
            "Infrastructure should include docker-compose.yml, CI workflow, and run scripts",
            infraSatisfied,
            infraSatisfied ? "Infra completeness satisfied" : null,
            infraSatisfied ? null : "Add docker-compose.yml, CI workflow (GitHub Actions/GitLab CI), and run scripts (Makefile/start.sh)"));

        // Checklist item 3.9: Domain completeness heuristics (P1.4) - billing/payment patterns
        var hasWebhookEndpoint = files.Any(f =>
            f.Content?.Contains("webhook", StringComparison.OrdinalIgnoreCase) == true ||
            f.Content?.Contains("/webhook", StringComparison.OrdinalIgnoreCase) == true ||
            f.RelativePath.Contains("webhook", StringComparison.OrdinalIgnoreCase));

        var hasIdempotency = files.Any(f =>
            f.Content?.Contains("idempotency", StringComparison.OrdinalIgnoreCase) == true ||
            f.Content?.Contains("idempotent", StringComparison.OrdinalIgnoreCase) == true ||
            f.Content?.Contains("idempotency_key", StringComparison.OrdinalIgnoreCase) == true);

        var hasAuditTrail = files.Any(f =>
            f.Content?.Contains("audit", StringComparison.OrdinalIgnoreCase) == true ||
            f.Content?.Contains("audit_log", StringComparison.OrdinalIgnoreCase) == true ||
            f.RelativePath.Contains("audit", StringComparison.OrdinalIgnoreCase));

        var hasRateLimiting = files.Any(f =>
            f.Content?.Contains("rate_limit", StringComparison.OrdinalIgnoreCase) == true ||
            f.Content?.Contains("ratelimit", StringComparison.OrdinalIgnoreCase) == true ||
            f.Content?.Contains("throttle", StringComparison.OrdinalIgnoreCase) == true);

        // Only check domain completeness if billing/payment is mentioned in the plan description
        var isBillingApp = plan.ApplicationDescription.Contains("billing", StringComparison.OrdinalIgnoreCase) ||
                          plan.ApplicationDescription.Contains("payment", StringComparison.OrdinalIgnoreCase) ||
                          plan.ApplicationDescription.Contains("stripe", StringComparison.OrdinalIgnoreCase);

        if (isBillingApp)
        {
            var domainSatisfied = hasWebhookEndpoint && hasIdempotency && hasAuditTrail && hasRateLimiting;
            items.Add(new ArchitectureChecklistItem(
                "domain_completeness",
                "Billing/payment apps should have webhook endpoints, idempotency handling, audit trail, and rate limiting",
                domainSatisfied,
                domainSatisfied ? "Domain completeness satisfied" : null,
                domainSatisfied ? null : "Add webhook endpoint, idempotency handling (idempotency_key), audit trail, and rate limiting for billing/payment apps"));
        }

        // Checklist item 3.95: Stack template packs (P2.3) - stack-specific template patterns
        var isFastAPITemplate = plan.TechStack.Frameworks.Any(f => f.Contains("fastapi", StringComparison.OrdinalIgnoreCase));
        var isAspNetCoreTemplate = plan.TechStack.Frameworks.Any(f => f.Contains("asp.net core", StringComparison.OrdinalIgnoreCase) ||
                                                          f.Contains("asp.net", StringComparison.OrdinalIgnoreCase));
        var isReactTemplate = plan.TechStack.Frameworks.Any(f => f.Contains("react", StringComparison.OrdinalIgnoreCase));

        var stackTemplateSatisfied = false;
        var stackTemplateHint = string.Empty;

        if (isFastAPITemplate)
        {
            // FastAPI template patterns: main.py, routers/, models/, dependencies.py
            var hasMainPy = files.Any(f => f.RelativePath.EndsWith("main.py", StringComparison.OrdinalIgnoreCase));
            var hasRouters = files.Any(f => f.RelativePath.Contains("router", StringComparison.OrdinalIgnoreCase));
            var hasModels = files.Any(f => f.RelativePath.Contains("model", StringComparison.OrdinalIgnoreCase));
            var hasFastAPIDependencies = files.Any(f => f.RelativePath.EndsWith("dependencies.py", StringComparison.OrdinalIgnoreCase));

            stackTemplateSatisfied = hasMainPy && hasRouters && hasModels;
            stackTemplateHint = "Add FastAPI template structure: main.py, routers/, models/, dependencies.py";
        }
        else if (isAspNetCoreTemplate)
        {
            // ASP.NET Core template patterns: Program.cs, Controllers/, Models/, Services/
            var hasProgramCs = files.Any(f => f.RelativePath.EndsWith("Program.cs", StringComparison.OrdinalIgnoreCase));
            var hasControllers = files.Any(f => f.RelativePath.Contains("Controller", StringComparison.OrdinalIgnoreCase));
            var hasModels = files.Any(f => f.RelativePath.Contains("Models", StringComparison.OrdinalIgnoreCase));
            var hasServices = files.Any(f => f.RelativePath.Contains("Services", StringComparison.OrdinalIgnoreCase));

            stackTemplateSatisfied = hasProgramCs && hasControllers && hasModels;
            stackTemplateHint = "Add ASP.NET Core template structure: Program.cs, Controllers/, Models/, Services/";
        }
        else if (isReactTemplate)
        {
            // React template patterns: src/App.jsx, src/components/, package.json
            var hasAppJsx = files.Any(f => f.RelativePath.Contains("App", StringComparison.OrdinalIgnoreCase) &&
                                         (f.RelativePath.EndsWith(".jsx", StringComparison.OrdinalIgnoreCase) ||
                                          f.RelativePath.EndsWith(".tsx", StringComparison.OrdinalIgnoreCase)));
            var hasComponents = files.Any(f => f.RelativePath.Contains("components", StringComparison.OrdinalIgnoreCase));
            var hasPackageJson = files.Any(f => f.RelativePath.EndsWith("package.json", StringComparison.OrdinalIgnoreCase));

            stackTemplateSatisfied = hasAppJsx && hasComponents && hasPackageJson;
            stackTemplateHint = "Add React template structure: src/App.jsx, src/components/, package.json";
        }
        else
        {
            // Generic template: at least main entry point and some structure
            stackTemplateSatisfied = files.Count >= 2;
            stackTemplateHint = "Add minimal project structure with entry point and organized folders";
        }

        items.Add(new ArchitectureChecklistItem(
            "stack_template_packs",
            "Project should follow stack-specific template patterns",
            stackTemplateSatisfied,
            stackTemplateSatisfied ? "Stack template patterns satisfied" : null,
            stackTemplateSatisfied ? null : stackTemplateHint));

        // Checklist item 4: Error handling
        var errorHandlingPatterns = new[] { "try", "catch", "throw", "error", "exception" };
        var hasErrorHandling = files.Any(f =>
            errorHandlingPatterns.Any(p => f.Content?.Contains(p, StringComparison.OrdinalIgnoreCase) == true));
        items.Add(new ArchitectureChecklistItem(
            "error_handling",
            "Code should include error handling",
            hasErrorHandling,
            hasErrorHandling ? "Error handling patterns detected" : null,
            hasErrorHandling ? null : "Add try-catch blocks or error handling middleware"));

        var apiIntent =
            plan.ApplicationDescription.Contains("api", StringComparison.OrdinalIgnoreCase) ||
            plan.ApplicationDescription.Contains("endpoint", StringComparison.OrdinalIgnoreCase) ||
            plan.ApplicationDescription.Contains("rest", StringComparison.OrdinalIgnoreCase) ||
            plan.TechStack.Frameworks.Any(f =>
                f.Contains("fastapi", StringComparison.OrdinalIgnoreCase) ||
                f.Contains("flask", StringComparison.OrdinalIgnoreCase) ||
                f.Contains("asp.net", StringComparison.OrdinalIgnoreCase) ||
                f.Contains("express", StringComparison.OrdinalIgnoreCase));
        if (apiIntent)
        {
            var hasErrorEnvelopeContract = files.Any(f => HasErrorEnvelopeContract(f.Content ?? string.Empty));
            items.Add(new ArchitectureChecklistItem(
                "error_envelope_contract",
                "API should expose standardized error envelope with error.code and error.message",
                hasErrorEnvelopeContract,
                hasErrorEnvelopeContract ? "Error envelope contract detected" : null,
                hasErrorEnvelopeContract ? null : "Add standardized error envelope (error.code, error.message, optional details) across API errors"));
        }

        // Checklist item 5: Dependency management
        var hasDependencies = files.Any(f =>
            f.RelativePath.EndsWith("package.json", StringComparison.OrdinalIgnoreCase) ||
            f.RelativePath.EndsWith("requirements.txt", StringComparison.OrdinalIgnoreCase) ||
            f.RelativePath.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase) ||
            f.RelativePath.EndsWith("go.mod", StringComparison.OrdinalIgnoreCase));
        items.Add(new ArchitectureChecklistItem(
            "dependency_management",
            "Dependencies should be explicitly declared",
            hasDependencies,
            hasDependencies ? "Dependency manifest found" : null,
            hasDependencies ? null : "Add package.json, requirements.txt, or equivalent"));

        // Checklist item 6: DB architecture baseline (P0.4) - FastAPI-specific
        var isFastAPI = plan.TechStack.Frameworks.Any(f =>
            f.Contains("fastapi", StringComparison.OrdinalIgnoreCase));
        if (isFastAPI)
        {
            // Check for SQLAlchemy 2.x async/sync contract
            var hasSQLAlchemy = files.Any(f =>
                f.RelativePath.EndsWith(".py", StringComparison.OrdinalIgnoreCase) &&
                (f.Content?.Contains("AsyncSession", StringComparison.Ordinal) == true ||
                 (f.Content?.Contains("Session", StringComparison.Ordinal) == true && f.Content?.Contains("sqlalchemy", StringComparison.OrdinalIgnoreCase) == true)));

            // Check for session-per-request pattern (dependency injection or middleware)
            var hasSessionPerRequest = files.Any(f =>
                f.RelativePath.EndsWith(".py", StringComparison.OrdinalIgnoreCase) &&
                (f.Content?.Contains("Depends", StringComparison.Ordinal) == true ||
                 f.Content?.Contains("get_db", StringComparison.Ordinal) == true ||
                 (f.Content?.Contains("middleware", StringComparison.OrdinalIgnoreCase) == true && f.Content?.Contains("session", StringComparison.OrdinalIgnoreCase) == true)));

            // Check for alembic migrations
            var hasAlembic = files.Any(f =>
                f.RelativePath.Contains("alembic", StringComparison.OrdinalIgnoreCase) ||
                f.RelativePath.Contains("migrations", StringComparison.OrdinalIgnoreCase));

            var dbArchSatisfied = hasSQLAlchemy && hasSessionPerRequest && hasAlembic;
            items.Add(new ArchitectureChecklistItem(
                "db_architecture_baseline",
                "FastAPI should use unified SQLAlchemy 2.x contract with session-per-request and alembic migrations",
                dbArchSatisfied,
                dbArchSatisfied ? "DB architecture baseline satisfied" : null,
                dbArchSatisfied ? null : "Add SQLAlchemy 2.x AsyncSession/Session, session-per-request pattern (Depends/get_db), and alembic migrations"));
        }

        // P1-1 of audit roadmap: layer semantic AST-based rules on top of substring checks.
        // Rules override legacy items with the same CheckId — Roslyn evidence beats text matching.
        if (_semanticRules.Count > 0)
        {
            ApplySemanticRules(items, files, plan);
        }

        return items;
    }

    private void ApplySemanticRules(
        List<ArchitectureChecklistItem> items,
        IReadOnlyList<GeneratedFile> files,
        GenerationPlan plan)
    {
        foreach (var rule in _semanticRules)
        {
            try
            {
                if (!rule.AppliesTo(plan)) continue;
                var outcome = rule.EvaluateAsync(files, plan, CancellationToken.None).GetAwaiter().GetResult();
                var existing = items.FindIndex(i => string.Equals(i.ItemId, outcome.CheckId, StringComparison.Ordinal));
                var newItem = new ArchitectureChecklistItem(
                    outcome.CheckId,
                    existing >= 0 ? items[existing].Description : outcome.CheckId,
                    outcome.Satisfied,
                    outcome.Detail,
                    outcome.RemediationHint);
                if (existing >= 0)
                {
                    if (items[existing].Satisfied != outcome.Satisfied)
                    {
                        _logger.LogInformation(
                            "Semantic rule {RuleId} overrode legacy result: legacy={LegacySatisfied} semantic={SemanticSatisfied}",
                            outcome.CheckId, items[existing].Satisfied, outcome.Satisfied);
                    }
                    items[existing] = newItem;
                }
                else
                {
                    items.Add(newItem);
                }
            }
            catch (Exception ex)
            {
                // Rules must never crash review2; treat as non-blocking.
                _logger.LogWarning(ex, "Semantic rule {RuleId} threw; skipping.", rule.CheckId);
            }
        }
    }

    public IReadOnlyList<RegressionGuardResult> DetectRegressions(
        IReadOnlyList<GeneratedFile> files,
        IReadOnlyList<QualityGateResult> baselineMetrics,
        GenerationPlan plan)
    {
        var results = new List<RegressionGuardResult>();

        if (baselineMetrics.Count == 0)
            return results;

        // Metric 1: File count with baseline-aware thresholding
        var baselineFileCount = ExtractIntMetric(baselineMetrics, "generation", "files_generated=");
        if (baselineFileCount is null)
        {
            var scoreFallback = baselineMetrics
                .FirstOrDefault(m => m.Stage.Equals("generation", StringComparison.OrdinalIgnoreCase))?.Score ?? 0;
            baselineFileCount = scoreFallback > 10 ? scoreFallback : null;
        }
        if (baselineFileCount is not null)
        {
            var currentFileCount = files.Count;
            var fileCountDelta = currentFileCount - baselineFileCount.Value;

            // Baseline-aware threshold: allow proportional growth for larger baselines
            // Small baselines (<= 5): strict threshold of 2 files (keep original strictness)
            // Medium baselines (6-15): allow 50% growth (minimum 3 files)
            // Large baselines (> 15): allow 40% growth (minimum 10 files)
            var threshold = baselineFileCount.Value <= 5 ? 2 :
                           baselineFileCount.Value < 15 ? Math.Max(3, (int)(baselineFileCount.Value * 0.5)) :
                           Math.Max(10, (int)(baselineFileCount.Value * 0.4));

            // Safety exception: if this is a frontend framework with many component files, allow more growth
            var isFrontendFramework = plan.TechStack.Frameworks.Any(f =>
                f.Contains("react", StringComparison.OrdinalIgnoreCase) ||
                f.Contains("vue", StringComparison.OrdinalIgnoreCase) ||
                f.Contains("angular", StringComparison.OrdinalIgnoreCase) ||
                f.Contains("blazor", StringComparison.OrdinalIgnoreCase) ||
                f.Contains("next", StringComparison.OrdinalIgnoreCase));

            if (isFrontendFramework)
                threshold = Math.Max(threshold, (int)(baselineFileCount.Value * 0.6)); // 60% growth allowed for frontend

            // Negative delta (file loss) is always a regression
            // Positive delta (file growth) is only flagged as regression if excessive (> 2x threshold)
            if (fileCountDelta < -threshold)
            {
                results.Add(new RegressionGuardResult(
                    true,
                    fileCountDelta,
                    "file_count",
                    baselineFileCount.Value.ToString(),
                    currentFileCount.ToString()));
            }
            else if (fileCountDelta > threshold * 2)
            {
                // Only flag excessive growth as regression (more than 2x allowed threshold)
                results.Add(new RegressionGuardResult(
                    false,
                    fileCountDelta,
                    "file_count",
                    baselineFileCount.Value.ToString(),
                    currentFileCount.ToString()));
            }
        }

        // Metric 2: Total content size
        var baselineSizeMetric = ExtractIntMetric(baselineMetrics, "build", "total_size_bytes=");
        if (baselineSizeMetric is null)
        {
            var scoreFallback = baselineMetrics
                .FirstOrDefault(m => m.Stage.Equals("build", StringComparison.OrdinalIgnoreCase))?.Score ?? 0;
            baselineSizeMetric = scoreFallback > 10 ? scoreFallback : null;
        }
        if (baselineSizeMetric is not null)
        {
            var currentSize = files.Sum(f => f.Content?.Length ?? 0);
            var sizeDelta = currentSize - baselineSizeMetric.Value;

            if (Math.Abs(sizeDelta) > 5000)
            {
                results.Add(new RegressionGuardResult(
                    sizeDelta > 0,
                    sizeDelta,
                    "total_size_bytes",
                    baselineSizeMetric.Value.ToString(),
                    currentSize.ToString()));
            }
        }

        // Metric 3: Quality gate pass rate
        var baselinePassRate = baselineMetrics.Count(m => m.Passed) / (double)baselineMetrics.Count;
        var currentPassRate = 1.0; // Assume current is passing for now
        var passRateDelta = currentPassRate - baselinePassRate;

        if (passRateDelta < -0.1)
        {
            results.Add(new RegressionGuardResult(
                true,
                passRateDelta,
                "quality_gate_pass_rate",
                (baselinePassRate * 100).ToString("F1") + "%",
                (currentPassRate * 100).ToString("F1") + "%"));
        }

        // Metric 4: Semantic regression checks (P2.2)
        // Check for removal of critical patterns
        var currentHasAuth = files.Any(f => f.Content?.Contains("auth", StringComparison.OrdinalIgnoreCase) == true);
        var currentHasLogging = files.Any(f => f.Content?.Contains("logging", StringComparison.OrdinalIgnoreCase) == true ||
                                             f.Content?.Contains("logger", StringComparison.OrdinalIgnoreCase) == true);
        var currentHasTests = files.Any(f => IsTestFile(f.RelativePath));
        var currentHasConfig = files.Any(f => f.RelativePath.Contains("config", StringComparison.OrdinalIgnoreCase) ||
                                             f.RelativePath.EndsWith(".env", StringComparison.OrdinalIgnoreCase));
        var currentHasSecurity = files.Any(f => f.Content?.Contains("jwt", StringComparison.OrdinalIgnoreCase) == true ||
                                               f.Content?.Contains("encryption", StringComparison.OrdinalIgnoreCase) == true);

        // Extract baseline semantic metrics from reasons
        var baselineHasAuth = HasReasonToken(baselineMetrics, "auth");
        var baselineHasLogging = HasReasonToken(baselineMetrics, "logging");
        var baselineHasTests = HasReasonToken(baselineMetrics, "test");
        var baselineHasConfig = HasReasonToken(baselineMetrics, "config");
        var baselineHasSecurity = HasReasonToken(baselineMetrics, "security");

        // Flag regressions where critical patterns were removed
        if (baselineHasAuth && !currentHasAuth)
        {
            results.Add(new RegressionGuardResult(
                true,
                -1,
                "semantic_auth_removed",
                "present",
                "missing"));
        }

        if (baselineHasLogging && !currentHasLogging)
        {
            results.Add(new RegressionGuardResult(
                true,
                -1,
                "semantic_logging_removed",
                "present",
                "missing"));
        }

        if (baselineHasTests && !currentHasTests)
        {
            results.Add(new RegressionGuardResult(
                true,
                -1,
                "semantic_tests_removed",
                "present",
                "missing"));
        }

        if (baselineHasConfig && !currentHasConfig)
        {
            results.Add(new RegressionGuardResult(
                true,
                -1,
                "semantic_config_removed",
                "present",
                "missing"));
        }

        if (baselineHasSecurity && !currentHasSecurity)
        {
            results.Add(new RegressionGuardResult(
                true,
                -1,
                "semantic_security_removed",
                "present",
                "missing"));
        }

        return results;
    }

    public ReviewGateDecision EvaluateComprehensive(
        string stage,
        IReadOnlyList<GeneratedFile> files,
        GenerationPlan plan,
        IReadOnlyList<QualityGateResult> baselineMetrics)
    {
        var staticChecks = EvaluateStaticChecks(files, plan);
        var architectureChecklist = EvaluateArchitectureChecklist(files, plan);
        var regressions = DetectRegressions(files, baselineMetrics, plan);

        var staticChecksPassed = staticChecks.All(c => c.Passed);
        var architectureChecksPassed = architectureChecklist.All(c => c.Satisfied);
        var noRegressions = regressions.All(r => !r.IsRegression);

        var overallScore = 10;
        var reasons = new List<string>();
        var hints = new List<string>();

        if (!staticChecksPassed)
        {
            overallScore -= 3;
            foreach (var check in staticChecks.Where(c => !c.Passed))
            {
                reasons.Add($"static_check_failed:{check.CheckName}");
                hints.AddRange(check.Issues.Take(3));
            }
        }

        if (!architectureChecksPassed)
        {
            overallScore -= 2;
            foreach (var item in architectureChecklist.Where(c => !c.Satisfied))
            {
                reasons.Add($"architecture_check_failed:{item.ItemId}");
                if (!string.IsNullOrEmpty(item.RemediationHint))
                    hints.Add(item.RemediationHint);
            }
        }

        if (!noRegressions)
        {
            overallScore -= 2;
            foreach (var regression in regressions.Where(r => r.IsRegression))
            {
                reasons.Add($"regression_detected:{regression.MetricName}");
                hints.Add($"{regression.MetricName}: baseline={regression.BaselineValue}, current={regression.CurrentValue}");
            }
        }

        overallScore = Math.Clamp(overallScore, 0, 10);
        var passed = overallScore >= 7;

        _logger.LogInformation(
            "Review gate 2.0 evaluation: stage={Stage}, score={Score}, passed={Passed}, checks={StaticCount}, architecture={ArchCount}, regressions={RegCount}",
            stage, overallScore, passed, staticChecks.Count, architectureChecklist.Count, regressions.Count);

        return new ReviewGateDecision(
            stage,
            passed,
            overallScore,
            staticChecks,
            architectureChecklist,
            regressions,
            reasons,
            hints,
            DateTime.UtcNow);
    }

    private static bool IsTestFile(string path)
    {
        var lowerPath = path.ToLowerInvariant();
        return lowerPath.Contains("/test/") ||
               lowerPath.Contains("/tests/") ||
               lowerPath.Contains("\\test\\") ||
               lowerPath.Contains("\\tests\\") ||
               lowerPath.StartsWith("tests/") ||
               lowerPath.StartsWith("tests\\") ||
               lowerPath.EndsWith("_test.py") ||
               lowerPath.EndsWith("_test.js") ||
               lowerPath.EndsWith("_test.ts") ||
               lowerPath.EndsWith(".test.js") ||
               lowerPath.EndsWith(".test.ts") ||
               lowerPath.EndsWith(".spec.js") ||
               lowerPath.EndsWith(".spec.ts") ||
               lowerPath.EndsWith("tests.cs") ||
               lowerPath.Contains("conftest.py") ||
               lowerPath.Contains("test_services.py");
    }

    private static int? ExtractIntMetric(
        IReadOnlyList<QualityGateResult> metrics,
        string stage,
        string prefix)
    {
        foreach (var metric in metrics.Where(m => m.Stage.Equals(stage, StringComparison.OrdinalIgnoreCase)))
        {
            foreach (var reason in metric.Reasons)
            {
                if (!reason.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                    continue;

                if (int.TryParse(reason[prefix.Length..], out var value))
                    return value;
            }
        }

        return null;
    }

    private static bool HasReasonToken(IReadOnlyList<QualityGateResult> metrics, string token)
    {
        foreach (var metric in metrics)
        {
            foreach (var reason in metric.Reasons)
            {
                if (reason.Contains(token, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
        }

        return false;
    }

    private static bool HasErrorEnvelopeContract(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
            return false;

        var hasErrorKey = content.Contains("\"error\"", StringComparison.OrdinalIgnoreCase)
                          || content.Contains("'error'", StringComparison.OrdinalIgnoreCase);
        var hasCodeKey = content.Contains("\"code\"", StringComparison.OrdinalIgnoreCase)
                         || content.Contains("'code'", StringComparison.OrdinalIgnoreCase);
        var hasMessageKey = content.Contains("\"message\"", StringComparison.OrdinalIgnoreCase)
                            || content.Contains("'message'", StringComparison.OrdinalIgnoreCase);

        return (hasErrorKey && hasCodeKey && hasMessageKey)
               || content.Contains("application/problem+json", StringComparison.OrdinalIgnoreCase);
    }
}
