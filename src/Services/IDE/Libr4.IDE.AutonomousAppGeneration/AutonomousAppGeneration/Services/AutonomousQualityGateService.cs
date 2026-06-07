using Libr4.IDE.Application.AutonomousAppGeneration.Infrastructure;
using Libr4.IDE.Domain.AutonomousAppGeneration;
using Libr4.Shared.Contracts;
using Microsoft.Extensions.Options;

namespace Libr4.IDE.Application.AutonomousAppGeneration.Services;

public sealed class AutonomousQualityGateService : IAutonomousQualityGateService
{
    private readonly AutonomousQualityGateOptions _options;

    public AutonomousQualityGateService(IOptions<AutonomousQualityGateOptions> options)
    {
        _options = options.Value;
    }

    public QualityGateResult EvaluatePlan(GenerationPlan plan)
    {
        var reasons = new List<string>();
        var score = 10;

        if (string.IsNullOrWhiteSpace(plan.ApplicationName)) { score -= 2; reasons.Add("missing_application_name"); }
        if (plan.Phases.Count < 3) { score -= 2; reasons.Add("insufficient_phases"); }
        if (plan.RequiredAgents.Count < 3) { score -= 1; reasons.Add("insufficient_agents"); }
        if (plan.BuildCommands.Count == 0) { score -= 3; reasons.Add("missing_build_commands"); }
        if (plan.TestCommands.Count == 0) { score -= 3; reasons.Add("missing_test_commands"); }
        if (string.IsNullOrWhiteSpace(plan.RuntimeImage)) { score -= 1; reasons.Add("missing_runtime_image"); }
        if (plan.TechStack.Languages.Count == 0 || plan.TechStack.Frameworks.Count == 0) { score -= 2; reasons.Add("weak_tech_stack_definition"); }

        score = Math.Clamp(score, 0, 10);
        var passed = score >= Math.Clamp(_options.PlanMinScore, 1, 10);

        ErrorEnvelope? errorEnvelope = null;
        if (!passed)
        {
            errorEnvelope = new ErrorEnvelope(
                ErrorCodes.QualityGateFailed,
                $"Plan quality gate failed with score {score}/{_options.PlanMinScore}",
                new
                {
                    Stage = "plan",
                    Score = score,
                    RequiredScore = _options.PlanMinScore,
                    Reasons = reasons
                });
        }

        return new QualityGateResult("plan", score, passed, reasons, errorEnvelope);
    }

    public QualityGateResult EvaluateGeneratedFiles(IReadOnlyList<GeneratedFile> files, GenerationPlan plan)
    {
        var reasons = new List<string>();
        var score = 10;
        var minimumFiles = IsDotNetPlan(plan) ? 8
            : IsJavaReactFullStackPlan(plan) ? 10
            : 5;
        if (files.Count < minimumFiles) { score -= 3; reasons.Add("too_few_files"); }

        var paths = files.Select(f => StackArtifactCompleteness.SanitizeRelativePath(f.RelativePath))
            .Where(p => p.Length > 0)
            .ToList();
        if (IsJavaReactFullStackPlan(plan))
        {
            if (!paths.Any(p => p.Equals("backend/pom.xml", StringComparison.OrdinalIgnoreCase)))
            {
                score -= 2;
                reasons.Add("missing_project_files");
            }

            if (!paths.Any(p =>
                    p.StartsWith("backend/", StringComparison.OrdinalIgnoreCase)
                    && p.EndsWith(".java", StringComparison.OrdinalIgnoreCase)))
            {
                score -= 2;
                reasons.Add("missing_entrypoint");
            }

            if (!paths.Any(p => p.Equals("frontend/package.json", StringComparison.OrdinalIgnoreCase)))
            {
                score -= 2;
                reasons.Add("missing_project_files");
            }

            if (!paths.Any(p =>
                    p.StartsWith("frontend/", StringComparison.OrdinalIgnoreCase)
                    && (p.EndsWith(".tsx", StringComparison.OrdinalIgnoreCase)
                        || p.EndsWith(".jsx", StringComparison.OrdinalIgnoreCase))))
            {
                score -= 1;
                reasons.Add("missing_entrypoint");
            }

            if (!paths.Any(p =>
                    p.StartsWith("backend/", StringComparison.OrdinalIgnoreCase)
                    && p.Contains("Controller", StringComparison.OrdinalIgnoreCase)))
            {
                score -= 1;
                reasons.Add("missing_controllers");
            }
        }
        else if (IsDotNetPlan(plan))
        {
            if (!paths.Any(p => p.EndsWith(".sln", StringComparison.OrdinalIgnoreCase))) { score -= 1; reasons.Add("missing_solution"); }
            if (!paths.Any(p => p.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase))) { score -= 2; reasons.Add("missing_project_files"); }
            if (!paths.Any(p => p.EndsWith("Program.cs", StringComparison.OrdinalIgnoreCase))) { score -= 2; reasons.Add("missing_entrypoint"); }
            if (!paths.Any(p => p.Contains("Controllers", StringComparison.OrdinalIgnoreCase))) { score -= 2; reasons.Add("missing_controllers"); }
            if (!paths.Any(p => p.Contains("Services", StringComparison.OrdinalIgnoreCase))) { score -= 2; reasons.Add("missing_services"); }
        }
        else if (IsPythonPlan(plan))
        {
            if (!paths.Any(p => p.EndsWith("app.py", StringComparison.OrdinalIgnoreCase) ||
                                p.EndsWith("main.py", StringComparison.OrdinalIgnoreCase) ||
                                p.EndsWith("manage.py", StringComparison.OrdinalIgnoreCase)))
            {
                score -= 2;
                reasons.Add("missing_entrypoint");
            }

            if (!paths.Any(p => p.EndsWith("requirements.txt", StringComparison.OrdinalIgnoreCase) ||
                                p.EndsWith("pyproject.toml", StringComparison.OrdinalIgnoreCase)))
            {
                score -= 2;
                reasons.Add("missing_project_files");
            }

            var combined = string.Join('\n', files.Select(f => f.Content ?? string.Empty));
            if (IntentSuggestsHttpApi(BuildIntentBlob(plan)) && !HasValidationSignalsForPythonApi(combined))
            {
                score -= 1;
                reasons.Add("missing_api_validation_contracts");
            }

            if (IntentSuggestsHttpApi(BuildIntentBlob(plan)) && !HasErrorEnvelopeSignals(combined))
            {
                score -= 1;
                reasons.Add("missing_error_envelope_contract");
            }
        }
        else if (IsNodeOnlyPlan(plan))
        {
            if (!paths.Any(p => p.EndsWith("package.json", StringComparison.OrdinalIgnoreCase)))
            {
                score -= 2;
                reasons.Add("missing_project_files");
            }

            if (!paths.Any(p => p.EndsWith("index.js", StringComparison.OrdinalIgnoreCase) ||
                                p.EndsWith("server.js", StringComparison.OrdinalIgnoreCase) ||
                                p.EndsWith("main.js", StringComparison.OrdinalIgnoreCase) ||
                                p.EndsWith("index.ts", StringComparison.OrdinalIgnoreCase)))
            {
                score -= 2;
                reasons.Add("missing_entrypoint");
            }
        }

