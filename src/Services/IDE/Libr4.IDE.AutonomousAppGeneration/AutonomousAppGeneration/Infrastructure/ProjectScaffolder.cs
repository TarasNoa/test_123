using System.Text;
using Libr4.IDE.Domain.AutonomousAppGeneration;

namespace Libr4.IDE.Application.AutonomousAppGeneration.Infrastructure;

/// <summary>
/// Produces the deterministic structural spine of a .NET project (solution +
/// project files + common metadata) directly from a <see cref="GenerationPlan"/>.
/// Running this before the LLM guarantees these formulaic files are never missing
/// due to streaming truncation and keeps LLM budget for real code.
/// </summary>
internal static class ProjectScaffolder
{
    public static IReadOnlyList<GeneratedFile> Scaffold(GenerationPlan plan)
    {
        // Only .NET stacks get deterministic scaffolding; other stacks fall through to pure LLM.
        if (!IsDotNet(plan)) return Array.Empty<GeneratedFile>();

        var appName = SanitizeAppName(plan.ApplicationName);
        var hasBlazor = HasAny(plan, "blazor");
        var hasEfCore = HasAny(plan, "ef core", "entityframework", "entity framework");
        var hasPostgres = plan.TechStack.Databases.Any(d => d.Contains("postgres", StringComparison.OrdinalIgnoreCase));
        var hasSerilog = HasAny(plan, "serilog");
        var hasFluentValidation = HasAny(plan, "fluentvalidation", "fluent validation");
        var hasJwt = HasAny(plan, "jwt", "identityserver", "authentication") ||
                     plan.ApplicationDescription.Contains("auth", StringComparison.OrdinalIgnoreCase) ||
                     plan.ApplicationDescription.Contains("login", StringComparison.OrdinalIgnoreCase);

        var apiProject = $"{appName}.Api";
        var clientProject = $"{appName}.Client";
        var testsProject = $"{appName}.Tests";

        var files = new List<GeneratedFile>();

        // API project file
        files.Add(new GeneratedFile(
            $"src/{apiProject}/{apiProject}.csproj",
            "xml",
            BuildApiCsproj(hasEfCore, hasPostgres, hasSerilog, hasFluentValidation, hasJwt)));

        // Tests project file
        files.Add(new GeneratedFile(
            $"tests/{testsProject}/{testsProject}.csproj",
            "xml",
            BuildTestsCsproj(apiProject)));

        // Blazor client project file (only when stack calls for it)
        if (hasBlazor)
        {
            files.Add(new GeneratedFile(
                $"src/{clientProject}/{clientProject}.csproj",
                "xml",
                BuildBlazorClientCsproj()));
        }

        // Solution file wiring the projects together
        files.Add(new GeneratedFile(
            $"{appName}.sln",
            "text",
            BuildSolution(appName, apiProject, testsProject, hasBlazor ? clientProject : null)));

        // Global.json pins the SDK so the sandbox picks the right toolchain.
        files.Add(new GeneratedFile(
            "global.json",
            "json",
            "{\n  \"sdk\": {\n    \"version\": \"8.0.0\",\n    \"rollForward\": \"latestFeature\"\n  }\n}\n"));

        // Directory.Build.props applies common build settings to every project.
        files.Add(new GeneratedFile(
            "Directory.Build.props",
            "xml",
            "<Project>\n  <PropertyGroup>\n    <TargetFramework>net8.0</TargetFramework>\n    <Nullable>enable</Nullable>\n    <ImplicitUsings>enable</ImplicitUsings>\n    <TreatWarningsAsErrors>false</TreatWarningsAsErrors>\n  </PropertyGroup>\n</Project>\n"));

        return files;
    }

    /// <summary>
    /// Scaffold .NET solution/project files only when the stack is actually .NET.
    /// Using <see cref="GenerationPlan.RuntimeImage"/> alone is unsafe: a mistaken dotnet image
    /// with Python languages would inject .csproj into a Flask workspace and break <c>dotnet build</c> gates.
    /// </summary>
    private static bool IsDotNet(GenerationPlan plan)
    {
        var langs = plan.TechStack.Languages;
        var fw = plan.TechStack.Frameworks;

        var explicitDotNet = langs.Any(l =>
            l.Contains("c#", StringComparison.OrdinalIgnoreCase) ||
            l.Contains("csharp", StringComparison.OrdinalIgnoreCase) ||
            l.Contains(".net", StringComparison.OrdinalIgnoreCase)) ||
            fw.Any(f =>
                f.Contains("asp.net", StringComparison.OrdinalIgnoreCase) ||
                f.Contains("aspnet", StringComparison.OrdinalIgnoreCase) ||
                f.Contains("blazor", StringComparison.OrdinalIgnoreCase));

        if (explicitDotNet) return true;

        var explicitOther = langs.Any(l =>
            l.Contains("python", StringComparison.OrdinalIgnoreCase) ||
            l.Equals("py", StringComparison.OrdinalIgnoreCase) ||
            l.Contains("javascript", StringComparison.OrdinalIgnoreCase) ||
            l.Contains("typescript", StringComparison.OrdinalIgnoreCase) ||
            l.Equals("node", StringComparison.OrdinalIgnoreCase) ||
            l.Contains("go", StringComparison.OrdinalIgnoreCase) ||
            l.Contains("rust", StringComparison.OrdinalIgnoreCase));

        if (explicitOther) return false;

        return plan.RuntimeImage.Contains("dotnet", StringComparison.OrdinalIgnoreCase);
    }

