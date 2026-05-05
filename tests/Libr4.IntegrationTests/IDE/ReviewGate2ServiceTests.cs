using FluentAssertions;
using Libr4.IDE.Application.AutonomousAppGeneration.Services;
using Libr4.IDE.Domain.AutonomousAppGeneration;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Libr4.IntegrationTests.IDE;

public sealed class ReviewGate2ServiceTests
{
    [Fact]
    public void EvaluateStaticChecks_ShouldDetectSecurityIssues()
    {
        var service = new ReviewGate2Service(NullLogger<ReviewGate2Service>.Instance);

        var files = new[]
        {
            new GeneratedFile("src/config.cs", "csharp", "var key = \"AKIA1234567890ABCDEF\";"),
            new GeneratedFile("src/safe.cs", "csharp", "var name = \"John\";"),
        };

        var checks = service.EvaluateStaticChecks(files, BuildTestPlan());

        var securityCheck = checks.First(c => c.CheckName == "security_scan");
        securityCheck.Passed.Should().BeFalse();
        securityCheck.IssueCount.Should().Be(1);
        securityCheck.Issues.Should().Contain(i => i.Contains("aws_access_key_pattern"));
    }

    [Fact]
    public void EvaluateStaticChecks_ShouldPassWhenNoSecurityIssues()
    {
        var service = new ReviewGate2Service(NullLogger<ReviewGate2Service>.Instance);

        var files = new[]
        {
            new GeneratedFile("src/clean.cs", "csharp", "public class MyClass { }"),
        };

        var checks = service.EvaluateStaticChecks(files, BuildTestPlan());

        var securityCheck = checks.First(c => c.CheckName == "security_scan");
        securityCheck.Passed.Should().BeTrue();
        securityCheck.IssueCount.Should().Be(0);
    }

    [Fact]
    public void EvaluateStaticChecks_ShouldDetectTestFiles()
    {
        var service = new ReviewGate2Service(NullLogger<ReviewGate2Service>.Instance);

        var files = new[]
        {
            new GeneratedFile("src/MyClass.cs", "csharp", "public class MyClass { }"),
            new GeneratedFile("tests/MyClassTests.cs", "csharp", "public class MyClassTests { }"),
        };

        var checks = service.EvaluateStaticChecks(files, BuildTestPlan());

        var testCheck = checks.First(c => c.CheckName == "test_coverage");
        testCheck.Passed.Should().BeTrue();
    }

    [Fact]
    public void EvaluateArchitectureChecklist_ShouldValidateSeparationOfConcerns()
    {
        var service = new ReviewGate2Service(NullLogger<ReviewGate2Service>.Instance);

        var files = new[]
        {
            new GeneratedFile("src/controllers/UserController.cs", "csharp", "public class UserController { }"),
            new GeneratedFile("src/services/UserService.cs", "csharp", "public class UserService { }"),
            new GeneratedFile("src/models/User.cs", "csharp", "public class User { }"),
        };

        var checklist = service.EvaluateArchitectureChecklist(files, BuildTestPlan());

        var socItem = checklist.First(c => c.ItemId == "separation_of_concerns");
        socItem.Satisfied.Should().BeTrue();
    }

    [Fact]
    public void EvaluateArchitectureChecklist_ShouldValidateConfigExternalization()
    {
        var service = new ReviewGate2Service(NullLogger<ReviewGate2Service>.Instance);

        var files = new[]
        {
            new GeneratedFile("src/Program.cs", "csharp", "var app = builder.Build();"),
            new GeneratedFile("appsettings.json", "json", "{ \"ConnectionString\": \"...\" }"),
        };

        var checklist = service.EvaluateArchitectureChecklist(files, BuildTestPlan());

        var configItem = checklist.First(c => c.ItemId == "config_externalization");
        configItem.Satisfied.Should().BeTrue();
    }

    [Fact]
    public void EvaluateArchitectureChecklist_ConfigExternalization_AcceptsEnvExample()
    {
        var service = new ReviewGate2Service(NullLogger<ReviewGate2Service>.Instance);

        var files = new[]
        {
            new GeneratedFile("src/main.py", "python", "print('hello')"),
            new GeneratedFile(".env.example", "text", "DATABASE_URL=postgresql://localhost/mydb"),
        };

        var checklist = service.EvaluateArchitectureChecklist(files, BuildTestPlan());

        var configItem = checklist.First(c => c.ItemId == "config_externalization");
        configItem.Satisfied.Should().BeTrue();
    }

    [Fact]
    public void EvaluateArchitectureChecklist_ConfigExternalization_AcceptsSettingsPy()
    {
        var service = new ReviewGate2Service(NullLogger<ReviewGate2Service>.Instance);

        var files = new[]
        {
            new GeneratedFile("src/main.py", "python", "print('hello')"),
            new GeneratedFile("src/settings.py", "python", "DATABASE_URL = os.getenv('DATABASE_URL')"),
        };

        var checklist = service.EvaluateArchitectureChecklist(files, BuildTestPlan());

        var configItem = checklist.First(c => c.ItemId == "config_externalization");
        configItem.Satisfied.Should().BeTrue();
    }

    [Fact]
    public void EvaluateArchitectureChecklist_ConfigExternalization_AcceptsConfigPy()
    {
        var service = new ReviewGate2Service(NullLogger<ReviewGate2Service>.Instance);

        var files = new[]
        {
            new GeneratedFile("src/main.py", "python", "print('hello')"),
            new GeneratedFile("src/config.py", "python", "class Config: DATABASE_URL = os.getenv('DB')"),
        };

        var checklist = service.EvaluateArchitectureChecklist(files, BuildTestPlan());

        var configItem = checklist.First(c => c.ItemId == "config_externalization");
        configItem.Satisfied.Should().BeTrue();
    }

