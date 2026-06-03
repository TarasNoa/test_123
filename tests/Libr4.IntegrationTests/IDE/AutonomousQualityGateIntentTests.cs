using FluentAssertions;
using Libr4.IDE.Application.AutonomousAppGeneration.Services;
using Libr4.IDE.Domain.AutonomousAppGeneration;
using Microsoft.Extensions.Options;
using Xunit;

namespace Libr4.IntegrationTests.IDE;

public sealed class AutonomousQualityGateIntentTests
{
    [Fact]
    public void Generation_ShouldDetectMissingPythonValidationAndErrorEnvelopeContracts()
    {
        var svc = new AutonomousQualityGateService(Options.Create(new AutonomousQualityGateOptions
        {
            GenerationMinScore = 9,
            EnableIntentHeuristics = true,
        }));

        var plan = new GenerationPlan(
            "TaskApi",
            "Generate a task management HTTP API with Python and Flask.",
            new TechStack(
                new[] { "Python" },
                new[] { "Flask" },
                Array.Empty<string>(),
                Array.Empty<string>(),
                "test"),
            new[]
            {
                new GenerationPhase(1, "api", "Implement API", Array.Empty<AgentAssignment>()),
                new GenerationPhase(2, "domain", "Domain", Array.Empty<AgentAssignment>()),
                new GenerationPhase(3, "tests", "Tests", Array.Empty<AgentAssignment>()),
            },
            new[] { "planner", "generator", "tester" },
            "python:3.11",
            new[] { "python -m pip install -r requirements.txt" },
            new[] { "pytest -q" },
            3);

        var files = new List<GeneratedFile>
        {
            new("app.py", "python", "@app.route('/tasks', methods=['POST'])\ndef create():\n    return {'ok': True}\n"),
            new("requirements.txt", "text", "flask==3.0.0\npytest==7.4.0\n"),
            new("tests/test_app.py", "python", "def test_health():\n    assert True\n"),
            new("tasks.py", "python", "class Task: pass\n"),
            new("models.py", "python", "class M: pass\n"),
        };

        var r = svc.EvaluateGeneratedFiles(files, plan);
        r.Reasons.Should().Contain("missing_api_validation_contracts");
        r.Reasons.Should().Contain("missing_error_envelope_contract");
    }

    [Fact]
    public void Generation_ShouldPassPythonContractChecks_WhenValidationAndErrorEnvelopePresent()
    {
        var svc = new AutonomousQualityGateService(Options.Create(new AutonomousQualityGateOptions
        {
            GenerationMinScore = 9,
            EnableIntentHeuristics = true,
        }));

        var plan = new GenerationPlan(
            "TaskApi",
            "Generate a task management HTTP API with Python and FastAPI.",
            new TechStack(
                new[] { "Python" },
                new[] { "FastAPI" },
                Array.Empty<string>(),
                Array.Empty<string>(),
                "test"),
            new[]
            {
                new GenerationPhase(1, "api", "Implement API", Array.Empty<AgentAssignment>()),
                new GenerationPhase(2, "domain", "Domain", Array.Empty<AgentAssignment>()),
                new GenerationPhase(3, "tests", "Tests", Array.Empty<AgentAssignment>()),
            },
            new[] { "planner", "generator", "tester" },
            "python:3.11",
            new[] { "python -m pip install -r requirements.txt" },
            new[] { "pytest -q" },
            3);

        var files = new List<GeneratedFile>
        {
            new("main.py", "python",
                "from pydantic import BaseModel, Field\n" +
                "class TaskCreateRequest(BaseModel):\n    title: str = Field(..., min_length=1, max_length=200)\n" +
                "def error_response(code, message):\n    return {'error': {'code': code, 'message': message}}\n"),
            new("requirements.txt", "text", "fastapi==0.110.0\npytest==7.4.0\n"),
            new("tests/test_main.py", "python", "def test_health():\n    assert True\n"),
            new("tasks.py", "python", "class Task: pass\n"),
            new("models.py", "python", "class M: pass\n"),
        };

        var r = svc.EvaluateGeneratedFiles(files, plan);
        r.Reasons.Should().NotContain("missing_api_validation_contracts");
        r.Reasons.Should().NotContain("missing_error_envelope_contract");
    }

