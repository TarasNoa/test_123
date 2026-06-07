using Libr4.IDE.Domain.AutonomousAppGeneration;

namespace Libr4.IDE.Application.AutonomousAppGeneration.Infrastructure.StackRecovery.Ecosystems;

/// <summary>Canonical golden paths for Tier 1 (deep) and Tier 2 (standard) autonomous generation.</summary>
public static class GoldenStackPathRegistry
{
    private static readonly Lazy<IReadOnlyList<GoldenStackPath>> All = new(BuildAll);

    public static IReadOnlyList<GoldenStackPath> AllPaths => All.Value;

    public static GoldenStackPath? FindById(string id) =>
        All.Value.FirstOrDefault(p => string.Equals(p.Id, id, StringComparison.OrdinalIgnoreCase));

    public static GoldenStackPath? DetectFromRequest(string? userRequest, GenerationPlan? plan = null)
    {
        if (string.IsNullOrWhiteSpace(userRequest) && plan is null)
            return null;

        var blob = string.Join(' ',
            new[]
            {
                userRequest ?? string.Empty,
                plan?.ApplicationDescription ?? string.Empty,
                plan is null ? string.Empty : string.Join(' ', plan.TechStack.Languages),
                plan is null ? string.Empty : string.Join(' ', plan.TechStack.Frameworks)
            }).ToLowerInvariant();

        if (blob.Contains("django", StringComparison.Ordinal) && blob.Contains("solidjs", StringComparison.Ordinal))
            return FindById("python-django-solidjs");

        var ranked = All.Value
            .Select(path => (path, score: ScorePathMatch(blob, path)))
            .Where(x => x.score > 0)
            .OrderByDescending(x => x.score)
            .ThenByDescending(x => x.path.Tier == EcosystemSupportTier.Tier1 ? 1 : 0);

        return ranked.Select(x => x.path).FirstOrDefault();
    }

    private static int ScorePathMatch(string blob, GoldenStackPath path)
    {
        if (!MatchesPath(blob, path))
            return 0;

        var score = path.BackendFrameworks.Count * 10 + path.FrontendFrameworks.Count * 5;
        if (path.Languages.FirstOrDefault() is { } primary && ContainsLanguageToken(blob, primary))
            score += 8;
        return score;
    }

    public static bool MatchesPath(string blob, GoldenStackPath path)
    {
        var primaryLang = path.Languages.FirstOrDefault();
        var primaryLangHit = primaryLang is not null && ContainsLanguageToken(blob, primaryLang);
        var backendHit = path.BackendFrameworks.Any(fw => blob.Contains(fw, StringComparison.OrdinalIgnoreCase));

        var stackRecognized = path.BackendFrameworks.Count > 0
            ? backendHit && (primaryLangHit || path.Languages.Count == 1)
            : primaryLangHit;

        if (!stackRecognized)
            return false;

        if (path.FrontendFrameworks.Count == 0)
            return true;

        var frontendHit = path.FrontendFrameworks.Any(fw => blob.Contains(fw, StringComparison.OrdinalIgnoreCase));
        if (!frontendHit && path.Id is "vue-nuxt" or "angular-frontend")
            return false;

        return frontendHit;
    }

    private static bool ContainsLanguageToken(string blob, string language)
    {
        var token = language.ToLowerInvariant();
        return token switch
        {
            "c#" => blob.Contains("c#", StringComparison.Ordinal) || blob.Contains("csharp", StringComparison.Ordinal),
            "go" => blob.Contains("golang", StringComparison.Ordinal) || blob.Contains(" go ", StringComparison.Ordinal),
            "java" => blob.Contains("java", StringComparison.Ordinal) && !blob.Contains("javascript", StringComparison.Ordinal),
            _ => blob.Contains(token, StringComparison.Ordinal)
        };
    }

