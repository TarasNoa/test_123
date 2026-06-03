using FluentAssertions;
using Libr4.IDE.Application.AutonomousAppGeneration.Infrastructure;
using Libr4.IDE.Domain.AutonomousAppGeneration;
using Xunit;

namespace Libr4.IntegrationTests.IDE;

public sealed class RepoBootstrapHttpTestArtifactsTests
{
    [Fact]
    public void Apply_AddsWebApplicationFactoryTests_AndMvcTestingPackage()
    {
        var files = new List<GeneratedFile>
        {
            new("src/GeneratedApp.Api/Program.cs", "csharp", "var app = WebApplication.CreateBuilder(args).Build(); app.Run();"),
            new("tests/GeneratedApp.Api.Tests/GeneratedApp.Api.Tests.csproj", "xml",
                """
                <Project Sdk="Microsoft.NET.Sdk">
                  <ItemGroup>
                    <PackageReference Include="xunit" Version="2.9.0" />
                  </ItemGroup>
                </Project>
                """)
        };

        var changed = RepoBootstrapHttpTestArtifacts.Apply(files, "tests/GeneratedApp.Api.Tests");

        changed.Should().BeGreaterThan(0);
        files.Should().Contain(f => f.RelativePath == "tests/GeneratedApp.Api.Tests/KanbanAuthHttpTests.cs");
        files.Single(f => f.RelativePath == "tests/GeneratedApp.Api.Tests/KanbanAuthHttpTests.cs")
            .Content.Should().Contain("WebApplicationFactory<Program>");
        files.Single(f => f.RelativePath == "src/GeneratedApp.Api/Program.cs")
            .Content.Should().Contain("partial class Program");
        files.Single(f => f.RelativePath == "tests/GeneratedApp.Api.Tests/GeneratedApp.Api.Tests.csproj")
            .Content.Should().Contain("Microsoft.AspNetCore.Mvc.Testing");
    }
}
