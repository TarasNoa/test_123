using FluentAssertions;
using Libr4.IDE.Application.AutonomousAppGeneration.Infrastructure;
using Libr4.IDE.Domain.AutonomousAppGeneration;
using Xunit;

namespace Libr4.IntegrationTests.IDE;

public sealed class PythonDependencyGraphNormalizerTests
{
    [Fact]
    public void Normalize_Run2Scenario_PrefixAndDatabaseStub()
    {
        var files = BuildRun2LikeProject();

        PythonDependencyGraphNormalizer.Normalize(
            files,
            "ModuleNotFoundError: No module named 'src.app.database'")
            .Should().BeGreaterThan(0);

        files.Should().Contain(f => f.RelativePath.Equals("src/app/database.py", StringComparison.OrdinalIgnoreCase));

        var router = files.Single(f => f.RelativePath == "src/app/routers/crmbackend.py");
        router.Content.Should().Contain("from app.database import get_db");
        router.Content.Should().NotContain("src.app.database");

        var database = files.Single(f => f.RelativePath == "src/app/database.py");
        database.Content.Should().Contain("def get_db");
        database.Content.Should().Contain("from app.models import Base");
    }

    [Fact]
    public void Normalize_PrefixOnly_FixesSrcAppImports()
    {
        var files = new List<GeneratedFile>
        {
            new("src/app/__init__.py", "python", "from src.app.exceptions import register_exception_handlers\n"),
            new("src/app/exceptions.py", "python", "def register_exception_handlers():\n    pass\n")
        };

        PythonDependencyGraphNormalizer.Normalize(files, "ImportError").Should().BeGreaterThan(0);

        files.Single(f => f.RelativePath == "src/app/__init__.py").Content
            .Should().Contain("from app.exceptions import register_exception_handlers");
    }

    [Fact]
    public void Normalize_SymbolRemap_CustomerToCustomerDb()
    {
        var files = new List<GeneratedFile>
        {
            new("src/app/models.py", "python", "class CustomerDB:\n    pass\n"),
            new("src/tests/test_api.py", "python", "from app.models import Customer\n")
        };

        PythonDependencyGraphNormalizer.Normalize(
            files,
            "ImportError: cannot import name 'Customer'")
            .Should().BeGreaterThan(0);

        files.Single(f => f.RelativePath == "src/tests/test_api.py").Content
            .Should().Contain("from app.models import CustomerDB");
    }

    [Fact]
    public void ResolvePackageRoot_SrcAppLayout_ReturnsSrc()
    {
        var files = new List<GeneratedFile>
        {
            new("src/app/models.py", "python", "pass\n"),
            new("src/main.py", "python", "pass\n")
        };

        PythonDependencyGraphNormalizer.ResolvePackageRoot(files).Should().Be("src");
    }

    private static List<GeneratedFile> BuildRun2LikeProject() =>
        new()
        {
            new("src/main.py", "python", """
                from fastapi import FastAPI
                from app.routers.crmbackend import router as crm_router

                app = FastAPI()
                app.include_router(crm_router)
                """),
            new("src/app/__init__.py", "python", """
                from src.app.exceptions import register_exception_handlers
                from src.app.routers.crmbackend import router as crm_router
                """),
            new("src/app/exceptions.py", "python", """
                def register_exception_handlers(app):
                    pass
                """),
            new("src/app/models.py", "python", """
                from sqlalchemy.orm import declarative_base
                Base = declarative_base()

                class CustomerDB:
                    pass
                """),
            new("src/app/routers/crmbackend.py", "python", """
                from fastapi import APIRouter, Depends
                from sqlalchemy.orm import Session
                from src.app.database import get_db

                router = APIRouter()
                """),
            new("src/app/services.py", "python", """
                def list_customers(db):
                    return []
                """),
            new("src/tests/test_api.py", "python", """
                from app.main import app
                from app.models import Base, Customer
                from app.services import get_db
                """)
        };
}