        if (plan.TestCommands.Count > 0 && !HasStackAppropriateTests(paths, plan))
        {
            score -= 2;
            reasons.Add("missing_test_project");
        }

        if (plan.TechStack.Databases.Count > 0 && !HasStackAppropriateDataLayer(paths, plan))
        {
            score -= 1;
            reasons.Add("missing_data_layer");
        }

        var emptyFiles = files.Count(f =>
            string.IsNullOrWhiteSpace(f.Content)
            && !f.RelativePath.EndsWith("__init__.py", StringComparison.OrdinalIgnoreCase));
        if (emptyFiles > 0)
        {
            score -= Math.Min(3, emptyFiles);
            reasons.Add("contains_empty_files");
        }

        if (_options.EnableIntentHeuristics)
            ApplyIntentHeuristics(files, plan, reasons, ref score);

        ApplyPythonApiRuntimeContractGuard(files, plan, reasons, ref score);
        ApplyComplexStackFidelityGuard(files, plan, reasons, ref score);
        ApplyFrameworkFidelityGuard(files, plan, reasons, ref score);
        ApplyProductQualityLockGuard(files, plan, reasons, ref score);

        score = Math.Clamp(score, 0, 10);
        var passed = score >= Math.Clamp(_options.GenerationMinScore, 1, 10);

        ErrorEnvelope? errorEnvelope = null;
        if (!passed)
        {
            errorEnvelope = new ErrorEnvelope(
                ErrorCodes.QualityGateFailed,
                $"Generated files quality gate failed with score {score}/{_options.GenerationMinScore}",
                new
                {
                    Stage = "generation",
                    Score = score,
                    RequiredScore = _options.GenerationMinScore,
                    Reasons = reasons
                });
        }

