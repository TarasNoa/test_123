using FluentAssertions;
using Libr4.IDE.Application.AutonomousAppGeneration.Services.PlatformUtilization;
using Libr4.IDE.Domain.AutonomousAppGeneration;
using Xunit;

namespace Libr4.IntegrationTests.IDE;

public sealed class PlatformJitPlaybookCatalogTests
{
    [Fact]
    public void TryMatch_PytestImport_ReturnsRemediationPlaybook()
    {
        var errors = new[]
        {
            new ErrorReport("ImportError", "ModuleNotFoundError: No module named 'main'", string.Empty, "tests/test_api.py", 3)
        };
        var plan = new GenerationPlan(
            "crm", "crm",
            new TechStack(["Python"], ["FastAPI"], [], [], "fastapi"),
            [], [], "python:3.12",
            ["pip install -r requirements.txt"],
            ["pytest"],
            5);

        var match = PlatformJitPlaybookCatalog.TryMatch(errors, "ModuleNotFoundError: No module named 'main'", plan);

        match.Should().NotBeNull();
        match!.PlaybookId.Should().Be("pytest_import_remediation");
        match.InjectionText.Should().Contain("SKIP:");
        match.InjectionText.Should().Contain("run_tests");
    }
}