    private static bool HasAny(GenerationPlan plan, params string[] needles) =>
        needles.Any(n =>
            plan.TechStack.Frameworks.Any(f => f.Contains(n, StringComparison.OrdinalIgnoreCase)) ||
            plan.TechStack.Infrastructure.Any(i => i.Contains(n, StringComparison.OrdinalIgnoreCase)) ||
            plan.TechStack.Rationale.Contains(n, StringComparison.OrdinalIgnoreCase));

    private static string SanitizeAppName(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return "GeneratedApp";
        var sb = new StringBuilder();
        bool upperNext = true;
        foreach (var ch in raw)
        {
            if (char.IsLetterOrDigit(ch))
            {
                sb.Append(upperNext ? char.ToUpperInvariant(ch) : ch);
                upperNext = false;
            }
            else
            {
                upperNext = true;
            }
        }
        var name = sb.ToString();
        if (string.IsNullOrEmpty(name)) return "GeneratedApp";
        if (char.IsDigit(name[0])) name = "App" + name;
        return name;
    }

    private static string BuildApiCsproj(bool ef, bool postgres, bool serilog, bool fv, bool jwt)
    {
        var sb = new StringBuilder();
        sb.AppendLine("<Project Sdk=\"Microsoft.NET.Sdk.Web\">");
        sb.AppendLine();
        sb.AppendLine("  <PropertyGroup>");
        sb.AppendLine("    <TargetFramework>net8.0</TargetFramework>");
        sb.AppendLine("    <Nullable>enable</Nullable>");
        sb.AppendLine("    <ImplicitUsings>enable</ImplicitUsings>");
        sb.AppendLine("  </PropertyGroup>");
        sb.AppendLine();
        sb.AppendLine("  <ItemGroup>");
        sb.AppendLine("    <PackageReference Include=\"Swashbuckle.AspNetCore\" Version=\"6.6.2\" />");
        if (ef)
        {
            sb.AppendLine("    <PackageReference Include=\"Microsoft.EntityFrameworkCore\" Version=\"8.0.8\" />");
            sb.AppendLine("    <PackageReference Include=\"Microsoft.EntityFrameworkCore.Design\" Version=\"8.0.8\" />");
        }
        if (postgres && ef)
        {
            sb.AppendLine("    <PackageReference Include=\"Npgsql.EntityFrameworkCore.PostgreSQL\" Version=\"8.0.4\" />");
        }
        if (serilog)
        {
            sb.AppendLine("    <PackageReference Include=\"Serilog.AspNetCore\" Version=\"8.0.2\" />");
            sb.AppendLine("    <PackageReference Include=\"Serilog.Sinks.Console\" Version=\"6.0.0\" />");
        }
        if (fv)
        {
            sb.AppendLine("    <PackageReference Include=\"FluentValidation.AspNetCore\" Version=\"11.3.0\" />");
        }
        if (jwt)
        {
            sb.AppendLine("    <PackageReference Include=\"Microsoft.AspNetCore.Authentication.JwtBearer\" Version=\"8.0.8\" />");
            sb.AppendLine("    <PackageReference Include=\"System.IdentityModel.Tokens.Jwt\" Version=\"8.0.2\" />");
        }
        sb.AppendLine("  </ItemGroup>");
        sb.AppendLine();
        sb.AppendLine("</Project>");
        return sb.ToString();
    }

    private static string BuildTestsCsproj(string apiProject)
    {
        var sb = new StringBuilder();
        sb.AppendLine("<Project Sdk=\"Microsoft.NET.Sdk\">");
        sb.AppendLine();
        sb.AppendLine("  <PropertyGroup>");
        sb.AppendLine("    <TargetFramework>net8.0</TargetFramework>");
        sb.AppendLine("    <Nullable>enable</Nullable>");
        sb.AppendLine("    <ImplicitUsings>enable</ImplicitUsings>");
        sb.AppendLine("    <IsPackable>false</IsPackable>");
        sb.AppendLine("  </PropertyGroup>");
        sb.AppendLine();
        sb.AppendLine("  <ItemGroup>");
        sb.AppendLine("    <PackageReference Include=\"Microsoft.NET.Test.Sdk\" Version=\"17.11.1\" />");
        sb.AppendLine("    <PackageReference Include=\"xunit\" Version=\"2.9.0\" />");
        sb.AppendLine("    <PackageReference Include=\"xunit.runner.visualstudio\" Version=\"2.8.2\" />");
        sb.AppendLine("    <PackageReference Include=\"Microsoft.AspNetCore.Mvc.Testing\" Version=\"8.0.8\" />");
        sb.AppendLine("    <PackageReference Include=\"Moq\" Version=\"4.20.72\" />");
        sb.AppendLine("    <PackageReference Include=\"FluentAssertions\" Version=\"6.12.1\" />");
        sb.AppendLine("  </ItemGroup>");
        sb.AppendLine();
        sb.AppendLine("  <ItemGroup>");
        sb.AppendLine($"    <ProjectReference Include=\"..\\..\\src\\{apiProject}\\{apiProject}.csproj\" />");
        sb.AppendLine("  </ItemGroup>");
        sb.AppendLine();
        sb.AppendLine("</Project>");
        return sb.ToString();
    }