    [Fact]
    public void Generation_ShouldSurfaceAuthIntentGap_WhenPlanMentionsJwtButCodeDoesNot()
    {
        var svc = new AutonomousQualityGateService(Options.Create(new AutonomousQualityGateOptions
        {
            GenerationMinScore = 9,
            EnableIntentHeuristics = true,
        }));

        var plan = new GenerationPlan(
            "AuthSample",
            "REST API issuing JWT access tokens for user login.",
            new TechStack(
                new[] { "C#" },
                new[] { "ASP.NET Core" },
                Array.Empty<string>(),
                Array.Empty<string>(),
                "test"),
            new[]
            {
                new GenerationPhase(1, "api", "Implement HTTP API", Array.Empty<AgentAssignment>()),
                new GenerationPhase(2, "domain", "Domain model", Array.Empty<AgentAssignment>()),
                new GenerationPhase(3, "tests", "Tests", Array.Empty<AgentAssignment>()),
            },
            new[] { "planner", "generator", "tester" },
            "mcr.microsoft.com/dotnet/sdk:8.0",
            new[] { "dotnet build" },
            new[] { "dotnet test" },
            3);

        var files = new List<GeneratedFile>
        {
            new("App.sln", "text", "x"),
            new("src/App/App.csproj", "xml", "<Project Sdk=\"Microsoft.NET.Sdk.Web\" />"),
            new("src/App/Program.cs", "csharp",
                "var b=WebApplication.CreateBuilder(); var a=b.Build(); a.MapGet(\"/\",()=>\"ok\"); a.Run();"),
            new("src/App/Controllers/HealthController.cs", "csharp", "class HealthController{}"),
            new("src/App/Services/HealthService.cs", "csharp", "class HealthService{}"),
            new("src/App/Models/Item.cs", "csharp", "class Item{}"),
            new("tests/App.Tests/App.Tests.csproj", "xml", "<Project Sdk=\"Microsoft.NET.Sdk\" />"),
            new("tests/App.Tests/UnitTest1.cs", "csharp", "public class UnitTest1{}"),
        };

        var r = svc.EvaluateGeneratedFiles(files, plan);
        r.Passed.Should().BeFalse();
        r.Reasons.Should().Contain("intent_auth_not_reflected_in_code");
    }

    [Fact]
    public void Generation_ShouldPassIntentChecks_WhenHeuristicsDisabled()
    {
        var svc = new AutonomousQualityGateService(Options.Create(new AutonomousQualityGateOptions
        {
            GenerationMinScore = 9,
            EnableIntentHeuristics = false,
        }));

        var plan = new GenerationPlan(
            "AuthSample",
            "REST API issuing JWT access tokens for user login.",
            new TechStack(
                new[] { "C#" },
                new[] { "ASP.NET Core" },
                Array.Empty<string>(),
                Array.Empty<string>(),
                "test"),
            new[]
            {
                new GenerationPhase(1, "api", "Implement HTTP API", Array.Empty<AgentAssignment>()),
                new GenerationPhase(2, "domain", "Domain model", Array.Empty<AgentAssignment>()),
                new GenerationPhase(3, "tests", "Tests", Array.Empty<AgentAssignment>()),
            },
            new[] { "planner", "generator", "tester" },
            "mcr.microsoft.com/dotnet/sdk:8.0",
            new[] { "dotnet build" },
            new[] { "dotnet test" },
            3);

        var files = new List<GeneratedFile>
        {
            new("App.sln", "text", "x"),
            new("src/App/App.csproj", "xml", "<Project Sdk=\"Microsoft.NET.Sdk.Web\" />"),
            new("src/App/Program.cs", "csharp",
                "var b=WebApplication.CreateBuilder(); var a=b.Build(); a.MapGet(\"/\",()=>\"ok\"); a.Run();"),
            new("src/App/Controllers/HealthController.cs", "csharp", "class HealthController{}"),
            new("src/App/Services/HealthService.cs", "csharp", "class HealthService{}"),
            new("src/App/Models/Item.cs", "csharp", "class Item{}"),
            new("tests/App.Tests/App.Tests.csproj", "xml", "<Project Sdk=\"Microsoft.NET.Sdk\" />"),
            new("tests/App.Tests/UnitTest1.cs", "csharp", "public class UnitTest1{}"),
        };

        var r = svc.EvaluateGeneratedFiles(files, plan);
        r.Passed.Should().BeTrue();
    }

