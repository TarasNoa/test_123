using FluentAssertions;
using Libr4.IDE.Application.AutonomousAppGeneration.Infrastructure;
using Libr4.IDE.Domain.AutonomousAppGeneration;
using Xunit;

namespace Libr4.IntegrationTests.IDE;

public sealed class PythonProjectLayoutNormalizerTests
{
    [Fact]
    public void DiscoverAppEntry_SrcAppMain_PrefersPackageMainOverSrcMain()
    {
        var files = new List<GeneratedFile>
        {
            new("src/main.py", "python", "from fastapi import FastAPI\napp = FastAPI()\n"),
            new("src/app/main.py", "python", "from fastapi import FastAPI\napp = FastAPI()\n")
        };

        var discovery = PythonProjectLayoutNormalizer.DiscoverAppEntry(files);
        discovery.Should().NotBeNull();
        discovery!.ModuleFilePath.Should().Be("src/app/main.py");
        discovery.ImportModule.Should().Be("app.main");
        discovery.SysPathRoot.Should().Be("src");
    }

    [Fact]
    public void Normalize_LayoutA_FlatMainAndRootTests()
    {
        var files = BuildFiles(
            mainPath: "main.py",
            testPath: "tests/test_api.py",
            testContent: """
                from main import app
                from fastapi.testclient import TestClient

                client = TestClient(app)
                """);

        PythonProjectLayoutNormalizer.Normalize(files, BuildLog("tests/test_api.py")).Should().BeGreaterThan(0);

        var test = files.Single(f => f.RelativePath == "tests/test_api.py");
        test.Content.Should().Contain("from main import app");
    }

    [Fact]
    public void Normalize_LayoutB_SrcMainWithSrcTests()
    {
        var files = BuildFiles(
            mainPath: "src/main.py",
            testPath: "src/tests/test_api.py",
            testContent: """
                from main import app
                from fastapi.testclient import TestClient

                client = TestClient(app)
                """);

        PythonProjectLayoutNormalizer.Normalize(files, BuildLog("src/tests/test_api.py")).Should().BeGreaterThan(0);

        var test = files.Single(f => f.RelativePath == "src/tests/test_api.py");
        test.Content.Should().Contain("from main import app");
        test.Content.Should().NotContain("src/src");
        test.Content.Should().Contain("sys.path.insert(0, os.path.abspath(os.path.join(os.path.dirname(__file__), '..'))");
    }

    [Fact]
    public void Normalize_LayoutC_SrcPackageApp()
    {
        var files = new List<GeneratedFile>
        {
            new("src/app/main.py", "python", "from fastapi import FastAPI\napp = FastAPI()\n"),
            new("src/tests/test_api.py", "python", """
                from main import app
                from fastapi.testclient import TestClient

                client = TestClient(app)
                """)
        };

        PythonProjectLayoutNormalizer.Normalize(files, BuildLog("src/tests/test_api.py")).Should().BeGreaterThan(0);

        var test = files.Single(f => f.RelativePath == "src/tests/test_api.py");
        test.Content.Should().Contain("from app.main import app");
        files.Should().Contain(f => f.RelativePath == "src/app/__init__.py");
    }

    [Fact]
    public void Normalize_LayoutD_BackendPackageWithRootTests()
    {
        var files = new List<GeneratedFile>
        {
            new("backend/app/main.py", "python", "from fastapi import FastAPI\napp = FastAPI()\n"),
            new("tests/test_api.py", "python", """
                from main import app
                from fastapi.testclient import TestClient

                client = TestClient(app)
                """)
        };

        PythonProjectLayoutNormalizer.Normalize(files, BuildLog("tests/test_api.py")).Should().BeGreaterThan(0);

        var test = files.Single(f => f.RelativePath == "tests/test_api.py");
        test.Content.Should().Contain("from backend.app.main import app");
        test.Content.Should().Contain("sys.path.insert");
    }

    [Fact]
    public void Normalize_FixesBrokenSrcSrcSysPath()
    {
        var files = BuildFiles(
            mainPath: "src/main.py",
            testPath: "src/tests/test_api.py",
            testContent: """
                import os
                import sys
                sys.path.insert(0, os.path.abspath(os.path.join(os.path.dirname(__file__), '..', 'src')))
                from main import app
                from fastapi.testclient import TestClient

                client = TestClient(app)
                """);

        PythonProjectLayoutNormalizer.Normalize(
            files,
            "ModuleNotFoundError: No module named 'main' src/tests/test_api.py src/src")
            .Should().BeGreaterThan(0);

        var test = files.Single(f => f.RelativePath == "src/tests/test_api.py");
        test.Content.Should().NotContain("src/src");
        test.Content.Should().Contain("from main import app");
    }

    private static List<GeneratedFile> BuildFiles(string mainPath, string testPath, string testContent) =>
        new()
        {
            new(mainPath, "python", "from fastapi import FastAPI\napp = FastAPI()\n"),
            new(testPath, "python", testContent)
        };

    private static string BuildLog(string testPath) =>
        $"""
         ERROR collecting {testPath}
         ImportError while importing test module '{testPath}'.
         E   ModuleNotFoundError: No module named 'main'
         """;
}