    private static IReadOnlyList<GoldenStackPath> BuildAll() =>
    [
        // --- Tier 1: ~80% market, Java banking is the reference implementation ---
        Path(
            "java-spring-react",
            "Java + Spring Boot + React",
            EcosystemSupportTier.Tier1,
            RemediationDepth.GoldenPath,
            ["Java", "TypeScript"],
            ["Spring Boot"],
            ["React"],
            "backend/ + frontend/",
            "eclipse-temurin:21-jdk",
            ["cd backend && mvn -B -ntp -DskipTests package", "cd frontend && npm ci && npm run build"],
            ["cd backend && mvn -B -ntp test", "cd frontend && npm test -- --watch=false"],
            "[[JAVA_REACT_FULLSTACK]]"),

        Path(
            "csharp-aspnet-react",
            "C# + ASP.NET Core + React",
            EcosystemSupportTier.Tier1,
            RemediationDepth.GoldenPath,
            ["C#", "TypeScript"],
            ["ASP.NET Core"],
            ["React"],
            "backend/ + frontend/",
            "mcr.microsoft.com/dotnet/sdk:8.0",
            ["cd backend && dotnet restore && dotnet build", "cd frontend && npm ci && npm run build"],
            ["cd backend && dotnet test", "cd frontend && npm test -- --watch=false"],
            "[[ASPNET_REACT_FULLSTACK]]"),

        Path(
            "python-fastapi-react",
            "Python + FastAPI + React",
            EcosystemSupportTier.Tier1,
            RemediationDepth.GoldenPath,
            ["Python", "TypeScript"],
            ["FastAPI"],
            ["React"],
            "backend/ + frontend/",
            "python:3.12-slim",
            ["cd backend && python -m pip install -r requirements.txt && python -m compileall .", "cd frontend && npm ci && npm run build"],
            ["cd backend && pytest -q", "cd frontend && npm test -- --watch=false"],
            "[[FASTAPI_REACT_FULLSTACK]]"),

        Path(
            "python-django",
            "Python + Django",
            EcosystemSupportTier.Tier1,
            RemediationDepth.GoldenPath,
            ["Python"],
            ["Django"],
            [],
            "backend/",
            "python:3.12-slim",
            ["cd backend && python -m pip install -r requirements.txt && python manage.py check"],
            ["cd backend && python manage.py test"],
            "[[DJANGO_BACKEND]]"),

        Path(
            "python-django-solidjs",
            "Python + Django + SolidJS",
            EcosystemSupportTier.Tier1,
            RemediationDepth.GoldenPath,
            ["Python", "TypeScript"],
            ["Django"],
            ["SolidJS"],
            "backend/ + frontend/",
            "python:3.12-slim",
            [
                "cd backend && python -m pip install -r requirements.txt && python manage.py check",
                "cd frontend && npm ci && npm run build"
            ],
            [
                "cd backend && python manage.py test",
                "cd frontend && npm test -- --watch=false"
            ],
            "[[DJANGO_SOLIDJS_FULLSTACK]]"),

        Path(
            "typescript-nextjs-fullstack",
            "TypeScript + Next.js Fullstack",
            EcosystemSupportTier.Tier1,
            RemediationDepth.GoldenPath,
            ["TypeScript"],
            ["Next.js"],
            ["React"],
            "app/",
            "node:20",
            ["npm ci && npm run build"],
            ["npm test -- --watch=false"],
            "[[NEXTJS_FULLSTACK]]"),

        Path(
            "typescript-nestjs-react",
            "TypeScript + NestJS + React",
            EcosystemSupportTier.Tier1,
            RemediationDepth.GoldenPath,
            ["TypeScript"],
            ["NestJS"],
            ["React"],
            "backend/ + frontend/",
            "node:20",
            ["cd backend && npm ci && npm run build", "cd frontend && npm ci && npm run build"],
            ["cd backend && npm test", "cd frontend && npm test -- --watch=false"],
            "[[NESTJS_REACT_FULLSTACK]]"),

        Path(
            "javascript-express",
            "JavaScript + Express",
            EcosystemSupportTier.Tier1,
            RemediationDepth.GoldenPath,
            ["JavaScript"],
            ["Express"],
            [],
            "backend/",
            "node:20",
            ["cd backend && npm ci && npm run build"],
            ["cd backend && npm test"],
            "[[EXPRESS_BACKEND]]"),

        Path(
            "go-gin-react",
            "Go + Gin + React",
            EcosystemSupportTier.Tier1,
            RemediationDepth.GoldenPath,
            ["Go", "TypeScript"],
            ["Gin"],
            ["React"],
            "backend/ + frontend/",
            "golang:1.22",
            ["cd backend && go build -o /tmp/app ./...", "cd frontend && npm ci && npm run build"],
            ["cd backend && go test ./...", "cd frontend && npm test -- --watch=false"],
            "[[GO_GIN_REACT_FULLSTACK]]"),

        Path(
            "rust-axum",
            "Rust + Axum",
            EcosystemSupportTier.Tier1,
            RemediationDepth.GoldenPath,
            ["Rust"],
            ["Axum"],
            [],
            "backend/",
            "rust:1.77",
            ["cd backend && cargo build --release"],
            ["cd backend && cargo test"],
            "[[RUST_AXUM_BACKEND]]"),

        Path(
            "php-laravel-vue",
            "PHP + Laravel + Vue",
            EcosystemSupportTier.Tier1,
            RemediationDepth.GoldenPath,
            ["PHP", "TypeScript"],
            ["Laravel"],
            ["Vue"],
            "backend/ + frontend/",
            "php:8.3-cli",
            ["cd backend && composer install && php artisan config:clear", "cd frontend && npm ci && npm run build"],
            ["cd backend && php artisan test", "cd frontend && npm test -- --watch=false"],
            "[[LARAVEL_VUE_FULLSTACK]]"),

        // --- Tier 2: plan alignment + standard remediation ---
        Path(
            "kotlin-spring",
            "Kotlin + Spring Boot",
            EcosystemSupportTier.Tier2,
            RemediationDepth.Standard,
            ["Kotlin"],
            ["Spring Boot"],
            [],
            "backend/",
            "eclipse-temurin:21-jdk",
            ["cd backend && ./gradlew build -x test"],
            ["cd backend && ./gradlew test"],
            "[[KOTLIN_SPRING_BACKEND]]"),

        Path(
            "ruby-rails",
            "Ruby on Rails",
            EcosystemSupportTier.Tier2,
            RemediationDepth.Standard,
            ["Ruby"],
            ["Ruby on Rails"],
            [],
            "backend/",
            "ruby:3.3",
            ["cd backend && bundle install && bin/rails assets:precompile"],
            ["cd backend && bin/rails test"],
            "[[RAILS_BACKEND]]"),

        Path(
            "elixir-phoenix",
            "Elixir + Phoenix",
            EcosystemSupportTier.Tier2,
            RemediationDepth.Standard,
            ["Elixir"],
            ["Phoenix"],
            [],
            "backend/",
            "elixir:1.16",
            ["cd backend && mix deps.get && mix compile"],
            ["cd backend && mix test"],
            "[[PHOENIX_BACKEND]]"),

        Path(
            "dart-flutter",
            "Dart + Flutter",
            EcosystemSupportTier.Tier2,
            RemediationDepth.Standard,
            ["Dart"],
            ["Flutter"],
            [],
            "app/",
            "ghcr.io/cirruslabs/flutter:stable",
            ["flutter pub get && flutter build apk --debug"],
            ["flutter test"],
            "[[FLUTTER_APP]]"),

        Path(
            "swift-swiftui",
            "Swift + SwiftUI",
            EcosystemSupportTier.Tier2,
            RemediationDepth.Standard,
            ["Swift"],
            ["SwiftUI"],
            [],
            "app/",
            "swift:5.10",
            ["swift build"],
            ["swift test"],
            "[[SWIFTUI_APP]]"),

        Path(
            "react-native",
            "React Native",
            EcosystemSupportTier.Tier2,
            RemediationDepth.Standard,
            ["TypeScript"],
            ["React Native"],
            [],
            "app/",
            "node:20",
            ["npm ci && npx react-native build-android"],
            ["npm test"],
            "[[REACT_NATIVE_APP]]"),

        Path(
            "angular-frontend",
            "Angular",
            EcosystemSupportTier.Tier2,
            RemediationDepth.Standard,
            ["TypeScript"],
            ["Angular"],
            [],
            "frontend/",
            "node:20",
            ["cd frontend && npm ci && npm run build"],
            ["cd frontend && npm test -- --watch=false"],
            "[[ANGULAR_FRONTEND]]"),

        Path(
            "vue-nuxt",
            "Vue + Nuxt",
            EcosystemSupportTier.Tier2,
            RemediationDepth.Standard,
            ["TypeScript"],
            ["Nuxt", "Vue"],
            [],
            "frontend/",
            "node:20",
            ["cd frontend && npm ci && npm run build"],
            ["cd frontend && npm test -- --watch=false"],
            "[[NUXT_FRONTEND]]")
    ];

    private static GoldenStackPath Path(
        string id,
        string displayName,
        EcosystemSupportTier tier,
        RemediationDepth depth,
        string[] languages,
        string[] backend,
        string[] frontend,
        string layout,
        string runtime,
        string[] build,
        string[] test,
        string marker) =>
        new(id, displayName, tier, depth, languages, backend, frontend, layout, runtime, build, test, marker);
}