    [Fact]
    public void Generation_ShouldFailComplexFastApiIntent_WhenRequiredArtifactsMissing()
    {
        var svc = new AutonomousQualityGateService(Options.Create(new AutonomousQualityGateOptions
        {
            GenerationMinScore = 9,
            EnableIntentHeuristics = true,
        }));

        var plan = new GenerationPlan(
            "BillingApi",
            "Build production-ready FastAPI service with PostgreSQL, Redis, Celery workers, Stripe webhooks, Docker Compose and CI pipeline.",
            new TechStack(
                new[] { "Python" },
                new[] { "FastAPI" },
                new[] { "PostgreSQL", "Redis" },
                new[] { "Docker", "GitHub Actions" },
                "complex stack"),
            new[]
            {
                new GenerationPhase(1, "api", "Implement API", Array.Empty<AgentAssignment>()),
                new GenerationPhase(2, "worker", "Worker and queues", Array.Empty<AgentAssignment>()),
                new GenerationPhase(3, "infra", "Infra + CI", Array.Empty<AgentAssignment>()),
            },
            new[] { "planner", "generator", "tester" },
            "python:3.11",
            new[] { "pip install -r requirements.txt" },
            new[] { "pytest -q" },
            3);

        var files = new List<GeneratedFile>
        {
            new("src/main.py", "python", "from fastapi import FastAPI\napp=FastAPI()\n"),
            new("src/requirements.txt", "text", "fastapi\nsqlalchemy\n"),
            new("tests/test_main.py", "python", "def test_ok():\n    assert True\n"),
            new("src/models.py", "python", "class Model: pass\n"),
            new("Dockerfile", "text", "FROM python:3.11\n"),
        };

        var r = svc.EvaluateGeneratedFiles(files, plan);
        r.Passed.Should().BeFalse();
        r.Reasons.Should().Contain("missing_stack_artifact:docker_compose");
        r.Reasons.Should().Contain("missing_stack_artifact:alembic_migrations");
        r.Reasons.Should().Contain("missing_stack_artifact:worker_lane");
        r.Reasons.Should().Contain("missing_stack_artifact:ci_pipeline");
    }

