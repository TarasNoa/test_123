using Libr4.IDE.Application.AutonomousAppGeneration.Context.Jit;
using Libr4.IDE.Application.AutonomousAppGeneration.Infrastructure;
using Libr4.IDE.Domain.AutonomousAppGeneration;

namespace Libr4.IDE.AutonomousAppGeneration.Agents;

/// <summary>
/// Per-file generation manifest for any supported stack (not only Java+React).
/// </summary>
public static class UniversalExpandedFileManifest
{
    public static IReadOnlyList<PlannedFileEntry> AllForPlan(GenerationPlan plan)
    {
        var entries = new List<PlannedFileEntry>();
        AppendBackendEntries(plan, entries);
        if (StackLayoutHeuristics.HasSeparatedFrontend(plan))
            AppendFrontendEntries(plan, entries);
        AppendDatabaseEntries(plan, entries);
        AppendDevOpsEntries(plan, entries);
        Libr4MdManifest.AppendForPlan(plan, entries);

        return entries
            .GroupBy(e => e.Path, StringComparer.OrdinalIgnoreCase)
            .Select(g => g.First())
            .ToList();
    }

    private static void AppendBackendEntries(GenerationPlan plan, List<PlannedFileEntry> entries)
    {
        var kind = StackPlanHeuristics.Classify(plan);
        var backend = StackLayoutHeuristics.BackendRoot(plan);

        if (StackLayoutHeuristics.UsesDjango(plan))
        {
            AppendDjangoBackend(plan, entries, backend);
            return;
        }

        if (StackLayoutHeuristics.UsesFastApi(plan) || (kind == StackKind.Python && !StackLayoutHeuristics.HasSeparatedFrontend(plan)))
        {
            AppendFastApiBackend(plan, entries, backend);
            return;
        }

        if (kind == StackKind.Python)
        {
            AppendFlaskBackend(plan, entries, backend);
            return;
        }

        if (kind is StackKind.Java or StackKind.JavaReactFullStack)
        {
            AppendJavaBackend(plan, entries);
            return;
        }

        if (kind is StackKind.Go or StackKind.GoReactFullStack)
        {
            AppendGoBackend(plan, entries, backend);
            return;
        }

        if (kind is StackKind.Php or StackKind.PhpVueFullStack)
        {
            AppendPhpBackend(plan, entries, backend);
            return;
        }

        if (kind is StackKind.Node || StackPlanHeuristics.IsNode(plan))
        {
            AppendNodeBackend(plan, entries, backend);
            return;
        }

        if (kind == StackKind.Rust)
        {
            AppendRustBackend(plan, entries, backend);
            return;
        }

        if (kind == StackKind.Ruby)
        {
            AppendRubyBackend(plan, entries, backend);
            return;
        }

        if (kind == StackKind.DotNet)
        {
            AppendDotNetBackend(plan, entries);
            return;
        }

        AppendGenericBackend(plan, entries, backend);
    }

    private static void AppendDjangoBackend(GenerationPlan plan, List<PlannedFileEntry> entries, string backend)
    {
        var slug = StackLayoutHeuristics.ProjectSlug(plan);
        var domain = SummarizeDomain(plan);
        const string pkg = "Use top-level Django app package 'meals' (INSTALLED_APPS='meals', imports meals.* — NOT calorievisionapp.meals). ";
        entries.Add(Entry($"{backend}manage.py", AgentPhase.Backend, "Django manage.py entrypoint.", "python-django"));
        entries.Add(Entry($"{backend}requirements.txt", AgentPhase.Backend, "Django, DRF, openai SDK, pillow, cors headers.", "python-django"));
        entries.Add(Entry($"{backend}{slug}/__init__.py", AgentPhase.Backend, "Django project package (empty __init__.py).", "python-django"));
        entries.Add(Entry($"{backend}{slug}/settings.py", AgentPhase.Backend, pkg + "SQLite, REST_FRAMEWORK, CORS localhost:5173, OPENAI_API_KEY, custom exception handler.", "python-django"));
        entries.Add(Entry($"{backend}{slug}/urls.py", AgentPhase.Backend, pkg + "path('api/meals/', include('meals.urls')).", "python-django"));
        entries.Add(Entry($"{backend}{slug}/wsgi.py", AgentPhase.Backend, "WSGI entry.", "python-django"));
        entries.Add(Entry($"{backend}meals/__init__.py", AgentPhase.Backend, "Meals app package (empty __init__.py).", "python-django"));
        entries.Add(Entry($"{backend}meals/apps.py", AgentPhase.Backend, pkg + "MealsConfig with name='meals'.", "python-django"));
        entries.Add(Entry($"{backend}meals/models.py", AgentPhase.Backend, $"Meal model for {domain}.", "python-django"));
        entries.Add(Entry($"{backend}meals/serializers.py", AgentPhase.Backend, "DRF serializers for meal analysis responses.", "python-django"));
        entries.Add(Entry($"{backend}meals/views.py", AgentPhase.Backend, "POST /api/meals/analyze and GET /api/meals/history.", "python-django"));
        entries.Add(Entry($"{backend}meals/urls.py", AgentPhase.Backend, "Meals API routes.", "python-django"));
        entries.Add(Entry($"{backend}meals/exceptions.py", AgentPhase.Backend, "DRF exception handler returning JSON {error, code, message}.", "python-django"));
        entries.Add(Entry($"{backend}meals/services/openai_vision.py", AgentPhase.Backend, "OpenAI gpt-4o Vision integration using openai Python SDK.", "python-django"));
        entries.Add(Entry($"{backend}meals/tests.py", AgentPhase.Backend, $"Django APITestCase tests for meals API ({domain}).", "python-django"));
    }