    [Fact]
    public void EvaluateArchitectureChecklist_ConfigExternalization_AcceptsConfigDirectory()
    {
        var service = new ReviewGate2Service(NullLogger<ReviewGate2Service>.Instance);

        var files = new[]
        {
            new GeneratedFile("src/main.py", "python", "print('hello')"),
            new GeneratedFile("config/settings.yaml", "yaml", "database: localhost"),
        };

        var checklist = service.EvaluateArchitectureChecklist(files, BuildTestPlan());

        var configItem = checklist.First(c => c.ItemId == "config_externalization");
        configItem.Satisfied.Should().BeTrue();
    }

    [Fact]
    public void EvaluateArchitectureChecklist_ConfigExternalization_AcceptsPydanticBaseSettings()
    {
        var service = new ReviewGate2Service(NullLogger<ReviewGate2Service>.Instance);

        var files = new[]
        {
            new GeneratedFile("src/main.py", "python", "print('hello')"),
            new GeneratedFile("src/config.py", "python", "class Settings(BaseSettings): database_url: str"),
        };

        var checklist = service.EvaluateArchitectureChecklist(files, BuildTestPlan());

        var configItem = checklist.First(c => c.ItemId == "config_externalization");
        configItem.Satisfied.Should().BeTrue();
    }

    [Fact]
    public void EvaluateArchitectureChecklist_ConfigExternalization_AcceptsOsGetenv()
    {
        var service = new ReviewGate2Service(NullLogger<ReviewGate2Service>.Instance);

        var files = new[]
        {
            new GeneratedFile("src/main.py", "python", "db_url = os.getenv('DATABASE_URL')"),
        };

        var checklist = service.EvaluateArchitectureChecklist(files, BuildTestPlan());

        var configItem = checklist.First(c => c.ItemId == "config_externalization");
        configItem.Satisfied.Should().BeTrue();
    }

    [Fact]
    public void EvaluateArchitectureChecklist_ConfigExternalization_AcceptsEnvironGet()
    {
        var service = new ReviewGate2Service(NullLogger<ReviewGate2Service>.Instance);

        var files = new[]
        {
            new GeneratedFile("src/main.py", "python", "db_url = os.environ.get('DATABASE_URL')"),
        };

        var checklist = service.EvaluateArchitectureChecklist(files, BuildTestPlan());

        var configItem = checklist.First(c => c.ItemId == "config_externalization");
        configItem.Satisfied.Should().BeTrue();
    }

    [Fact]
    public void EvaluateArchitectureChecklist_ConfigExternalization_FailsWhenNoConfig()
    {
        var service = new ReviewGate2Service(NullLogger<ReviewGate2Service>.Instance);

        var files = new[]
        {
            new GeneratedFile("src/main.py", "python", "db_url = 'localhost'"),
        };

        var checklist = service.EvaluateArchitectureChecklist(files, BuildTestPlan());

        var configItem = checklist.First(c => c.ItemId == "config_externalization");
        configItem.Satisfied.Should().BeFalse();
    }

    [Fact]
    public void EvaluateArchitectureChecklist_ShouldValidateDocumentation()
    {
        var service = new ReviewGate2Service(NullLogger<ReviewGate2Service>.Instance);

        var files = new[]
        {
            new GeneratedFile("src/MyClass.cs", "csharp", "/// <summary>My class</summary>\npublic class MyClass { }"),
            new GeneratedFile("README.md", "markdown", "# My Project"),
        };

        var checklist = service.EvaluateArchitectureChecklist(files, BuildTestPlan());

        var docItem = checklist.First(c => c.ItemId == "documentation");
        docItem.Satisfied.Should().BeTrue();
    }

    [Fact]
    public void EvaluateArchitectureChecklist_ShouldValidateErrorHandling()
    {
        var service = new ReviewGate2Service(NullLogger<ReviewGate2Service>.Instance);

        var files = new[]
        {
            new GeneratedFile("src/MyClass.cs", "csharp", "try { } catch (Exception ex) { }"),
        };

        var checklist = service.EvaluateArchitectureChecklist(files, BuildTestPlan());

        var errorItem = checklist.First(c => c.ItemId == "error_handling");
        errorItem.Satisfied.Should().BeTrue();
    }

    [Fact]
    public void EvaluateArchitectureChecklist_ShouldRequireErrorEnvelopeForApiPlans()
    {
        var service = new ReviewGate2Service(NullLogger<ReviewGate2Service>.Instance);
        var plan = new GenerationPlan(
            applicationName: "TaskApi",
            applicationDescription: "HTTP API for tasks",
            techStack: new TechStack(
                languages: new[] { "Python" },
                frameworks: new[] { "FastAPI" },
                databases: Array.Empty<string>(),
                infrastructure: Array.Empty<string>(),
                rationale: "test"),
            phases: new[] { new GenerationPhase(1, "api", "Build API", Array.Empty<AgentAssignment>()) },
            requiredAgents: Array.Empty<string>(),
            runtimeImage: "python:3.11",
            buildCommands: Array.Empty<string>(),
            testCommands: Array.Empty<string>(),
            maxIterations: 1);

        var files = new[]
        {
            new GeneratedFile("src/main.py", "python", "from fastapi import FastAPI\napp = FastAPI()\n")
        };

        var checklist = service.EvaluateArchitectureChecklist(files, plan);
        var envelope = checklist.Should().ContainSingle(i => i.ItemId == "error_envelope_contract").Subject;
        envelope.Satisfied.Should().BeFalse();
    }