    [Fact]
    public void Generation_ShouldPassComplexFastApiIntent_WhenRequiredArtifactsPresent()
    {
        var svc = new AutonomousQualityGateService(Options.Create(new AutonomousQualityGateOptions
        {
            GenerationMinScore = 9,
            EnableIntentHeuristics = true,
        }));

        var plan = new GenerationPlan(
            "BillingApi",
            "Build production-ready FastAPI service with PostgreSQL, Redis, Celery workers, Stripe webhooks, Docker Compose and CI pipeline.",
            new TechStack(
                new[] { "Python" },
                new[] { "FastAPI" },
                new[] { "PostgreSQL", "Redis" },
                new[] { "Docker", "GitHub Actions" },
                "complex stack"),
            new[]
            {
                new GenerationPhase(1, "api", "Implement API", Array.Empty<AgentAssignment>()),
                new GenerationPhase(2, "worker", "Worker and queues", Array.Empty<AgentAssignment>()),
                new GenerationPhase(3, "infra", "Infra + CI", Array.Empty<AgentAssignment>()),
            },
            new[] { "planner", "generator", "tester" },
            "python:3.11",
            new[] { "pip install -r requirements.txt" },
            new[] { "pytest -q" },
            3);

        var files = new List<GeneratedFile>
        {
            new("src/main.py", "python",
                "from fastapi import FastAPI\n" +
                "from pydantic import BaseModel, Field\n" +
                "app=FastAPI()\n" +
                "class Payload(BaseModel):\n    name: str = Field(..., min_length=1)\n" +
                "def error_response(code, message):\n    return {'error': {'code': code, 'message': message}}\n" +
                "# stripe webhook handler\n"),
            new("src/worker.py", "python", "from celery import Celery\ncelery = Celery('billing', broker='redis://redis:6379/0')\n"),
            new("alembic/env.py", "python", "from sqlalchemy import engine_from_config\n"),
            new("docker-compose.yml", "yaml", "services:\n  api:\n  db:\n  redis:\n  worker:\n"),
            new(".github/workflows/ci.yml", "yaml", "name: CI\non: [push]\njobs:\n  test:\n"),
            new("src/settings.py", "python", "DATABASE_URL='postgresql://app:pwd@db/app'\nREDIS_URL='redis://redis:6379/0'\n"),
            new("src/requirements.txt", "text", "fastapi\nsqlalchemy\nredis\ncelery\npsycopg[binary]\n"),
            new("tests/test_main.py", "python", "def test_ok():\n    assert True\n"),
        };

        var r = svc.EvaluateGeneratedFiles(files, plan);
        r.Passed.Should().BeTrue();
        r.Reasons.Should().NotContain(x => x.StartsWith("missing_stack_artifact:", StringComparison.Ordinal));
    }

    [Fact]
    public void Generation_ShouldFailPythonApiRuntimeContract_WhenDockerUsesPythonMain()
    {
        var svc = new AutonomousQualityGateService(Options.Create(new AutonomousQualityGateOptions
        {
            GenerationMinScore = 9,
            EnableIntentHeuristics = true,
        }));

        var plan = new GenerationPlan(
            "FastApiApp",
            "Generate FastAPI HTTP API with authentication and health endpoints.",
            new TechStack(
                new[] { "Python" },
                new[] { "FastAPI" },
                Array.Empty<string>(),
                Array.Empty<string>(),
                "api runtime contract"),
            new[]
            {
                new GenerationPhase(1, "api", "Build HTTP API", Array.Empty<AgentAssignment>()),
                new GenerationPhase(2, "tests", "Build tests", Array.Empty<AgentAssignment>()),
                new GenerationPhase(3, "docker", "Build container", Array.Empty<AgentAssignment>()),
            },
            new[] { "planner", "generator", "tester" },
            "python:3.11",
            new[] { "pip install -r requirements.txt" },
            new[] { "pytest -q" },
            3);

        var files = new List<GeneratedFile>
        {
            new("src/main.py", "python", "from fastapi import FastAPI\napp = FastAPI()\n"),
            new("src/requirements.txt", "text", "fastapi==0.110.0\nuvicorn==0.29.0\n"),
            new("tests/test_main.py", "python", "def test_ok():\n    assert True\n"),
            new("Dockerfile", "text", "FROM python:3.11\nWORKDIR /app\nCOPY src/ .\nCMD [\"python\", \"main.py\"]\n"),
            new("src/models.py", "python", "class M: pass\n"),
        };

        var r = svc.EvaluateGeneratedFiles(files, plan);
        r.Passed.Should().BeFalse();
        r.Reasons.Should().Contain("missing_api_runtime_contract:docker_asgi_entrypoint");
    }

