using System.Diagnostics;
using FluentAssertions;
using Libr4.IDE.Application.AutonomousAppGeneration.Infrastructure;
using Libr4.IDE.Domain.AutonomousAppGeneration;
using Xunit;

namespace Libr4.IntegrationTests.IDE;

/// <summary>
/// Compile-time verification of the deterministic repo-bootstrap golden artifact set.
/// </summary>
public sealed class RepoBootstrapGoldenCompileTests
{
    [Fact]
    public async Task GoldenRepoBootstrapArtifacts_ShouldCompileAndPassDotNetTest()
    {
        var plan = new GenerationPlan(
            applicationName: "GeneratedApp",
            applicationDescription: "[[REPO_BOOTSTRAP_REQUIRED]]",
            techStack: new TechStack(
                languages: new[] { "C#" },
                frameworks: new[] { "ASP.NET Core" },
                databases: Array.Empty<string>(),
                infrastructure: Array.Empty<string>(),
                rationale: "golden"),
            phases: Array.Empty<GenerationPhase>(),
            requiredAgents: Array.Empty<string>(),
            runtimeImage: "mcr.microsoft.com/dotnet/sdk:8.0",
            buildCommands: new[] { "dotnet build" },
            testCommands: new[] { "dotnet test" },
            maxIterations: 3);

        const string bootstrap =
            """{"clone_url":"https://github.com/roovo/obsidian-card-board.git","repository":"roovo/obsidian-card-board","license":"MIT"}""";

        var files = BuildGoldenFiles();
        UpstreamProductIntegrator.ApplyDotNetIntegration(files, plan, bootstrap);
        UpstreamSemanticAdaptationEnricher.Apply(plan, files);
        RepoBootstrapHttpTestArtifacts.Apply(files, "tests/GeneratedApp.Api.Tests");
        CsprojPackageReconciler.ReconcilePackages(files);

        var workDir = Path.Combine(Path.GetTempPath(), "libr4-golden-" + Guid.NewGuid().ToString("N"));
        try
        {
            foreach (var file in files)
            {
                var path = Path.Combine(workDir, file.RelativePath.Replace('/', Path.DirectorySeparatorChar));
                Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                await File.WriteAllTextAsync(path, file.Content ?? string.Empty);
            }

            var buildCode = await RunDotNetAsync(workDir, "build", "tests/GeneratedApp.Api.Tests/GeneratedApp.Api.Tests.csproj");
            buildCode.Should().Be(0, "golden repo-bootstrap project should compile");

            var testCode = await RunDotNetAsync(workDir, "test", "tests/GeneratedApp.Api.Tests/GeneratedApp.Api.Tests.csproj", "--no-build");
            testCode.Should().Be(0, "golden repo-bootstrap HTTP tests should pass");
        }
        finally
        {
            TryDeleteDirectory(workDir);
        }
    }

