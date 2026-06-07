using FluentAssertions;
using Libr4.IDE.Application.AutonomousAppGeneration.Infrastructure;
using Libr4.IDE.Domain.AutonomousAppGeneration;
using Xunit;

namespace Libr4.IntegrationTests.IDE;

public sealed class PythonPytestImportRemediationTests
{
    [Fact]
    public void ResolveImportModule_SrcAppMain_ReturnsAppMain()
    {
        PythonPytestImportRemediation.ResolveImportModule("src/app/main.py")
            .Should().Be("app.main");
    }

    [Fact]
    public void BuildSysPathInsert_SrcTestsToSrcAppMain_PointsAtSrcDirectory()
    {
        var expr = PythonPytestImportRemediation.BuildSysPathInsert("src/tests/test_api.py", "src/app/main.py");
        expr.Should().Contain("'..'");
    }

    [Fact]
    public void Apply_ReplacesBrokenSrcTestsImport_ForSrcAppMainLayout()
    {
        var files = new List<GeneratedFile>
        {
            new("src/app/main.py", "python", "from fastapi import FastAPI\napp = FastAPI()\n"),
            new("src/tests/test_api.py", "python", """
                from main import app
                from fastapi.testclient import TestClient

                client = TestClient(app)

                def test_health():
                    assert client.get("/health").status_code in (200, 404)
                """),
            new("requirements.txt", "text", "fastapi>=0.115.0\npytest>=8.3.0\n")
        };

        var buildLog = """
            ERROR collecting src/tests/test_api.py
            ImportError while importing test module '/workspace/src/tests/test_api.py'.
            src/tests/test_api.py:19: in <module>
                from main import app
            E   ModuleNotFoundError: No module named 'main'
            """;

        PythonPytestImportRemediation.Apply(files, buildLog).Should().BeGreaterThan(0);

        var test = files.Single(f => f.RelativePath.Equals("src/tests/test_api.py", StringComparison.OrdinalIgnoreCase));
        test.Content.Should().Contain("from app.main import app");
        test.Content.Should().Contain("sys.path.insert");
    }
}
