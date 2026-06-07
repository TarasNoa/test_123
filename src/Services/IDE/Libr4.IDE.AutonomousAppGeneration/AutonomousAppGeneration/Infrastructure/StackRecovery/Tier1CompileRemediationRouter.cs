using Libr4.IDE.Application.AutonomousAppGeneration.Infrastructure;
using Libr4.IDE.Application.AutonomousAppGeneration.Infrastructure.StackRecovery.Ecosystems;
using Libr4.IDE.Application.AutonomousAppGeneration.Infrastructure.StackRecovery.Remediation;
using Libr4.IDE.Domain.AutonomousAppGeneration;

namespace Libr4.IDE.Application.AutonomousAppGeneration.Infrastructure.StackRecovery;

/// <summary>Routes Tier 1 golden-path compile remediation (Java banking is the reference depth).</summary>
public static class Tier1CompileRemediationRouter
{
    public static string? ResolveGoldenPathId(GenerationPlan plan, string? explicitId = null)
    {
        if (!string.IsNullOrWhiteSpace(explicitId))
            return explicitId;

        var detected = GoldenStackPathRegistry.DetectFromRequest(null, plan);
        return detected?.RemediationDepth == RemediationDepth.GoldenPath ? detected.Id : null;
    }

    public static int ApplyNormalize(IList<GeneratedFile> files, GenerationPlan plan, string? goldenPathId = null)
    {
        var pathId = ResolveGoldenPathId(plan, goldenPathId);
        if (pathId is null)
            return 0;

        return pathId switch
        {
            "java-spring-react" => JavaSpringCompileRemediation.Apply(files, plan, Array.Empty<ErrorReport>()),
            "csharp-aspnet-react" => AspNetReactCompileRemediation.Apply(files, plan, Array.Empty<ErrorReport>()),
            "python-fastapi-react" => FastApiReactCompileRemediation.Apply(files, plan, Array.Empty<ErrorReport>()),
            "python-django" or "python-django-solidjs" => DjangoCompileRemediation.Apply(files, plan, Array.Empty<ErrorReport>()),
            "typescript-nextjs-fullstack" => NextJsFullStackCompileRemediation.Apply(files, plan, Array.Empty<ErrorReport>()),
            "typescript-nestjs-react" => NestJsReactCompileRemediation.Apply(files, plan, Array.Empty<ErrorReport>()),
            "javascript-express" => ExpressCompileRemediation.Apply(files, plan, Array.Empty<ErrorReport>()),
            "go-gin-react" => GoGinReactCompileRemediation.Apply(files, plan, Array.Empty<ErrorReport>()),
            "rust-axum" => RustAxumCompileRemediation.Apply(files, plan, Array.Empty<ErrorReport>()),
            "php-laravel-vue" => LaravelVueCompileRemediation.Apply(files, plan, Array.Empty<ErrorReport>()),
            _ => 0
        };
    }

    public static int ApplyCompile(
        IList<GeneratedFile> files,
        GenerationPlan plan,
        IReadOnlyList<ErrorReport> errors,
        string? buildLog)
    {
        var pathId = ResolveGoldenPathId(plan);
        if (pathId is null)
            return 0;

        return pathId switch
        {
            "java-spring-react" => JavaSpringCompileRemediation.Apply(files, plan, errors),
            "csharp-aspnet-react" => AspNetReactCompileRemediation.Apply(files, plan, errors),
            "python-fastapi-react" => FastApiReactCompileRemediation.Apply(files, plan, errors),
            "python-django" => DjangoCompileRemediation.Apply(files, plan, errors),
            "typescript-nextjs-fullstack" => NextJsFullStackCompileRemediation.Apply(files, plan, errors),
            "typescript-nestjs-react" => NestJsReactCompileRemediation.Apply(files, plan, errors),
            "javascript-express" => ExpressCompileRemediation.Apply(files, plan, errors),
            "go-gin-react" => GoGinReactCompileRemediation.Apply(files, plan, errors),
            "rust-axum" => RustAxumCompileRemediation.Apply(files, plan, errors),
            "php-laravel-vue" => LaravelVueCompileRemediation.Apply(files, plan, errors),
            _ => 0
        };
    }
}