    private static string BuildBlazorClientCsproj()
    {
        var sb = new StringBuilder();
        sb.AppendLine("<Project Sdk=\"Microsoft.NET.Sdk.BlazorWebAssembly\">");
        sb.AppendLine();
        sb.AppendLine("  <PropertyGroup>");
        sb.AppendLine("    <TargetFramework>net8.0</TargetFramework>");
        sb.AppendLine("    <Nullable>enable</Nullable>");
        sb.AppendLine("    <ImplicitUsings>enable</ImplicitUsings>");
        sb.AppendLine("  </PropertyGroup>");
        sb.AppendLine();
        sb.AppendLine("  <ItemGroup>");
        sb.AppendLine("    <PackageReference Include=\"Microsoft.AspNetCore.Components.WebAssembly\" Version=\"8.0.8\" />");
        sb.AppendLine("    <PackageReference Include=\"Microsoft.AspNetCore.Components.WebAssembly.DevServer\" Version=\"8.0.8\" PrivateAssets=\"all\" />");
        sb.AppendLine("    <PackageReference Include=\"Microsoft.AspNetCore.Components.WebAssembly.Authentication\" Version=\"8.0.8\" />");
        sb.AppendLine("  </ItemGroup>");
        sb.AppendLine();
        sb.AppendLine("</Project>");
        return sb.ToString();
    }

    private static string BuildSolution(string appName, string apiProject, string testsProject, string? clientProject)
    {
        // Project type GUIDs:
        //   {9A19103F-16F7-4668-BE54-9A1E7A4F7556} - SDK-style C# project
        var projectType = "{9A19103F-16F7-4668-BE54-9A1E7A4F7556}";
        var apiGuid = "{11111111-1111-1111-1111-111111111111}";
        var testsGuid = "{22222222-2222-2222-2222-222222222222}";
        var clientGuid = "{33333333-3333-3333-3333-333333333333}";

        var sb = new StringBuilder();
        sb.AppendLine("Microsoft Visual Studio Solution File, Format Version 12.00");
        sb.AppendLine("# Visual Studio Version 17");
        sb.AppendLine($"Project(\"{projectType}\") = \"{apiProject}\", \"src\\{apiProject}\\{apiProject}.csproj\", \"{apiGuid}\"");
        sb.AppendLine("EndProject");
        sb.AppendLine($"Project(\"{projectType}\") = \"{testsProject}\", \"tests\\{testsProject}\\{testsProject}.csproj\", \"{testsGuid}\"");
        sb.AppendLine("EndProject");
        if (clientProject is not null)
        {
            sb.AppendLine($"Project(\"{projectType}\") = \"{clientProject}\", \"src\\{clientProject}\\{clientProject}.csproj\", \"{clientGuid}\"");
            sb.AppendLine("EndProject");
        }
        sb.AppendLine("Global");
        sb.AppendLine("\tGlobalSection(SolutionConfigurationPlatforms) = preSolution");
        sb.AppendLine("\t\tDebug|Any CPU = Debug|Any CPU");
        sb.AppendLine("\t\tRelease|Any CPU = Release|Any CPU");
        sb.AppendLine("\tEndGlobalSection");
        sb.AppendLine("\tGlobalSection(ProjectConfigurationPlatforms) = postSolution");
        foreach (var guid in new[] { apiGuid, testsGuid, clientProject is null ? null : clientGuid })
        {
            if (guid is null) continue;
            sb.AppendLine($"\t\t{guid}.Debug|Any CPU.ActiveCfg = Debug|Any CPU");
            sb.AppendLine($"\t\t{guid}.Debug|Any CPU.Build.0 = Debug|Any CPU");
            sb.AppendLine($"\t\t{guid}.Release|Any CPU.ActiveCfg = Release|Any CPU");
            sb.AppendLine($"\t\t{guid}.Release|Any CPU.Build.0 = Release|Any CPU");
        }
        sb.AppendLine("\tEndGlobalSection");
        sb.AppendLine("EndGlobal");
        return sb.ToString();
    }
}
