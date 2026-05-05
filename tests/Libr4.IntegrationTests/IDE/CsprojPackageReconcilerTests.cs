using FluentAssertions;
using Libr4.IDE.Application.AutonomousAppGeneration.Infrastructure;
using Libr4.IDE.Domain.AutonomousAppGeneration;
using Xunit;

namespace Libr4.IntegrationTests.IDE;

/// <summary>
/// Audit P1-11: regression tests for the using-directive ↔ PackageReference reconciler.
/// </summary>
public sealed class CsprojPackageReconcilerTests
{
    private const string MinimalCsproj =
        """
        <Project Sdk="Microsoft.NET.Sdk.Web">
          <PropertyGroup>
            <TargetFramework>net8.0</TargetFramework>
          </PropertyGroup>
          <ItemGroup>
            <PackageReference Include="Microsoft.AspNetCore.Authentication.JwtBearer" Version="8.0.8" />
          </ItemGroup>
        </Project>
        """;

    [Fact]
    public void ReconcilePackages_AddsMissingOpenTelemetryAndPolly()
    {
        var program = new GeneratedFile(
            "src/App/Program.cs",
            "csharp",
            """
            using OpenTelemetry.Metrics;
            using OpenTelemetry.Trace;
            using Polly;
            using Serilog;
            var builder = WebApplication.CreateBuilder(args);
            """);
        var csproj = new GeneratedFile("src/App/App.csproj", "xml", MinimalCsproj);
        var files = new List<GeneratedFile> { program, csproj };

        var added = CsprojPackageReconciler.ReconcilePackages(files);

        added.Should().BeGreaterThan(0);
        csproj.Content.Should().Contain("OpenTelemetry.Extensions.Hosting");
        csproj.Content.Should().Contain("Polly");
        csproj.Content.Should().Contain("Serilog");
    }

    [Fact]
    public void ReconcilePackages_DoesNotDuplicateExistingPackages()
    {
        var program = new GeneratedFile(
            "src/App/Program.cs",
            "csharp",
            "using Microsoft.AspNetCore.Authentication.JwtBearer;");
        var csproj = new GeneratedFile("src/App/App.csproj", "xml", MinimalCsproj);
        var files = new List<GeneratedFile> { program, csproj };

        var added = CsprojPackageReconciler.ReconcilePackages(files);

        added.Should().Be(0);
        // Already present once; should still be present exactly once.
        var occurrences = System.Text.RegularExpressions.Regex.Matches(
            csproj.Content,
            @"PackageReference\s+Include=""Microsoft\.AspNetCore\.Authentication\.JwtBearer""")
            .Count;
        occurrences.Should().Be(1);
    }

    [Fact]
    public void ReconcilePackages_IgnoresBclAndOwnNamespaces()
    {
        var program = new GeneratedFile(
            "src/App/Program.cs",
            "csharp",
            """
            using System;
            using System.IO;
            using Microsoft.AspNetCore.Mvc;
            using Microsoft.Extensions.DependencyInjection;
            """);
        var csproj = new GeneratedFile("src/App/App.csproj", "xml", MinimalCsproj);
        var files = new List<GeneratedFile> { program, csproj };

        var added = CsprojPackageReconciler.ReconcilePackages(files);

        added.Should().Be(0);
    }

    [Fact]
    public void ReconcilePackages_OnlyTouchesOwnedCsproj()
    {
        // A plan with two projects: Api and Tests. Api uses Polly, Tests uses xUnit only.
        var apiCs = new GeneratedFile("src/Api/Program.cs", "csharp", "using Polly;");
        var apiCsproj = new GeneratedFile(
            "src/Api/Api.csproj",
            "xml",
            """<Project Sdk="Microsoft.NET.Sdk.Web"><ItemGroup><PackageReference Include="Microsoft.OpenApi" Version="1.6.21" /></ItemGroup></Project>""");
        var testsCs = new GeneratedFile(
            "tests/Tests/SmokeTests.cs",
            "csharp",
            "using Xunit;");
        var testsCsproj = new GeneratedFile(
            "tests/Tests/Tests.csproj",
            "xml",
            """<Project Sdk="Microsoft.NET.Sdk"><ItemGroup><PackageReference Include="xunit" Version="2.9.0" /></ItemGroup></Project>""");

        var files = new List<GeneratedFile> { apiCs, apiCsproj, testsCs, testsCsproj };
        var added = CsprojPackageReconciler.ReconcilePackages(files);

        added.Should().BeGreaterThan(0);
        apiCsproj.Content.Should().Contain("Polly");
        // Tests project should NOT have Polly added; it's not used in the test code.
        testsCsproj.Content.Should().NotContain("Polly");
    }

    [Fact]
    public void ReconcilePackages_EmptyInputIsNoop()
    {
        var added = CsprojPackageReconciler.ReconcilePackages(new List<GeneratedFile>());
        added.Should().Be(0);
    }
}