    [Fact]
    public void Generation_ShouldPassPythonApiRuntimeContract_WhenDockerUsesUvicorn()
    {
        var svc = new AutonomousQualityGateService(Options.Create(new AutonomousQualityGateOptions
        {
            GenerationMinScore = 9,
            EnableIntentHeuristics = true,
        }));

        var plan = new GenerationPlan(
            "FastApiApp",
            "Generate FastAPI HTTP API with authentication and health endpoints.",
            new TechStack(
                new[] { "Python" },
                new[] { "FastAPI" },
                Array.Empty<string>(),
                Array.Empty<string>(),
                "api runtime contract"),
            new[]
            {
                new GenerationPhase(1, "api", "Build HTTP API", Array.Empty<AgentAssignment>()),
                new GenerationPhase(2, "tests", "Build tests", Array.Empty<AgentAssignment>()),
                new GenerationPhase(3, "docker", "Build container", Array.Empty<AgentAssignment>()),
            },
            new[] { "planner", "generator", "tester" },
            "python:3.11",
            new[] { "pip install -r requirements.txt" },
            new[] { "pytest -q" },
            3);

        var files = new List<GeneratedFile>
        {
            new("src/main.py", "python", "from fastapi import FastAPI\napp = FastAPI()\n"),
            new("src/requirements.txt", "text", "fastapi==0.110.0\nuvicorn==0.29.0\n"),
            new("tests/test_main.py", "python", "def test_ok():\n    assert True\n"),
            new("Dockerfile", "text", "FROM python:3.11\nWORKDIR /app\nCOPY src/ .\nCMD [\"uvicorn\", \"main:app\", \"--host\", \"0.0.0.0\", \"--port\", \"8000\"]\n"),
            new("src/models.py", "python", "class M: pass\n"),
        };

        var r = svc.EvaluateGeneratedFiles(files, plan);
        r.Reasons.Should().NotContain("missing_api_runtime_contract:docker_asgi_entrypoint");
    }

    [Fact]
    public void Generation_ShouldDetectMissingStrictErrorEnvelope_WhenOnlyErrorResponseHelperExists()
    {
        var svc = new AutonomousQualityGateService(Options.Create(new AutonomousQualityGateOptions
        {
            GenerationMinScore = 9,
            EnableIntentHeuristics = true,
        }));

        var plan = new GenerationPlan(
            "TaskApi",
            "Generate a task management HTTP API with Python and FastAPI.",
            new TechStack(
                new[] { "Python" },
                new[] { "FastAPI" },
                Array.Empty<string>(),
                Array.Empty<string>(),
                "test"),
            new[]
            {
                new GenerationPhase(1, "api", "Implement API", Array.Empty<AgentAssignment>()),
                new GenerationPhase(2, "domain", "Domain", Array.Empty<AgentAssignment>()),
                new GenerationPhase(3, "tests", "Tests", Array.Empty<AgentAssignment>()),
            },
            new[] { "planner", "generator", "tester" },
            "python:3.11",
            new[] { "python -m pip install -r requirements.txt" },
            new[] { "pytest -q" },
            3);

        var files = new List<GeneratedFile>
        {
            new("main.py", "python",
                "from pydantic import BaseModel, Field\n" +
                "class TaskCreateRequest(BaseModel):\n    title: str = Field(..., min_length=1, max_length=200)\n" +
                "def error_response(msg):\n    return {'error': msg}\n"),
            new("requirements.txt", "text", "fastapi==0.110.0\npytest==7.4.0\n"),
            new("tests/test_main.py", "python", "def test_health():\n    assert True\n"),
            new("tasks.py", "python", "class Task: pass\n"),
            new("models.py", "python", "class M: pass\n"),
        };

        var r = svc.EvaluateGeneratedFiles(files, plan);
        r.Reasons.Should().Contain("missing_error_envelope_contract");
    }

