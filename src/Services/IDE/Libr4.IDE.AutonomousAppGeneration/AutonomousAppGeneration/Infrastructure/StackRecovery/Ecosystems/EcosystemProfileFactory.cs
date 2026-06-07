namespace Libr4.IDE.Application.AutonomousAppGeneration.Infrastructure.StackRecovery.Ecosystems;

/// <summary>Builds <see cref="EcosystemProfile"/> instances with conventional manifests and entry-point heuristics per language id.</summary>
internal static class EcosystemProfileFactory
{
    public static EcosystemProfile Language(
        string id,
        string displayName,
        params string[] hints) =>
        new()
        {
            Id = id,
            DisplayName = displayName,
            Category = EcosystemCategory.Language,
            LanguageHints = hints.Length > 0 ? hints : new[] { displayName },
            FileExtensionHints = InferExtensions(id),
            Manifests = InferManifests(id),
            EntryPoints = InferEntryPoints(id),
            DuplicateTypeNames = InferDuplicateTypes(id),
            BasePriority = 10
        };

    public static EcosystemProfile Framework(
        string id,
        string displayName,
        EcosystemCategory category,
        string[] frameworkHints,
        ManifestRule[]? manifests = null,
        EntryPointRule[]? entryPoints = null,
        string[]? duplicateTypes = null,
        string[]? extensions = null,
        int basePriority = 20) =>
        new()
        {
            Id = id,
            DisplayName = displayName,
            Category = category,
            FrameworkHints = frameworkHints,
            FileExtensionHints = extensions ?? Array.Empty<string>(),
            Manifests = manifests ?? Array.Empty<ManifestRule>(),
            EntryPoints = entryPoints ?? Array.Empty<EntryPointRule>(),
            DuplicateTypeNames = duplicateTypes ?? Array.Empty<string>(),
            BasePriority = basePriority
        };

    private static IReadOnlyList<string> InferExtensions(string id) => id switch
    {
        "java" or "kotlin" or "scala" or "groovy" => new[] { ".java", ".kt", ".scala", ".groovy" },
        "csharp" or "fsharp" => new[] { ".cs", ".fs" },
        "python" => new[] { ".py" },
        "javascript" => new[] { ".js", ".mjs", ".cjs" },
        "typescript" => new[] { ".ts", ".tsx", ".mts", ".cts" },
        "go" => new[] { ".go" },
        "rust" => new[] { ".rs" },
        "ruby" => new[] { ".rb" },
        "php" => new[] { ".php" },
        "swift" => new[] { ".swift" },
        "dart" => new[] { ".dart" },
        "c" or "cpp" => new[] { ".c", ".h", ".cpp", ".hpp", ".cc" },
        "zig" => new[] { ".zig" },
        "lua" => new[] { ".lua" },
        "r" => new[] { ".r", ".R" },
        "elixir" => new[] { ".ex", ".exs" },
        "clojure" => new[] { ".clj", ".cljs" },
        "haskell" => new[] { ".hs" },
        "erlang" => new[] { ".erl" },
        "julia" => new[] { ".jl" },
        "nim" => new[] { ".nim" },
        "crystal" => new[] { ".cr" },
        "ocaml" => new[] { ".ml", ".mli" },
        "elm" => new[] { ".elm" },
        "solidity" => new[] { ".sol" },
        "verilog" or "vhdl" => new[] { ".v", ".sv", ".vhdl", ".vhd" },
        "shell" or "bash" => new[] { ".sh", ".bash" },
        "powershell" => new[] { ".ps1" },
        "perl" => new[] { ".pl", ".pm" },
        "objectivec" => new[] { ".m", ".mm" },
        "delphi" => new[] { ".pas", ".dpr" },
        "ada" => new[] { ".adb", ".ads" },
        "fortran" => new[] { ".f90", ".f95", ".f" },
        "cobol" => new[] { ".cob", ".cbl" },
        "prolog" => new[] { ".pl", ".pro" },
        "lisp" or "scheme" or "racket" => new[] { ".lisp", ".cl", ".scm", ".rkt" },
        "sql" => new[] { ".sql" },
        "wasm" => new[] { ".wat", ".wasm" },
        "assembly" => new[] { ".asm", ".s" },
        "matlab" => new[] { ".m" },
        "v" => new[] { ".v" },
        "vbnet" => new[] { ".vb" },
        "apex" => new[] { ".cls", ".trigger" },
        "abap" => new[] { ".abap" },
        "dlang" => new[] { ".d" },
        "reasonml" => new[] { ".re", ".rei" },
        "rescript" => new[] { ".res", ".resi" },
        "coffeescript" => new[] { ".coffee" },
        "wolfram" => new[] { ".wl" },
        "tcl" => new[] { ".tcl" },
        "awk" => new[] { ".awk" },
        "graphql" => new[] { ".graphql", ".gql" },
        _ => Array.Empty<string>()
    };