    [Fact]
    public void EvaluateArchitectureChecklist_ShouldPassErrorEnvelopeForApiPlans()
    {
        var service = new ReviewGate2Service(NullLogger<ReviewGate2Service>.Instance);
        var plan = new GenerationPlan(
            applicationName: "TaskApi",
            applicationDescription: "HTTP API for tasks",
            techStack: new TechStack(
                languages: new[] { "Python" },
                frameworks: new[] { "FastAPI" },
                databases: Array.Empty<string>(),
                infrastructure: Array.Empty<string>(),
                rationale: "test"),
            phases: new[] { new GenerationPhase(1, "api", "Build API", Array.Empty<AgentAssignment>()) },
            requiredAgents: Array.Empty<string>(),
            runtimeImage: "python:3.11",
            buildCommands: Array.Empty<string>(),
            testCommands: Array.Empty<string>(),
            maxIterations: 1);

        var files = new[]
        {
            new GeneratedFile("src/main.py", "python", "return {'error': {'code': 'not_found', 'message': 'Task not found'}}")
        };

        var checklist = service.EvaluateArchitectureChecklist(files, plan);
        var envelope = checklist.Should().ContainSingle(i => i.ItemId == "error_envelope_contract").Subject;
        envelope.Satisfied.Should().BeTrue();
    }

    [Fact]
    public void EvaluateArchitectureChecklist_ShouldValidateDependencyManagement()
    {
        var service = new ReviewGate2Service(NullLogger<ReviewGate2Service>.Instance);

        var files = new[]
        {
            new GeneratedFile("src/index.js", "javascript", "const express = require('express');"),
            new GeneratedFile("package.json", "json", "{ \"dependencies\": { \"express\": \"^4.0.0\" } }"),
        };

        var checklist = service.EvaluateArchitectureChecklist(files, BuildTestPlan());

        var depItem = checklist.First(c => c.ItemId == "dependency_management");
        depItem.Satisfied.Should().BeTrue();
    }

    [Fact]
    public void DetectRegressions_ShouldIdentifyFileCountRegression()
    {
        var service = new ReviewGate2Service(NullLogger<ReviewGate2Service>.Instance);

        var files = new[]
        {
            new GeneratedFile("src/file1.cs", "csharp", "public class File1 { }"),
            new GeneratedFile("src/file2.cs", "csharp", "public class File2 { }"),
        };

        var baseline = new[]
        {
            new QualityGateResult("generation", 5, true, new[] { "files_generated=5" }),
        };

        var regressions = service.DetectRegressions(files, baseline, BuildTestPlan());

        // File loss (5 -> 2, delta=-3) is always a regression
        regressions.Should().ContainSingle(r => r.MetricName == "file_count" && r.IsRegression);
    }

    [Fact]
    public void DetectRegressions_ShouldIdentifySizeRegression()
    {
        var service = new ReviewGate2Service(NullLogger<ReviewGate2Service>.Instance);

        var largeContent = string.Concat(Enumerable.Repeat("x", 70000));
        var files = new[]
        {
            new GeneratedFile("src/large.cs", "csharp", largeContent),
        };

        var baseline = new[]
        {
            new QualityGateResult("build", 100, true, Array.Empty<string>()),
        };

        var regressions = service.DetectRegressions(files, baseline, BuildTestPlan());

        regressions.Should().ContainSingle(r => r.MetricName == "total_size_bytes" && r.IsRegression);
    }

    [Fact]
    public void DetectRegressions_ShouldReturnEmptyWhenNoRegressions()
    {
        var service = new ReviewGate2Service(NullLogger<ReviewGate2Service>.Instance);

        var files = new[]
        {
            new GeneratedFile("src/file1.cs", "csharp", "public class File1 { }"),
        };

        var baseline = new[]
        {
            new QualityGateResult("generation", 1, true, Array.Empty<string>()),
        };

        var regressions = service.DetectRegressions(files, baseline, BuildTestPlan());

        regressions.Should().BeEmpty();
    }

    [Fact]
    public void DetectRegressions_FileCountBaselineAwareThreshold_SmallBaseline()
    {
        var service = new ReviewGate2Service(NullLogger<ReviewGate2Service>.Instance);

        var files = Enumerable.Range(1, 7).Select(i =>
            new GeneratedFile($"src/file{i}.cs", "csharp", "public class File{i} { }")).ToArray();

        var baseline = new[]
        {
            new QualityGateResult("generation", 5, true, new[] { "files_generated=5" }),
        };

        var regressions = service.DetectRegressions(files, baseline, BuildTestPlan());

        // Small baseline (5 files) -> threshold=2
        // Growth from 5 to 7 (delta=2) is not > 2*threshold=4, so no regression
        regressions.Should().BeEmpty();
    }

    [Fact]
    public void DetectRegressions_FileCountBaselineAwareThreshold_MediumBaseline()
    {
        var service = new ReviewGate2Service(NullLogger<ReviewGate2Service>.Instance);

        var files = Enumerable.Range(1, 20).Select(i =>
            new GeneratedFile($"src/file{i}.cs", "csharp", "public class File{i} { }")).ToArray();

        var baseline = new[]
        {
            new QualityGateResult("generation", 15, true, new[] { "files_generated=15" }),
        };

        var regressions = service.DetectRegressions(files, baseline, BuildTestPlan());

        regressions.Should().BeEmpty();
    }