    private static List<GeneratedFile> BuildGoldenFiles()
    {
        const string program = """
            using Microsoft.AspNetCore.Authentication.JwtBearer;
            using Microsoft.IdentityModel.Tokens;
            using System.Text;

            var builder = WebApplication.CreateBuilder(args);
            builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(o =>
                {
                    o.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidIssuer = "libr4",
                        ValidateAudience = true,
                        ValidAudience = "libr4-clients",
                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey = new SymmetricSecurityKey(
                            Encoding.UTF8.GetBytes("dev-only-secret-key-dev-only-secret-key"))
                    };
                });
            builder.Services.AddAuthorization();
            builder.Services.AddControllers();
            var app = builder.Build();
            app.UseAuthentication();
            app.UseAuthorization();
            app.MapControllers();
            app.Run();
            public partial class Program { }
            """;

        const string auth = """
            using Microsoft.AspNetCore.Mvc;
            using Microsoft.IdentityModel.Tokens;
            using System.IdentityModel.Tokens.Jwt;
            using System.Security.Claims;
            using System.Text;

            namespace GeneratedApp.Api.Controllers;

            [ApiController]
            [Route("api/auth")]
            public sealed class AuthController : ControllerBase
            {
                [HttpPost("token")]
                public IActionResult IssueToken()
                {
                    var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("dev-only-secret-key-dev-only-secret-key"));
                    var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
                    var token = new JwtSecurityToken(
                        issuer: "libr4",
                        audience: "libr4-clients",
                        claims: new[] { new Claim(ClaimTypes.NameIdentifier, "demo-user") },
                        expires: DateTime.UtcNow.AddHours(1),
                        signingCredentials: creds);
                    return Ok(new { access_token = new JwtSecurityTokenHandler().WriteToken(token) });
                }
            }
            """;

        return new List<GeneratedFile>
        {
            new("BOOTSTRAP_EVIDENCE.md", "markdown", "repository_url: https://github.com/example/repo license: MIT"),
            new("upstream/README.md", "markdown", "kanban board columns: \"Backlog\", \"In Progress\", \"Done\""),
            new("upstream/src/board.ts", "typescript", "export interface Card { id: string; } const lanes = ['Backlog','Done'];"),
            new("src/GeneratedApp.Api/GeneratedApp.Api.csproj", "xml",
                """
                <Project Sdk="Microsoft.NET.Sdk.Web">
                  <PropertyGroup>
                    <TargetFramework>net8.0</TargetFramework>
                    <Nullable>enable</Nullable>
                    <ImplicitUsings>enable</ImplicitUsings>
                  </PropertyGroup>
                  <ItemGroup>
                    <PackageReference Include="Microsoft.AspNetCore.Authentication.JwtBearer" Version="8.0.8" />
                    <PackageReference Include="Microsoft.IdentityModel.Tokens" Version="8.0.2" />
                    <PackageReference Include="System.IdentityModel.Tokens.Jwt" Version="8.0.2" />
                  </ItemGroup>
                </Project>
                """),
            new("src/GeneratedApp.Api/Program.cs", "csharp", program),
            new("src/GeneratedApp.Api/Controllers/AuthController.cs", "csharp", auth),
            new("tests/GeneratedApp.Api.Tests/GeneratedApp.Api.Tests.csproj", "xml",
                """
                <Project Sdk="Microsoft.NET.Sdk">
                  <PropertyGroup>
                    <TargetFramework>net8.0</TargetFramework>
                    <ImplicitUsings>enable</ImplicitUsings>
                    <Nullable>enable</Nullable>
                    <IsPackable>false</IsPackable>
                  </PropertyGroup>
                  <ItemGroup>
                    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.11.1" />
                    <PackageReference Include="xunit" Version="2.9.0" />
                    <PackageReference Include="xunit.runner.visualstudio" Version="2.8.2" />
                  </ItemGroup>
                  <ItemGroup>
                    <ProjectReference Include="../../src/GeneratedApp.Api/GeneratedApp.Api.csproj" />
                  </ItemGroup>
                </Project>
                """)
        };
    }

    private static async Task<int> RunDotNetAsync(string workDir, string command, string projectPath, string? extraArgs = null)
    {
        var args = $"{command} \"{projectPath}\"";
        if (!string.IsNullOrWhiteSpace(extraArgs))
            args += " " + extraArgs;

        var psi = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = args,
            WorkingDirectory = workDir,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = Process.Start(psi)!;
        var stdout = await process.StandardOutput.ReadToEndAsync();
        var stderr = await process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();
        if (process.ExitCode != 0)
            throw new InvalidOperationException($"dotnet {command} failed:\n{stdout}\n{stderr}");
        return process.ExitCode;
    }

    private static void TryDeleteDirectory(string path)
    {
        try
        {
            if (Directory.Exists(path))
                Directory.Delete(path, recursive: true);
        }
        catch
        {
            // best effort
        }
    }
}
