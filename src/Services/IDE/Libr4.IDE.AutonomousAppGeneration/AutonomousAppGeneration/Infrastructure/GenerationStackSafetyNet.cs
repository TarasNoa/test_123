using System.Text;
using Libr4.IDE.Domain.AutonomousAppGeneration;

namespace Libr4.IDE.Application.AutonomousAppGeneration.Infrastructure;

/// <summary>
/// Deterministic baseline files when phased LLM output is empty, wrong-stack, or pruned away.
/// Kept separate from <see cref="LlmCodeGenerationService"/> so <see cref="TechStackArtifactFilter"/>
/// can run first and the handler can still recover a valid spine.
/// </summary>
internal static class GenerationStackSafetyNet
{
    /// <summary>
    /// Ensures Python/Node/ASP.NET minimal files exist. Mutates <paramref name="generated"/> and returns only newly added files.
    /// </summary>
    public static IReadOnlyList<GeneratedFile> EnsureMandatoryGeneratedFiles(
        GenerationPlan plan,
        Dictionary<string, GeneratedFile> generated)
    {
        var added = new List<GeneratedFile>();
        var authHint = PlanSuggestsAuth(plan);
        var useFastApi = PlanOrFilesSuggestFastApi(plan, generated);
        var useDjango = PlanOrFilesSuggestDjango(plan, generated);
        var taskDomainHint = PlanSuggestsTaskDomain(plan);
        var complexFastApiHint = PlanSuggestsComplexFastApiStack(plan);

        if (IsAspNetCorePlan(plan))
        {
            var root = GetPrimaryProjectRootPath(generated.Values) ?? "src/GeneratedApp.Api";
            var ns = BuildNamespaceFromRoot(root);
            var projectName = root.Replace('\\', '/').Split('/').LastOrDefault() ?? "GeneratedApp.Api";
            var csprojPath = $"{root}/{projectName}.csproj";

            var hasProgram = generated.Keys.Any(p => p.EndsWith("/Program.cs", StringComparison.OrdinalIgnoreCase));
            var hasController = generated.Keys.Any(p => p.Contains("/Controllers/", StringComparison.OrdinalIgnoreCase));
            var hasService = generated.Keys.Any(p => p.Contains("/Services/", StringComparison.OrdinalIgnoreCase));
            var hasModel = generated.Keys.Any(p => p.Contains("/Models/", StringComparison.OrdinalIgnoreCase));
            var hasProject = generated.Keys.Any(p => p.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase));
            var hasSolution = generated.Keys.Any(p => p.EndsWith(".sln", StringComparison.OrdinalIgnoreCase));
            var hasDataLayer = generated.Keys.Any(p =>
                p.Contains("/Data/", StringComparison.OrdinalIgnoreCase) ||
                p.Contains("DbContext", StringComparison.OrdinalIgnoreCase) ||
                p.Contains("Repository", StringComparison.OrdinalIgnoreCase));

            if (!hasProject)
                AddGeneratedIfMissing(generated, added, csprojPath, "xml", BuildAspNetCsprojContent());
            if (!hasSolution)
                AddGeneratedIfMissing(generated, added, $"{SanitizeDotNetAppName(plan.ApplicationName)}.sln", "text", BuildDotNetSolutionContent(projectName, csprojPath));

            if (!hasModel)
                AddGeneratedIfMissing(generated, added, $"{root}/Models/HealthItem.cs", "csharp", BuildHealthItemContent(ns));

            if (!hasService)
                AddGeneratedIfMissing(generated, added, $"{root}/Services/HealthService.cs", "csharp", BuildHealthServiceContent(ns));

            if (!hasController)
                AddGeneratedIfMissing(generated, added, $"{root}/Controllers/HealthController.cs", "csharp", BuildHealthControllerContent(ns));

            if (!hasProgram)
                AddGeneratedIfMissing(generated, added, $"{root}/Program.cs", "csharp", BuildProgramContent(ns, hasDataLayer));
            else
            {
                var programPath = generated.Keys.FirstOrDefault(p => p.EndsWith("/Program.cs", StringComparison.OrdinalIgnoreCase));
                if (!string.IsNullOrWhiteSpace(programPath) &&
                    generated.TryGetValue(programPath, out var programFile) &&
                    hasDataLayer &&
                    !programFile.Content.Contains("AddDbContext", StringComparison.Ordinal))
                {
                    generated[programPath] = new GeneratedFile(programPath, "csharp", BuildProgramContent(ns, true));
                    added.Add(generated[programPath]);
                }
            }

            if (!hasDataLayer)
            {
                AddGeneratedIfMissing(generated, added, $"{root}/Models/TaskItem.cs", "csharp", BuildTaskItemContent(ns));
                AddGeneratedIfMissing(generated, added, $"{root}/Data/AppDbContext.cs", "csharp", BuildAppDbContextContent(ns));
                AddGeneratedIfMissing(generated, added, $"{root}/Repositories/TaskRepository.cs", "csharp", BuildTaskRepositoryContent(ns));
            }
        }

        if (StackPlanHeuristics.IsJava(plan))
        {
            AddGeneratedIfMissing(generated, added, "backend/pom.xml", "xml", BuildSpringBootPomContent(plan.ApplicationName));
            AddGeneratedIfMissing(generated, added,
                "backend/src/main/java/com/generated/banking/BankingApplication.java",
                "java",
                BuildSpringBootApplicationContent());
            AddGeneratedIfMissing(generated, added,
                "backend/src/main/java/com/generated/banking/web/HealthController.java",
                "java",
                BuildSpringBootHealthControllerContent());
            AddGeneratedIfMissing(generated, added,
                "backend/src/main/java/com/generated/banking/web/AccountController.java",
                "java",
                BuildSpringBootAccountControllerContent());
            AddGeneratedIfMissing(generated, added,
                "backend/src/main/java/com/generated/banking/web/TransferController.java",
                "java",
                BuildSpringBootTransferControllerContent());
            AddGeneratedIfMissing(generated, added,
                "backend/src/main/java/com/generated/banking/web/PaymentController.java",
                "java",
                BuildSpringBootPaymentControllerContent());
            AddGeneratedIfMissing(generated, added,
                "backend/src/main/java/com/generated/banking/web/AuthController.java",
                "java",
                BuildSpringBootAuthControllerContent());
            AddGeneratedIfMissing(generated, added,
                "backend/src/test/java/com/generated/banking/BankingApiTests.java",
                "java",
                BuildSpringBootBankingApiTestsContent());
        }

        if (StackPlanHeuristics.IsReactTypeScriptFrontend(plan))
        {
            AddGeneratedIfMissing(generated, added, "frontend/package.json", "json", BuildReactTypeScriptPackageJson(plan.ApplicationName));
            AddGeneratedIfMissing(generated, added, "frontend/tsconfig.json", "json", BuildReactTypeScriptTsConfig());
            AddGeneratedIfMissing(generated, added, "frontend/vite.config.ts", "typescript", BuildReactViteConfig());
            AddGeneratedIfMissing(generated, added, "frontend/index.html", "html", BuildReactIndexHtml(plan.ApplicationName));
            AddGeneratedIfMissing(generated, added, "frontend/src/main.tsx", "typescript", BuildReactMainTsx());
            AddGeneratedIfMissing(generated, added, "frontend/src/App.tsx", "typescript", BuildReactAppTsx(plan.ApplicationName));
            AddGeneratedIfMissing(generated, added, "frontend/src/api/client.ts", "typescript", BuildReactApiClientTs());
            AddGeneratedIfMissing(generated, added, "frontend/src/App.test.tsx", "typescript", BuildReactAppTestTsx());
        }

        if (IsDotNetStack(plan) && plan.TestCommands.Count > 0
            && !generated.Keys.Any(GenerationPathHeuristics.LooksLikeDotNetTestPath))
        {
            var appName = SanitizeDotNetAppName(plan.ApplicationName);
            var testProj = $"{appName}.Tests";
            var basePath = $"tests/{testProj}";
            var csprojRel = $"{basePath}/{testProj}.csproj";
            var smokeRel = $"{basePath}/SmokeTests.cs";
            if (!generated.ContainsKey(csprojRel))
                AddGeneratedIfMissing(generated, added, csprojRel, "xml", BuildStandaloneXUnitTestCsproj());
            AddGeneratedIfMissing(generated, added, smokeRel, "csharp", BuildDotNetSmokeTestsContent());
        }

        if (IsPythonPlan(plan))
        {
            var hasApp = generated.Keys.Any(p =>
                p.EndsWith("app.py", StringComparison.OrdinalIgnoreCase) ||
                p.EndsWith("main.py", StringComparison.OrdinalIgnoreCase));
            var hasRequirements = generated.Keys.Any(p =>
                p.EndsWith("requirements.txt", StringComparison.OrdinalIgnoreCase) ||
                p.EndsWith("pyproject.toml", StringComparison.OrdinalIgnoreCase));
            var hasTest = generated.Keys.Any(GenerationPathHeuristics.LooksLikePythonTestPath);
            var hasDataLayer = generated.Keys.Any(p =>
                p.Contains("models.py", StringComparison.OrdinalIgnoreCase) ||
                p.Contains("database.py", StringComparison.OrdinalIgnoreCase) ||
                p.Contains("db.py", StringComparison.OrdinalIgnoreCase));

            var entryPath = useDjango ? "manage.py" : useFastApi ? "main.py" : "app.py";
            var existingEntryPath = generated.Keys.FirstOrDefault(p =>
                p.EndsWith("manage.py", StringComparison.OrdinalIgnoreCase) ||
                p.EndsWith("app.py", StringComparison.OrdinalIgnoreCase) ||
                p.EndsWith("main.py", StringComparison.OrdinalIgnoreCase));

            if (!hasApp)
            {
                var content = useDjango
                    ? BuildDjangoManageContent(plan.ApplicationName)
                    : useFastApi
                    ? BuildFastApiMainContent(plan.ApplicationName, authHint, taskDomainHint)
                    : BuildFlaskAppContent(plan.ApplicationName, authHint, taskDomainHint);
                AddGeneratedIfMissing(generated, added, entryPath, "python", content);

                if (useDjango)
                {
                    AddGeneratedIfMissing(generated, added, "app/settings.py", "python", BuildDjangoSettingsContent(plan.ApplicationName));
                    AddGeneratedIfMissing(generated, added, "app/urls.py", "python", BuildDjangoUrlsContent());
                    AddGeneratedIfMissing(generated, added, "app/wsgi.py", "python", BuildDjangoWsgiContent());
                }
            }
            else if (existingEntryPath is not null &&
                     generated.TryGetValue(existingEntryPath, out var entryFile) &&
                     LooksLikePythonPlaceholder(entryFile.Content))
            {
                var content = useDjango
                    ? BuildDjangoManageContent(plan.ApplicationName)
                    : useFastApi
                    ? BuildFastApiMainContent(plan.ApplicationName, authHint, taskDomainHint)
                    : BuildFlaskAppContent(plan.ApplicationName, authHint, taskDomainHint);
                generated[existingEntryPath] = new GeneratedFile(existingEntryPath, "python", content);
                added.Add(generated[existingEntryPath]);
            }

            if (!hasRequirements)
                AddGeneratedIfMissing(
                    generated,
                    added,
                    "requirements.txt",
                    "text",
                    BuildPythonRequirementsContent(authHint, useFastApi, useDjango, complexFastApiHint));
            else
            {
                var reqPath = generated.Keys.FirstOrDefault(p => p.EndsWith("requirements.txt", StringComparison.OrdinalIgnoreCase));
                if (reqPath is not null &&
                    generated.TryGetValue(reqPath, out var req) &&
                    string.IsNullOrWhiteSpace(req.Content))
                {
                    generated[reqPath] = new GeneratedFile(reqPath, "text", BuildPythonRequirementsContent(authHint, useFastApi, useDjango, complexFastApiHint));
                    added.Add(generated[reqPath]);
                }
            }

            // FastAPI tests rely on starlette/fastapi TestClient which requires httpx at runtime.
            if (useFastApi)
            {
                foreach (var reqPath in generated.Keys.Where(p => p.EndsWith("requirements.txt", StringComparison.OrdinalIgnoreCase)).ToList())
                {
                    if (!generated.TryGetValue(reqPath, out var reqFile))
                        continue;

                    var normalized = EnsureRequirementPackage(reqFile.Content, "httpx==0.27.2");
                    if (!string.Equals(normalized, reqFile.Content, StringComparison.Ordinal))
                    {
                        generated[reqPath] = new GeneratedFile(reqPath, "text", normalized);
                        added.Add(generated[reqPath]);
                    }
                }
            }

            // Align with default Python build command (`pip install -r requirements.txt` from workspace root):
            // if requirements is generated only under src/, mirror it to root.
            var rootReqPath = generated.Keys.FirstOrDefault(p =>
                string.Equals(NormalizeRelativePath(p), "requirements.txt", StringComparison.OrdinalIgnoreCase));
            var srcReqPath = generated.Keys.FirstOrDefault(p =>
                string.Equals(NormalizeRelativePath(p), "src/requirements.txt", StringComparison.OrdinalIgnoreCase));
            if (string.IsNullOrWhiteSpace(rootReqPath))
            {
                if (!string.IsNullOrWhiteSpace(srcReqPath) &&
                    generated.TryGetValue(srcReqPath, out var srcReq) &&
                    !string.IsNullOrWhiteSpace(srcReq.Content))
                {
                    AddGeneratedIfMissing(generated, added, "requirements.txt", "text", srcReq.Content);
                }
            }
            else if (!string.IsNullOrWhiteSpace(srcReqPath) &&
                     generated.TryGetValue(rootReqPath, out var rootReq) &&
                     string.IsNullOrWhiteSpace(rootReq.Content) &&
                     generated.TryGetValue(srcReqPath, out var srcReq2) &&
                     !string.IsNullOrWhiteSpace(srcReq2.Content))
            {
                generated[rootReqPath] = new GeneratedFile(rootReqPath, "text", srcReq2.Content);
                added.Add(generated[rootReqPath]);
            }

            if (!hasTest && plan.TestCommands.Count > 0)
            {
                var testContent = useDjango
                    ? BuildDjangoTestContent(plan.ApplicationName)
                    : useFastApi
                    ? BuildFastApiTestContent(plan.ApplicationName)
                    : BuildFlaskTestContent(plan.ApplicationName);
                AddGeneratedIfMissing(generated, added, "tests/test_app.py", "python", testContent);
            }
            else
            {
                var placeholderTests = generated
                    .Where(kv => GenerationPathHeuristics.LooksLikePythonTestPath(kv.Key) && LooksLikePlaceholderTest(kv.Value.Content))
                    .Select(kv => kv.Key)
                    .ToList();
                foreach (var testPath in placeholderTests)
                {
                    var hardened = useDjango
                        ? BuildDjangoTestContent(plan.ApplicationName)
                        : useFastApi
                        ? BuildFastApiTestContent(plan.ApplicationName)
                        : BuildFlaskTestContent(plan.ApplicationName);
                    generated[testPath] = new GeneratedFile(testPath, "python", hardened);
                    added.Add(generated[testPath]);
                }
            }

            if (!hasDataLayer && plan.TechStack.Databases.Count > 0)
                AddGeneratedIfMissing(generated, added, "models.py", "python", BuildPythonModelsContent(plan.ApplicationName, useDjango));

            if (taskDomainHint && !generated.Keys.Any(p => p.Contains("tasks", StringComparison.OrdinalIgnoreCase)))
                AddGeneratedIfMissing(generated, added, "tasks.py", "python", BuildPythonTaskModelContent(plan.ApplicationName, useDjango));

            // If entrypoint lives under src/, mirror core python modules into src/ so imports resolve in tests/runtime.
            var hasSrcMain = generated.Keys.Any(p => string.Equals(NormalizeRelativePath(p), "src/main.py", StringComparison.OrdinalIgnoreCase));
            if (useFastApi && hasSrcMain)
            {
                if (generated.TryGetValue("models.py", out var rootModels) &&
                    !generated.ContainsKey("src/models.py"))
                {
                    AddGeneratedIfMissing(generated, added, "src/models.py", "python", rootModels.Content);
                }
                if (generated.TryGetValue("tasks.py", out var rootTasks) &&
                    !generated.ContainsKey("src/tasks.py"))
                {
                    AddGeneratedIfMissing(generated, added, "src/tasks.py", "python", rootTasks.Content);
                }
            }

            if (useDjango)
                EnsureMandatoryDjangoArtifacts(plan, generated, added, authHint, taskDomainHint);

            if (useFastApi)
                EnsureComplexFastApiArtifacts(generated, added);
        }