    [Fact]
    public void DetectRegressions_FileCountBaselineAwareThreshold_LargeBaseline()
    {
        var service = new ReviewGate2Service(NullLogger<ReviewGate2Service>.Instance);

        var files = Enumerable.Range(1, 25).Select(i =>
            new GeneratedFile($"src/file{i}.cs", "csharp", "public class File{i} { }")).ToArray();

        var baseline = new[]
        {
            new QualityGateResult("generation", 20, true, new[] { "files_generated=20" }),
        };

        var regressions = service.DetectRegressions(files, baseline, BuildTestPlan());

        regressions.Should().BeEmpty();
    }

    [Fact]
    public void DetectRegressions_FileCountBaselineAwareThreshold_FrontendFramework_AllowsMoreGrowth()
    {
        var service = new ReviewGate2Service(NullLogger<ReviewGate2Service>.Instance);

        var files = Enumerable.Range(1, 18).Select(i =>
            new GeneratedFile($"src/file{i}.tsx", "typescript", "export const File{i} = () => <div />;")).ToArray();

        var baseline = new[]
        {
            new QualityGateResult("generation", 10, true, new[] { "files_generated=10" }),
        };

        var plan = new GenerationPlan(
            applicationName: "ReactApp",
            applicationDescription: "Test React app",
            techStack: new TechStack(
                languages: new[] { "TypeScript" },
                frameworks: new[] { "React" },
                databases: Array.Empty<string>(),
                infrastructure: Array.Empty<string>(),
                rationale: "test"),
            phases: new[]
            {
                new GenerationPhase(1, "phase1", "Test phase", Array.Empty<AgentAssignment>())
            },
            requiredAgents: Array.Empty<string>(),
            runtimeImage: "node:22-alpine",
            buildCommands: Array.Empty<string>(),
            testCommands: Array.Empty<string>(),
            maxIterations: 1);

        var regressions = service.DetectRegressions(files, baseline, plan);

        regressions.Should().BeEmpty();
    }

    [Fact]
    public void DetectRegressions_FileCountBaselineAwareThreshold_FastAPIComplex_AllowsLegitimateGrowth()
    {
        var service = new ReviewGate2Service(NullLogger<ReviewGate2Service>.Instance);

        var files = Enumerable.Range(1, 10).Select(i =>
            new GeneratedFile($"src/file{i}.py", "python", "# FastAPI code")).ToArray();

        var baseline = new[]
        {
            new QualityGateResult("generation", 6, true, Array.Empty<string>()),
        };

        var plan = new GenerationPlan(
            applicationName: "FastAPIApp",
            applicationDescription: "Test FastAPI app",
            techStack: new TechStack(
                languages: new[] { "Python" },
                frameworks: new[] { "FastAPI" },
                databases: Array.Empty<string>(),
                infrastructure: Array.Empty<string>(),
                rationale: "test"),
            phases: new[]
            {
                new GenerationPhase(1, "phase1", "Test phase", Array.Empty<AgentAssignment>())
            },
            requiredAgents: Array.Empty<string>(),
            runtimeImage: "python:3.11-slim",
            buildCommands: Array.Empty<string>(),
            testCommands: Array.Empty<string>(),
            maxIterations: 1);

        var regressions = service.DetectRegressions(files, baseline, plan);

        regressions.Should().BeEmpty();
    }

    [Fact]
    public void EvaluateComprehensive_ShouldProducePassingDecisionForCleanCode()
    {
        var service = new ReviewGate2Service(NullLogger<ReviewGate2Service>.Instance);

        var files = new[]
        {
            new GeneratedFile("src/MyClass.cs", "csharp", "/// <summary>My class</summary>\npublic class MyClass { }"),
            new GeneratedFile("tests/MyClassTests.cs", "csharp", "public class MyClassTests { }"),
            new GeneratedFile("appsettings.json", "json", "{ }"),
            new GeneratedFile("README.md", "markdown", "# My Project"),
        };

        var baseline = new[]
        {
            new QualityGateResult("generation", 4, true, Array.Empty<string>()),
            new QualityGateResult("build", 500, true, Array.Empty<string>()),
        };

        var decision = service.EvaluateComprehensive(
            "post_generation",
            files,
            BuildTestPlan(),
            baseline);

        decision.Passed.Should().BeTrue();
        decision.OverallScore.Should().BeGreaterThanOrEqualTo(7);
        decision.StaticChecks.Should().NotBeEmpty();
        decision.ArchitectureChecklist.Should().NotBeEmpty();
    }

    [Fact]
    public void EvaluateComprehensive_ShouldProduceFailingDecisionForSecurityIssues()
    {
        var service = new ReviewGate2Service(NullLogger<ReviewGate2Service>.Instance);

        var files = new[]
        {
            new GeneratedFile("src/config.cs", "csharp", "var key = \"AKIA1234567890ABCDEF\";"),
        };

        var baseline = new[]
        {
            new QualityGateResult("generation", 1, true, Array.Empty<string>()),
        };

        var decision = service.EvaluateComprehensive(
            "post_generation",
            files,
            BuildTestPlan(),
            baseline);

        decision.Passed.Should().BeFalse();
        decision.OverallScore.Should().BeLessThan(7);
        decision.Reasons.Should().Contain(r => r.Contains("static_check_failed"));
    }

