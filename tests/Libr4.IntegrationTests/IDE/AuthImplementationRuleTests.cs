using FluentAssertions;
using Libr4.IDE.Application.AutonomousAppGeneration.Services;
using Libr4.IDE.Application.AutonomousAppGeneration.Services.Rules;
using Libr4.IDE.Domain.AutonomousAppGeneration;
using Xunit;

namespace Libr4.IntegrationTests.IDE;

public sealed class AuthImplementationRuleTests
{
    private readonly AuthImplementationRule_DotNet _sut = new();

    [Fact]
    public async Task Pass_When_AddAuthentication_Wired_In_Pipeline()
    {
        var files = new[]
        {
            new GeneratedFile("src/App/Program.cs", "csharp",
                @"var builder = WebApplication.CreateBuilder(args);
builder.Services.AddAuthentication().AddJwtBearer(o => { o.Authority = ""https://x""; });
var app = builder.Build();
app.UseAuthentication();
app.UseAuthorization();
app.Run();
")
        };

        var outcome = await _sut.EvaluateAsync(files, MakeDotNetPlan(), CancellationToken.None);

        outcome.Satisfied.Should().BeTrue();
        outcome.Detail.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Pass_When_Authorize_Attribute_Used()
    {
        var files = new[]
        {
            new GeneratedFile("src/App/Controllers/SecureController.cs", "csharp",
                @"using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
[Authorize]
public class SecureController : ControllerBase
{
    public IActionResult Index() => Ok();
}")
        };

        var outcome = await _sut.EvaluateAsync(files, MakeDotNetPlan(), CancellationToken.None);

        outcome.Satisfied.Should().BeTrue();
    }

    [Fact]
    public async Task Fail_When_Auth_Only_In_Comments_Or_Readme()
    {
        // Critical: legacy substring rule passed on a README/comment mentioning JWT.
        // Roslyn rule must reject because nothing wires the pipeline.
        var files = new[]
        {
            new GeneratedFile("src/App/Program.cs", "csharp",
                @"// JWT is configured later
// AddAuthentication should be called here in production
var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();
app.MapGet(""/"", () => ""hello"");
app.Run();
"),
            new GeneratedFile("README.md", "markdown",
                @"# Auth
We use JWT and OAuth and Authorize attributes everywhere.
")
        };

        var outcome = await _sut.EvaluateAsync(files, MakeDotNetPlan(), CancellationToken.None);

        outcome.Satisfied.Should().BeFalse();
        outcome.RemediationHint.Should().NotBeNull();
    }

    [Fact]
    public void AppliesTo_PythonPlan_ReturnsFalse()
    {
        var pythonPlan = new GenerationPlan(
            applicationName: "PyApp",
            applicationDescription: "Build Python service",
            techStack: new TechStack(
                new[] { "python" },
                new[] { "fastapi" },
                Array.Empty<string>(),
                Array.Empty<string>(),
                "test"),
            phases: Array.Empty<GenerationPhase>(),
            requiredAgents: Array.Empty<string>(),
            runtimeImage: "python:3.12",
            buildCommands: new[] { "pip install -r requirements.txt" },
            testCommands: new[] { "pytest" });

        _sut.AppliesTo(pythonPlan).Should().BeFalse();
    }

    private static GenerationPlan MakeDotNetPlan() => new GenerationPlan(
        applicationName: "DotNetApp",
        applicationDescription: "Build ASP.NET Core API",
        techStack: new TechStack(
            new[] { "C#" },
            new[] { "ASP.NET Core" },
            Array.Empty<string>(),
            Array.Empty<string>(),
            "test"),
        phases: Array.Empty<GenerationPhase>(),
        requiredAgents: Array.Empty<string>(),
        runtimeImage: "mcr.microsoft.com/dotnet/sdk:8.0",
        buildCommands: new[] { "dotnet build" },
        testCommands: new[] { "dotnet test" });
}