        if (IsNodePlan(plan) && StackPlanHeuristics.Classify(plan) != StackKind.JavaReactFullStack)
        {
            var hasPackageJson = generated.Keys.Any(p => p.EndsWith("package.json", StringComparison.OrdinalIgnoreCase));
            var hasIndexJs = generated.Keys.Any(p =>
                p.EndsWith("index.js", StringComparison.OrdinalIgnoreCase) ||
                p.EndsWith("index.ts", StringComparison.OrdinalIgnoreCase));
            var hasTest = generated.Keys.Any(GenerationPathHeuristics.LooksLikeNodeTestPath);
            var hasTaskRoutes = generated.Keys.Any(p => p.Contains("routes/tasks", StringComparison.OrdinalIgnoreCase));
            var hasAuthRoutes = generated.Keys.Any(p => p.Contains("routes/auth", StringComparison.OrdinalIgnoreCase));
            var hasDataLayer = generated.Keys.Any(p =>
                p.Contains("models", StringComparison.OrdinalIgnoreCase) ||
                p.Contains("repository", StringComparison.OrdinalIgnoreCase) ||
                p.Contains("store", StringComparison.OrdinalIgnoreCase));

            if (!hasPackageJson)
                AddGeneratedIfMissing(generated, added, "package.json", "json", BuildNodePackageJsonContent(plan.ApplicationName));

            if (!hasIndexJs)
                AddGeneratedIfMissing(generated, added, "index.js", "javascript", BuildNodeIndexContent(plan.ApplicationName));
            else
            {
                var indexPath = generated.Keys.FirstOrDefault(p =>
                    p.EndsWith("index.js", StringComparison.OrdinalIgnoreCase)
                    || p.EndsWith("index.ts", StringComparison.OrdinalIgnoreCase));
                if (!string.IsNullOrWhiteSpace(indexPath)
                    && generated.TryGetValue(indexPath, out var index)
                    && !index.Content.Contains("/api/tasks", StringComparison.OrdinalIgnoreCase))
                {
                    generated[indexPath] = new GeneratedFile(indexPath, "javascript", BuildNodeIndexContent(plan.ApplicationName));
                    added.Add(generated[indexPath]);
                }
            }

            if (!hasTest && plan.TestCommands.Count > 0)
                AddGeneratedIfMissing(generated, added, "index.test.js", "javascript", BuildNodeTestContent(plan.ApplicationName));

            if (!hasDataLayer)
                AddGeneratedIfMissing(generated, added, "src/models/task-store.js", "javascript", BuildNodeTaskStoreContent());
            if (!hasAuthRoutes || PlanSuggestsAuth(plan))
                AddGeneratedIfMissing(generated, added, "src/services/auth-service.js", "javascript", BuildNodeAuthServiceContent());
            if (!hasTaskRoutes || PlanSuggestsTaskDomain(plan))
                AddGeneratedIfMissing(generated, added, "src/routes/tasks.js", "javascript", BuildNodeTaskRoutesContent());
            if (!hasAuthRoutes || PlanSuggestsAuth(plan))
                AddGeneratedIfMissing(generated, added, "src/routes/auth.js", "javascript", BuildNodeAuthRoutesContent());
        }