    [Fact]
    public void EvaluateComprehensive_ShouldIncludeRemediationHints()
    {
        var service = new ReviewGate2Service(NullLogger<ReviewGate2Service>.Instance);

        var files = new[]
        {
            new GeneratedFile("src/MyClass.cs", "csharp", "public class MyClass { }"),
        };

        var baseline = Array.Empty<QualityGateResult>();

        var decision = service.EvaluateComprehensive(
            "post_generation",
            files,
            BuildTestPlan(),
            baseline);

        decision.RemediationHints.Should().NotBeEmpty();
    }

    [Fact]
    public void EvaluateArchitectureChecklist_ShouldDetectMissingDBArchitectureBaselineForFastAPI()
    {
        var service = new ReviewGate2Service(NullLogger<ReviewGate2Service>.Instance);

        var files = new[]
        {
            new GeneratedFile("app/main.py", "python", "from fastapi import FastAPI\napp = FastAPI()")
        };

        var plan = new GenerationPlan(
            applicationName: "TestApp",
            applicationDescription: "Test",
            techStack: new TechStack(
                languages: new[] { "Python" },
                frameworks: new[] { "FastAPI" },
                databases: Array.Empty<string>(),
                infrastructure: Array.Empty<string>(),
                rationale: "test"),
            phases: Array.Empty<GenerationPhase>(),
            requiredAgents: Array.Empty<string>(),
            runtimeImage: "python:3.11-slim",
            buildCommands: Array.Empty<string>(),
            testCommands: Array.Empty<string>(),
            maxIterations: 1);

        var checklist = service.EvaluateArchitectureChecklist(files, plan);

        var dbArchItem = checklist.Should().ContainSingle(i => i.ItemId == "db_architecture_baseline").Subject;
        dbArchItem.Satisfied.Should().BeFalse();
    }

    [Fact]
    public void EvaluateArchitectureChecklist_ShouldPassDBArchitectureBaselineForFastAPI()
    {
        var service = new ReviewGate2Service(NullLogger<ReviewGate2Service>.Instance);

        var files = new[]
        {
            new GeneratedFile("app/database.py", "python", "from sqlalchemy.ext.asyncio import AsyncSession\nasync def get_db(): pass"),
            new GeneratedFile("app/main.py", "python", "from fastapi import Depends\ndef endpoint(db: AsyncSession = Depends(get_db)): pass"),
            new GeneratedFile("alembic/versions/001_initial.py", "python", "revision = '001'")
        };

        var plan = new GenerationPlan(
            applicationName: "TestApp",
            applicationDescription: "Test",
            techStack: new TechStack(
                languages: new[] { "Python" },
                frameworks: new[] { "FastAPI" },
                databases: Array.Empty<string>(),
                infrastructure: Array.Empty<string>(),
                rationale: "test"),
            phases: Array.Empty<GenerationPhase>(),
            requiredAgents: Array.Empty<string>(),
            runtimeImage: "python:3.11-slim",
            buildCommands: Array.Empty<string>(),
            testCommands: Array.Empty<string>(),
            maxIterations: 1);

        var checklist = service.EvaluateArchitectureChecklist(files, plan);

        var dbArchItem = checklist.Should().ContainSingle(i => i.ItemId == "db_architecture_baseline").Subject;
        dbArchItem.Satisfied.Should().BeTrue();
    }

    [Fact]
    public void EvaluateArchitectureChecklist_ShouldNotCheckDBArchitectureForNonFastAPI()
    {
        var service = new ReviewGate2Service(NullLogger<ReviewGate2Service>.Instance);

        var files = new[]
        {
            new GeneratedFile("app/main.py", "python", "from flask import Flask\napp = Flask(__name__)")
        };

        var plan = new GenerationPlan(
            applicationName: "TestApp",
            applicationDescription: "Test",
            techStack: new TechStack(
                languages: new[] { "Python" },
                frameworks: new[] { "Flask" },
                databases: Array.Empty<string>(),
                infrastructure: Array.Empty<string>(),
                rationale: "test"),
            phases: Array.Empty<GenerationPhase>(),
            requiredAgents: Array.Empty<string>(),
            runtimeImage: "python:3.11-slim",
            buildCommands: Array.Empty<string>(),
            testCommands: Array.Empty<string>(),
            maxIterations: 1);

        var checklist = service.EvaluateArchitectureChecklist(files, plan);

        checklist.Should().NotContain(i => i.ItemId == "db_architecture_baseline");
    }

    [Fact]
    public void EvaluateArchitectureChecklist_ShouldDetectPlaceholderTests()
    {
        var service = new ReviewGate2Service(NullLogger<ReviewGate2Service>.Instance);

        var files = new[]
        {
            new GeneratedFile("tests/test_api.py", "python", "def test_placeholder():\n    assert True")
        };

        var checklist = service.EvaluateArchitectureChecklist(files, BuildTestPlan());

        var testQualityItem = checklist.Should().ContainSingle(i => i.ItemId == "test_quality_floor").Subject;
        testQualityItem.Satisfied.Should().BeFalse();
    }

    [Fact]
    public void EvaluateArchitectureChecklist_ShouldPassTestQualityFloor()
    {
        var service = new ReviewGate2Service(NullLogger<ReviewGate2Service>.Instance);

        var files = new[]
        {
            new GeneratedFile("tests/test_api.py", "python", "import pytest\n@pytest.mark.integration\ndef test_api_error():\n    response = client.get('/api/404')\n    assert response.status_code == 404")
        };

        var checklist = service.EvaluateArchitectureChecklist(files, BuildTestPlan());

        var testQualityItem = checklist.Should().ContainSingle(i => i.ItemId == "test_quality_floor").Subject;
        testQualityItem.Satisfied.Should().BeTrue();
    }