    [Fact]
    public void FixGate_ShouldPass_WhenOnlyNonActionableErrorsRemainWithoutPatches()
    {
        var svc = new AutonomousQualityGateService(Options.Create(new AutonomousQualityGateOptions
        {
            FixMinScore = 9
        }));

        var errors = new List<ErrorReport>
        {
            new("non_actionable_error", "No actionable remediation required", "none")
        };
        var patches = new List<GeneratedFile>();

        var r = svc.EvaluateFixProgress(errors, patches);
        r.Passed.Should().BeTrue();
        r.Score.Should().Be(10);
        r.Reasons.Should().Contain("non_actionable_errors_only");
    }

    [Fact]
    public void Generation_ShouldFailRepoBootstrapIntent_WhenNoAdaptationEvidencePresent()
    {
        var svc = new AutonomousQualityGateService(Options.Create(new AutonomousQualityGateOptions
        {
            GenerationMinScore = 9,
            EnableIntentHeuristics = true,
        }));

        var plan = new GenerationPlan(
            "GeneratedApp",
            "Use Obscura GitHub repo bootstrap to adapt open-source Kanban app with auth.",
            new TechStack(
                new[] { "C#" },
                new[] { "ASP.NET Core" },
                new[] { "PostgreSQL" },
                Array.Empty<string>(),
                "repo bootstrap"),
            new[]
            {
                new GenerationPhase(1, "bootstrap", "Discover and adapt GitHub repository", Array.Empty<AgentAssignment>()),
                new GenerationPhase(2, "api", "Implement API", Array.Empty<AgentAssignment>()),
                new GenerationPhase(3, "tests", "Validate behavior", Array.Empty<AgentAssignment>()),
            },
            new[] { "planner", "generator", "tester" },
            "mcr.microsoft.com/dotnet/sdk:8.0",
            new[] { "dotnet build" },
            new[] { "dotnet test" },
            4);

        var files = new List<GeneratedFile>
        {
            new("GeneratedApp.sln", "text", "x"),
            new("src/GeneratedApp/GeneratedApp.csproj", "xml", "<Project Sdk=\"Microsoft.NET.Sdk.Web\" />"),
            new("src/GeneratedApp/Program.cs", "csharp", "var app=WebApplication.CreateBuilder().Build();app.MapGet(\"/\",()=>\"Hello from GeneratedApp\");app.Run();"),
            new("src/GeneratedApp/Controllers/HealthController.cs", "csharp", "class HealthController{}"),
            new("src/GeneratedApp/Services/HealthService.cs", "csharp", "class HealthService{}"),
            new("src/GeneratedApp/Models/HealthItem.cs", "csharp", "class HealthItem{}"),
            new("tests/GeneratedApp.Tests/GeneratedApp.Tests.csproj", "xml", "<Project Sdk=\"Microsoft.NET.Sdk\" />"),
            new("tests/GeneratedApp.Tests/HealthTests.cs", "csharp", "public class HealthTests { }"),
        };

        var r = svc.EvaluateGeneratedFiles(files, plan);
        r.Passed.Should().BeFalse();
        r.Reasons.Should().Contain("repo_bootstrap_not_reflected_in_code");
        r.Reasons.Should().Contain("generic_template_output_detected");
        r.Reasons.Should().Contain("business_tests_missing_or_superficial");
    }