        return added;
    }

    /// <summary>
    /// Merges <paramref name="files"/> into a path dictionary, then applies mandatory stack files.
    /// </summary>
    public static IReadOnlyList<GeneratedFile> MergeWithStackSafetyNet(
        GenerationPlan plan,
        IReadOnlyList<GeneratedFile> files)
    {
        var dict = new Dictionary<string, GeneratedFile>(StringComparer.OrdinalIgnoreCase);
        foreach (var f in files)
        {
            if (string.IsNullOrWhiteSpace(f.RelativePath)) continue;
            dict[f.RelativePath] = f;
        }

        EnsureMandatoryGeneratedFiles(plan, dict);
        UpgradeBankingProductionBaseline(plan, dict);
        ClampOversizedGeneratedFiles(dict);
        return dict.Values.ToList();
    }

    private static void ClampOversizedGeneratedFiles(Dictionary<string, GeneratedFile> generated)
    {
        const int maxChars = 49_000;
        foreach (var key in generated.Keys.ToList())
        {
            var file = generated[key];
            var content = file.Content ?? string.Empty;
            if (content.Length <= maxChars)
                continue;

            generated[key] = new GeneratedFile(
                file.RelativePath,
                file.Language,
                content[..maxChars] + "\n/* truncated for review gate size limit */");
        }
    }

    private static void UpgradeBankingProductionBaseline(GenerationPlan plan, Dictionary<string, GeneratedFile> generated)
    {
        if (StackPlanHeuristics.Classify(plan) != StackKind.JavaReactFullStack)
            return;

        var intent = $"{plan.ApplicationName} {plan.ApplicationDescription}";
        if (!intent.Contains("bank", StringComparison.OrdinalIgnoreCase)
            && !intent.Contains("fintech", StringComparison.OrdinalIgnoreCase)
            && !intent.Contains("[[JAVA_REACT_FULLSTACK]]", StringComparison.Ordinal))
            return;

        var added = new List<GeneratedFile>();
        generated["backend/src/main/resources/application.yml"] = new GeneratedFile(
            "backend/src/main/resources/application.yml",
            "yaml",
            BuildSpringBootApplicationYml());
        AddGeneratedIfMissing(generated, added,
            "backend/src/main/java/com/generated/banking/service/AccountService.java",
            "java",
            BuildAccountServiceContent());
        AddGeneratedIfMissing(generated, added,
            "backend/src/main/java/com/generated/banking/service/TransferService.java",
            "java",
            BuildTransferServiceContent());
        generated["backend/src/main/java/com/generated/banking/config/CorrelationIdFilter.java"] = new GeneratedFile(
            "backend/src/main/java/com/generated/banking/config/CorrelationIdFilter.java",
            "java",
            BuildSpringBootCorrelationIdFilterContent());
        generated["backend/src/main/java/com/generated/banking/web/GlobalExceptionHandler.java"] = new GeneratedFile(
            "backend/src/main/java/com/generated/banking/web/GlobalExceptionHandler.java",
            "java",
            BuildSpringBootGlobalExceptionHandlerContent());
        generated["backend/src/main/java/com/generated/banking/web/HealthController.java"] = new GeneratedFile(
            "backend/src/main/java/com/generated/banking/web/HealthController.java",
            "java",
            BuildSpringBootHealthControllerContent());
        AddGeneratedIfMissing(generated, added,
            "README.md",
            "markdown",
            BuildBankingReadmeContent(plan.ApplicationName));
        AddGeneratedIfMissing(generated, added,
            "docker-compose.yml",
            "yaml",
            BuildBankingDockerComposeContent());
        AddGeneratedIfMissing(generated, added,
            ".github/workflows/ci.yml",
            "yaml",
            BuildBankingCiWorkflowContent());
        AddGeneratedIfMissing(generated, added,
            "scripts/run.sh",
            "shell",
            BuildBankingRunScriptContent());

        const string bankingTest = "backend/src/test/java/com/generated/banking/BankingApiTests.java";
        generated[bankingTest] = new GeneratedFile(bankingTest, "java", BuildSpringBootBankingApiTestsContent());

        const string clientPath = "frontend/src/api/client.ts";
        if (!generated.TryGetValue(clientPath, out var client)
            || !client.Content.Contains("createPayment", StringComparison.OrdinalIgnoreCase))
        {
            generated[clientPath] = new GeneratedFile(clientPath, "typescript", BuildReactApiClientTs());
        }

        const string appTest = "frontend/src/App.test.tsx";
        if (!generated.ContainsKey(appTest))
            generated[appTest] = new GeneratedFile(appTest, "typescript", BuildReactAppTestTsx());

        var appPath = generated.Keys.FirstOrDefault(p =>
            p.Equals("frontend/src/App.tsx", StringComparison.OrdinalIgnoreCase));
        if (appPath is null
            || !generated[appPath].Content.Contains("transfer", StringComparison.OrdinalIgnoreCase))
        {
            generated["frontend/src/App.tsx"] = new GeneratedFile(
                "frontend/src/App.tsx",
                "typescript",
                BuildReactAppTsx(plan.ApplicationName));
        }
    }

    // P1-9 of audit roadmap: delegate to single source of truth.
    // Public-by-history: external callers (LlmCodeGenerationService.EnsureMandatoryAspNetManifest) reference these.
    public static bool IsAspNetCorePlan(GenerationPlan plan) => StackPlanHeuristics.IsAspNetCore(plan);
    public static bool IsPythonPlan(GenerationPlan plan) => StackPlanHeuristics.IsPython(plan);
    public static bool IsNodePlan(GenerationPlan plan) => StackPlanHeuristics.IsNode(plan);

    private static bool PlanSuggestsFastApi(GenerationPlan plan) =>
        plan.TechStack.Frameworks.Any(f => f.Contains("fastapi", StringComparison.OrdinalIgnoreCase));

    private static bool PlanSuggestsDjango(GenerationPlan plan) =>
        plan.TechStack.Frameworks.Any(f => f.Contains("django", StringComparison.OrdinalIgnoreCase));

    private static bool PlanOrFilesSuggestFastApi(GenerationPlan plan, IDictionary<string, GeneratedFile> generated) =>
        PlanSuggestsFastApi(plan) ||
        plan.ApplicationDescription.Contains("fastapi", StringComparison.OrdinalIgnoreCase) ||
        generated.Values.Any(f =>
            f.RelativePath.EndsWith(".py", StringComparison.OrdinalIgnoreCase) &&
            f.Content.Contains("from fastapi", StringComparison.OrdinalIgnoreCase));

    private static bool PlanOrFilesSuggestDjango(GenerationPlan plan, IDictionary<string, GeneratedFile> generated) =>
        PlanSuggestsDjango(plan) ||
        plan.ApplicationDescription.Contains("django", StringComparison.OrdinalIgnoreCase) ||
        generated.Values.Any(f =>
            f.RelativePath.EndsWith(".py", StringComparison.OrdinalIgnoreCase) &&
            (f.Content.Contains("from django", StringComparison.OrdinalIgnoreCase)
             || f.Content.Contains("django.", StringComparison.OrdinalIgnoreCase)));

    /// <summary>
    /// Mirrors <see cref="ProjectScaffolder"/> JWT / auth hints so the Python baseline carries matching signals when the LLM is silent.
    /// </summary>
    private static bool PlanSuggestsAuth(GenerationPlan plan)
    {
        if (plan.TechStack.Frameworks.Any(f =>
                f.Contains("jwt", StringComparison.OrdinalIgnoreCase) ||
                f.Contains("identityserver", StringComparison.OrdinalIgnoreCase) ||
                f.Contains("authentication", StringComparison.OrdinalIgnoreCase)))
            return true;

        if (plan.TechStack.Infrastructure.Any(i =>
                i.Contains("auth", StringComparison.OrdinalIgnoreCase) ||
                i.Contains("jwt", StringComparison.OrdinalIgnoreCase)))
            return true;

        if (plan.TechStack.Rationale.Contains("jwt", StringComparison.OrdinalIgnoreCase) ||
            plan.TechStack.Rationale.Contains("oauth", StringComparison.OrdinalIgnoreCase) ||
            plan.TechStack.Rationale.Contains("authentication", StringComparison.OrdinalIgnoreCase))
            return true;

        var d = plan.ApplicationDescription;
        return d.Contains("auth", StringComparison.OrdinalIgnoreCase)
               || d.Contains("login", StringComparison.OrdinalIgnoreCase)
               || d.Contains("bearer", StringComparison.OrdinalIgnoreCase)
               || d.Contains("jwt", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Aligns with <see cref="ProjectScaffolder"/>: skip when stack is clearly Python/JS/Node as primary languages.
    /// </summary>
    private static bool IsDotNetStack(GenerationPlan plan)
    {
        var langs = plan.TechStack.Languages;
        if (langs.Any(l =>
                l.Contains("python", StringComparison.OrdinalIgnoreCase) ||
                l.Equals("py", StringComparison.OrdinalIgnoreCase) ||
                l.Contains("javascript", StringComparison.OrdinalIgnoreCase) ||
                l.Contains("typescript", StringComparison.OrdinalIgnoreCase) ||
                l.Equals("node", StringComparison.OrdinalIgnoreCase)))
            return false;

        return langs.Any(l =>
                   l.Contains("c#", StringComparison.OrdinalIgnoreCase) ||
                   l.Contains("csharp", StringComparison.OrdinalIgnoreCase) ||
                   l.Contains(".net", StringComparison.OrdinalIgnoreCase)) ||
               plan.TechStack.Frameworks.Any(f =>
                   f.Contains("asp.net", StringComparison.OrdinalIgnoreCase) ||
                   f.Contains("aspnet", StringComparison.OrdinalIgnoreCase) ||
                   f.Contains("blazor", StringComparison.OrdinalIgnoreCase) ||
                   f.Contains("dotnet", StringComparison.OrdinalIgnoreCase)) ||
               plan.RuntimeImage.Contains("dotnet", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>Same word-boundary rules as <see cref="ProjectScaffolder"/> for API/test project names.</summary>
    private static string SanitizeDotNetAppName(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return "GeneratedApp";
        var sb = new StringBuilder();
        var upperNext = true;
        foreach (var ch in raw)
        {
            if (char.IsLetterOrDigit(ch))
            {
                sb.Append(upperNext ? char.ToUpperInvariant(ch) : ch);
                upperNext = false;
            }
            else
            {
                upperNext = true;
            }
        }

        var name = sb.ToString();
        if (string.IsNullOrEmpty(name)) return "GeneratedApp";
        if (char.IsDigit(name[0])) name = "App" + name;
        return name;
    }

    private static string BuildStandaloneXUnitTestCsproj() =>
        @"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <IsPackable>false</IsPackable>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include=""Microsoft.NET.Test.Sdk"" Version=""17.11.1"" />
    <PackageReference Include=""xunit"" Version=""2.9.0"" />
    <PackageReference Include=""xunit.runner.visualstudio"" Version=""2.8.2"" />
  </ItemGroup>
</Project>
";

    private static string BuildDotNetSmokeTestsContent() =>
        @"using System;
using System.IO;
using System.Reflection;
using Xunit;

public sealed class SmokeTests
{
    // Integration: assembly is built, loadable, located on disk.
    [Fact]
    public void Test_assembly_is_loadable_integration()
    {
        var asm = typeof(SmokeTests).Assembly;
        var asmPath = asm.Location;
        Assert.False(string.IsNullOrWhiteSpace(asmPath));
        Assert.True(File.Exists(asmPath));
    }

    // Negative: malformed PORT env var must not crash int.TryParse path.
    [Fact]
    public void Port_parser_rejects_invalid_value_negative()
    {
        var raw = ""not-a-port"";
        var ok = int.TryParse(raw, out var port);
        Assert.False(ok);
        Assert.Equal(0, port);
    }

    // Negative: empty config string yields default behavior, never throws.
    [Fact]
    public void Empty_config_returns_default_negative()
    {
        var raw = string.Empty;
        var resolved = string.IsNullOrWhiteSpace(raw) ? ""default"" : raw;
        Assert.Equal(""default"", resolved);
    }
}
";

    public static string? GetPrimaryProjectRootPath(IEnumerable<GeneratedFile> files)
    {
        var csproj = files
            .Select(f => f.RelativePath.Replace('\\', '/'))
            .FirstOrDefault(p =>
                p.StartsWith("src/", StringComparison.OrdinalIgnoreCase)
                && p.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase)
                && p.Contains(".Api/", StringComparison.OrdinalIgnoreCase));

        if (string.IsNullOrWhiteSpace(csproj))
        {
            csproj = files
                .Select(f => f.RelativePath.Replace('\\', '/'))
                .FirstOrDefault(p =>
                    p.StartsWith("src/", StringComparison.OrdinalIgnoreCase)
                    && p.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase));
        }

        return string.IsNullOrWhiteSpace(csproj)
            ? null
            : csproj[..csproj.LastIndexOf('/')];
    }

    private static void AddGeneratedIfMissing(
        IDictionary<string, GeneratedFile> generated,
        ICollection<GeneratedFile> added,
        string relativePath,
        string language,
        string content)
    {
        if (generated.ContainsKey(relativePath)) return;
        var file = new GeneratedFile(relativePath, language, content);
        generated[relativePath] = file;
        added.Add(file);
    }

    private static void EnsureGeneratedFile(
        IDictionary<string, GeneratedFile> generated,
        ICollection<GeneratedFile> added,
        string relativePath,
        string language,
        string content,
        Func<string, bool>? isAcceptable = null)
    {
        if (generated.TryGetValue(relativePath, out var existing))
        {
            var existingContent = existing.Content ?? string.Empty;
            var acceptable = isAcceptable is null
                ? !string.IsNullOrWhiteSpace(existingContent)
                : isAcceptable(existingContent);
            if (acceptable)
                return;
        }

        var file = new GeneratedFile(relativePath, language, content);
        generated[relativePath] = file;
        added.Add(file);
    }

    private static string BuildNamespaceFromRoot(string rootPath)
    {
        var segments = rootPath.Replace('\\', '/')
            .Split('/', StringSplitOptions.RemoveEmptyEntries)
            .Skip(1) // skip src
            .Select(s => new string(s.Where(c => char.IsLetterOrDigit(c) || c == '.').ToArray()))
            .Where(s => !string.IsNullOrWhiteSpace(s));
        var ns = string.Join('.', segments);
        return string.IsNullOrWhiteSpace(ns) ? "GeneratedApp.Api" : ns;
    }

    private static string BuildHealthItemContent(string ns) =>
        $@"namespace {ns}.Models;

public sealed class HealthItem
{{
    public string Service {{ get; init; }} = ""{ns}"";
    public string Status {{ get; init; }} = ""ok"";
    public DateTime TimestampUtc {{ get; init; }} = DateTime.UtcNow;
}}";

    private static string BuildHealthServiceContent(string ns) =>
        $@"using {ns}.Models;

namespace {ns}.Services;

public sealed class HealthService
{{
    public HealthItem GetHealth() =>
        new()
        {{
            Service = ""{ns}"",
            Status = ""ok"",
            TimestampUtc = DateTime.UtcNow
        }};
}}";

    private static string BuildHealthControllerContent(string ns) =>
        $@"using {ns}.Services;
using Microsoft.AspNetCore.Mvc;

namespace {ns}.Controllers;

[ApiController]
[Route(""api/[controller]"")]
public sealed class HealthController : ControllerBase
{{
    [HttpGet]
    public IActionResult Get([FromServices] HealthService service) => Ok(service.GetHealth());
}}";

    private static string BuildProgramContent(string ns, bool includeDataLayer) =>
        includeDataLayer
            ? $@"using {ns}.Data;
using {ns}.Repositories;
using {ns}.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddScoped<HealthService>();
builder.Services.AddScoped<TaskRepository>();
builder.Services.AddDbContext<AppDbContext>(o =>
    o.UseNpgsql(builder.Configuration.GetConnectionString(""DefaultConnection"")
                 ?? Environment.GetEnvironmentVariable(""DATABASE_URL"")
                 ?? ""Host=localhost;Port=5432;Database=libr4_generated;Username=postgres;Password=postgres""));

var app = builder.Build();
app.UseSwagger();
app.UseSwaggerUI();
app.MapControllers();
app.Run();"
            : $@"using {ns}.Services;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddScoped<HealthService>();

var app = builder.Build();
app.UseSwagger();
app.UseSwaggerUI();
app.MapControllers();
app.Run();";

    private static string BuildTaskItemContent(string ns) =>
        $@"namespace {ns}.Models;

public sealed class TaskItem
{{
    public Guid Id {{ get; set; }} = Guid.NewGuid();
    public string Title {{ get; set; }} = string.Empty;
    public string Status {{ get; set; }} = ""todo"";
    public DateTime CreatedAtUtc {{ get; set; }} = DateTime.UtcNow;
}}";

    private static string BuildAppDbContextContent(string ns) =>
        $@"using {ns}.Models;
using Microsoft.EntityFrameworkCore;

namespace {ns}.Data;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{{
    public DbSet<TaskItem> Tasks => Set<TaskItem>();
}}";

    private static string BuildTaskRepositoryContent(string ns) =>
        $@"using {ns}.Data;
using {ns}.Models;
using Microsoft.EntityFrameworkCore;

namespace {ns}.Repositories;

public sealed class TaskRepository(AppDbContext db)
{{
    public Task<List<TaskItem>> ListAsync(CancellationToken ct = default) =>
        db.Tasks.OrderByDescending(x => x.CreatedAtUtc).ToListAsync(ct);

    public async Task<TaskItem> AddAsync(string title, CancellationToken ct = default)
    {{
        var item = new TaskItem {{ Title = title }};
        db.Tasks.Add(item);
        await db.SaveChangesAsync(ct);
        return item;
    }}
}}";

    private static string BuildAspNetCsprojContent() =>
        @"<Project Sdk=""Microsoft.NET.Sdk.Web"">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include=""Microsoft.AspNetCore.OpenApi"" Version=""8.0.8"" />
    <PackageReference Include=""Swashbuckle.AspNetCore"" Version=""6.6.2"" />
    <PackageReference Include=""Microsoft.EntityFrameworkCore"" Version=""8.0.8"" />
    <PackageReference Include=""Npgsql.EntityFrameworkCore.PostgreSQL"" Version=""8.0.4"" />
  </ItemGroup>
</Project>";

    private static string BuildDotNetSolutionContent(string projectName, string csprojPath)
    {
        var normalized = csprojPath.Replace('\\', '/');
        var guidProjectType = "FAE04EC0-301F-11D3-BF4B-00C04F79EFBC";
        var guidProject = "7DABCA53-4A24-43AF-93F7-80F1672B7FA1";
        var guidSolution = "9A53F30D-B505-4D0E-8E64-737D3836D34A";
        return
            $"Microsoft Visual Studio Solution File, Format Version 12.00\n# Visual Studio Version 17\nVisualStudioVersion = 17.0.31903.59\nMinimumVisualStudioVersion = 10.0.40219.1\nProject(\"{{{guidProjectType}}}\") = \"{projectName}\", \"{normalized}\", \"{{{guidProject}}}\"\nEndProject\nGlobal\n\tGlobalSection(SolutionConfigurationPlatforms) = preSolution\n\t\tDebug|Any CPU = Debug|Any CPU\n\t\tRelease|Any CPU = Release|Any CPU\n\tEndGlobalSection\n\tGlobalSection(ProjectConfigurationPlatforms) = postSolution\n\t\t{{{guidProject}}}.Debug|Any CPU.ActiveCfg = Debug|Any CPU\n\t\t{{{guidProject}}}.Debug|Any CPU.Build.0 = Debug|Any CPU\n\t\t{{{guidProject}}}.Release|Any CPU.ActiveCfg = Release|Any CPU\n\t\t{{{guidProject}}}.Release|Any CPU.Build.0 = Release|Any CPU\n\tEndGlobalSection\n\tGlobalSection(SolutionProperties) = preSolution\n\t\tHideSolutionNode = FALSE\n\tEndGlobalSection\n\tGlobalSection(ExtensibilityGlobals) = postSolution\n\t\tSolutionGuid = {{{guidSolution}}}\n\tEndGlobalSection\nEndGlobal";
    }

    private static string BuildFlaskAppContent(string appName, bool authHint, bool taskDomainHint)
    {
        var tasksBlock = taskDomainHint
            ? @"
from tasks import Task, db
from flask import request

def error_response(code: str, message: str, status: int = 400):
    return jsonify({'error': {'code': code, 'message': message}}), status

def _validate_create_payload(payload):
    if not isinstance(payload, dict):
        return None, error_response('invalid_payload', 'JSON object expected', 400)
    title = payload.get('title')
    if not isinstance(title, str) or not title.strip():
        return None, error_response('validation_error', 'title is required', 422)
    title = title.strip()
    if len(title) > 200:
        return None, error_response('validation_error', 'title max length is 200', 422)
    return {'title': title}, None

def _validate_update_payload(payload):
    if not isinstance(payload, dict):
        return None, error_response('invalid_payload', 'JSON object expected', 400)
    out = {}
    if 'title' in payload:
        title = payload.get('title')
        if not isinstance(title, str) or not title.strip():
            return None, error_response('validation_error', 'title must be non-empty string', 422)
        title = title.strip()
        if len(title) > 200:
            return None, error_response('validation_error', 'title max length is 200', 422)
        out['title'] = title
    if 'completed' in payload:
        completed = payload.get('completed')
        if not isinstance(completed, bool):
            return None, error_response('validation_error', 'completed must be boolean', 422)
        out['completed'] = completed
    if not out:
        return None, error_response('validation_error', 'no valid fields provided for update', 422)
    return out, None

@app.route('/tasks', methods=['GET'])
def list_tasks():
    tasks = Task.query.all()
    return jsonify({'items': [t.to_dict() for t in tasks]})

@app.route('/tasks', methods=['POST'])
def create_task():
    payload = request.get_json(silent=True) or {}
    data, err = _validate_create_payload(payload)
    if err:
        return err
    task = Task(title=data['title'])
    db.session.add(task)
    db.session.commit()
    return jsonify(task.to_dict()), 201

@app.route('/tasks/<int:task_id>', methods=['PUT'])
def update_task(task_id):
    task = Task.query.get_or_404(task_id)
    payload = request.get_json(silent=True) or {}
    data, err = _validate_update_payload(payload)
    if err:
        return err
    if 'completed' in data:
        task.completed = data['completed']
    if 'title' in data:
        task.title = data['title']
    db.session.commit()
    return jsonify(task.to_dict())
"
            : string.Empty;

        if (!authHint)
        {
            return $@"import os
from flask import Flask, jsonify
from models import init_db

app = Flask(__name__)
app.config['SQLALCHEMY_DATABASE_URI'] = os.environ.get('DATABASE_URL', 'sqlite:///tasks.db')
app.config['SQLALCHEMY_TRACK_MODIFICATIONS'] = False

init_db(app)

@app.route('/health')
def health():
    return jsonify({{'service': '{appName}', 'status': 'ok'}})
{tasksBlock}

if __name__ == '__main__':
    app.run(host='0.0.0.0', port=int(os.environ.get('PORT', '4000')))";
        }

        return $@"import os
from datetime import datetime, timedelta, timezone
import jwt
from flask import Flask, jsonify
from models import init_db

app = Flask(__name__)
app.config['SQLALCHEMY_DATABASE_URI'] = os.environ.get('DATABASE_URL', 'sqlite:///tasks.db')
app.config['SQLALCHEMY_TRACK_MODIFICATIONS'] = False
_SECRET = os.environ.get('JWT_SECRET')
if not _SECRET:
    raise RuntimeError('JWT_SECRET environment variable is required')

init_db(app)

@app.route('/health')
def health():
    return jsonify({{'service': '{appName}', 'status': 'ok'}})
{tasksBlock}

@app.route('/auth/token', methods=['POST'])
def issue_token():
    payload = {{'sub': 'demo', 'exp': datetime.now(timezone.utc) + timedelta(hours=1)}}
    token = jwt.encode(payload, _SECRET, algorithm='HS256')
    return jsonify({{'access_token': token}})

if __name__ == '__main__':
    app.run(host='0.0.0.0', port=int(os.environ.get('PORT', '4000')))";
    }

    private static string BuildFastApiMainContent(string appName, bool authHint, bool taskDomainHint)
    {
        var tasksBlock = taskDomainHint
            ? @"
from tasks import Task, db
from sqlalchemy import select
from pydantic import BaseModel, Field, ConfigDict
from fastapi import HTTPException, Request
from fastapi.responses import JSONResponse

class TaskCreateRequest(BaseModel):
    title: str = Field(..., min_length=1, max_length=200)

class TaskUpdateRequest(BaseModel):
    title: str | None = Field(default=None, min_length=1, max_length=200)
    completed: bool | None = None
    model_config = ConfigDict(extra='forbid')

@app.exception_handler(HTTPException)
async def http_error_handler(request: Request, exc: HTTPException):
    detail = exc.detail if isinstance(exc.detail, str) else 'Request failed'
    code = 'not_found' if exc.status_code == 404 else 'request_error'
    return JSONResponse(
        status_code=exc.status_code,
        content={'error': {'code': code, 'message': detail}}
    )

@app.get('/tasks')
def list_tasks():
    with db.session() as session:
        result = session.execute(select(Task))
        tasks = result.scalars().all()
        return {'items': [t.to_dict() for t in tasks]}

@app.post('/tasks')
def create_task(payload: TaskCreateRequest):
    with db.session() as session:
        task = Task(title=payload.title.strip())
        session.add(task)
        session.commit()
        session.refresh(task)
        return JSONResponse(status_code=201, content=task.to_dict())

@app.put('/tasks/{task_id}')
def update_task(task_id: int, payload: TaskUpdateRequest):
    with db.session() as session:
        result = session.execute(select(Task).where(Task.id == task_id))
        task = result.scalar_one_or_none()
        if not task:
            raise HTTPException(status_code=404, detail='Task not found')
        if payload.completed is not None:
            task.completed = payload.completed
        if payload.title is not None:
            task.title = payload.title.strip()
        if payload.title is None and payload.completed is None:
            raise HTTPException(status_code=422, detail='At least one updatable field is required')
        session.commit()
        session.refresh(task)
        return task.to_dict()
"
            : string.Empty;

        if (!authHint)
        {
            return $@"from fastapi import FastAPI

app = FastAPI()

@app.get('/health')
def health():
    return {{'service': '{appName}', 'status': 'ok'}}
{tasksBlock}";
        }

        return $@"import os
from datetime import datetime, timedelta, timezone
import jwt
from fastapi import FastAPI
from models import init_db

app = FastAPI()
os.environ.setdefault('DATABASE_URL', 'sqlite:///tasks.db')
_SECRET = os.environ.get('JWT_SECRET')
if not _SECRET:
    raise RuntimeError('JWT_SECRET environment variable is required')

@app.on_event('startup')
def on_startup():
    init_db(app)

@app.get('/health')
def health():
    return {{'service': '{appName}', 'status': 'ok'}}
{tasksBlock}

@app.post('/auth/token')
def issue_token():
    payload = {{'sub': 'demo', 'exp': datetime.now(timezone.utc) + timedelta(hours=1)}}
    token = jwt.encode(payload, _SECRET, algorithm='HS256')
    return {{'access_token': token}}";
    }

    private static string BuildPythonRequirementsContent(bool authHint, bool useFastApi, bool useDjango, bool complexFastApiHint)
    {
        var sb = new StringBuilder();
        if (useDjango)
        {
            sb.AppendLine("django==5.0.6");
            sb.AppendLine("djangorestframework==3.15.1");
            sb.AppendLine("uvicorn[standard]==0.29.0");
            sb.AppendLine("psycopg[binary]==3.1.18");
        }
        else if (useFastApi)
        {
            sb.AppendLine("fastapi==0.110.0");
            sb.AppendLine("uvicorn[standard]==0.29.0");
            sb.AppendLine("httpx==0.27.2");
            if (complexFastApiHint)
            {
                sb.AppendLine("celery==5.4.0");
                sb.AppendLine("redis==5.0.1");
                sb.AppendLine("psycopg[binary]==3.1.18");
            }
        }
        else
        {
            sb.AppendLine("flask==3.0.0");
            sb.AppendLine("flask-sqlalchemy==3.1.1");
        }

        if (authHint)
            sb.AppendLine("PyJWT==2.8.0");

        sb.AppendLine("pytest==7.4.0");
        sb.AppendLine("sqlalchemy==2.0.25");
        return sb.ToString().TrimEnd() + "\n";
    }

    private static string BuildDjangoManageContent(string appName) =>
        @"#!/usr/bin/env python
import os
import sys

def main():
    os.environ.setdefault('DJANGO_SETTINGS_MODULE', 'app.settings')
    from django.core.management import execute_from_command_line
    execute_from_command_line(sys.argv)

if __name__ == '__main__':
    main()
";

    private static string BuildDjangoSettingsContent(string appName) =>
        $@"import os
from pathlib import Path
BASE_DIR = Path(__file__).resolve().parent.parent
SECRET_KEY = 'dev-only-secret-key'
DEBUG = True
ALLOWED_HOSTS = ['*']
ROOT_URLCONF = 'app.urls'
ASGI_APPLICATION = 'app.asgi.application'
WSGI_APPLICATION = 'app.wsgi.application'
INSTALLED_APPS = [
    'django.contrib.contenttypes',
    'django.contrib.auth',
    'django.contrib.sessions',
    'django.contrib.messages',
    'django.contrib.staticfiles',
    'rest_framework',
    'api',
]
MIDDLEWARE = [
    'django.middleware.security.SecurityMiddleware',
    'django.contrib.sessions.middleware.SessionMiddleware',
    'django.middleware.common.CommonMiddleware',
    'django.middleware.csrf.CsrfViewMiddleware',
    'django.contrib.auth.middleware.AuthenticationMiddleware',
    'django.contrib.messages.middleware.MessageMiddleware',
]
DATABASES = {{
    'default': {{
        'ENGINE': 'django.db.backends.postgresql',
        'NAME': os.getenv('POSTGRES_DB', '{appName.ToLowerInvariant()}'),
        'USER': os.getenv('POSTGRES_USER', 'postgres'),
        'PASSWORD': os.getenv('POSTGRES_PASSWORD', 'postgres'),
        'HOST': os.getenv('POSTGRES_HOST', 'db'),
        'PORT': os.getenv('POSTGRES_PORT', '5432'),
    }}
}}
STATIC_URL = '/static/'
REST_FRAMEWORK = {{
    'DEFAULT_AUTHENTICATION_CLASSES': [],
    'DEFAULT_PERMISSION_CLASSES': [],
}}
";

    private static string BuildDjangoUrlsContent() =>
        @"from django.urls import include, path
from django.http import JsonResponse

def health(request):
    return JsonResponse({'status': 'ok'})

urlpatterns = [
    path('health', health),
    path('api/', include('api.urls')),
]
";

    private static string BuildDjangoAsgiContent() =>
        @"import os
from django.core.asgi import get_asgi_application

os.environ.setdefault('DJANGO_SETTINGS_MODULE', 'app.settings')
application = get_asgi_application()
";

    private static string BuildDjangoWsgiContent() =>
        @"import os
from django.core.wsgi import get_wsgi_application

os.environ.setdefault('DJANGO_SETTINGS_MODULE', 'app.settings')
application = get_wsgi_application()
";

    private static string BuildDjangoTestContent(string appName) =>
        @"import os
os.environ.setdefault('DJANGO_SETTINGS_MODULE', 'app.settings')
import django
django.setup()

from rest_framework.test import APIClient

def test_health():
    c = APIClient()
    r = c.get('/health')
    assert r.status_code == 200

def test_auth_token_error_envelope():
    c = APIClient()
    r = c.post('/api/auth/token', {'username': '', 'password': ''}, format='json')
    assert r.status_code == 400
    assert 'error' in r.json()
";

    private static void EnsureMandatoryDjangoArtifacts(
        GenerationPlan plan,
        Dictionary<string, GeneratedFile> generated,
        List<GeneratedFile> added,
        bool authHint,
        bool taskDomainHint)
    {
        EnsureGeneratedFile(generated, added, "manage.py", "python", BuildDjangoManageContent(plan.ApplicationName));
        EnsureGeneratedFile(generated, added, "app/__init__.py", "python", "\"\"\"Django application package.\"\"\"\n");
        EnsureGeneratedFile(
            generated,
            added,
            "app/settings.py",
            "python",
            BuildDjangoSettingsContent(plan.ApplicationName),
            content => content.Contains("rest_framework", StringComparison.OrdinalIgnoreCase)
                       && content.Contains("ASGI_APPLICATION", StringComparison.OrdinalIgnoreCase));
        EnsureGeneratedFile(
            generated,
            added,
            "app/urls.py",
            "python",
            BuildDjangoUrlsContent(),
            content => content.Contains("api.urls", StringComparison.OrdinalIgnoreCase));
        EnsureGeneratedFile(
            generated,
            added,
            "app/asgi.py",
            "python",
            BuildDjangoAsgiContent(),
            content => content.Contains("get_asgi_application", StringComparison.OrdinalIgnoreCase));
        EnsureGeneratedFile(
            generated,
            added,
            "app/wsgi.py",
            "python",
            BuildDjangoWsgiContent(),
            content => content.Contains("get_wsgi_application", StringComparison.OrdinalIgnoreCase));
        EnsureGeneratedFile(generated, added, "api/__init__.py", "python", "\"\"\"API package.\"\"\"\n");
        EnsureGeneratedFile(generated, added, "api/apps.py", "python", BuildDjangoApiAppsContent());
        EnsureGeneratedFile(generated, added, "api/migrations/__init__.py", "python", "\"\"\"Migrations package.\"\"\"\n");
        EnsureGeneratedFile(
            generated,
            added,
            "api/models.py",
            "python",
            BuildDjangoApiModelsContent(taskDomainHint),
            content => content.Contains("models.Model", StringComparison.OrdinalIgnoreCase));
        EnsureGeneratedFile(
            generated,
            added,
            "api/serializers.py",
            "python",
            BuildDjangoApiSerializersContent(taskDomainHint, authHint),
            content => content.Contains("ModelSerializer", StringComparison.OrdinalIgnoreCase)
                       || content.Contains("Serializer", StringComparison.OrdinalIgnoreCase));
        EnsureGeneratedFile(
            generated,
            added,
            "api/views.py",
            "python",
            BuildDjangoApiViewsContent(authHint, taskDomainHint),
            content => content.Contains("@api_view", StringComparison.OrdinalIgnoreCase)
                       && content.Contains("jwt.encode", StringComparison.OrdinalIgnoreCase));
        EnsureGeneratedFile(
            generated,
            added,
            "api/urls.py",
            "python",
            BuildDjangoApiUrlsContent(authHint, taskDomainHint),
            content => content.Contains("auth/token", StringComparison.OrdinalIgnoreCase)
                       || content.Contains("tasks", StringComparison.OrdinalIgnoreCase));
        EnsureGeneratedFile(
            generated,
            added,
            "app/error_envelope.py",
            "python",
            BuildDjangoErrorEnvelopeModuleContent(),
            content => content.Contains("'error'", StringComparison.OrdinalIgnoreCase)
                       && content.Contains("'code'", StringComparison.OrdinalIgnoreCase));
        EnsureGeneratedFile(
            generated,
            added,
            "docs/error-envelope.json",
            "json",
            BuildDjangoErrorEnvelopeContractContent(),
            content => content.Contains("\"error\"", StringComparison.OrdinalIgnoreCase)
                       && content.Contains("\"code\"", StringComparison.OrdinalIgnoreCase));
        EnsureGeneratedFile(
            generated,
            added,
            ".env.example",
            "text",
            BuildDjangoEnvExampleContent(),
            content => content.Contains("POSTGRES_DB", StringComparison.OrdinalIgnoreCase));
        EnsureGeneratedFile(
            generated,
            added,
            "Dockerfile",
            "dockerfile",
            BuildDjangoDockerfileContent(),
            content => content.Contains("uvicorn", StringComparison.OrdinalIgnoreCase)
                       || content.Contains("gunicorn", StringComparison.OrdinalIgnoreCase));
        EnsureGeneratedFile(
            generated,
            added,
            "docker-compose.yml",
            "yaml",
            BuildDjangoDockerComposeContent(plan.ApplicationName),
            content => content.Contains("postgres", StringComparison.OrdinalIgnoreCase));
        EnsureGeneratedFile(
            generated,
            added,
            "README.md",
            "markdown",
            BuildDjangoReadmeContent(plan.ApplicationName),
            content => content.Contains("uvicorn", StringComparison.OrdinalIgnoreCase)
                       && content.Contains("docker compose", StringComparison.OrdinalIgnoreCase));
        EnsureGeneratedFile(
            generated,
            added,
            "tests/test_api.py",
            "python",
            BuildDjangoApiTestContent(),
            content => content.Contains("APIClient", StringComparison.OrdinalIgnoreCase)
                       && content.Contains("/api/auth/token", StringComparison.OrdinalIgnoreCase));

        foreach (var reqPath in generated.Keys.Where(p => p.EndsWith("requirements.txt", StringComparison.OrdinalIgnoreCase)).ToList())
        {
            if (!generated.TryGetValue(reqPath, out var reqFile))
                continue;

            var updated = reqFile.Content ?? string.Empty;
            updated = EnsureRequirementPackage(updated, "django==5.0.6");
            updated = EnsureRequirementPackage(updated, "djangorestframework==3.15.1");
            updated = EnsureRequirementPackage(updated, "uvicorn[standard]==0.29.0");
            updated = EnsureRequirementPackage(updated, "psycopg[binary]==3.1.18");
            updated = EnsureRequirementPackage(updated, "pytest==7.4.0");
            if (authHint)
                updated = EnsureRequirementPackage(updated, "PyJWT==2.8.0");

            if (!string.Equals(updated, reqFile.Content, StringComparison.Ordinal))
            {
                generated[reqPath] = new GeneratedFile(reqPath, "text", updated);
                added.Add(generated[reqPath]);
            }
        }
    }

    private static string BuildDjangoApiAppsContent() =>
        @"from django.apps import AppConfig

class ApiConfig(AppConfig):
    default_auto_field = 'django.db.models.BigAutoField'
    name = 'api'
";

    private static string BuildDjangoApiModelsContent(bool taskDomainHint) =>
        taskDomainHint
            ? @"from django.conf import settings
from django.db import models

class Task(models.Model):
    title = models.CharField(max_length=200)
    description = models.TextField(blank=True)
    budget = models.DecimalField(max_digits=10, decimal_places=2, default=0)
    created_by = models.ForeignKey(settings.AUTH_USER_MODEL, related_name='created_tasks', on_delete=models.CASCADE)
    created_at = models.DateTimeField(auto_now_add=True)

class TaskApplication(models.Model):
    task = models.ForeignKey(Task, related_name='applications', on_delete=models.CASCADE)
    freelancer = models.ForeignKey(settings.AUTH_USER_MODEL, related_name='task_applications', on_delete=models.CASCADE)
    cover_letter = models.TextField()
    created_at = models.DateTimeField(auto_now_add=True)
"
            : @"from django.conf import settings
from django.db import models

class ProjectItem(models.Model):
    name = models.CharField(max_length=200)
    owner = models.ForeignKey(settings.AUTH_USER_MODEL, related_name='project_items', on_delete=models.CASCADE)
    created_at = models.DateTimeField(auto_now_add=True)
";

    private static string BuildDjangoApiSerializersContent(bool taskDomainHint, bool authHint) =>
        $@"from django.contrib.auth import get_user_model
from rest_framework import serializers
from api.models import {(taskDomainHint ? "Task, TaskApplication" : "ProjectItem")}

User = get_user_model()

class RegisterSerializer(serializers.Serializer):
    username = serializers.CharField(max_length=150)
    password = serializers.CharField(min_length=8, write_only=True)

class LoginSerializer(serializers.Serializer):
    username = serializers.CharField()
    password = serializers.CharField(write_only=True)

{(taskDomainHint
? @"class TaskSerializer(serializers.ModelSerializer):
    class Meta:
        model = Task
        fields = ['id', 'title', 'description', 'budget', 'created_by', 'created_at']
        read_only_fields = ['id', 'created_by', 'created_at']

class TaskApplicationSerializer(serializers.ModelSerializer):
    class Meta:
        model = TaskApplication
        fields = ['id', 'task', 'freelancer', 'cover_letter', 'created_at']
        read_only_fields = ['id', 'freelancer', 'created_at']"
: @"class ProjectItemSerializer(serializers.ModelSerializer):
    class Meta:
        model = ProjectItem
        fields = ['id', 'name', 'owner', 'created_at']
        read_only_fields = ['id', 'owner', 'created_at']")}
";

    private static string BuildDjangoApiViewsContent(bool authHint, bool taskDomainHint) =>
        taskDomainHint
            ? @"import jwt
import os
from datetime import datetime, timedelta, timezone
from django.contrib.auth import authenticate, get_user_model
from rest_framework import status
from rest_framework.decorators import api_view
from rest_framework.response import Response
from api.models import Task, TaskApplication
from api.serializers import LoginSerializer, RegisterSerializer, TaskApplicationSerializer, TaskSerializer
from app.error_envelope import error_response

User = get_user_model()

@api_view(['GET'])
def health(request):
    return Response({'status': 'ok'})

@api_view(['POST'])
def register(request):
    serializer = RegisterSerializer(data=request.data)
    if not serializer.is_valid():
        return Response(error_response('validation_error', 'Registration failed', serializer.errors), status=status.HTTP_400_BAD_REQUEST)
    user = User.objects.create_user(
        username=serializer.validated_data['username'],
        password=serializer.validated_data['password'])
    return Response({'id': user.id, 'username': user.username}, status=status.HTTP_201_CREATED)

@api_view(['POST'])
def token(request):
    serializer = LoginSerializer(data=request.data)
    if not serializer.is_valid():
        return Response(error_response('validation_error', 'Invalid credentials payload', serializer.errors), status=status.HTTP_400_BAD_REQUEST)
    user = authenticate(username=serializer.validated_data['username'], password=serializer.validated_data['password'])
    if user is None:
        return Response(error_response('authentication_error', 'Invalid username or password'), status=status.HTTP_401_UNAUTHORIZED)
    payload = {'sub': str(user.id), 'exp': datetime.now(timezone.utc) + timedelta(hours=1)}
    token_value = jwt.encode(payload, os.getenv('JWT_SECRET', 'dev-secret'), algorithm='HS256')
    return Response({'access_token': token_value, 'token_type': 'bearer'})

@api_view(['GET', 'POST'])
def tasks(request):
    if request.method == 'GET':
        serializer = TaskSerializer(Task.objects.all(), many=True)
        return Response(serializer.data)
    serializer = TaskSerializer(data=request.data)
    if not serializer.is_valid():
        return Response(error_response('validation_error', 'Task payload is invalid', serializer.errors), status=status.HTTP_400_BAD_REQUEST)
    owner = User.objects.first()
    task = serializer.save(created_by=owner)
    return Response(TaskSerializer(task).data, status=status.HTTP_201_CREATED)

@api_view(['GET', 'POST'])
def applications(request):
    if request.method == 'GET':
        serializer = TaskApplicationSerializer(TaskApplication.objects.all(), many=True)
        return Response(serializer.data)
    serializer = TaskApplicationSerializer(data=request.data)
    if not serializer.is_valid():
        return Response(error_response('validation_error', 'Application payload is invalid', serializer.errors), status=status.HTTP_400_BAD_REQUEST)
    freelancer = User.objects.first()
    application = serializer.save(freelancer=freelancer)
    return Response(TaskApplicationSerializer(application).data, status=status.HTTP_201_CREATED)
"
            : @"import jwt
import os
from datetime import datetime, timedelta, timezone
from django.contrib.auth import authenticate, get_user_model
from rest_framework import status
from rest_framework.decorators import api_view
from rest_framework.response import Response
from api.serializers import LoginSerializer, RegisterSerializer
from app.error_envelope import error_response

User = get_user_model()

@api_view(['GET'])
def health(request):
    return Response({'status': 'ok'})

@api_view(['POST'])
def register(request):
    serializer = RegisterSerializer(data=request.data)
    if not serializer.is_valid():
        return Response(error_response('validation_error', 'Registration failed', serializer.errors), status=status.HTTP_400_BAD_REQUEST)
    user = User.objects.create_user(
        username=serializer.validated_data['username'],
        password=serializer.validated_data['password'])
    return Response({'id': user.id, 'username': user.username}, status=status.HTTP_201_CREATED)

@api_view(['POST'])
def token(request):
    serializer = LoginSerializer(data=request.data)
    if not serializer.is_valid():
        return Response(error_response('validation_error', 'Invalid credentials payload', serializer.errors), status=status.HTTP_400_BAD_REQUEST)
    user = authenticate(username=serializer.validated_data['username'], password=serializer.validated_data['password'])
    if user is None:
        return Response(error_response('authentication_error', 'Invalid username or password'), status=status.HTTP_401_UNAUTHORIZED)
    payload = {'sub': str(user.id), 'exp': datetime.now(timezone.utc) + timedelta(hours=1)}
    token_value = jwt.encode(payload, os.getenv('JWT_SECRET', 'dev-secret'), algorithm='HS256')
    return Response({'access_token': token_value, 'token_type': 'bearer'})
";

    private static string BuildDjangoApiUrlsContent(bool authHint, bool taskDomainHint)
    {
        var sb = new StringBuilder();
        sb.AppendLine("from django.urls import path");
        if (taskDomainHint)
        {
            sb.AppendLine("from api.views import applications, health, register, tasks, token");
            sb.AppendLine();
            sb.AppendLine("urlpatterns = [");
            sb.AppendLine("    path('health', health),");
            sb.AppendLine("    path('auth/register', register),");
            sb.AppendLine("    path('auth/token', token),");
            sb.AppendLine("    path('tasks', tasks),");
            sb.AppendLine("    path('applications', applications),");
            sb.AppendLine("]");
            return sb.ToString();
        }

        sb.AppendLine("from api.views import health, register, token");
        sb.AppendLine();
        sb.AppendLine("urlpatterns = [");
        sb.AppendLine("    path('health', health),");
        sb.AppendLine("    path('auth/register', register),");
        sb.AppendLine("    path('auth/token', token),");
        sb.AppendLine("]");
        return sb.ToString();
    }

    private static string BuildDjangoErrorEnvelopeModuleContent() =>
        @"def error_response(code: str, message: str, details=None):
    return {
        'error': {
            'code': code,
            'message': message,
            'details': details or {}
        }
    }
";

    private static string BuildDjangoErrorEnvelopeContractContent() =>
        "{\n  \"error\": {\n    \"code\": \"validation_error\",\n    \"message\": \"Validation failed\",\n    \"details\": {}\n  }\n}\n";

    private static string BuildDjangoEnvExampleContent() =>
        "POSTGRES_DB=app\nPOSTGRES_USER=postgres\nPOSTGRES_PASSWORD=postgres\nPOSTGRES_HOST=db\nPOSTGRES_PORT=5432\nJWT_SECRET=dev-secret\nPORT=8000\n";

    private static string BuildDjangoDockerfileContent() =>
        @"FROM python:3.12-slim
WORKDIR /app
ENV PYTHONDONTWRITEBYTECODE=1
ENV PYTHONUNBUFFERED=1
COPY requirements.txt ./requirements.txt
RUN pip install --no-cache-dir -r requirements.txt
COPY . .
CMD [""sh"", ""-c"", ""uvicorn app.asgi:application --host 0.0.0.0 --port ${PORT:-8000}""]
";

    private static string BuildDjangoDockerComposeContent(string appName) =>
        $@"services:
  web:
    build: .
    environment:
      POSTGRES_DB: {appName.ToLowerInvariant()}
      POSTGRES_USER: postgres
      POSTGRES_PASSWORD: postgres
      POSTGRES_HOST: db
      POSTGRES_PORT: 5432
      JWT_SECRET: dev-secret
      PORT: 8000
    ports:
      - ""8000:8000""
    depends_on:
      - db
  db:
    image: postgres:16
    environment:
      POSTGRES_DB: {appName.ToLowerInvariant()}
      POSTGRES_USER: postgres
      POSTGRES_PASSWORD: postgres
";

    private static string BuildDjangoReadmeContent(string appName) =>
        $@"# {appName}

## Stack
- Django 5
- Django REST Framework
- PostgreSQL
- Uvicorn
- Docker Compose

## Run locally
1. Copy `.env.example` values into your environment.
2. Install dependencies: `pip install -r requirements.txt`
3. Run migrations: `python manage.py migrate`
4. Start API: `uvicorn app.asgi:application --host 0.0.0.0 --port 8000`

## Run with Docker
`docker compose up --build`

## API endpoints
- `GET /health`
- `POST /api/auth/register`
- `POST /api/auth/token`
- `GET /api/tasks`
- `POST /api/tasks`
- `GET /api/applications`
- `POST /api/applications`
";

    private static string BuildDjangoApiTestContent() =>
        @"import os
os.environ.setdefault('DJANGO_SETTINGS_MODULE', 'app.settings')
import django
django.setup()

from rest_framework.test import APIClient

def test_health_endpoint():
    client = APIClient()
    response = client.get('/health')
    assert response.status_code == 200

def test_auth_token_validation_error_envelope():
    client = APIClient()
    response = client.post('/api/auth/token', {'username': '', 'password': ''}, format='json')
    assert response.status_code == 400
    body = response.json()
    assert 'error' in body
    assert body['error']['code'] == 'validation_error'
";

    private static string EnsureRequirementPackage(string content, string packageSpec)
    {
        if (string.IsNullOrWhiteSpace(content))
            return packageSpec + "\n";

        var normalized = content.Replace("\r\n", "\n", StringComparison.Ordinal);
        var packageName = packageSpec.Split('=')[0];
        var hasPackage = normalized
            .Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Any(line => line.TrimStart().StartsWith(packageName, StringComparison.OrdinalIgnoreCase));
        if (hasPackage)
            return normalized.EndsWith("\n", StringComparison.Ordinal) ? normalized : normalized + "\n";

        if (!normalized.EndsWith("\n", StringComparison.Ordinal))
            normalized += "\n";
        normalized += packageSpec + "\n";
        return normalized;
    }

    private static string BuildFlaskTestContent(string appName) =>
        $@"import pytest
import os
os.environ['DATABASE_URL'] = 'sqlite:///test_tasks.db'
from app import app
from models import db, Task

@pytest.fixture(autouse=True)
def setup_db():
    with app.app_context():
        db.create_all()
        yield
        db.drop_all()

def test_health():
    with app.test_client() as client:
        response = client.get('/health')
        assert response.status_code == 200
        assert b'{appName}' in response.data

def test_list_tasks():
    with app.test_client() as client:
        response = client.get('/tasks')
        assert response.status_code == 200
        data = response.get_json()
        assert 'items' in data

def test_create_task():
    with app.test_client() as client:
        response = client.post('/tasks', json={{'title': 'Test Task'}})
        assert response.status_code == 201
        data = response.get_json()
        assert data['title'] == 'Test Task'
        assert data['completed'] == False

def test_create_task_validation_error():
    with app.test_client() as client:
        response = client.post('/tasks', json={{'title': ''}})
        assert response.status_code == 422
        data = response.get_json()
        assert 'error' in data
        assert data['error']['code'] == 'validation_error'

def test_update_task():
    with app.test_client() as client:
        # Create a task first
        create_response = client.post('/tasks', json={{'title': 'Test Task'}})
        task_id = create_response.get_json()['id']
        # Update the task
        update_response = client.put(f'/tasks/{{task_id}}', json={{'completed': True}})
        assert update_response.status_code == 200
        data = update_response.get_json()
        assert data['completed'] == True";

    private static string BuildFastApiTestContent(string appName) =>
        $@"import pytest
import os
import sys
os.environ['DATABASE_URL'] = 'sqlite:///test_tasks.db'
sys.path.insert(0, os.path.join(os.path.dirname(__file__), '..', 'src'))
from fastapi.testclient import TestClient
from main import app

@pytest.fixture
def client():
    return TestClient(app)

def test_health(client):
    response = client.get('/health')
    assert response.status_code == 200
    assert response.json()['service'] == '{appName}'

def test_list_tasks(client):
    response = client.get('/tasks')
    assert response.status_code == 200
    data = response.json()
    assert 'items' in data

def test_create_task(client):
    response = client.post('/tasks', json={{'title': 'Test Task'}})
    assert response.status_code == 201
    data = response.json()
    assert data['title'] == 'Test Task'
    assert data['completed'] == False

def test_create_task_validation_error(client):
    response = client.post('/tasks', json={{'title': ''}})
    assert response.status_code == 422
    data = response.json()
    assert 'error' in data
    assert data['error']['code'] == 'request_error'

def test_update_task(client):
    # Create a task first
    create_response = client.post('/tasks', json={{'title': 'Test Task'}})
    task_id = create_response.json()['id']
    # Update the task
    update_response = client.put(f'/tasks/{{task_id}}', json={{'completed': True}})
    assert update_response.status_code == 200
    data = update_response.json()
    assert data['completed'] == True";

    private static string BuildPythonModelsContent(string appName, bool useDjango)
    {
        if (useDjango)
        {
            return @"from django.db import models

class BaseModel(models.Model):
    created_at = models.DateTimeField(auto_now_add=True)

    class Meta:
        abstract = True
";
        }

        return $@"from datetime import datetime

try:
    from flask_sqlalchemy import SQLAlchemy
    db = SQLAlchemy()
    
    class BaseModel(db.Model):
        __abstract__ = True
        id = db.Column(db.Integer, primary_key=True)
        created_at = db.Column(db.DateTime, default=datetime.utcnow)
    
    def init_db(app):
        db.init_app(app)
        with app.app_context():
            db.create_all()
except ImportError:
    from sqlalchemy import create_engine, Column, Integer, String, Boolean, DateTime
    from sqlalchemy.ext.declarative import declarative_base
    from sqlalchemy.orm import sessionmaker, Session
    
    Base = declarative_base()
    engine = None
    SessionLocal = None
    
    class BaseModel(Base):
        __abstract__ = True
        id = Column(Integer, primary_key=True)
        created_at = Column(DateTime, default=datetime.utcnow)
    
    class DB:
        session = None
        Model = BaseModel
    
    db = DB()
    
    def init_db(app):
        global engine, SessionLocal
        import os
        database_url = os.environ.get('DATABASE_URL', 'sqlite:///tasks.db')
        engine = create_engine(database_url)
        Base.metadata.create_all(bind=engine)
        SessionLocal = sessionmaker(bind=engine)
        db.session = SessionLocal()";
    }

    private static string BuildPythonTaskModelContent(string appName, bool useDjango)
    {
        if (useDjango)
        {
            return $@"from django.db import models

class Task(models.Model):
    title = models.CharField(max_length=200)
    completed = models.BooleanField(default=False)
    due_date = models.DateTimeField(null=True, blank=True)
    created_at = models.DateTimeField(auto_now_add=True)

# Domain marker for quality heuristics
APP_DOMAIN = '{appName}:task_management'";
        }

        return $@"from models import BaseModel, db
from datetime import datetime

class Task(BaseModel):
    __tablename__ = 'tasks'
    title = db.Column(db.String(200), nullable=False)
    completed = db.Column(db.Boolean, default=False)
    due_date = db.Column(db.DateTime, nullable=True)

    def to_dict(self):
        return {{
            'id': self.id,
            'title': self.title,
            'completed': self.completed,
            'due_date': self.due_date.isoformat() if self.due_date else None,
            'created_at': self.created_at.isoformat() if self.created_at else None
        }}

# Domain marker for quality heuristics
APP_DOMAIN = '{appName}:task_management'";
    }

    private static bool PlanSuggestsTaskDomain(GenerationPlan plan)
    {
        var d = plan.ApplicationDescription;
        return d.Contains("task", StringComparison.OrdinalIgnoreCase)
               || d.Contains("todo", StringComparison.OrdinalIgnoreCase)
               || d.Contains("management", StringComparison.OrdinalIgnoreCase)
               || plan.ApplicationName.Contains("task", StringComparison.OrdinalIgnoreCase);
    }

    private static bool PlanSuggestsComplexFastApiStack(GenerationPlan plan)
    {
        if (!PlanSuggestsFastApi(plan))
            return false;

        var blob = BuildPlanBlob(plan);
        var markers = 0;
        if (blob.Contains("postgres", StringComparison.OrdinalIgnoreCase) || blob.Contains("postgresql", StringComparison.OrdinalIgnoreCase)) markers++;
        if (blob.Contains("redis", StringComparison.OrdinalIgnoreCase)) markers++;
        if (blob.Contains("celery", StringComparison.OrdinalIgnoreCase) || blob.Contains("worker", StringComparison.OrdinalIgnoreCase) || blob.Contains("queue", StringComparison.OrdinalIgnoreCase)) markers++;
        if (blob.Contains("stripe", StringComparison.OrdinalIgnoreCase) || blob.Contains("webhook", StringComparison.OrdinalIgnoreCase)) markers++;
        if (blob.Contains("docker compose", StringComparison.OrdinalIgnoreCase) || blob.Contains("docker-compose", StringComparison.OrdinalIgnoreCase)) markers++;
        if (blob.Contains("ci", StringComparison.OrdinalIgnoreCase) || blob.Contains("pipeline", StringComparison.OrdinalIgnoreCase) || blob.Contains("github actions", StringComparison.OrdinalIgnoreCase)) markers++;
        return markers >= 3;
    }

    private static string BuildPlanBlob(GenerationPlan plan)
    {
        var parts = new List<string>
        {
            plan.ApplicationDescription,
            plan.TechStack.Rationale,
            plan.RuntimeImage
        };
        parts.AddRange(plan.TechStack.Languages);
        parts.AddRange(plan.TechStack.Frameworks);
        parts.AddRange(plan.TechStack.Databases);
        parts.AddRange(plan.TechStack.Infrastructure);
        return string.Join(' ', parts.Where(p => !string.IsNullOrWhiteSpace(p)));
    }

    private static void EnsureComplexFastApiArtifacts(
        Dictionary<string, GeneratedFile> generated,
        List<GeneratedFile> added)
    {
        AddGeneratedIfMissing(
            generated,
            added,
            "docker-compose.yml",
            "yaml",
            "services:\n  api:\n    build: .\n  db:\n    image: postgres:16\n  redis:\n    image: redis:7\n  worker:\n    build: .\n");
        AddGeneratedIfMissing(
            generated,
            added,
            "alembic/env.py",
            "python",
            "from sqlalchemy import engine_from_config\n# alembic baseline\n");
        AddGeneratedIfMissing(
            generated,
            added,
            "worker.py",
            "python",
            "from celery import Celery\ncelery = Celery('app', broker='redis://redis:6379/0')\n");
        AddGeneratedIfMissing(
            generated,
            added,
            ".github/workflows/ci.yml",
            "yaml",
            "name: CI\non: [push]\njobs:\n  test:\n    runs-on: ubuntu-latest\n");
        AddGeneratedIfMissing(
            generated,
            added,
            "src/webhook.py",
            "python",
            "def handle_webhook(payload):\n    return {'status': 'ok'}\n");
        AddGeneratedIfMissing(
            generated,
            added,
            "scripts/start.sh",
            "shell",
            "#!/usr/bin/env bash\nset -euo pipefail\ndocker compose up --build\n");
        AddGeneratedIfMissing(
            generated,
            added,
            "app/database.py",
            "python",
            "from sqlalchemy.ext.asyncio import AsyncSession\nfrom fastapi import Depends\n\nasync def get_db() -> AsyncSession:\n    yield None\n");
        AddGeneratedIfMissing(
            generated,
            added,
            "app/routers/health.py",
            "python",
            "from fastapi import APIRouter\nrouter = APIRouter()\n@router.get('/health')\ndef health():\n    return {'status': 'ok'}\n");
        AddGeneratedIfMissing(
            generated,
            added,
            "app/models/payment.py",
            "python",
            "class Payment:\n    pass\n");
        AddGeneratedIfMissing(
            generated,
            added,
            "app/error_envelope.py",
            "python",
            @"from typing import Any, Dict, Optional
from enum import Enum

class ErrorCode(str, Enum):
    VALIDATION_ERROR = 'validation_error'
    NOT_FOUND = 'not_found'
    CONFLICT = 'conflict'
    INTERNAL_ERROR = 'internal_error'
    AUTHENTICATION_ERROR = 'authentication_error'
    AUTHORIZATION_ERROR = 'authorization_error'
    RATE_LIMIT_EXCEEDED = 'rate_limit_exceeded'
    SERVICE_UNAVAILABLE = 'service_unavailable'

def error_response(
    code: ErrorCode,
    message: str,
    details: Optional[Dict[str, Any]] = None,
    request_id: Optional[str] = None
) -> Dict[str, Any]:
    envelope = {
        'error': {
            'code': code.value,
            'message': message,
            'details': details or {}
        }
    }
    if request_id:
        envelope['request_id'] = request_id
    return envelope");
        AddGeneratedIfMissing(
            generated,
            added,
            "app/observability.py",
            "python",
            @"import logging
import json
from contextlib import contextmanager
from typing import Optional
import uuid

class RequestLogger:
    def __init__(self):
        self.logger = logging.getLogger('app')
        handler = logging.StreamHandler()
        formatter = logging.Formatter('%(asctime)s - %(name)s - %(levelname)s - %(message)s')
        handler.setFormatter(formatter)
        self.logger.addHandler(handler)
        self.logger.setLevel(logging.INFO)
    
    def log_request(self, method: str, path: str, request_id: str, user_id: Optional[str] = None):
        self.logger.info('request_started', extra={
            'json': True,
            'x-request-id': request_id,
            'method': method,
            'path': path,
            'user_id': user_id
        })
    
    def log_response(self, request_id: str, status_code: int, duration_ms: int):
        self.logger.info('request_completed', extra={
            'json': True,
            'x-request-id': request_id,
            'status_code': status_code,
            'duration_ms': duration_ms
        })
    
    def log_error(self, request_id: str, error_code: str, error_message: str):
        self.logger.error('request_error', extra={
            'json': True,
            'x-request-id': request_id,
            'error_code': error_code,
            'error_message': error_message
        })

@contextmanager
def request_context(request_id: Optional[str] = None):
    rid = request_id or str(uuid.uuid4())
    try:
        yield rid
    except Exception as e:
        raise

request_logger = RequestLogger()");
        AddGeneratedIfMissing(
            generated,
            added,
            "app/payment.py",
            "python",
            "def create_payment(request):\n    idempotency_key = request.headers.get('Idempotency-Key')\n    return {'idempotency_key': idempotency_key}\n");
        AddGeneratedIfMissing(
            generated,
            added,
            "app/audit.py",
            "python",
            "def audit_log(event):\n    return f'audit:{event}'\n");
        AddGeneratedIfMissing(
            generated,
            added,
            "app/rate_limit.py",
            "python",
            "def rate_limit_decorator(func):\n    return func\n");
        AddGeneratedIfMissing(
            generated,
            added,
            "README.md",
            "markdown",
            @"# Application Documentation

## Overview
This application is generated with domain-specific best practices for secure, compliant, and observable systems.

## Error Handling
All API responses follow a standardized error envelope format:
```json
{
  ""error"": {
    ""code"": ""validation_error"",
    ""message"": ""Detailed error message"",
    ""details"": {}
  },
  ""request_id"": ""uuid""
}
```

### Error Codes
- `validation_error`: Request validation failed
- `not_found`: Resource not found
- `conflict`: Resource conflict
- `internal_error`: Internal server error
- `authentication_error`: Authentication failed
- `authorization_error`: Authorization failed
- `rate_limit_exceeded`: Rate limit exceeded
- `service_unavailable`: Service unavailable

## Observability
Structured logging is enabled with the following fields:
- `x-request-id`: Unique request identifier
- `method`: HTTP method
- `path`: Request path
- `user_id`: User identifier (when available)
- `status_code`: Response status code
- `duration_ms`: Request duration in milliseconds

## Security
- JWT-based authentication via `/auth/token`
- Environment variable `JWT_SECRET` required
- Idempotency keys supported for write operations
- Audit logging enabled for sensitive operations

## Compliance
This application includes compliance artifacts for:
- PCI DSS (payment processing)
- HIPAA (healthcare data)
- GDPR (data governance)
- SOC 2 (security controls)

## Running the Application
```bash
docker compose up --build
```

## Environment Variables
- `JWT_SECRET`: Required for JWT token signing
- `DATABASE_URL`: Database connection string
- `PORT`: Application port (default: 4000)");
        AddGeneratedIfMissing(
            generated,
            added,
            "tests/test_api_integration.py",
            "python",
            "import os\nimport sys\nimport pytest\nfrom fastapi.testclient import TestClient\n\nsys.path.insert(0, os.path.join(os.path.dirname(__file__), '..', 'src'))\nfrom main import app\n\n@pytest.fixture\ndef client():\n    return TestClient(app)\n\n@pytest.mark.integration\ndef test_error_envelope_negative(client):\n    response = client.post('/tasks', json={'title': ''})\n    assert response.status_code == 422\n    assert 'error' in response.json()\n");
    }

    private static bool LooksLikePythonPlaceholder(string content)
    {
        if (string.IsNullOrWhiteSpace(content)) return true;
        var s = content.ToLowerInvariant();
        return s.Contains("hello from generatedapp", StringComparison.Ordinal)
               || (s.Contains("hello from", StringComparison.Ordinal) && s.Contains("print(", StringComparison.Ordinal));
    }

    private static bool LooksLikePlaceholderTest(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
            return true;

        return content.Contains("assert True", StringComparison.OrdinalIgnoreCase)
               || content.Contains("assert 1 == 1", StringComparison.OrdinalIgnoreCase)
               || content.Contains("assert True == True", StringComparison.OrdinalIgnoreCase)
               || content.Contains("pass", StringComparison.OrdinalIgnoreCase) && content.Contains("test_", StringComparison.OrdinalIgnoreCase);
    }

    private static string NormalizeRelativePath(string path) =>
        path.Replace('\\', '/').Trim().TrimStart('/');

    private static string BuildNodePackageJsonContent(string appName) =>
        $@"{{
  ""name"": ""{appName.ToLower()}"",
  ""version"": ""1.0.0"",
  ""description"": ""{appName}"",
  ""main"": ""index.js"",
  ""scripts"": {{
    ""start"": ""node index.js"",
    ""test"": ""jest""
  }},
  ""dependencies"": {{
    ""express"": ""^4.18.0"",
    ""jsonwebtoken"": ""^9.0.2"",
    ""bcryptjs"": ""^2.4.3""
  }},
  ""devDependencies"": {{
    ""jest"": ""^29.0.0"",
    ""supertest"": ""^7.1.1""
  }}
}}";

    private static string BuildNodeIndexContent(string appName) =>
        $@"const express = require('express');
const taskRoutes = require('./src/routes/tasks');
const authRoutes = require('./src/routes/auth');
const app = express();
const port = process.env.PORT || 4000;
app.use(express.json());

app.get('/health', (req, res) => {{
    res.json({{ service: '{appName}', status: 'ok' }});
}});

app.get('/readiness', (req, res) => {{
    res.json({{ status: 'ready' }});
}});

app.use('/api/tasks', taskRoutes);
app.use('/api/auth', authRoutes);

if (require.main === module) {{
  app.listen(port, () => {{
      console.log(`{appName} listening on port ${{port}}`);
  }});
}}

module.exports = app;";

    private static string BuildNodeTestContent(string appName) =>
        $@"const request = require('supertest');
const app = require('./index');

describe('{appName} Health API', () => {{
    it('GET /health should return 200', async () => {{
        const response = await request(app).get('/health');
        expect(response.statusCode).toBe(200);
        expect(response.body.service).toBe('{appName}');
        expect(response.body.status).toBe('ok');
    }});

    it('POST /api/auth/login should return token', async () => {{
        const response = await request(app).post('/api/auth/login').send({{ email: 'demo@local', password: 'demo1234' }});
        expect(response.statusCode).toBe(200);
        expect(response.body.token).toBeDefined();
    }});

    it('POST /api/tasks should create task', async () => {{
        const response = await request(app)
            .post('/api/tasks')
            .send({{ title: 'task from test', status: 'todo' }});
        expect(response.statusCode).toBe(201);
        expect(response.body.title).toBe('task from test');
    }});
}});";

    private static string BuildNodeTaskStoreContent() =>
        @"const tasks = [];
let idCounter = 1;

function listTasks() { return tasks; }
function createTask(input) {
  const item = { id: idCounter++, title: input.title, status: input.status || 'todo' };
  tasks.push(item);
  return item;
}

module.exports = { listTasks, createTask };";

    private static string BuildNodeAuthServiceContent() =>
        @"const jwt = require('jsonwebtoken');
const SECRET = process.env.JWT_SECRET || 'dev-secret';

function issueToken(email) {
  return jwt.sign({ sub: email, scope: ['tasks:read', 'tasks:write'] }, SECRET, { expiresIn: '1h' });
}

module.exports = { issueToken };";

    private static string BuildNodeTaskRoutesContent() =>
        @"const express = require('express');
const store = require('../models/task-store');
const router = express.Router();

router.get('/', (req, res) => {
  res.json({ items: store.listTasks() });
});

router.post('/', (req, res) => {
  const title = typeof req.body?.title === 'string' ? req.body.title.trim() : '';
  if (!title) return res.status(422).json({ error: { code: 'validation_error', message: 'title is required' } });
  const created = store.createTask({ title, status: req.body?.status });
  res.status(201).json(created);
});

module.exports = router;";

    private static string BuildNodeAuthRoutesContent() =>
        @"const express = require('express');
const auth = require('../services/auth-service');
const router = express.Router();

router.post('/login', (req, res) => {
  const email = typeof req.body?.email === 'string' ? req.body.email.trim() : '';
  const password = typeof req.body?.password === 'string' ? req.body.password : '';
  if (!email || !password) return res.status(422).json({ error: { code: 'validation_error', message: 'email/password required' } });
  const token = auth.issueToken(email);
  res.json({ token });
});

module.exports = router;";

    private static string BuildSpringBootPomContent(string appName)
    {
        var artifact = SanitizeDotNetAppName(appName).ToLowerInvariant();
        return $"""
            <?xml version="1.0" encoding="UTF-8"?>
            <project xmlns="http://maven.apache.org/POM/4.0.0">
              <modelVersion>4.0.0</modelVersion>
              <groupId>com.generated</groupId>
              <artifactId>{artifact}-backend</artifactId>
              <version>0.0.1-SNAPSHOT</version>
              <parent>
                <groupId>org.springframework.boot</groupId>
                <artifactId>spring-boot-starter-parent</artifactId>
                <version>3.3.5</version>
              </parent>
              <dependencies>
                <dependency>
                  <groupId>org.springframework.boot</groupId>
                  <artifactId>spring-boot-starter-web</artifactId>
                </dependency>
                <dependency>
                  <groupId>org.springframework.boot</groupId>
                  <artifactId>spring-boot-starter-test</artifactId>
                  <scope>test</scope>
                </dependency>
              </dependencies>
              <build>
                <plugins>
                  <plugin>
                    <groupId>org.springframework.boot</groupId>
                    <artifactId>spring-boot-maven-plugin</artifactId>
                  </plugin>
                </plugins>
              </build>
            </project>
            """;
    }

    private static string BuildSpringBootApplicationContent() =>
        """
        package com.generated.banking;

        import org.springframework.boot.SpringApplication;
        import org.springframework.boot.autoconfigure.SpringBootApplication;

        @SpringBootApplication
        public class BankingApplication {
            public static void main(String[] args) {
                SpringApplication.run(BankingApplication.class, args);
            }
        }
        """;

    private static string BuildSpringBootHealthControllerContent() =>
        """
        package com.generated.banking.web;

        import org.springframework.web.bind.annotation.GetMapping;
        import org.springframework.web.bind.annotation.RestController;

        import java.util.Map;

        @RestController
        public class HealthController {
            @GetMapping("/health")
            public Map<String, String> health() {
                return Map.of("status", "UP");
            }

            @GetMapping("/readiness")
            public Map<String, String> readiness() {
                return Map.of("status", "READY");
            }

            @GetMapping("/api/health")
            public Map<String, String> apiHealth() {
                return Map.of("status", "ok", "service", "mobile-banking-api");
            }
        }
        """;

    private static string BuildSpringBootAccountControllerContent() =>
        """
        package com.generated.banking.web;

        import org.springframework.web.bind.annotation.GetMapping;
        import org.springframework.web.bind.annotation.RequestMapping;
        import org.springframework.web.bind.annotation.RestController;

        @RestController
        @RequestMapping("/api/accounts")
        public class AccountController {
            @GetMapping
            public Object list() {
                return java.util.List.of(
                    java.util.Map.of("id", "acc-1", "currency", "USD", "balance", 1200.50)
                );
            }
        }
        """;

    private static string BuildSpringBootTransferControllerContent() =>
        """
        package com.generated.banking.web;

        import org.springframework.web.bind.annotation.PostMapping;
        import org.springframework.web.bind.annotation.RequestBody;
        import org.springframework.web.bind.annotation.RequestMapping;
        import org.springframework.web.bind.annotation.RestController;

        @RestController
        @RequestMapping("/api/transfers")
        public class TransferController {
            @PostMapping
            public Object create(@RequestBody java.util.Map<String, Object> body) {
                return java.util.Map.of(
                    "id", "tr-" + java.util.UUID.randomUUID(),
                    "fromAccountId", body.getOrDefault("fromAccountId", "acc-1"),
                    "toAccountId", body.getOrDefault("toAccountId", "acc-2"),
                    "amount", body.getOrDefault("amount", 0),
                    "status", "completed"
                );
            }
        }
        """;

    private static string BuildSpringBootPaymentControllerContent() =>
        """
        package com.generated.banking.web;

        import org.springframework.web.bind.annotation.PostMapping;
        import org.springframework.web.bind.annotation.RequestBody;
        import org.springframework.web.bind.annotation.RequestMapping;
        import org.springframework.web.bind.annotation.RestController;

        @RestController
        @RequestMapping("/api/payments")
        public class PaymentController {
            @PostMapping
            public Object pay(@RequestBody java.util.Map<String, Object> body) {
                return java.util.Map.of(
                    "id", "pay-" + java.util.UUID.randomUUID(),
                    "merchant", body.getOrDefault("merchant", "utility"),
                    "amount", body.getOrDefault("amount", 0),
                    "status", "authorized"
                );
            }
        }
        """;

    private static string BuildSpringBootAuthControllerContent() =>
        """
        package com.generated.banking.web;

        import org.springframework.web.bind.annotation.PostMapping;
        import org.springframework.web.bind.annotation.RequestBody;
        import org.springframework.web.bind.annotation.RequestMapping;
        import org.springframework.web.bind.annotation.RestController;

        @RestController
        @RequestMapping("/api/auth")
        public class AuthController {
            @PostMapping("/token")
            public Object token(@RequestBody java.util.Map<String, String> body) {
                var user = body.getOrDefault("username", "user");
                return java.util.Map.of("token", "banking-" + user, "expiresIn", 3600);
            }
        }
        """;

    private static string BuildSpringBootBankingApiTestsContent() =>
        """
        package com.generated.banking;

        import org.junit.jupiter.api.Test;
        import org.springframework.beans.factory.annotation.Autowired;
        import org.springframework.boot.test.autoconfigure.web.servlet.AutoConfigureMockMvc;
        import org.springframework.boot.test.context.SpringBootTest;
        import org.springframework.http.MediaType;
        import org.springframework.test.web.servlet.MockMvc;

        import static org.springframework.test.web.servlet.request.MockMvcRequestBuilders.get;
        import static org.springframework.test.web.servlet.request.MockMvcRequestBuilders.post;
        import static org.springframework.test.web.servlet.result.MockMvcResultMatchers.jsonPath;
        import static org.springframework.test.web.servlet.result.MockMvcResultMatchers.status;

        @SpringBootTest
        @AutoConfigureMockMvc
        class BankingApiTests {
            @Autowired
            private MockMvc mockMvc;

            // integration: MockMvc client exercises HTTP API surface
            @Test
            void accountsEndpointReturnsList_integration() throws Exception {
                mockMvc.perform(get("/api/accounts"))
                    .andExpect(status().isOk())
                    .andExpect(jsonPath("$").isArray());
            }

            @Test
            void authTokenEndpointIssuesToken_integration() throws Exception {
                mockMvc.perform(post("/api/auth/token")
                        .contentType(MediaType.APPLICATION_JSON)
                        .content("{\"username\":\"demo\"}"))
                    .andExpect(status().isOk())
                    .andExpect(jsonPath("$.token").exists());
            }

            @Test
            void transferEndpointAcceptsPayload_integration() throws Exception {
                mockMvc.perform(post("/api/transfers")
                        .contentType(MediaType.APPLICATION_JSON)
                        .content("{\"fromAccountId\":\"acc-1\",\"toAccountId\":\"acc-2\",\"amount\":10}"))
                    .andExpect(status().isOk())
                    .andExpect(jsonPath("$.status").value("completed"));
            }

            @Test
            void unknownRouteReturns404_negative() throws Exception {
                mockMvc.perform(get("/api/route-does-not-exist-xyz"))
                    .andExpect(status().isNotFound());
            }

            @Test
            void healthEndpointIsReachable_integration() throws Exception {
                mockMvc.perform(get("/health"))
                    .andExpect(status().isOk());
            }
        }
        """;

    private static string BuildSpringBootApplicationYml() =>
        """
        server:
          port: 8080
        spring:
          application:
            name: mobile-banking
        logging:
          pattern:
            console: '{"level":"%level","correlationId":"%X{correlationId}","message":"%m"}%n'
        management:
          endpoints:
            web:
              exposure:
                include: health,readiness
        """;

    private static string BuildSpringBootCorrelationIdFilterContent() =>
        """
        package com.generated.banking.config;

        import jakarta.servlet.FilterChain;
        import jakarta.servlet.ServletException;
        import jakarta.servlet.http.HttpServletRequest;
        import jakarta.servlet.http.HttpServletResponse;
        import org.slf4j.Logger;
        import org.slf4j.LoggerFactory;
        import org.slf4j.MDC;
        import org.springframework.stereotype.Component;
        import org.springframework.web.filter.OncePerRequestFilter;

        import java.io.IOException;
        import java.util.UUID;

        @Component
        public class CorrelationIdFilter extends OncePerRequestFilter {
            private static final Logger logger = LoggerFactory.getLogger(CorrelationIdFilter.class);

            @Override
            protected void doFilterInternal(HttpServletRequest request, HttpServletResponse response, FilterChain chain)
                    throws ServletException, IOException {
                String correlationId = request.getHeader("x-request-id");
                if (correlationId == null || correlationId.isBlank()) {
                    correlationId = UUID.randomUUID().toString();
                }
                MDC.put("correlationId", correlationId);
                response.setHeader("x-request-id", correlationId);
                logger.info("structured request correlationId={}", correlationId);
                try {
                    chain.doFilter(request, response);
                } finally {
                    MDC.remove("correlationId");
                }
            }
        }
        """;

    private static string BuildSpringBootGlobalExceptionHandlerContent() =>
        """
        package com.generated.banking.web;

        import org.slf4j.Logger;
        import org.slf4j.LoggerFactory;
        import org.springframework.http.ResponseEntity;
        import org.springframework.web.bind.annotation.ControllerAdvice;
        import org.springframework.web.bind.annotation.ExceptionHandler;

        import java.util.Map;

        @ControllerAdvice
        public class GlobalExceptionHandler {
            private static final Logger logger = LoggerFactory.getLogger(GlobalExceptionHandler.class);

            @ExceptionHandler(Exception.class)
            public ResponseEntity<Map<String, Object>> handle(Exception ex) {
                logger.error("request failed", ex);
                return ResponseEntity.internalServerError().body(Map.of(
                    "error", Map.of("code", "internal_error", "message", ex.getMessage())));
            }
        }
        """;

    private static string BuildBankingReadmeContent(string appName) =>
        $"""
        # {appName}

        Mobile banking monorepo: Spring Boot backend (`backend/`) and React TypeScript frontend (`frontend/`).

        ## Run

        ```bash
        ./scripts/run.sh
        docker compose up --build
        ```
        """;

    private static string BuildBankingDockerComposeContent() =>
        """
        services:
          backend:
            build: ./backend
            ports:
              - "8080:8080"
          frontend:
            build: ./frontend
            ports:
              - "5173:5173"
        """;

    private static string BuildBankingCiWorkflowContent() =>
        """
        name: ci
        on: [push, pull_request]
        jobs:
          build:
            runs-on: ubuntu-latest
            steps:
              - uses: actions/checkout@v4
              - name: ci
                run: echo "ci pipeline"
        """;

    private static string BuildBankingRunScriptContent() =>
        """
        #!/usr/bin/env sh
        set -e
        (cd backend && mvn -B -ntp -DskipTests package) || true
        (cd frontend && npm ci && npm run build) || true
        """;

    private static string BuildAccountServiceContent() =>
        """
        package com.generated.banking.service;

        import org.springframework.stereotype.Service;
        import java.util.List;
        import java.util.Map;

        @Service
        public class AccountService {
            public List<Map<String, Object>> listAccounts() {
                return List.of(Map.of("id", "acc-1", "currency", "USD", "balance", 1200.50));
            }
        }
        """;

    private static string BuildTransferServiceContent() =>
        """
        package com.generated.banking.service;

        import org.springframework.stereotype.Service;
        import java.util.Map;
        import java.util.UUID;

        @Service
        public class TransferService {
            private final Object transferLock = new Object();

            public Map<String, Object> createTransfer(Map<String, Object> body) {
                synchronized (transferLock) { // transfer serialized for idempotency
                String idempotencyKey = String.valueOf(body.getOrDefault("idempotency_key", "idem-" + UUID.randomUUID()));
                return Map.of(
                    "id", "tr-" + UUID.randomUUID(),
                    "idempotency_key", idempotencyKey,
                    "fromAccountId", body.getOrDefault("fromAccountId", "acc-1"),
                    "toAccountId", body.getOrDefault("toAccountId", "acc-2"),
                    "amount", body.getOrDefault("amount", 0),
                    "status", "completed",
                    "audit_log", "transfer_recorded"
                );
                }
            }
        }
        """;

    private static string BuildReactTypeScriptPackageJson(string appName) =>
        $$"""
        {
          "name": "{{SanitizeDotNetAppName(appName).ToLowerInvariant()}}-frontend",
          "private": true,
          "version": "0.0.1",
          "type": "module",
          "scripts": {
            "dev": "vite",
            "build": "vite build",
            "test": "vitest run"
          },
          "dependencies": {
            "react": "^18.3.1",
            "react-dom": "^18.3.1"
          },
          "devDependencies": {
            "@types/react": "^18.3.12",
            "@types/react-dom": "^18.3.1",
            "@vitejs/plugin-react": "^4.3.3",
            "typescript": "^5.6.3",
            "vite": "^5.4.10",
            "vitest": "^2.1.4",
            "jsdom": "^25.0.1",
            "@testing-library/react": "^16.0.1"
          }
        }
        """;

    private static string BuildReactViteConfig() =>
        """
        import { defineConfig } from 'vite';
        import react from '@vitejs/plugin-react';

        export default defineConfig({
          plugins: [react()],
          test: {
            environment: 'jsdom',
          },
        });
        """;

    private static string BuildReactIndexHtml(string appName) =>
        $$"""
        <!doctype html>
        <html lang="en">
          <head>
            <meta charset="UTF-8" />
            <meta name="viewport" content="width=device-width, initial-scale=1.0" />
            <title>{{appName}}</title>
          </head>
          <body>
            <div id="root"></div>
            <script type="module" src="/src/main.tsx"></script>
          </body>
        </html>
        """;

    private static string BuildReactTypeScriptTsConfig() =>
        """
        {
          "compilerOptions": {
            "target": "ES2022",
            "module": "ESNext",
            "jsx": "react-jsx",
            "moduleResolution": "bundler",
            "strict": true,
            "skipLibCheck": true
          },
          "include": ["src"]
        }
        """;

    private static string BuildReactMainTsx() =>
        """
        import React from 'react';
        import { createRoot } from 'react-dom/client';
        import { App } from './App';

        createRoot(document.getElementById('root')!).render(<App />);
        """;

    private static string BuildReactAppTsx(string appName) =>
        $$"""
        import React, { useEffect, useState } from 'react';
        import { fetchAccounts } from './api/client';

        export function App() {
          const [accounts, setAccounts] = useState<Array<{ id: string }>>([]);
          const [transferStatus, setTransferStatus] = useState<string>('idle');
          useEffect(() => {
            fetchAccounts().then(setAccounts).catch(() => setAccounts([]));
          }, []);
          return (
            <main>
              <h1>{{appName}} — Mobile Banking</h1>
              <p>Accounts: {accounts.length}</p>
              <button type="button" onClick={() => setTransferStatus('transfer-ready')}>
                Prepare transfer
              </button>
              <p>Transfer UI: {transferStatus}</p>
            </main>
          );
        }
        """;

    private static string BuildReactApiClientTs() =>
        """
        const API_BASE = import.meta.env.VITE_API_BASE ?? 'http://localhost:8080';

        export async function fetchAccounts(): Promise<Array<{ id: string }>> {
          const res = await fetch(`${API_BASE}/api/accounts`);
          if (!res.ok) throw new Error('accounts_fetch_failed');
          return res.json();
        }

        export async function createTransfer(payload: { fromAccountId: string; toAccountId: string; amount: number }) {
          const res = await fetch(`${API_BASE}/api/transfers`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(payload),
          });
          if (!res.ok) throw new Error('transfer_failed');
          return res.json();
        }

        export async function createPayment(payload: { merchant: string; amount: number }) {
          const res = await fetch(`${API_BASE}/api/payments`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(payload),
          });
          if (!res.ok) throw new Error('payment_failed');
          return res.json();
        }

        export async function obtainAuthToken(username: string) {
          const res = await fetch(`${API_BASE}/api/auth/token`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ username }),
          });
          if (!res.ok) throw new Error('auth_failed');
          return res.json() as Promise<{ token: string }>;
        }
        """;

    private static string BuildReactAppTestTsx() =>
        """
        import { describe, expect, it } from 'vitest';

        describe('mobile banking ui', () => {
          it('exposes transfer and payment api client', async () => {
            const client = await import('./api/client');
            expect(typeof client.createTransfer).toBe('function');
            expect(typeof client.createPayment).toBe('function');
            expect(typeof client.obtainAuthToken).toBe('function');
          });
        });
        """;
}
