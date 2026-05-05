using FluentAssertions;
using Libr4.IDE.Application.AutonomousAppGeneration.Services.Templates;
using Xunit;

namespace Libr4.IntegrationTests.IDE;

public sealed class FallbackTemplateEngineTests
{
    private readonly ScribanFallbackTemplateEngine _sut = new();

    [Fact]
    public void Render_DotNetReadme_HasDotNetCommands()
    {
        var ctx = new FallbackTemplateContext
        {
            AppName = "MyApi",
            Stack = "dotnet",
            Language = "csharp",
            Framework = "asp.net",
            Database = "postgres",
            Port = 5000
        };

        var rendered = _sut.Render(FallbackArtefactTemplates.Readme, ctx);

        rendered.Should().Contain("# MyApi");
        rendered.Should().Contain("dotnet restore");
        rendered.Should().NotContain("npm install"); // exclusive branch
        rendered.Should().NotContain("uvicorn");
    }

    [Fact]
    public void Render_PythonReadme_HasPythonCommands()
    {
        var ctx = new FallbackTemplateContext
        {
            AppName = "PyApi", Stack = "python", Language = "python", Framework = "fastapi",
            Database = "postgres", Port = 8000
        };

        var rendered = _sut.Render(FallbackArtefactTemplates.Readme, ctx);

        rendered.Should().Contain("# PyApi");
        rendered.Should().Contain("uvicorn");
        rendered.Should().Contain("port 8000");
        rendered.Should().NotContain("dotnet restore");
    }

    [Fact]
    public void Render_NodeReadme_HasNodeCommands()
    {
        var ctx = new FallbackTemplateContext
        {
            AppName = "NodeApi", Stack = "node", Language = "javascript", Framework = "express",
            Database = "postgres", Port = 3000
        };

        var rendered = _sut.Render(FallbackArtefactTemplates.Readme, ctx);

        rendered.Should().Contain("npm install");
        rendered.Should().NotContain("dotnet restore");
    }

    [Fact]
    public void Render_DockerCompose_BindsAppNameAndPort()
    {
        var ctx = new FallbackTemplateContext { AppName = "X", Stack = "node", Port = 3000 };

        var rendered = _sut.Render(FallbackArtefactTemplates.DockerCompose, ctx);

        rendered.Should().Contain("PORT=3000");
        rendered.Should().Contain("\"3000:3000\"");
        rendered.Should().Contain("postgres:15");
    }

    [Fact]
    public void Render_CiWorkflow_HasStackSpecificStep()
    {
        var pyCtx = new FallbackTemplateContext { Stack = "python", AppName = "X" };
        var pyOut = _sut.Render(FallbackArtefactTemplates.CiWorkflow, pyCtx);
        pyOut.Should().Contain("setup-python@v5");
        pyOut.Should().Contain("pytest");

        var dotnetCtx = new FallbackTemplateContext { Stack = "dotnet", AppName = "X" };
        var dotnetOut = _sut.Render(FallbackArtefactTemplates.CiWorkflow, dotnetCtx);
        dotnetOut.Should().Contain("setup-dotnet@v4");
    }

    [Fact]
    public void Render_TemplateWithSyntaxError_ReturnsErrorComment_NoException()
    {
        var ctx = new FallbackTemplateContext { AppName = "X" };
        var brokenTemplate = "{{ if missing_close ";

        var rendered = _sut.Render(brokenTemplate, ctx);

        rendered.Should().StartWith("# template_parse_error");
    }

    [Fact]
    public void Render_AppNameWithQuotes_IsSanitized()
    {
        var ctx = new FallbackTemplateContext { AppName = "Bad\"Name\nHere", Stack = "dotnet", Port = 4000 };

        var rendered = _sut.Render(FallbackArtefactTemplates.Readme, ctx);

        rendered.Should().NotContain("\"");
        rendered.Should().Contain("Bad");
    }

    [Fact]
    public void Render_SecurityBaseline_BindsDatabaseAndAppName()
    {
        var ctx = new FallbackTemplateContext { AppName = "Sec", Database = "mysql" };

        var rendered = _sut.Render(FallbackArtefactTemplates.SecurityBaseline, ctx);

        rendered.Should().Contain("Sec");
        rendered.Should().Contain("mysql");
    }
}