    [Fact]
    public void Generation_ShouldFailKanbanIntent_WhenBoardFlowIsMissing()
    {
        var svc = new AutonomousQualityGateService(Options.Create(new AutonomousQualityGateOptions
        {
            GenerationMinScore = 9,
            EnableIntentHeuristics = true,
        }));

        var plan = new GenerationPlan(
            "TaskBoardApi",
            "Build kanban board with auth and tasks.",
            new TechStack(
                new[] { "C#" },
                new[] { "ASP.NET Core" },
                new[] { "PostgreSQL" },
                Array.Empty<string>(),
                "kanban"),
            new[]
            {
                new GenerationPhase(1, "api", "Implement HTTP API", Array.Empty<AgentAssignment>()),
                new GenerationPhase(2, "domain", "Implement task domain", Array.Empty<AgentAssignment>()),
                new GenerationPhase(3, "tests", "Implement tests", Array.Empty<AgentAssignment>()),
            },
            new[] { "planner", "generator", "tester" },
            "mcr.microsoft.com/dotnet/sdk:8.0",
            new[] { "dotnet build" },
            new[] { "dotnet test" },
            3);

        var files = new List<GeneratedFile>
        {
            new("TaskBoardApi.sln", "text", "x"),
            new("src/TaskBoardApi/TaskBoardApi.csproj", "xml", "<Project Sdk=\"Microsoft.NET.Sdk.Web\" />"),
            new("src/TaskBoardApi/Program.cs", "csharp", "var b=WebApplication.CreateBuilder(); b.Services.AddAuthentication(); var app=b.Build(); app.UseAuthentication(); app.MapGet(\"/tasks\",()=>Array.Empty<string>()); app.Run();"),
            new("src/TaskBoardApi/Controllers/TasksController.cs", "csharp", "class TasksController{}"),
            new("src/TaskBoardApi/Services/TaskService.cs", "csharp", "class TaskService{}"),
            new("src/TaskBoardApi/Data/AppDbContext.cs", "csharp", "class AppDbContext{}"),
            new("tests/TaskBoardApi.Tests/TaskBoardApi.Tests.csproj", "xml", "<Project Sdk=\"Microsoft.NET.Sdk\" />"),
            new("tests/TaskBoardApi.Tests/HealthTests.cs", "csharp", "public class HealthTests { }"),
        };

        var r = svc.EvaluateGeneratedFiles(files, plan);
        r.Passed.Should().BeFalse();
        r.Reasons.Should().Contain("intent_kanban_not_reflected_in_code");
    }

    [Fact]
    public void Generation_ShouldPassRepoBootstrapIntent_WhenBootstrapEvidenceAndBusinessFeaturesPresent()
    {
        var svc = new AutonomousQualityGateService(Options.Create(new AutonomousQualityGateOptions
        {
            GenerationMinScore = 9,
            EnableIntentHeuristics = true,
        }));

        var plan = new GenerationPlan(
            "KanbanAuthApi",
            "Use Obscura GitHub repo bootstrap with JWT auth and kanban board.\n[[REPO_BOOTSTRAP_REQUIRED]]",
            new TechStack(
                new[] { "C#" },
                new[] { "ASP.NET Core" },
                new[] { "PostgreSQL" },
                Array.Empty<string>(),
                "repo bootstrap"),
            new[]
            {
                new GenerationPhase(1, "Repo bootstrap & adaptation", "Adapt upstream", Array.Empty<AgentAssignment>()),
                new GenerationPhase(2, "Implement core", "Auth and kanban", Array.Empty<AgentAssignment>()),
                new GenerationPhase(3, "Tests", "Business tests", Array.Empty<AgentAssignment>()),
            },
            new[] { "planner", "generator", "tester" },
            "mcr.microsoft.com/dotnet/sdk:8.0",
            new[] { "dotnet build" },
            new[] { "dotnet test" },
            4);

        var files = new List<GeneratedFile>
        {
            new("KanbanAuthApi.sln", "text", "x"),
            new("src/KanbanAuthApi/KanbanAuthApi.csproj", "xml", "<Project Sdk=\"Microsoft.NET.Sdk.Web\" />"),
            new("src/KanbanAuthApi/Program.cs", "csharp",
                "builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(); app.UseAuthentication(); app.UseAuthorization(); app.MapControllers();"),
            new("src/KanbanAuthApi/Controllers/AuthController.cs", "csharp",
                "[ApiController][Route(\"api/auth\")] public class AuthController { [HttpPost(\"token\")] public IActionResult IssueToken() { var t = new JwtSecurityToken(); return Ok(t); } }"),
            new("src/KanbanAuthApi/Controllers/KanbanController.cs", "csharp",
                "[ApiController][Route(\"api/kanban\")][Authorize] public class KanbanController { [HttpGet(\"board\")] public object GetBoard() => new { columns = new[] { \"backlog\", \"in_progress\", \"done\" } }; }"),
            new("src/KanbanAuthApi/Data/AppDbContext.cs", "csharp", "class AppDbContext {}"),
            new("src/KanbanAuthApi/Services/KanbanService.cs", "csharp", "namespace KanbanAuthApi.Services; public sealed class KanbanService {}"),
            new("BOOTSTRAP_EVIDENCE.md", "markdown", "repository_url: https://github.com/example/repo license: mit adaptation: upstream"),
            new("tests/KanbanAuthApi.Tests/KanbanAuthApi.Tests.csproj", "xml", "<Project Sdk=\"Microsoft.NET.Sdk\" />"),
            new("tests/KanbanAuthApi.Tests/KanbanAuthFlowTests.cs", "csharp",
                "public class KanbanAuthFlowTests { [Fact] public void AuthTokenAndKanbanBoard() { Assert.Contains(\"kanban\", \"kanban\"); } }"),
        };

        var r = svc.EvaluateGeneratedFiles(files, plan);
        r.Passed.Should().BeTrue($"score={r.Score}; reasons={string.Join(',', r.Reasons)}");
        r.Reasons.Should().NotContain("repo_bootstrap_not_reflected_in_code");
        r.Reasons.Should().NotContain("intent_auth_not_reflected_in_code");
        r.Reasons.Should().NotContain("intent_kanban_not_reflected_in_code");
        r.Reasons.Should().NotContain("business_tests_missing_or_superficial");
    }

