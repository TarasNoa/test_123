using FluentAssertions;
using Libr4.IDE.Application.AutonomousAppGeneration.Infrastructure;
using Libr4.IDE.Domain.AutonomousAppGeneration;
using Xunit;

namespace Libr4.IntegrationTests.IDE;

public sealed class PythonDependencySyncEngineTests
{
    [Fact]
    public void SyncRequirements_AddsFastApiWhenImported()
    {
        var files = new List<GeneratedFile>
        {
            new("requirements.txt", "text", "pytest>=8.0.0\n"),
            new("main.py", "python", """
                from fastapi import FastAPI
                import uvicorn
                """)
        };

        PythonDependencySyncEngine.SyncRequirements(files).Should().Be(1);
        files[0].Content.Should().Contain("fastapi>=");
        files[0].Content.Should().Contain("uvicorn");
    }

    [Fact]
    public void SyncRequirements_AddsEmailValidatorWhenEmailStrUsed()
    {
        var files = new List<GeneratedFile>
        {
            new("src/requirements.txt", "text", "fastapi>=0.115.0\n"),
            new("src/schemas.py", "python", """
                from pydantic import BaseModel, EmailStr

                class User(BaseModel):
                    email: EmailStr
                """)
        };

        PythonDependencySyncEngine.SyncRequirements(files).Should().Be(1);
        files[0].Content.Should().Contain("email-validator>=");
    }

    [Fact]
    public void SyncRequirements_AddsSlowapiWhenImported()
    {
        var files = new List<GeneratedFile>
        {
            new("requirements.txt", "text", "fastapi>=0.115.0\n"),
            new("main.py", "python", """
                from slowapi import Limiter
                from slowapi.util import get_remote_address
                """)
        };

        PythonDependencySyncEngine.SyncRequirements(files).Should().Be(1);
        files[0].Content.Should().Contain("slowapi>=");
    }

    [Fact]
    public void SyncFromBuildLog_AddsUnknownModuleAsPipPackage()
    {
        var files = new List<GeneratedFile>
        {
            new("requirements.txt", "text", "fastapi>=0.115.0\n"),
            new("main.py", "python", "import slowapi\n")
        };
        const string buildLog = "ModuleNotFoundError: No module named 'email_validator'";

        PythonDependencySyncEngine.SyncFromBuildLog(files, buildLog).Should().Be(1);
        files[0].Content.Should().Contain("email-validator>=");
    }

    [Fact]
    public void SyncFromBuildLog_AddsGenericPackageWhenNotInCatalog()
    {
        var files = new List<GeneratedFile>
        {
            new("requirements.txt", "text", "fastapi>=0.115.0\n")
        };
        const string buildLog = "ModuleNotFoundError: No module named 'some_new_pkg'";

        PythonDependencySyncEngine.SyncFromBuildLog(files, buildLog).Should().Be(1);
        files[0].Content.Should().Contain("some-new-pkg>=");
    }

    [Fact]
    public void Sync_MirrorsNestedRequirementsToRoot()
    {
        var files = new List<GeneratedFile>
        {
            new("src/requirements.txt", "text", "fastapi>=0.115.0\n"),
            new("src/main.py", "python", "from slowapi import Limiter\n")
        };

        PythonDependencySyncEngine.Sync(files).Should().BeGreaterThan(0);
        files.Should().Contain(f =>
            f.RelativePath.Equals("requirements.txt", StringComparison.OrdinalIgnoreCase)
            && f.Content!.Contains("slowapi>="));
    }

    [Fact]
    public void ShouldSync_ReturnsTrueForModuleNotFoundInBuildLog()
    {
        var files = new List<GeneratedFile>
        {
            new("requirements.txt", "text", "fastapi>=0.115.0\n"),
            new("main.py", "python", "import httpx\n")
        };
        var errors = new[]
        {
            new ErrorReport("BuildError", "ModuleNotFoundError: No module named 'slowapi'", "main.py")
        };

        PythonDependencySyncEngine.ShouldSync("No module named 'slowapi'", errors, files).Should().BeTrue();
    }

    [Fact]
    public void TryResolveRequirementSpec_MapsEmailValidatorModule()
    {
        PythonDependencySyncEngine.TryResolveRequirementSpec("email_validator", out var spec).Should().BeTrue();
        spec.Should().Contain("email-validator");
    }

    [Fact]
    public void SyncRequirements_DoesNotAddLocalSrcPackage()
    {
        var files = new List<GeneratedFile>
        {
            new("requirements.txt", "text", "fastapi>=0.115.0\n"),
            new("src/main.py", "python", """
                from src.app.database import get_db
                from fastapi import FastAPI
                """)
        };

        PythonDependencySyncEngine.SyncRequirements(files).Should().Be(0);
        files[0].Content.Should().NotContain("src>=");
    }

    [Fact]
    public void SyncFromBuildLog_DoesNotAddLocalSrcFromModuleNotFound()
    {
        var files = new List<GeneratedFile>
        {
            new("requirements.txt", "text", "fastapi>=0.115.0\n")
        };
        const string buildLog = "ModuleNotFoundError: No module named 'src.app.database'";

        PythonDependencySyncEngine.SyncFromBuildLog(files, buildLog).Should().Be(0);
        files[0].Content.Should().NotContain("src>=");
    }

    [Fact]
    public void Classifier_MapsModuleNotFoundToRequirementsSyntax()
    {
        var errors = new[]
        {
            new ErrorReport("BuildError", "ModuleNotFoundError: No module named 'httpx'", "main.py")
        };

        var classified = RepairErrorClassifier.Classify(errors, "No module named 'httpx'");
        classified[0].Class.Should().Be(RepairErrorClassifier.RepairErrorClass.CompileSymbol);
        classified[0].Tier.Should().Be(RepairErrorClassifier.RepairTier.Level2Compile);
    }
}