    private static void AppendFastApiBackend(GenerationPlan plan, List<PlannedFileEntry> entries, string backend)
    {
        var slug = StackLayoutHeuristics.ProjectSlug(plan);
        entries.Add(Entry($"{backend}requirements.txt", AgentPhase.Backend, "FastAPI, uvicorn, pydantic, httpx.", "python-fastapi"));
        entries.Add(Entry($"{backend}main.py", AgentPhase.Backend, "FastAPI app entry with CORS and routers.", "python-fastapi"));
        entries.Add(Entry($"{backend}app/__init__.py", AgentPhase.Backend, "Application package.", "python-fastapi"));
        entries.Add(Entry($"{backend}app/routers/{slug}.py", AgentPhase.Backend, "REST routes for the planned domain.", "python-fastapi"));
        entries.Add(Entry($"{backend}app/models.py", AgentPhase.Backend, "Pydantic models / persistence models.", "python-fastapi"));
        entries.Add(Entry($"{backend}app/services.py", AgentPhase.Backend, "Core business services.", "python-fastapi"));
        entries.Add(Entry($"{backend}app/exceptions.py", AgentPhase.Backend, "HTTP exception handler returning JSON {error, code, message}.", "python-fastapi"));
        entries.Add(Entry($"{backend}tests/test_api.py", AgentPhase.Backend, "pytest API tests for core routes.", "python-fastapi"));
    }

    private static void AppendFlaskBackend(GenerationPlan plan, List<PlannedFileEntry> entries, string backend)
    {
        entries.Add(Entry($"{backend}requirements.txt", AgentPhase.Backend, "Flask and runtime deps.", "python"));
        entries.Add(Entry($"{backend}app.py", AgentPhase.Backend, "Flask application entry.", "python"));
        entries.Add(Entry($"{backend}routes.py", AgentPhase.Backend, "HTTP routes.", "python"));
    }

    private static void AppendJavaBackend(GenerationPlan plan, List<PlannedFileEntry> entries)
    {
        foreach (var legacy in MultiAgentIncrementalManifest.LegacyBackendManifestEntries())
            entries.Add(Entry(legacy.Path, AgentPhase.Backend, legacy.Desc, legacy.Role ?? "java-spring"));
    }

    private static void AppendNodeBackend(GenerationPlan plan, List<PlannedFileEntry> entries, string backend)
    {
        var usesNest = plan.TechStack.Frameworks.Any(f => f.Contains("nestjs", StringComparison.OrdinalIgnoreCase));
        var role = usesNest ? "typescript" : "javascript";
        entries.Add(Entry($"{backend}package.json", AgentPhase.Backend, usesNest ? "NestJS backend package.json." : "Express/Node backend package.json.", role));
        entries.Add(Entry($"{backend}tsconfig.json", AgentPhase.Backend, "TypeScript config.", role));
        entries.Add(Entry($"{backend}src/main.ts", AgentPhase.Backend, usesNest ? "NestJS bootstrap." : "Express server bootstrap.", role));
        entries.Add(Entry($"{backend}src/app.module.ts", AgentPhase.Backend, "NestJS root module (if NestJS).", role));
        entries.Add(Entry($"{backend}src/routes/index.ts", AgentPhase.Backend, "API route registration.", role));
        entries.Add(Entry($"{backend}src/controllers/health.controller.ts", AgentPhase.Backend, "Health endpoint.", role));
        entries.Add(Entry($"{backend}src/services/domain.service.ts", AgentPhase.Backend, $"Domain service for {SummarizeDomain(plan)}.", role));
        entries.Add(Entry($"{backend}src/middleware/errorEnvelope.ts", AgentPhase.Backend, "Express/Nest error middleware returning {error, code, message}.", role));
        entries.Add(Entry($"{backend}src/__tests__/api.test.ts", AgentPhase.Backend, "API integration tests for domain endpoints.", role));
    }