    private static IReadOnlyList<ManifestRule> InferManifests(string id) => id switch
    {
        "java" or "kotlin" => new[] { new ManifestRule("pom.xml"), new ManifestRule("build.gradle"), new ManifestRule("build.gradle.kts") },
        "scala" => new[] { new ManifestRule("build.sbt"), new ManifestRule("pom.xml") },
        "csharp" or "fsharp" => new[] { new ManifestRule(".csproj"), new ManifestRule(".fsproj"), new ManifestRule(".sln") },
        "python" => new[] { new ManifestRule("requirements.txt"), new ManifestRule("pyproject.toml"), new ManifestRule("Pipfile"), new ManifestRule("setup.py") },
        "javascript" or "typescript" => new[] { new ManifestRule("package.json"), new ManifestRule("pnpm-lock.yaml"), new ManifestRule("yarn.lock") },
        "go" => new[] { new ManifestRule("go.mod"), new ManifestRule("go.sum") },
        "rust" => new[] { new ManifestRule("Cargo.toml"), new ManifestRule("Cargo.lock") },
        "ruby" => new[] { new ManifestRule("Gemfile"), new ManifestRule(".gemspec") },
        "php" => new[] { new ManifestRule("composer.json") },
        "swift" => new[] { new ManifestRule("Package.swift"), new ManifestRule(".xcodeproj") },
        "dart" => new[] { new ManifestRule("pubspec.yaml") },
        "elixir" => new[] { new ManifestRule("mix.exs") },
        "clojure" => new[] { new ManifestRule("deps.edn"), new ManifestRule("project.clj") },
        "haskell" => new[] { new ManifestRule("stack.yaml"), new ManifestRule("cabal.project") },
        "nim" => new[] { new ManifestRule(".nimble") },
        "crystal" => new[] { new ManifestRule("shard.yml") },
        "julia" => new[] { new ManifestRule("Project.toml") },
        "zig" => new[] { new ManifestRule("build.zig") },
        "docker" => new[] { new ManifestRule("Dockerfile"), new ManifestRule("docker-compose.yml"), new ManifestRule("docker-compose.yaml") },
        _ => Array.Empty<ManifestRule>()
    };

    private static IReadOnlyList<EntryPointRule> InferEntryPoints(string id)
    {
        return id switch
        {
            "java" or "kotlin" => new[]
            {
                new EntryPointRule(
                    new[] { ".java", ".kt" },
                    new[] { "@SpringBootApplication", "public static void main" },
                    new[] { "Application.java", "Main.kt" },
                    30)
            },
            "csharp" => new[]
            {
                new EntryPointRule(
                    new[] { ".cs" },
                    new[] { "WebApplication.CreateBuilder", "static void Main", "static async Task Main" },
                    new[] { "Program.cs" },
                    30)
            },
            "python" => new[]
            {
                new EntryPointRule(
                    new[] { ".py" },
                    new[] { "FastAPI(", "Flask(__name__)", "Django", "if __name__ == \"__main__\"" },
                    new[] { "main.py", "app.py", "wsgi.py" },
                    25)
            },
            "javascript" or "typescript" => new[]
            {
                new EntryPointRule(
                    new[] { ".js", ".ts", ".tsx", ".mjs" },
                    new[] { "express()", "createServer(", "listen(", "export default function" },
                    new[] { "server.ts", "index.ts", "main.ts", "app.ts" },
                    25),
                new EntryPointRule(
                    new[] { ".tsx", ".jsx" },
                    new[] { "createRoot(", "ReactDOM.render", "hydrateRoot(" },
                    new[] { "main.tsx", "index.tsx", "App.tsx" },
                    20)
            },
            "go" => new[]
            {
                new EntryPointRule(
                    new[] { ".go" },
                    new[] { "func main()", "gin.Default()", "echo.New()", "fiber.New()" },
                    new[] { "main.go", "cmd/" },
                    25)
            },
            "rust" => new[]
            {
                new EntryPointRule(
                    new[] { ".rs" },
                    new[] { "fn main()", "#[tokio::main]", "Router::new()", "actix_web::" },
                    new[] { "main.rs", "lib.rs" },
                    25)
            },
            "ruby" => new[]
            {
                new EntryPointRule(
                    new[] { ".rb" },
                    new[] { "Rails.application", "Sinatra::Base", "class ApplicationController" },
                    new[] { "config.ru", "application.rb" },
                    20)
            },
            "php" => new[]
            {
                new EntryPointRule(
                    new[] { ".php" },
                    new[] { "Laravel\\", "Symfony\\Component", "public function index" },
                    new[] { "index.php", "artisan" },
                    20)
            },
            "swift" => new[]
            {
                new EntryPointRule(
                    new[] { ".swift" },
                    new[] { "@main", "UIApplicationMain", "Vapor()" },
                    new[] { "App.swift", "main.swift" },
                    20)
            },
            "dart" => new[]
            {
                new EntryPointRule(
                    new[] { ".dart" },
                    new[] { "void main(", "runApp(" },
                    new[] { "main.dart" },
                    20)
            },
            "elixir" => new[]
            {
                new EntryPointRule(
                    new[] { ".ex" },
                    new[] { "defmodule", "use Phoenix", "Plug.Application" },
                    new[] { "application.ex", "router.ex" },
                    20)
            },
            _ => Array.Empty<EntryPointRule>()
        };
    }

    private static IReadOnlyList<string> InferDuplicateTypes(string id) => id switch
    {
        "java" or "kotlin" => new[] { "AuthController", "UserRepository", "SecurityConfig" },
        "csharp" => new[] { "AuthController", "Program", "Startup" },
        "python" => new[] { "AuthRouter", "app" },
        "javascript" or "typescript" => new[] { "AuthController", "authRouter", "App" },
        "go" => new[] { "main", "AuthHandler" },
        "rust" => new[] { "main", "auth" },
        "php" => new[] { "AuthController" },
        "ruby" => new[] { "ApplicationController", "SessionsController" },
        _ => Array.Empty<string>()
    };
}