    [Fact]
    public void Generation_ShouldAcceptWebApplicationFactoryHttpTests_ForRepoBootstrap()
    {
        var svc = new AutonomousQualityGateService(Options.Create(new AutonomousQualityGateOptions
        {
            GenerationMinScore = 9,
            EnableIntentHeuristics = true,
        }));

        var plan = new GenerationPlan(
            "GeneratedApp",
            "[[REPO_BOOTSTRAP_REQUIRED]] adapt upstream with JWT auth and kanban",
            new TechStack(
                new[] { "C#" },
                new[] { "ASP.NET Core" },
                Array.Empty<string>(),
                Array.Empty<string>(),
                "repo bootstrap"),
            Array.Empty<GenerationPhase>(),
            Array.Empty<string>(),
            "mcr.microsoft.com/dotnet/sdk:8.0",
            new[] { "dotnet build" },
            new[] { "dotnet test" },
            4);

        var files = new List<GeneratedFile>
        {
            new("BOOTSTRAP_EVIDENCE.md", "markdown", "repository_url: https://github.com/example/repo license: mit"),
            new("upstream/README.md", "markdown", "adapted from upstream kanban"),
            new("src/GeneratedApp.Api/Controllers/AuthController.cs", "csharp", "JwtSecurityToken token; [Route(\"api/auth\")]"),
            new("src/GeneratedApp.Api/Controllers/KanbanController.cs", "csharp", "[Authorize][Route(\"api/kanban\")] board columns tasks"),
            new("tests/GeneratedApp.Api.Tests/KanbanAuthHttpTests.cs", "csharp",
                """
                using Microsoft.AspNetCore.Mvc.Testing;
                public sealed class KanbanAuthHttpTests : IClassFixture<WebApplicationFactory<Program>>
                {
                    private readonly HttpClient _client;
                    [Fact] public async Task Token() => await _client.PostAsync("/api/auth/token", null);
                    [Fact] public async Task Board() => await _client.GetAsync("/api/kanban/board");
                }
                """),
        };

        var r = svc.EvaluateGeneratedFiles(files, plan);
        r.Reasons.Should().NotContain("business_tests_missing_or_superficial");
    }
}