    private static void AppendGoBackend(GenerationPlan plan, List<PlannedFileEntry> entries, string backend)
    {
        entries.Add(Entry($"{backend}go.mod", AgentPhase.Backend, "Go module definition.", "go"));
        entries.Add(Entry($"{backend}main.go", AgentPhase.Backend, "HTTP server entry.", "go"));
        entries.Add(Entry($"{backend}internal/handlers/health.go", AgentPhase.Backend, "Health handler.", "go"));
        entries.Add(Entry($"{backend}internal/handlers/api.go", AgentPhase.Backend, "Domain API handlers.", "go"));
    }

    private static void AppendRustBackend(GenerationPlan plan, List<PlannedFileEntry> entries, string backend)
    {
        entries.Add(Entry($"{backend}Cargo.toml", AgentPhase.Backend, "Rust crate manifest.", "rust"));
        entries.Add(Entry($"{backend}src/main.rs", AgentPhase.Backend, "Axum/Actix server entry.", "rust"));
        entries.Add(Entry($"{backend}src/routes.rs", AgentPhase.Backend, "HTTP routes.", "rust"));
    }

    private static void AppendPhpBackend(GenerationPlan plan, List<PlannedFileEntry> entries, string backend)
    {
        entries.Add(Entry($"{backend}composer.json", AgentPhase.Backend, "Laravel/Symfony composer manifest.", "php"));
        entries.Add(Entry($"{backend}artisan", AgentPhase.Backend, "Laravel CLI entry.", "php"));
        entries.Add(Entry($"{backend}routes/api.php", AgentPhase.Backend, "API routes.", "php"));
        entries.Add(Entry($"{backend}app/Models/Entity.php", AgentPhase.Backend, "Domain entity model.", "php"));
    }

    private static void AppendRubyBackend(GenerationPlan plan, List<PlannedFileEntry> entries, string backend)
    {
        entries.Add(Entry($"{backend}Gemfile", AgentPhase.Backend, "Rails gems.", "ruby"));
        entries.Add(Entry($"{backend}config/routes.rb", AgentPhase.Backend, "Rails routes.", "ruby"));
        entries.Add(Entry($"{backend}app/controllers/api_controller.rb", AgentPhase.Backend, "API controller.", "ruby"));
        entries.Add(Entry($"{backend}app/models/record.rb", AgentPhase.Backend, "ActiveRecord model.", "ruby"));
    }

    private static void AppendDotNetBackend(GenerationPlan plan, List<PlannedFileEntry> entries)
    {
        var name = plan.ApplicationName;
        entries.Add(Entry($"src/{name}.Api/{name}.Api.csproj", AgentPhase.Backend, "ASP.NET Core project file.", "csharp"));
        entries.Add(Entry($"src/{name}.Api/Program.cs", AgentPhase.Backend, "Minimal hosting / middleware pipeline.", "csharp"));
        entries.Add(Entry($"src/{name}.Api/Controllers/HealthController.cs", AgentPhase.Backend, "Health endpoint.", "csharp"));
        entries.Add(Entry($"src/{name}.Api/Controllers/ApiController.cs", AgentPhase.Backend, "Domain API controller.", "csharp"));
        entries.Add(Entry($"src/{name}.Api/Models/Entity.cs", AgentPhase.Backend, "Domain model.", "csharp"));
        entries.Add(Entry($"tests/{name}.Api.Tests/{name}.Api.Tests.csproj", AgentPhase.Backend, "xUnit test project.", "csharp"));
        entries.Add(Entry($"tests/{name}.Api.Tests/ApiEndpointTests.cs", AgentPhase.Backend, "WebApplicationFactory HTTP tests.", "csharp"));
    }

    private static void AppendGenericBackend(GenerationPlan plan, List<PlannedFileEntry> entries, string backend)
    {
        entries.Add(Entry($"{backend}README.md", AgentPhase.Backend, "Backend module overview.", "generic"));
        entries.Add(Entry($"{backend}src/main.txt", AgentPhase.Backend, $"Backend entry stub for {SummarizeDomain(plan)}.", "generic"));
    }