    [Fact]
    public void EvaluateArchitectureChecklist_ShouldRequireIntegrationAndNegativeTests()
    {
        var service = new ReviewGate2Service(NullLogger<ReviewGate2Service>.Instance);

        var files = new[]
        {
            new GeneratedFile("tests/test_unit.py", "python", "def test_unit():\n    assert calculate(1, 2) == 3")
        };

        var checklist = service.EvaluateArchitectureChecklist(files, BuildTestPlan());

        var testQualityItem = checklist.Should().ContainSingle(i => i.ItemId == "test_quality_floor").Subject;
        testQualityItem.Satisfied.Should().BeFalse();
    }

    [Fact]
    public void EvaluateArchitectureChecklist_ShouldDetectMissingObservabilityBaseline()
    {
        var service = new ReviewGate2Service(NullLogger<ReviewGate2Service>.Instance);

        var files = new[]
        {
            new GeneratedFile("app/main.py", "python", "from fastapi import FastAPI\napp = FastAPI()")
        };

        var checklist = service.EvaluateArchitectureChecklist(files, BuildTestPlan());

        var obsItem = checklist.Should().ContainSingle(i => i.ItemId == "observability_baseline").Subject;
        obsItem.Satisfied.Should().BeFalse();
    }

    [Fact]
    public void EvaluateArchitectureChecklist_ShouldPassObservabilityBaseline()
    {
        var service = new ReviewGate2Service(NullLogger<ReviewGate2Service>.Instance);

        var files = new[]
        {
            new GeneratedFile("app/logging.py", "python", "import logging\nimport json\nlogger = logging.getLogger('app')\nlogger.info('structured log', extra={'json': True})"),
            new GeneratedFile("app/middleware.py", "python", "x_request_id = 'correlation'"),
            new GeneratedFile("app/health.py", "python", "@app.get('/health')\ndef health(): pass"),
            new GeneratedFile("app/readiness.py", "python", "@app.get('/readiness')\ndef readiness(): pass")
        };

        var checklist = service.EvaluateArchitectureChecklist(files, BuildTestPlan());

        var obsItem = checklist.Should().ContainSingle(i => i.ItemId == "observability_baseline").Subject;
        obsItem.Satisfied.Should().BeTrue();
    }

    [Fact]
    public void EvaluateArchitectureChecklist_ShouldDetectMissingInfraCompleteness()
    {
        var service = new ReviewGate2Service(NullLogger<ReviewGate2Service>.Instance);

        var files = new[]
        {
            new GeneratedFile("app/main.py", "python", "from fastapi import FastAPI\napp = FastAPI()")
        };

        var checklist = service.EvaluateArchitectureChecklist(files, BuildTestPlan());

        var infraItem = checklist.Should().ContainSingle(i => i.ItemId == "infra_completeness").Subject;
        infraItem.Satisfied.Should().BeFalse();
    }

    [Fact]
    public void EvaluateArchitectureChecklist_ShouldPassInfraCompleteness()
    {
        var service = new ReviewGate2Service(NullLogger<ReviewGate2Service>.Instance);

        var files = new[]
        {
            new GeneratedFile("docker-compose.yml", "yaml", "version: '3'"),
            new GeneratedFile(".github/workflows/ci.yml", "yaml", "name: CI"),
            new GeneratedFile("Makefile", "makefile", "run: docker-compose up"),
            new GeneratedFile("scripts/start.sh", "shell", "#!/bin/bash")
        };

        var checklist = service.EvaluateArchitectureChecklist(files, BuildTestPlan());

        var infraItem = checklist.Should().ContainSingle(i => i.ItemId == "infra_completeness").Subject;
        infraItem.Satisfied.Should().BeTrue();
    }

    [Fact]
    public void EvaluateArchitectureChecklist_ShouldDetectMissingDomainCompletenessForBillingApp()
    {
        var service = new ReviewGate2Service(NullLogger<ReviewGate2Service>.Instance);

        var files = new[]
        {
            new GeneratedFile("app/main.py", "python", "from fastapi import FastAPI\napp = FastAPI()")
        };

        var plan = new GenerationPlan(
            applicationName: "BillingApp",
            applicationDescription: "Billing and payment processing application with Stripe integration",
            techStack: new TechStack(
                languages: new[] { "Python" },
                frameworks: new[] { "FastAPI" },
                databases: Array.Empty<string>(),
                infrastructure: Array.Empty<string>(),
                rationale: "test"),
            phases: Array.Empty<GenerationPhase>(),
            requiredAgents: Array.Empty<string>(),
            runtimeImage: "python:3.11-slim",
            buildCommands: Array.Empty<string>(),
            testCommands: Array.Empty<string>(),
            maxIterations: 1);

        var checklist = service.EvaluateArchitectureChecklist(files, plan);

        var domainItem = checklist.Should().ContainSingle(i => i.ItemId == "domain_completeness").Subject;
        domainItem.Satisfied.Should().BeFalse();
    }