        return new QualityGateResult("generation", score, passed, reasons, errorEnvelope);
    }

    /// <summary>
    /// Soft checks: plan narrative vs aggregate source signals. Reduces false confidence when the LLM
    /// scaffolded structure but skipped the requested capability.
    /// </summary>
    private static void ApplyIntentHeuristics(
        IReadOnlyList<GeneratedFile> files,
        GenerationPlan plan,
        List<string> reasons,
        ref int score)
    {
        var intentBlob = BuildIntentBlob(plan);
        if (string.IsNullOrWhiteSpace(intentBlob))
            return;

        if (files.Count == 0)
            return;

        var combined = string.Join('\n', files.Select(f => f.Content ?? string.Empty));
        if (string.IsNullOrWhiteSpace(combined))
            return;

        if (IntentSuggestsAuth(intentBlob) && !HasAuthImplementationSignals(combined))
        {
            score -= 2;
            reasons.Add("intent_auth_not_reflected_in_code");
        }

        if (IntentSuggestsHttpApi(intentBlob) && !HasHttpApiSignals(combined))
        {
            score -= 1;
            reasons.Add("intent_http_api_not_reflected_in_code");
        }

        if (IntentSuggestsTaskDomain(intentBlob) && !HasTaskDomainSignals(combined, files))
        {
            score -= 1;
            reasons.Add("intent_task_domain_not_reflected_in_code");
        }
    }

    private static string BuildIntentBlob(GenerationPlan plan)
    {
        var sb = new System.Text.StringBuilder(512);
        sb.Append(plan.ApplicationName).Append(' ');
        sb.Append(plan.ApplicationDescription).Append(' ');
        sb.Append(plan.TechStack.Rationale).Append(' ');
        foreach (var p in plan.Phases)
        {
            sb.Append(p.Name).Append(' ');
            sb.Append(p.Description).Append(' ');
        }

        return sb.ToString().Trim();
    }

    private static bool IntentSuggestsAuth(string blob)
    {
        return ContainsWholeWord(blob, "jwt")
               || ContainsWholeWord(blob, "oauth")
               || blob.Contains("authentication", StringComparison.OrdinalIgnoreCase)
               || blob.Contains("authorization", StringComparison.OrdinalIgnoreCase)
               || blob.Contains("login", StringComparison.OrdinalIgnoreCase)
               || blob.Contains("bearer", StringComparison.OrdinalIgnoreCase)
               || blob.Contains("identity", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IntentSuggestsHttpApi(string blob)
    {
        return blob.Contains("rest", StringComparison.OrdinalIgnoreCase)
               || blob.Contains("http api", StringComparison.OrdinalIgnoreCase)
               || blob.Contains("web api", StringComparison.OrdinalIgnoreCase)
               || blob.Contains("endpoint", StringComparison.OrdinalIgnoreCase)
               || blob.Contains("microservice", StringComparison.OrdinalIgnoreCase)
               || (blob.Contains("api", StringComparison.OrdinalIgnoreCase) &&
                   (blob.Contains("service", StringComparison.OrdinalIgnoreCase) ||
                    blob.Contains("server", StringComparison.OrdinalIgnoreCase)));
    }

    private static bool IntentSuggestsTaskDomain(string blob)
    {
        if (ContainsWholeWord(blob, "kanban") || ContainsWholeWord(blob, "todo"))
            return true;

        if (!ContainsWholeWord(blob, "task") && !ContainsWholeWord(blob, "tasks"))
            return false;

        return blob.Contains("task management", StringComparison.OrdinalIgnoreCase)
               || blob.Contains("project management", StringComparison.OrdinalIgnoreCase)
               || blob.Contains("task board", StringComparison.OrdinalIgnoreCase)
               || blob.Contains("kanban board", StringComparison.OrdinalIgnoreCase)
               || (ContainsWholeWord(blob, "assign") && ContainsWholeWord(blob, "board"))
               || (ContainsWholeWord(blob, "ticket") && ContainsWholeWord(blob, "workflow"));
    }

    private static bool IntentSuggestsKanban(string blob)
    {
        if (IntentSuggestsFintechBanking(blob))
            return false;

        return blob.Contains("kanban", StringComparison.OrdinalIgnoreCase)
               || blob.Contains("backlog", StringComparison.OrdinalIgnoreCase)
               || (blob.Contains("board", StringComparison.OrdinalIgnoreCase)
                   && !blob.Contains("dashboard", StringComparison.OrdinalIgnoreCase))
               || blob.Contains("column", StringComparison.OrdinalIgnoreCase)
               || blob.Contains("swimlane", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IntentSuggestsFintechBanking(string blob)
    {
        return blob.Contains("banking", StringComparison.OrdinalIgnoreCase)
               || blob.Contains("fintech", StringComparison.OrdinalIgnoreCase)
               || blob.Contains("банк", StringComparison.OrdinalIgnoreCase)
               || blob.Contains("перевод", StringComparison.OrdinalIgnoreCase)
               || blob.Contains("платеж", StringComparison.OrdinalIgnoreCase)
               || blob.Contains("mobile banking", StringComparison.OrdinalIgnoreCase)
               || blob.Contains("[[JAVA_REACT_FULLSTACK]]", StringComparison.Ordinal);
    }

    private static bool HasFintechBankingSignals(string combined, IReadOnlyList<GeneratedFile> files)
    {
        var hasAccounts = combined.Contains("account", StringComparison.OrdinalIgnoreCase)
                          || combined.Contains("/api/accounts", StringComparison.OrdinalIgnoreCase);
        var hasTransfersOrPayments = combined.Contains("transfer", StringComparison.OrdinalIgnoreCase)
                                     || combined.Contains("payment", StringComparison.OrdinalIgnoreCase)
                                     || combined.Contains("перевод", StringComparison.OrdinalIgnoreCase)
                                     || combined.Contains("платеж", StringComparison.OrdinalIgnoreCase);
        var hasAuth = combined.Contains("auth", StringComparison.OrdinalIgnoreCase)
                      || combined.Contains("jwt", StringComparison.OrdinalIgnoreCase)
                      || combined.Contains("security", StringComparison.OrdinalIgnoreCase);

        var hasBackend = files.Any(f =>
            f.RelativePath.Contains("backend/", StringComparison.OrdinalIgnoreCase)
            && f.RelativePath.EndsWith(".java", StringComparison.OrdinalIgnoreCase));
        var hasFrontend = files.Any(f =>
            f.RelativePath.Contains("frontend/", StringComparison.OrdinalIgnoreCase)
            && (f.RelativePath.EndsWith(".tsx", StringComparison.OrdinalIgnoreCase)
                || f.RelativePath.EndsWith(".ts", StringComparison.OrdinalIgnoreCase)));

        return hasBackend && hasFrontend && hasAccounts && (hasTransfersOrPayments || hasAuth);
    }

    private static bool IntentSuggestsRepoBootstrap(string blob)
    {
        return blob.Contains("repo_bootstrap_context", StringComparison.OrdinalIgnoreCase)
               || blob.Contains("github", StringComparison.OrdinalIgnoreCase)
               || blob.Contains("repository", StringComparison.OrdinalIgnoreCase)
               || blob.Contains("open-source", StringComparison.OrdinalIgnoreCase)
               || blob.Contains("opensource", StringComparison.OrdinalIgnoreCase)
               || blob.Contains("obscura", StringComparison.OrdinalIgnoreCase)
               || blob.Contains("license", StringComparison.OrdinalIgnoreCase)
               || blob.Contains("лиценз", StringComparison.OrdinalIgnoreCase)
               || blob.Contains("репозитор", StringComparison.OrdinalIgnoreCase);
    }

    private static void ApplyComplexStackFidelityGuard(
        IReadOnlyList<GeneratedFile> files,
        GenerationPlan plan,
        List<string> reasons,
        ref int score)
    {
        if (!IsPythonPlan(plan))
            return;

        var intentBlob = BuildIntentBlob(plan);
        if (!IntentSuggestsComplexFastApiStack(intentBlob))
            return;

        var normalizedPaths = files
            .Select(f => GenerationPathHeuristics.NormalizeSlashes(f.RelativePath))
            .ToList();
        var combined = string.Join('\n', files.Select(f => f.Content ?? string.Empty));

        var hasDockerCompose = normalizedPaths.Any(p =>
            p.EndsWith("docker-compose.yml", StringComparison.OrdinalIgnoreCase) ||
            p.EndsWith("docker-compose.yaml", StringComparison.OrdinalIgnoreCase) ||
            p.EndsWith("compose.yml", StringComparison.OrdinalIgnoreCase) ||
            p.EndsWith("compose.yaml", StringComparison.OrdinalIgnoreCase));
        if (!hasDockerCompose)
        {
            score -= 2;
            reasons.Add("missing_stack_artifact:docker_compose");
        }

        var hasAlembic = normalizedPaths.Any(p =>
            p.Contains("alembic/", StringComparison.OrdinalIgnoreCase) ||
            p.EndsWith("alembic.ini", StringComparison.OrdinalIgnoreCase));
        if (!hasAlembic)
        {
            score -= 2;
            reasons.Add("missing_stack_artifact:alembic_migrations");
        }

        var hasWorker = normalizedPaths.Any(p =>
            p.Contains("worker", StringComparison.OrdinalIgnoreCase) ||
            p.Contains("celery", StringComparison.OrdinalIgnoreCase))
            || combined.Contains("Celery(", StringComparison.OrdinalIgnoreCase);
        if (!hasWorker)
        {
            score -= 2;
            reasons.Add("missing_stack_artifact:worker_lane");
        }

        var hasCiPipeline = normalizedPaths.Any(p =>
            p.Contains(".github/workflows/", StringComparison.OrdinalIgnoreCase) ||
            p.EndsWith(".gitlab-ci.yml", StringComparison.OrdinalIgnoreCase) ||
            p.EndsWith("azure-pipelines.yml", StringComparison.OrdinalIgnoreCase));
        if (!hasCiPipeline)
        {
            score -= 2;
            reasons.Add("missing_stack_artifact:ci_pipeline");
        }

        var hasRedisSignal = combined.Contains("redis", StringComparison.OrdinalIgnoreCase);
        if (!hasRedisSignal)
        {
            score -= 1;
            reasons.Add("missing_stack_capability:redis");
        }

        var hasPostgresSignal = combined.Contains("postgres", StringComparison.OrdinalIgnoreCase)
                                || combined.Contains("psycopg", StringComparison.OrdinalIgnoreCase)
                                || combined.Contains("asyncpg", StringComparison.OrdinalIgnoreCase);
        if (!hasPostgresSignal)
        {
            score -= 1;
            reasons.Add("missing_stack_capability:postgres");
        }

        var hasWebhookSignal = combined.Contains("webhook", StringComparison.OrdinalIgnoreCase);
        if (!hasWebhookSignal)
        {
            score -= 1;
            reasons.Add("missing_stack_capability:webhook");
        }
    }

    private static void ApplyPythonApiRuntimeContractGuard(
        IReadOnlyList<GeneratedFile> files,
        GenerationPlan plan,
        List<string> reasons,
        ref int score)
    {
        if (!IsPythonPlan(plan))
            return;

        if (StackLayoutHeuristics.UsesDjango(plan))
            return;

        var intentBlob = BuildIntentBlob(plan);
        if (!IntentSuggestsHttpApi(intentBlob))
            return;

        var dockerfile = files.FirstOrDefault(f =>
            f.RelativePath.EndsWith("Dockerfile", StringComparison.OrdinalIgnoreCase));
        var combined = string.Join('\n', files.Select(f => f.Content ?? string.Empty));

        var hasAsgiRuntimeSignal = combined.Contains("uvicorn", StringComparison.OrdinalIgnoreCase)
                                   || combined.Contains("hypercorn", StringComparison.OrdinalIgnoreCase)
                                   || combined.Contains("gunicorn", StringComparison.OrdinalIgnoreCase)
                                   || combined.Contains("FastAPI(", StringComparison.OrdinalIgnoreCase);
        if (!hasAsgiRuntimeSignal)
        {
            score -= 2;
            reasons.Add("missing_api_runtime_contract:asgi_server");
        }

        if (dockerfile is not null)
        {
            var dockerText = dockerfile.Content ?? string.Empty;
            var hasValidApiEntrypoint = dockerText.Contains("uvicorn", StringComparison.OrdinalIgnoreCase)
                                        || dockerText.Contains("gunicorn", StringComparison.OrdinalIgnoreCase)
                                        || dockerText.Contains("hypercorn", StringComparison.OrdinalIgnoreCase);
            var hasInvalidPythonEntrypoint = dockerText.Contains("CMD [\"python\"", StringComparison.OrdinalIgnoreCase)
                                             || dockerText.Contains("CMD ['python'", StringComparison.OrdinalIgnoreCase)
                                             || dockerText.Contains("python main.py", StringComparison.OrdinalIgnoreCase)
                                             || dockerText.Contains("python app.py", StringComparison.OrdinalIgnoreCase);

            if (!hasValidApiEntrypoint || hasInvalidPythonEntrypoint)
            {
                score -= 2;
                reasons.Add("missing_api_runtime_contract:docker_asgi_entrypoint");
            }
        }
    }

    private static bool ContainsWholeWord(string text, string word)
    {
        if (string.IsNullOrEmpty(word) || string.IsNullOrEmpty(text))
            return false;

        for (var i = 0; i <= text.Length - word.Length; i++)
        {
            if (string.Compare(text, i, word, 0, word.Length, StringComparison.OrdinalIgnoreCase) != 0)
                continue;

            var leftOk = i == 0 || !char.IsLetterOrDigit(text[i - 1]);
            var end = i + word.Length;
            var rightOk = end >= text.Length || !char.IsLetterOrDigit(text[end]);
            if (leftOk && rightOk)
                return true;
        }

        return false;
    }

    private static bool HasAuthImplementationSignals(string content)
    {
        return content.Contains("UseAuthentication", StringComparison.OrdinalIgnoreCase)
               || content.Contains("UseAuthorization", StringComparison.OrdinalIgnoreCase)
               || content.Contains("AddAuthentication", StringComparison.OrdinalIgnoreCase)
               || content.Contains("JwtBearer", StringComparison.OrdinalIgnoreCase)
               || content.Contains("JwtSecurityToken", StringComparison.OrdinalIgnoreCase)
               || content.Contains("[Authorize", StringComparison.OrdinalIgnoreCase)
               || content.Contains("AuthorizeAttribute", StringComparison.OrdinalIgnoreCase)
               || content.Contains("OpenIdConnect", StringComparison.OrdinalIgnoreCase)
               || content.Contains("OAuth", StringComparison.OrdinalIgnoreCase)
               || content.Contains("passport", StringComparison.OrdinalIgnoreCase)
               || content.Contains("jsonwebtoken", StringComparison.OrdinalIgnoreCase)
               || content.Contains("flask_login", StringComparison.OrdinalIgnoreCase)
               || content.Contains("Flask-Login", StringComparison.OrdinalIgnoreCase)
               || content.Contains("jwt.encode", StringComparison.OrdinalIgnoreCase)
               || content.Contains("JWTAuthentication", StringComparison.OrdinalIgnoreCase)
               || content.Contains("rest_framework_simplejwt", StringComparison.OrdinalIgnoreCase)
               || content.Contains("authenticate(", StringComparison.OrdinalIgnoreCase);
    }

    private static bool HasHttpApiSignals(string content)
    {
        return content.Contains("MapGet", StringComparison.OrdinalIgnoreCase)
               || content.Contains("MapPost", StringComparison.OrdinalIgnoreCase)
               || content.Contains("MapPut", StringComparison.OrdinalIgnoreCase)
               || content.Contains("MapDelete", StringComparison.OrdinalIgnoreCase)
               || content.Contains("HttpGet", StringComparison.OrdinalIgnoreCase)
               || content.Contains("HttpPost", StringComparison.OrdinalIgnoreCase)
               || content.Contains("HttpPut", StringComparison.OrdinalIgnoreCase)
               || content.Contains("HttpDelete", StringComparison.OrdinalIgnoreCase)
               || content.Contains("[Route", StringComparison.OrdinalIgnoreCase)
               || content.Contains("APIRouter", StringComparison.OrdinalIgnoreCase)
               || content.Contains("@app.route", StringComparison.OrdinalIgnoreCase)
               || content.Contains("router.get", StringComparison.OrdinalIgnoreCase)
               || content.Contains("router.post", StringComparison.OrdinalIgnoreCase)
               || content.Contains("FastAPI", StringComparison.OrdinalIgnoreCase)
               || content.Contains("@api_view", StringComparison.OrdinalIgnoreCase)
               || content.Contains("APIView", StringComparison.OrdinalIgnoreCase)
               || content.Contains("ModelViewSet", StringComparison.OrdinalIgnoreCase)
               || content.Contains("DefaultRouter", StringComparison.OrdinalIgnoreCase);
    }

    private static bool HasTaskDomainSignals(string content, IReadOnlyList<GeneratedFile> files)
    {
        if (content.Contains("TaskController", StringComparison.OrdinalIgnoreCase)
            || content.Contains("TasksController", StringComparison.OrdinalIgnoreCase)
            || content.Contains("TaskService", StringComparison.OrdinalIgnoreCase)
            || content.Contains("class Task", StringComparison.OrdinalIgnoreCase)
            || content.Contains("TaskDto", StringComparison.OrdinalIgnoreCase))
            return true;

        return files.Any(f =>
            f.RelativePath.Contains("task", StringComparison.OrdinalIgnoreCase)
            || f.RelativePath.Contains("todo", StringComparison.OrdinalIgnoreCase));
    }

    private static bool HasKanbanSignals(string content, IReadOnlyList<GeneratedFile> files)
    {
        var hasKanbanKeyword = content.Contains("kanban", StringComparison.OrdinalIgnoreCase);
        var hasColumnKeyword = content.Contains("column", StringComparison.OrdinalIgnoreCase)
                               || content.Contains("lane", StringComparison.OrdinalIgnoreCase)
                               || content.Contains("swimlane", StringComparison.OrdinalIgnoreCase);
        var hasStatusFlow =
            content.Contains("backlog", StringComparison.OrdinalIgnoreCase)
            || content.Contains("todo", StringComparison.OrdinalIgnoreCase)
            || content.Contains("inprogress", StringComparison.OrdinalIgnoreCase)
            || content.Contains("in_progress", StringComparison.OrdinalIgnoreCase)
            || content.Contains("doing", StringComparison.OrdinalIgnoreCase)
            || content.Contains("done", StringComparison.OrdinalIgnoreCase)
            || content.Contains("move task", StringComparison.OrdinalIgnoreCase)
            || content.Contains("transition", StringComparison.OrdinalIgnoreCase);

        if (hasKanbanKeyword || (hasColumnKeyword && hasStatusFlow))
            return true;

        return files.Any(f =>
            f.RelativePath.Contains("kanban", StringComparison.OrdinalIgnoreCase)
            || f.RelativePath.Contains("columns", StringComparison.OrdinalIgnoreCase)
            || f.RelativePath.Contains("swimlane", StringComparison.OrdinalIgnoreCase));
    }

    private static bool HasRepoBootstrapAdaptationSignals(IReadOnlyList<GeneratedFile> files, string content)
    {
        var hasGitHubLink = content.Contains("github.com/", StringComparison.OrdinalIgnoreCase);
        var hasUpstreamAdaptationNarrative =
            content.Contains("upstream", StringComparison.OrdinalIgnoreCase) ||
            content.Contains("adapted from", StringComparison.OrdinalIgnoreCase) ||
            content.Contains("forked from", StringComparison.OrdinalIgnoreCase) ||
            content.Contains("based on", StringComparison.OrdinalIgnoreCase) ||
            content.Contains("bootstrap source", StringComparison.OrdinalIgnoreCase)
            || content.Contains("исходный репозитор", StringComparison.OrdinalIgnoreCase);

        var hasBootstrapArtifact = files.Any(f =>
            f.RelativePath.EndsWith("BOOTSTRAP_EVIDENCE.md", StringComparison.OrdinalIgnoreCase) ||
            f.RelativePath.EndsWith("ADAPTATION_BRIDGE.md", StringComparison.OrdinalIgnoreCase) ||
            f.RelativePath.EndsWith("UPSTREAM_INTEGRATION.md", StringComparison.OrdinalIgnoreCase) ||
            f.RelativePath.EndsWith("UPSTREAM_SEMANTIC_EXTRACT.md", StringComparison.OrdinalIgnoreCase) ||
            f.RelativePath.EndsWith("REPO_ADAPTATION.md", StringComparison.OrdinalIgnoreCase) ||
            f.RelativePath.EndsWith("UPSTREAM.md", StringComparison.OrdinalIgnoreCase) ||
            f.RelativePath.EndsWith("MIGRATION_NOTES.md", StringComparison.OrdinalIgnoreCase));

        var hasUpstreamSnapshot = files.Any(f =>
        {
            var path = GenerationPathHeuristics.NormalizeSlashes(f.RelativePath);
            return path.StartsWith("upstream/", StringComparison.OrdinalIgnoreCase)
                   || path.Contains("/upstream/", StringComparison.OrdinalIgnoreCase);
        });

        return hasBootstrapArtifact || hasUpstreamSnapshot || (hasGitHubLink && hasUpstreamAdaptationNarrative);
    }

    private static bool LooksLikeGenericTemplateOutput(IReadOnlyList<GeneratedFile> files, string content)
    {
        var hasGeneratedAppPhrase = content.Contains("hello from generatedapp", StringComparison.OrdinalIgnoreCase)
                                    || content.Contains("generated app", StringComparison.OrdinalIgnoreCase)
                                    || content.Contains("sample weather forecast", StringComparison.OrdinalIgnoreCase)
                                    || content.Contains("template", StringComparison.OrdinalIgnoreCase) && content.Contains("todo", StringComparison.OrdinalIgnoreCase);
        if (!hasGeneratedAppPhrase)
            return false;

        var hasBusinessSignals = HasAuthImplementationSignals(content)
                                 || HasKanbanSignals(content, files)
                                 || HasTaskDomainSignals(content, files);
        return !hasBusinessSignals;
    }

    private static bool HasMeaningfulBusinessTests(IReadOnlyList<GeneratedFile> files)
    {
        var testFiles = files.Where(f =>
                GenerationPathHeuristics.NormalizeSlashes(f.RelativePath).Contains("test", StringComparison.OrdinalIgnoreCase))
            .ToList();
        if (testFiles.Count == 0)
            return false;

        var testContent = string.Join('\n', testFiles.Select(f => f.Content ?? string.Empty));
        var hasOnlyHealthStyleTests =
            testContent.Contains("health", StringComparison.OrdinalIgnoreCase)
            && !testContent.Contains("auth", StringComparison.OrdinalIgnoreCase)
            && !testContent.Contains("kanban", StringComparison.OrdinalIgnoreCase)
            && !testContent.Contains("task", StringComparison.OrdinalIgnoreCase)
            && !testContent.Contains("board", StringComparison.OrdinalIgnoreCase);

        if (hasOnlyHealthStyleTests)
            return false;

        var hasHttpIntegrationTests =
            testContent.Contains("WebApplicationFactory", StringComparison.Ordinal)
            && testContent.Contains("HttpClient", StringComparison.Ordinal)
            && (testContent.Contains("/api/auth", StringComparison.OrdinalIgnoreCase)
                || testContent.Contains("api/auth", StringComparison.OrdinalIgnoreCase))
            && (testContent.Contains("/api/kanban", StringComparison.OrdinalIgnoreCase)
                || testContent.Contains("api/kanban", StringComparison.OrdinalIgnoreCase));

        var hasBankingBusinessTests =
            testContent.Contains("transfer", StringComparison.OrdinalIgnoreCase)
            && (testContent.Contains("payment", StringComparison.OrdinalIgnoreCase)
                || testContent.Contains("accounts", StringComparison.OrdinalIgnoreCase)
                || testContent.Contains("MockMvc", StringComparison.Ordinal));

        var hasPythonApiTests =
            (testContent.Contains("APITestCase", StringComparison.OrdinalIgnoreCase)
             || testContent.Contains("APIClient", StringComparison.OrdinalIgnoreCase)
             || testContent.Contains("pytest", StringComparison.OrdinalIgnoreCase)
             || testContent.Contains("TestCase", StringComparison.OrdinalIgnoreCase))
            && (testContent.Contains("/api/", StringComparison.OrdinalIgnoreCase)
                || testContent.Contains("client.post", StringComparison.OrdinalIgnoreCase)
                || testContent.Contains("client.get", StringComparison.OrdinalIgnoreCase));

        var hasDomainBusinessTests =
            testContent.Contains("meal", StringComparison.OrdinalIgnoreCase)
            || testContent.Contains("calorie", StringComparison.OrdinalIgnoreCase)
            || testContent.Contains("analyze", StringComparison.OrdinalIgnoreCase)
            || testContent.Contains("upload", StringComparison.OrdinalIgnoreCase)
            || testContent.Contains("transfer", StringComparison.OrdinalIgnoreCase)
            || testContent.Contains("payment", StringComparison.OrdinalIgnoreCase);

        return hasHttpIntegrationTests
               || hasBankingBusinessTests
               || hasPythonApiTests
               || hasDomainBusinessTests
               || testContent.Contains("auth", StringComparison.OrdinalIgnoreCase)
               || testContent.Contains("token", StringComparison.OrdinalIgnoreCase)
               || testContent.Contains("kanban", StringComparison.OrdinalIgnoreCase)
               || (ContainsWholeWord(testContent, "task") && testContent.Contains("board", StringComparison.OrdinalIgnoreCase));
    }

    private static bool HasValidationSignalsForPythonApi(string content)
    {
        return content.Contains("BaseModel", StringComparison.OrdinalIgnoreCase)
               || content.Contains("Field(", StringComparison.OrdinalIgnoreCase)
               || content.Contains("marshmallow", StringComparison.OrdinalIgnoreCase)
               || content.Contains("validation_error", StringComparison.OrdinalIgnoreCase)
               || content.Contains("_validate_", StringComparison.OrdinalIgnoreCase)
               || content.Contains("serializers.Serializer", StringComparison.OrdinalIgnoreCase)
               || content.Contains("serializers.ModelSerializer", StringComparison.OrdinalIgnoreCase)
               || content.Contains("ValidationError", StringComparison.OrdinalIgnoreCase);
    }

    private static bool HasErrorEnvelopeSignals(string content)
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

    private static void ApplyProductQualityLockGuard(
        IReadOnlyList<GeneratedFile> files,
        GenerationPlan plan,
        List<string> reasons,
        ref int score)
    {
        if (files.Count == 0)
            return;

        var intentBlob = BuildIntentBlob(plan);
        var combined = string.Join('\n', files.Select(f => f.Content ?? string.Empty));
        if (string.IsNullOrWhiteSpace(combined))
            return;

        if (IntentSuggestsRepoBootstrap(intentBlob)
            && !QualityGateShouldSkipBootstrapKanbanChecks(plan, intentBlob)
            && !HasRepoBootstrapAdaptationSignals(files, combined))
        {
            score -= 4;
            reasons.Add("repo_bootstrap_not_reflected_in_code");
        }

        if (IntentSuggestsFintechBanking(intentBlob) && !HasFintechBankingSignals(combined, files))
        {
            score -= 3;
            reasons.Add("intent_banking_not_reflected_in_code");
        }

        if (IntentSuggestsKanban(intentBlob)
            && !QualityGateShouldSkipBootstrapKanbanChecks(plan, intentBlob)
            && !HasKanbanSignals(combined, files))
        {
            score -= 3;
            reasons.Add("intent_kanban_not_reflected_in_code");
        }

        if (LooksLikeGenericTemplateOutput(files, combined))
        {
            score -= 3;
            reasons.Add("generic_template_output_detected");
        }

        var requiresBusinessTests = IntentSuggestsTaskDomain(intentBlob)
                                    || IntentSuggestsKanban(intentBlob)
                                    || IntentSuggestsFintechBanking(intentBlob);
        if (requiresBusinessTests && !HasMeaningfulBusinessTests(files))
        {
            score -= 2;
            reasons.Add("business_tests_missing_or_superficial");
        }
    }

    private static bool QualityGateShouldSkipBootstrapKanbanChecks(GenerationPlan plan, string intentBlob) =>
        JavaReactPlanSanitizer.ShouldApply(plan, intentBlob)
        || GoldenStackPlanAligner.ShouldApply(plan, intentBlob);

    // P1-9 of audit roadmap: delegate to StackPlanHeuristics single source of truth.
    // IsDotNet is the broad classification (matches legacy semantics: C# language OR
    // dotnet framework OR dotnet runtime; no api-intent, no exclusion of python/node).
    private static bool IsDotNetPlan(GenerationPlan plan) => StackPlanHeuristics.IsDotNet(plan);
    private static bool IsPythonPlan(GenerationPlan plan) => StackPlanHeuristics.IsPython(plan);
    private static bool IsNodePlan(GenerationPlan plan) => StackPlanHeuristics.IsNode(plan);
    private static bool IsJavaReactFullStackPlan(GenerationPlan plan) =>
        StackPlanHeuristics.Classify(plan) == StackKind.JavaReactFullStack;
    private static bool IsNodeOnlyPlan(GenerationPlan plan) =>
        IsNodePlan(plan) && !StackPlanHeuristics.IsJava(plan);

    private static bool IntentSuggestsComplexFastApiStack(string blob)
    {
        if (!(blob.Contains("fastapi", StringComparison.OrdinalIgnoreCase)
              || blob.Contains("python", StringComparison.OrdinalIgnoreCase)))
            return false;

        var markers = 0;
        if (blob.Contains("postgres", StringComparison.OrdinalIgnoreCase) || blob.Contains("postgresql", StringComparison.OrdinalIgnoreCase)) markers++;
        if (blob.Contains("redis", StringComparison.OrdinalIgnoreCase)) markers++;
        if (blob.Contains("celery", StringComparison.OrdinalIgnoreCase) || blob.Contains("worker", StringComparison.OrdinalIgnoreCase)) markers++;
        if (blob.Contains("stripe", StringComparison.OrdinalIgnoreCase) || blob.Contains("webhook", StringComparison.OrdinalIgnoreCase)) markers++;
        if (blob.Contains("docker compose", StringComparison.OrdinalIgnoreCase) || blob.Contains("docker-compose", StringComparison.OrdinalIgnoreCase)) markers++;
        if (blob.Contains("ci", StringComparison.OrdinalIgnoreCase) || blob.Contains("pipeline", StringComparison.OrdinalIgnoreCase) || blob.Contains("github actions", StringComparison.OrdinalIgnoreCase)) markers++;

        return markers >= 3;
    }

    private static bool HasStackAppropriateTests(IReadOnlyList<string> paths, GenerationPlan plan)
    {
        if (IsDotNetPlan(plan))
            return paths.Any(GenerationPathHeuristics.LooksLikeDotNetTestPath);

        if (IsPythonPlan(plan))
            return paths.Any(GenerationPathHeuristics.LooksLikePythonTestPath);

        if (IsJavaReactFullStackPlan(plan))
        {
            return paths.Any(p =>
                       p.StartsWith("backend/", StringComparison.OrdinalIgnoreCase)
                       && p.Contains("test", StringComparison.OrdinalIgnoreCase))
                   || paths.Any(p =>
                       p.StartsWith("frontend/", StringComparison.OrdinalIgnoreCase)
                       && p.Contains("test", StringComparison.OrdinalIgnoreCase));
        }

        if (IsNodeOnlyPlan(plan))
            return paths.Any(GenerationPathHeuristics.LooksLikeNodeTestPath);

        return paths.Any(p =>
            GenerationPathHeuristics.NormalizeSlashes(p).Contains("test", StringComparison.OrdinalIgnoreCase));
    }

    private static bool HasStackAppropriateDataLayer(IReadOnlyList<string> paths, GenerationPlan plan)
    {
        if (IsDotNetPlan(plan))
            return paths.Any(p => p.Contains("DbContext", StringComparison.OrdinalIgnoreCase));

        if (IsPythonPlan(plan))
            return paths.Any(p =>
                p.Contains("models.py", StringComparison.OrdinalIgnoreCase) ||
                p.Contains("database.py", StringComparison.OrdinalIgnoreCase) ||
                p.Contains("db.py", StringComparison.OrdinalIgnoreCase) ||
                p.Contains("alembic", StringComparison.OrdinalIgnoreCase));

        if (IsNodePlan(plan))
            return paths.Any(p =>
                p.Contains("prisma", StringComparison.OrdinalIgnoreCase) ||
                p.Contains("sequelize", StringComparison.OrdinalIgnoreCase) ||
                p.Contains("typeorm", StringComparison.OrdinalIgnoreCase) ||
                p.Contains("mongoose", StringComparison.OrdinalIgnoreCase) ||
                p.Contains("/models/", StringComparison.OrdinalIgnoreCase));

        return paths.Any(p =>
            p.Contains("model", StringComparison.OrdinalIgnoreCase) ||
            p.Contains("db", StringComparison.OrdinalIgnoreCase));
    }

    public QualityGateResult EvaluateBuild(ExecutionResult execution)
    {
        var reasons = new List<string>();
        var score = 10;
        var buildCommands = execution.CommandExecutions
            .Where(c => c.Phase.Contains("build", StringComparison.OrdinalIgnoreCase))
            .ToList();
        var hasBuildCommands = buildCommands.Count > 0;
        var buildFailed = hasBuildCommands
            ? buildCommands.Any(c => c.ExitCode != 0)
            : !execution.Succeeded;
        var buildExitCode = hasBuildCommands
            ? buildCommands.Select(c => c.ExitCode).FirstOrDefault(c => c != 0)
            : execution.ExitCode;

        if (buildFailed)
        {
            score -= 6;
            reasons.Add("build_failed");
        }

        if (buildExitCode != 0)
        {
            score -= 2;
            reasons.Add("build_non_zero_exit");
        }

        if (!hasBuildCommands)
        {
            score -= 2;
            reasons.Add("build_commands_missing");
        }

        score = Math.Clamp(score, 0, 10);
        var passed = score >= Math.Clamp(_options.BuildMinScore, 1, 10);

        ErrorEnvelope? errorEnvelope = null;
        if (!passed)
        {
            errorEnvelope = new ErrorEnvelope(
                ErrorCodes.CompilationError,
                $"Build quality gate failed with score {score}/{_options.BuildMinScore}",
                new
                {
                    Stage = "build",
                    Score = score,
                    RequiredScore = _options.BuildMinScore,
                    Reasons = reasons,
                    ExitCode = buildExitCode,
                    Succeeded = !buildFailed
                });
        }

        return new QualityGateResult("build", score, passed, reasons, errorEnvelope);
    }

    public QualityGateResult EvaluateExecution(ExecutionResult execution, GenerationPlan plan)
    {
        var reasons = new List<string>();
        var score = 10;

        if (!execution.Succeeded) { score -= 6; reasons.Add("execution_failed"); }
        if (execution.ExitCode != 0) { score -= 2; reasons.Add("non_zero_exit_code"); }
        if (execution.CommandExecutions.Count == 0) { score -= 2; reasons.Add("no_command_audit_records"); }
        if (plan.TestCommands.Count > 0 && !execution.CommandExecutions.Any(c => c.Phase.Contains("test", StringComparison.OrdinalIgnoreCase)))
        {
            score -= 2;
            reasons.Add("test_phase_not_observed");
        }

        score = Math.Clamp(score, 0, 10);
        var passed = score >= Math.Clamp(_options.ExecutionMinScore, 1, 10);

        ErrorEnvelope? errorEnvelope = null;
        if (!passed)
        {
            errorEnvelope = new ErrorEnvelope(
                ErrorCodes.RuntimeError,
                $"Execution quality gate failed with score {score}/{_options.ExecutionMinScore}",
                new
                {
                    Stage = "execution",
                    Score = score,
                    RequiredScore = _options.ExecutionMinScore,
                    Reasons = reasons,
                    ExitCode = execution.ExitCode,
                    Succeeded = execution.Succeeded
                });
        }

        return new QualityGateResult("execution", score, passed, reasons, errorEnvelope);
    }

    public QualityGateResult EvaluateFixProgress(IReadOnlyList<ErrorReport> errors, IReadOnlyList<GeneratedFile> patches)
    {
        var reasons = new List<string>();
        var score = 10;

        // If there is nothing to fix and no patches are produced, this is a valid
        // terminal state after successful remediation.
        if (errors.Count == 0 && patches.Count == 0)
        {
            reasons.Add("no_fixes_needed");
            return new QualityGateResult("fix", 10, true, reasons, null);
        }

        var hasOnlyNonActionableErrors =
            errors.Count > 0 &&
            errors.All(e => string.Equals(e.ErrorType, "non_actionable_error", StringComparison.OrdinalIgnoreCase));
        if (hasOnlyNonActionableErrors && patches.Count == 0)
        {
            reasons.Add("non_actionable_errors_only");
            return new QualityGateResult("fix", 10, true, reasons, null);
        }

        if (errors.Count == 0) { score -= 4; reasons.Add("no_actionable_errors"); }
        if (patches.Count == 0) { score -= 4; reasons.Add("no_patches_generated"); }

        var emptyPatches = patches.Count(p => string.IsNullOrWhiteSpace(p.Content));
        if (emptyPatches > 0)
        {
            score -= Math.Min(2, emptyPatches);
            reasons.Add("empty_patch_content");
        }

        score = Math.Clamp(score, 0, 10);
        var passed = score >= Math.Clamp(_options.FixMinScore, 1, 10);

        ErrorEnvelope? errorEnvelope = null;
        if (!passed)
        {
            errorEnvelope = new ErrorEnvelope(
                ErrorCodes.TestError,
                $"Fix progress quality gate failed with score {score}/{_options.FixMinScore}",
                new
                {
                    Stage = "fix",
                    Score = score,
                    RequiredScore = _options.FixMinScore,
                    Reasons = reasons,
                    ErrorCount = errors.Count,
                    PatchCount = patches.Count
                });
        }

        return new QualityGateResult("fix", score, passed, reasons, errorEnvelope);
    }

    private static void ApplyFrameworkFidelityGuard(
        IReadOnlyList<GeneratedFile> files,
        GenerationPlan plan,
        List<string> reasons,
        ref int score)
    {
        if (!IsPythonPlan(plan))
            return;

        var wantsDjango = plan.TechStack.Frameworks.Any(f => f.Contains("django", StringComparison.OrdinalIgnoreCase));
        if (!wantsDjango)
            return;

        var combined = string.Join('\n', files.Select(f => f.Content ?? string.Empty));
        var hasDjangoSignals = combined.Contains("from django", StringComparison.OrdinalIgnoreCase)
                               || files.Any(f => f.RelativePath.EndsWith("manage.py", StringComparison.OrdinalIgnoreCase));
        var hasFlaskSignals = combined.Contains("from flask", StringComparison.OrdinalIgnoreCase)
                              || combined.Contains("Flask(", StringComparison.OrdinalIgnoreCase);
        var hasFastApiSignals = combined.Contains("from fastapi", StringComparison.OrdinalIgnoreCase)
                                || combined.Contains("FastAPI(", StringComparison.OrdinalIgnoreCase);

        if (!hasDjangoSignals)
        {
            score -= 3;
            reasons.Add("framework_mismatch:django_missing");
        }

        if (hasFlaskSignals)
        {
            score -= 2;
            reasons.Add("framework_mismatch:flask_in_django_plan");
        }

        if (hasFastApiSignals)
        {
            score -= 2;
            reasons.Add("framework_mismatch:fastapi_in_django_plan");
        }
    }
}