    private static void AppendFrontendEntries(GenerationPlan plan, List<PlannedFileEntry> entries)
    {
        var frontend = StackLayoutHeuristics.FrontendRoot(plan);
        var role = StackLayoutHeuristics.UsesSolidJs(plan) ? "solidjs"
            : StackLayoutHeuristics.UsesVue(plan) ? "typescript"
            : "typescript";
        var ext = StackLayoutHeuristics.UsesSolidJs(plan) ? "tsx" : "tsx";
        var domain = SummarizeDomain(plan);

        entries.Add(Entry($"{frontend}package.json", AgentPhase.Frontend, "Frontend package.json with Vite.", role));
        entries.Add(Entry($"{frontend}tsconfig.json", AgentPhase.Frontend, "TypeScript config.", role));
        entries.Add(Entry($"{frontend}vite.config.ts", AgentPhase.Frontend, "Vite config with API proxy if needed.", role));
        entries.Add(Entry($"{frontend}index.html", AgentPhase.Frontend, "HTML shell.", role));
        entries.Add(Entry($"{frontend}src/index.{ext}", AgentPhase.Frontend, "Frontend bootstrap.", role));
        entries.Add(Entry($"{frontend}src/App.{ext}", AgentPhase.Frontend, $"Root UI shell for {domain}.", role));
        entries.Add(Entry($"{frontend}src/lib/api.ts", AgentPhase.Frontend, "Typed API client for backend REST endpoints.", role));
        entries.Add(Entry($"{frontend}src/components/PrimaryView.{ext}", AgentPhase.Frontend, "Main user workflow UI.", role));
        entries.Add(Entry($"{frontend}src/components/HistoryPanel.{ext}", AgentPhase.Frontend, "History / list panel.", role));
        entries.Add(Entry($"{frontend}src/App.test.{ext}", AgentPhase.Frontend, "Vitest smoke test.", role));
    }

    private static void AppendDatabaseEntries(GenerationPlan plan, List<PlannedFileEntry> entries)
    {
        var kind = StackPlanHeuristics.Classify(plan);
        var backend = StackLayoutHeuristics.BackendRoot(plan);

        if (StackLayoutHeuristics.UsesDjango(plan))
        {
            entries.Add(Entry($"{backend}meals/migrations/0001_initial.py", AgentPhase.Database, "Initial Django migration for Meal model.", "python-django"));
            return;
        }

        if (kind is StackKind.Java or StackKind.JavaReactFullStack)
        {
            foreach (var legacy in MultiAgentIncrementalManifest.LegacyDatabaseManifestEntries())
                entries.Add(Entry(legacy.Path, AgentPhase.Database, legacy.Desc, legacy.Role ?? "java-spring"));
            return;
        }

        if (kind == StackKind.DotNet)
        {
            entries.Add(Entry($"src/{plan.ApplicationName}.Api/Data/AppDbContext.cs", AgentPhase.Database, "EF Core DbContext.", "csharp"));
            return;
        }

        entries.Add(Entry($"{backend}db/schema.sql", AgentPhase.Database, "Initial SQL schema for the planned domain.", "generic-database"));
    }

    private static void AppendDevOpsEntries(GenerationPlan plan, List<PlannedFileEntry> entries)
    {
        var backend = StackLayoutHeuristics.BackendRoot(plan);
        entries.Add(Entry("docker-compose.yml", AgentPhase.DevOps, "Compose services for backend and optional frontend.", "generic-devops"));
        entries.Add(Entry($"{backend}Dockerfile", AgentPhase.DevOps, "Backend container image.", "generic-devops"));
        if (StackLayoutHeuristics.HasSeparatedFrontend(plan))
            entries.Add(Entry($"{StackLayoutHeuristics.FrontendRoot(plan)}Dockerfile", AgentPhase.DevOps, "Frontend container image.", "generic-devops"));
        entries.Add(Entry("README.md", AgentPhase.DevOps, "Run instructions aligned to the planned stack.", "generic-documentation"));
    }

    private static string SummarizeDomain(GenerationPlan plan)
    {
        var text = $"{plan.ApplicationName} {plan.ApplicationDescription}";
        return text.Length <= 120 ? text : text[..120];
    }

    private static PlannedFileEntry Entry(string path, AgentPhase phase, string description, string role) =>
        new(path, phase, description, role);
}