    [Fact]
    public void EvaluateArchitectureChecklist_ShouldPassDomainCompletenessForBillingApp()
    {
        var service = new ReviewGate2Service(NullLogger<ReviewGate2Service>.Instance);

        var files = new[]
        {
            new GeneratedFile("app/webhook.py", "python", "@app.post('/webhook/stripe')\ndef stripe_webhook(): pass"),
            new GeneratedFile("app/payment.py", "python", "idempotency_key = request.headers.get('Idempotency-Key')"),
            new GeneratedFile("app/audit.py", "python", "audit_log.log_payment()"),
            new GeneratedFile("app/rate_limit.py", "python", "@rate_limit_decorator\ndef create_payment(): pass")
        };

        var plan = new GenerationPlan(
            applicationName: "BillingApp",
            applicationDescription: "Billing and payment processing application with Stripe integration",
            techStack: new TechStack(
                languages: new[] { "Python" },
                frameworks: new[] { "FastAPI" },
                databases: Array.Empty<string>(),
                infrastructure: Array.Empty<string>(),
                rationale: "test"),
            phases: Array.Empty<GenerationPhase>(),
            requiredAgents: Array.Empty<string>(),
            runtimeImage: "python:3.11-slim",
            buildCommands: Array.Empty<string>(),
            testCommands: Array.Empty<string>(),
            maxIterations: 1);

        var checklist = service.EvaluateArchitectureChecklist(files, plan);

        var domainItem = checklist.Should().ContainSingle(i => i.ItemId == "domain_completeness").Subject;
        domainItem.Satisfied.Should().BeTrue();
    }

    [Fact]
    public void EvaluateArchitectureChecklist_ShouldNotCheckDomainCompletenessForNonBillingApp()
    {
        var service = new ReviewGate2Service(NullLogger<ReviewGate2Service>.Instance);

        var files = new[]
        {
            new GeneratedFile("app/main.py", "python", "from fastapi import FastAPI\napp = FastAPI()")
        };

        var plan = new GenerationPlan(
            applicationName: "BlogApp",
            applicationDescription: "Simple blog application",
            techStack: new TechStack(
                languages: new[] { "Python" },
                frameworks: new[] { "FastAPI" },
                databases: Array.Empty<string>(),
                infrastructure: Array.Empty<string>(),
                rationale: "test"),
            phases: Array.Empty<GenerationPhase>(),
            requiredAgents: Array.Empty<string>(),
            runtimeImage: "python:3.11-slim",
            buildCommands: Array.Empty<string>(),
            testCommands: Array.Empty<string>(),
            maxIterations: 1);

        var checklist = service.EvaluateArchitectureChecklist(files, plan);

        checklist.Should().NotContain(i => i.ItemId == "domain_completeness");
    }

    [Fact]
    public void DetectRegressions_ShouldDetectSemanticAuthRemoval()
    {
        var service = new ReviewGate2Service(NullLogger<ReviewGate2Service>.Instance);

        var currentFiles = new[]
        {
            new GeneratedFile("app/main.py", "python", "from fastapi import FastAPI\napp = FastAPI()")
        };

        var baselineMetrics = new[]
        {
            new QualityGateResult("generation", 8, true, new[] { "auth:authentication detected" })
        };

        var regressions = service.DetectRegressions(currentFiles, baselineMetrics, BuildTestPlan());

        regressions.Should().Contain(r => r.MetricName == "semantic_auth_removed" && r.IsRegression);
    }

    [Fact]
    public void DetectRegressions_ShouldDetectSemanticLoggingRemoval()
    {
        var service = new ReviewGate2Service(NullLogger<ReviewGate2Service>.Instance);

        var currentFiles = new[]
        {
            new GeneratedFile("app/main.py", "python", "from fastapi import FastAPI\napp = FastAPI()")
        };

        var baselineMetrics = new[]
        {
            new QualityGateResult("generation", 8, true, new[] { "logging:structured logging detected" })
        };

        var regressions = service.DetectRegressions(currentFiles, baselineMetrics, BuildTestPlan());

        regressions.Should().Contain(r => r.MetricName == "semantic_logging_removed" && r.IsRegression);
    }

    [Fact]
    public void DetectRegressions_ShouldDetectSemanticTestsRemoval()
    {
        var service = new ReviewGate2Service(NullLogger<ReviewGate2Service>.Instance);

        var currentFiles = new[]
        {
            new GeneratedFile("app/main.py", "python", "from fastapi import FastAPI\napp = FastAPI()")
        };

        var baselineMetrics = new[]
        {
            new QualityGateResult("generation", 8, true, new[] { "test:test coverage detected" })
        };

        var regressions = service.DetectRegressions(currentFiles, baselineMetrics, BuildTestPlan());

        regressions.Should().Contain(r => r.MetricName == "semantic_tests_removed" && r.IsRegression);
    }

    [Fact]
    public void DetectRegressions_ShouldNotFlagSemanticRegressionWhenPatternsPreserved()
    {
        var service = new ReviewGate2Service(NullLogger<ReviewGate2Service>.Instance);

        var currentFiles = new[]
        {
            new GeneratedFile("app/main.py", "python", "from fastapi import FastAPI\napp = FastAPI()\nimport logging\nlogger = logging.getLogger('app')")
        };

        var baselineMetrics = new[]
        {
            new QualityGateResult("generation", 8, true, new[] { "logging:structured logging detected" })
        };

        var regressions = service.DetectRegressions(currentFiles, baselineMetrics, BuildTestPlan());

        regressions.Should().NotContain(r => r.MetricName == "semantic_logging_removed");
    }

    [Fact]
    public void EvaluateArchitectureChecklist_ShouldDetectMissingFastAPITemplateStructure()
    {
        var service = new ReviewGate2Service(NullLogger<ReviewGate2Service>.Instance);

        var files = new[]
        {
            new GeneratedFile("app/main.py", "python", "from fastapi import FastAPI\napp = FastAPI()")
        };

        var plan = new GenerationPlan(
            applicationName: "FastAPIApp",
            applicationDescription: "FastAPI application",
            techStack: new TechStack(
                languages: new[] { "Python" },
                frameworks: new[] { "FastAPI" },
                databases: Array.Empty<string>(),
                infrastructure: Array.Empty<string>(),
                rationale: "test"),
            phases: Array.Empty<GenerationPhase>(),
            requiredAgents: Array.Empty<string>(),
            runtimeImage: "python:3.11-slim",
            buildCommands: Array.Empty<string>(),
            testCommands: Array.Empty<string>(),
            maxIterations: 1);

        var checklist = service.EvaluateArchitectureChecklist(files, plan);

        var stackItem = checklist.Should().ContainSingle(i => i.ItemId == "stack_template_packs").Subject;
        stackItem.Satisfied.Should().BeFalse();
    }

    [Fact]
    public void EvaluateArchitectureChecklist_ShouldPassFastAPITemplateStructure()
    {
        var service = new ReviewGate2Service(NullLogger<ReviewGate2Service>.Instance);

        var files = new[]
        {
            new GeneratedFile("app/main.py", "python", "from fastapi import FastAPI\napp = FastAPI()"),
            new GeneratedFile("app/routers/user.py", "python", "router = APIRouter()"),
            new GeneratedFile("app/models/user.py", "python", "class User: pass")
        };

        var plan = new GenerationPlan(
            applicationName: "FastAPIApp",
            applicationDescription: "FastAPI application",
            techStack: new TechStack(
                languages: new[] { "Python" },
                frameworks: new[] { "FastAPI" },
                databases: Array.Empty<string>(),
                infrastructure: Array.Empty<string>(),
                rationale: "test"),
            phases: Array.Empty<GenerationPhase>(),
            requiredAgents: Array.Empty<string>(),
            runtimeImage: "python:3.11-slim",
            buildCommands: Array.Empty<string>(),
            testCommands: Array.Empty<string>(),
            maxIterations: 1);

        var checklist = service.EvaluateArchitectureChecklist(files, plan);

        var stackItem = checklist.Should().ContainSingle(i => i.ItemId == "stack_template_packs").Subject;
        stackItem.Satisfied.Should().BeTrue();
    }

    [Fact]
    public void EvaluateArchitectureChecklist_ShouldDetectMissingASPNETCoreTemplateStructure()
    {
        var service = new ReviewGate2Service(NullLogger<ReviewGate2Service>.Instance);

        var files = new[]
        {
            new GeneratedFile("Program.cs", "csharp", "var app = WebApplication.CreateBuilder(args);")
        };

        var plan = new GenerationPlan(
            applicationName: "AspNetApp",
            applicationDescription: "ASP.NET Core application",
            techStack: new TechStack(
                languages: new[] { "C#" },
                frameworks: new[] { "ASP.NET Core" },
                databases: Array.Empty<string>(),
                infrastructure: Array.Empty<string>(),
                rationale: "test"),
            phases: Array.Empty<GenerationPhase>(),
            requiredAgents: Array.Empty<string>(),
            runtimeImage: "mcr.microsoft.com/dotnet/sdk:8.0",
            buildCommands: Array.Empty<string>(),
            testCommands: Array.Empty<string>(),
            maxIterations: 1);

        var checklist = service.EvaluateArchitectureChecklist(files, plan);

        var stackItem = checklist.Should().ContainSingle(i => i.ItemId == "stack_template_packs").Subject;
        stackItem.Satisfied.Should().BeFalse();
    }

    [Fact]
    public void EvaluateArchitectureChecklist_ShouldPassASPNETCoreTemplateStructure()
    {
        var service = new ReviewGate2Service(NullLogger<ReviewGate2Service>.Instance);

        var files = new[]
        {
            new GeneratedFile("Program.cs", "csharp", "var app = WebApplication.CreateBuilder(args);"),
            new GeneratedFile("Controllers/UserController.cs", "csharp", "public class UserController : ControllerBase"),
            new GeneratedFile("Models/User.cs", "csharp", "public class User")
        };

        var plan = new GenerationPlan(
            applicationName: "AspNetApp",
            applicationDescription: "ASP.NET Core application",
            techStack: new TechStack(
                languages: new[] { "C#" },
                frameworks: new[] { "ASP.NET Core" },
                databases: Array.Empty<string>(),
                infrastructure: Array.Empty<string>(),
                rationale: "test"),
            phases: Array.Empty<GenerationPhase>(),
            requiredAgents: Array.Empty<string>(),
            runtimeImage: "mcr.microsoft.com/dotnet/sdk:8.0",
            buildCommands: Array.Empty<string>(),
            testCommands: Array.Empty<string>(),
            maxIterations: 1);

        var checklist = service.EvaluateArchitectureChecklist(files, plan);

        var stackItem = checklist.Should().ContainSingle(i => i.ItemId == "stack_template_packs").Subject;
        stackItem.Satisfied.Should().BeTrue();
    }

    private static GenerationPlan BuildTestPlan()
    {
        return new GenerationPlan(
            applicationName: "TestApp",
            applicationDescription: "Test application",
            techStack: new TechStack(
                languages: new[] { "C#" },
                frameworks: new[] { "ASP.NET Core" },
                databases: Array.Empty<string>(),
                infrastructure: Array.Empty<string>(),
                rationale: "test"),
            phases: new[]
            {
                new GenerationPhase(1, "phase1", "Test phase", Array.Empty<AgentAssignment>())
            },
            requiredAgents: Array.Empty<string>(),
            runtimeImage: "mcr.microsoft.com/dotnet/sdk:8.0",
            buildCommands: Array.Empty<string>(),
            testCommands: Array.Empty<string>(),
            maxIterations: 1);
    }
}
