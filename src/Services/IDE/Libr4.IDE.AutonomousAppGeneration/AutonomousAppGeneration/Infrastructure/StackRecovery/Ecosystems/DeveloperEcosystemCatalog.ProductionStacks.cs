namespace Libr4.IDE.Application.AutonomousAppGeneration.Infrastructure.StackRecovery.Ecosystems;

/// <summary>
/// Production frameworks from the 50-language industry catalog (Tier 1–3 coverage).
/// Complements <see cref="BuildFrameworkProfiles"/> and <see cref="BuildExtendedFrameworkProfiles"/>.
/// </summary>
public static partial class DeveloperEcosystemCatalog
{
    private static IEnumerable<EcosystemProfile> BuildProductionStackProfiles()
    {
        // --- Java ecosystem ---
        yield return EcosystemProfileFactory.Framework("spring-mvc", "Spring MVC", EcosystemCategory.BackendFramework,
            new[] { "spring mvc", "spring webmvc" }, new[] { new ManifestRule("pom.xml") });
        yield return EcosystemProfileFactory.Framework("spring-security", "Spring Security", EcosystemCategory.BackendFramework,
            new[] { "spring security", "spring-security" }, new[] { new ManifestRule("pom.xml") },
            duplicateTypes: new[] { "SecurityConfig", "JwtAuthenticationFilter" });
        yield return EcosystemProfileFactory.Framework("maven", "Maven", EcosystemCategory.BackendFramework,
            new[] { "maven" }, new[] { new ManifestRule("pom.xml") });
        yield return EcosystemProfileFactory.Framework("gradle", "Gradle", EcosystemCategory.BackendFramework,
            new[] { "gradle" }, new[] { new ManifestRule("build.gradle"), new ManifestRule("build.gradle.kts") });
        yield return EcosystemProfileFactory.Framework("compose-multiplatform", "Compose Multiplatform", EcosystemCategory.FrontendFramework,
            new[] { "compose multiplatform", "compose-multiplatform" },
            new[] { new ManifestRule("build.gradle.kts") });

        // --- Kotlin / Scala / Groovy ---
        yield return EcosystemProfileFactory.Framework("akka", "Akka", EcosystemCategory.BackendFramework,
            new[] { "akka" }, new[] { new ManifestRule("build.sbt"), new ManifestRule("pom.xml") });
        yield return EcosystemProfileFactory.Framework("pekko", "Apache Pekko", EcosystemCategory.BackendFramework,
            new[] { "pekko", "apache pekko" }, new[] { new ManifestRule("build.sbt") });
        yield return EcosystemProfileFactory.Framework("zio", "ZIO", EcosystemCategory.BackendFramework,
            new[] { "zio" }, new[] { new ManifestRule("build.sbt") });
        yield return EcosystemProfileFactory.Framework("grails", "Grails", EcosystemCategory.BackendFramework,
            new[] { "grails" }, new[] { new ManifestRule("build.gradle") });

        // --- .NET / F# ---
        yield return EcosystemProfileFactory.Framework("minimal-apis", "ASP.NET Minimal APIs", EcosystemCategory.BackendFramework,
            new[] { "minimal api", "minimal apis", "minimal-api" },
            new[] { new ManifestRule(".csproj") },
            new[] { new EntryPointRule(new[] { ".cs" }, new[] { "WebApplication.CreateBuilder", "MapGet(" }, new[] { "Program.cs" }, 35) });
        yield return EcosystemProfileFactory.Framework("blazor-server", "Blazor Server", EcosystemCategory.FrontendFramework,
            new[] { "blazor server" }, new[] { new ManifestRule(".csproj") });
        yield return EcosystemProfileFactory.Framework("winui", "WinUI", EcosystemCategory.FrontendFramework,
            new[] { "winui", "win ui 3" }, new[] { new ManifestRule(".csproj") });
        yield return EcosystemProfileFactory.Framework("giraffe", "Giraffe", EcosystemCategory.BackendFramework,
            new[] { "giraffe" }, new[] { new ManifestRule(".fsproj") },
            new[] { new EntryPointRule(new[] { ".fs" }, new[] { "Giraffe" }, new[] { "Program.fs" }, 30) });
        yield return EcosystemProfileFactory.Framework("saturn", "Saturn", EcosystemCategory.BackendFramework,
            new[] { "saturn" }, new[] { new ManifestRule(".fsproj") });
        yield return EcosystemProfileFactory.Framework("winforms", "WinForms", EcosystemCategory.FrontendFramework,
            new[] { "winforms", "windows forms" }, new[] { new ManifestRule(".csproj"), new ManifestRule(".vbproj") });
        yield return EcosystemProfileFactory.Framework("vb-aspnet", "ASP.NET (VB.NET)", EcosystemCategory.BackendFramework,
            new[] { "vb.net asp", "vb asp.net" }, new[] { new ManifestRule(".vbproj") });

        // --- Python ---
        yield return EcosystemProfileFactory.Framework("litestar", "Litestar", EcosystemCategory.BackendFramework,
            new[] { "litestar" }, new[] { new ManifestRule("pyproject.toml"), new ManifestRule("requirements.txt") },
            new[] { new EntryPointRule(new[] { ".py" }, new[] { "Litestar(" }, new[] { "main.py" }, 30) });
        yield return EcosystemProfileFactory.Framework("sanic", "Sanic", EcosystemCategory.BackendFramework,
            new[] { "sanic" }, entryPoints: new[] { new EntryPointRule(new[] { ".py" }, new[] { "Sanic(" }, new[] { "app.py" }, 30) });
        yield return EcosystemProfileFactory.Framework("langchain", "LangChain", EcosystemCategory.BackendFramework,
            new[] { "langchain" }, new[] { new ManifestRule("requirements.txt"), new ManifestRule("pyproject.toml") });
        yield return EcosystemProfileFactory.Framework("llamaindex", "LlamaIndex", EcosystemCategory.BackendFramework,
            new[] { "llamaindex", "llama index" }, new[] { new ManifestRule("requirements.txt") });
        yield return EcosystemProfileFactory.Framework("celery", "Celery", EcosystemCategory.BackendFramework,
            new[] { "celery" }, new[] { new ManifestRule("requirements.txt") });

        // --- Go ---
        yield return EcosystemProfileFactory.Framework("chi-go", "Chi (Go)", EcosystemCategory.BackendFramework,
            new[] { "chi router", "go-chi", "chi-go" },
            new[] { new ManifestRule("go.mod") },
            new[] { new EntryPointRule(new[] { ".go" }, new[] { "chi.NewRouter()" }, new[] { "main.go" }, 30) });
        yield return EcosystemProfileFactory.Framework("buffalo-go", "Buffalo (Go)", EcosystemCategory.BackendFramework,
            new[] { "buffalo" }, new[] { new ManifestRule("go.mod") });

        // --- Rust ---
        yield return EcosystemProfileFactory.Framework("warp", "Warp (Rust)", EcosystemCategory.BackendFramework,
            new[] { "warp", "warp-rs" }, new[] { new ManifestRule("Cargo.toml") },
            new[] { new EntryPointRule(new[] { ".rs" }, new[] { "warp::" }, new[] { "main.rs" }, 30) });
        yield return EcosystemProfileFactory.Framework("clap", "Clap", EcosystemCategory.BackendFramework,
            new[] { "clap" }, new[] { new ManifestRule("Cargo.toml") });

        // --- C / C++ ---
        yield return EcosystemProfileFactory.Framework("gtk", "GTK", EcosystemCategory.FrontendFramework,
            new[] { "gtk", "gtk3", "gtk4" }, new[] { new ManifestRule("meson.build"), new ManifestRule("CMakeLists.txt") });
        yield return EcosystemProfileFactory.Framework("libmicrohttpd", "libmicrohttpd", EcosystemCategory.BackendFramework,
            new[] { "libmicrohttpd", "microhttpd" });
        yield return EcosystemProfileFactory.Framework("drogon", "Drogon", EcosystemCategory.BackendFramework,
            new[] { "drogon" }, new[] { new ManifestRule("CMakeLists.txt") });
        yield return EcosystemProfileFactory.Framework("crow-cpp", "Crow (C++)", EcosystemCategory.BackendFramework,
            new[] { "crow", "crow-cpp" }, new[] { new ManifestRule("CMakeLists.txt") });
        yield return EcosystemProfileFactory.Framework("qt", "Qt", EcosystemCategory.FrontendFramework,
            new[] { "qt", "qt6", "qmake" }, new[] { new ManifestRule("CMakeLists.txt"), new ManifestRule(".pro") });
        yield return EcosystemProfileFactory.Framework("wxwidgets", "wxWidgets", EcosystemCategory.FrontendFramework,
            new[] { "wxwidgets", "wx widgets" }, new[] { new ManifestRule("CMakeLists.txt") });

        // --- PHP ---
        yield return EcosystemProfileFactory.Framework("yii", "Yii", EcosystemCategory.BackendFramework,
            new[] { "yii", "yii2" }, new[] { new ManifestRule("composer.json") });
        yield return EcosystemProfileFactory.Framework("codeigniter", "CodeIgniter", EcosystemCategory.BackendFramework,
            new[] { "codeigniter" }, new[] { new ManifestRule("composer.json") });
        yield return EcosystemProfileFactory.Framework("slim-php", "Slim (PHP)", EcosystemCategory.BackendFramework,
            new[] { "slim framework", "slim-php" }, new[] { new ManifestRule("composer.json") },
            new[] { new EntryPointRule(new[] { ".php" }, new[] { "Slim\\Factory\\AppFactory" }, new[] { "index.php" }, 30) });

        // --- Ruby ---
        yield return EcosystemProfileFactory.Framework("hanami", "Hanami", EcosystemCategory.BackendFramework,
            new[] { "hanami" }, new[] { new ManifestRule("Gemfile") });

        // --- Elixir / Erlang ---
        yield return EcosystemProfileFactory.Framework("phoenix-liveview", "Phoenix LiveView", EcosystemCategory.FrontendFramework,
            new[] { "phoenix liveview", "liveview" }, new[] { new ManifestRule("mix.exs") });
        yield return EcosystemProfileFactory.Framework("cowboy", "Cowboy", EcosystemCategory.BackendFramework,
            new[] { "cowboy" }, new[] { new ManifestRule("rebar.config"), new ManifestRule("mix.exs") });
        yield return EcosystemProfileFactory.Framework("nitrogen", "Nitrogen", EcosystemCategory.BackendFramework,
            new[] { "nitrogen" }, new[] { new ManifestRule("rebar.config") });

        // --- Dart / Swift ---
        yield return EcosystemProfileFactory.Framework("shelf", "Shelf (Dart)", EcosystemCategory.BackendFramework,
            new[] { "shelf" }, new[] { new ManifestRule("pubspec.yaml") },
            new[] { new EntryPointRule(new[] { ".dart" }, new[] { "shelf_router", "shelf_io" }, new[] { "server.dart" }, 25) });
        yield return EcosystemProfileFactory.Framework("vapor", "Vapor", EcosystemCategory.BackendFramework,
            new[] { "vapor" }, new[] { new ManifestRule("Package.swift") },
            new[] { new EntryPointRule(new[] { ".swift" }, new[] { "Vapor(", "configure(_" }, new[] { "main.swift", "configure.swift" }, 35) });
        yield return EcosystemProfileFactory.Framework("hummingbird", "Hummingbird", EcosystemCategory.BackendFramework,
            new[] { "hummingbird" }, new[] { new ManifestRule("Package.swift") });
        yield return EcosystemProfileFactory.Framework("swiftui", "SwiftUI", EcosystemCategory.FrontendFramework,
            new[] { "swiftui" }, new[] { new ManifestRule("Package.swift"), new ManifestRule(".xcodeproj") },
            new[] { new EntryPointRule(new[] { ".swift" }, new[] { "SwiftUI", "@main struct" }, new[] { "App.swift", "ContentView.swift" }, 35) });
        yield return EcosystemProfileFactory.Framework("uikit", "UIKit", EcosystemCategory.FrontendFramework,
            new[] { "uikit" }, new[] { new ManifestRule(".xcodeproj") });
        yield return EcosystemProfileFactory.Framework("cocoa", "Cocoa", EcosystemCategory.FrontendFramework,
            new[] { "cocoa", "appkit" }, new[] { new ManifestRule(".xcodeproj") });

        // --- Zig / Nim / Crystal / OCaml / Haskell ---
        yield return EcosystemProfileFactory.Framework("zap", "zap (Zig)", EcosystemCategory.BackendFramework,
            new[] { "zap", "zig zap" }, new[] { new ManifestRule("build.zig") });
        yield return EcosystemProfileFactory.Framework("jester", "Jester (Nim)", EcosystemCategory.BackendFramework,
            new[] { "jester" }, new[] { new ManifestRule(".nimble") });
        yield return EcosystemProfileFactory.Framework("karax", "Karax (Nim)", EcosystemCategory.FrontendFramework,
            new[] { "karax" }, new[] { new ManifestRule(".nimble") });
        yield return EcosystemProfileFactory.Framework("kemal", "Kemal", EcosystemCategory.BackendFramework,
            new[] { "kemal" }, new[] { new ManifestRule("shard.yml") });
        yield return EcosystemProfileFactory.Framework("amber", "Amber", EcosystemCategory.FullStack,
            new[] { "amber framework", "amber crystal" }, new[] { new ManifestRule("shard.yml") });
        yield return EcosystemProfileFactory.Framework("dream", "Dream (OCaml)", EcosystemCategory.BackendFramework,
            new[] { "dream" }, new[] { new ManifestRule("dune-project") });
        yield return EcosystemProfileFactory.Framework("opium", "Opium", EcosystemCategory.BackendFramework,
            new[] { "opium" }, new[] { new ManifestRule("dune-project") });
        yield return EcosystemProfileFactory.Framework("yesod", "Yesod", EcosystemCategory.BackendFramework,
            new[] { "yesod" }, new[] { new ManifestRule("stack.yaml"), new ManifestRule("package.yaml") });
        yield return EcosystemProfileFactory.Framework("scotty", "Scotty", EcosystemCategory.BackendFramework,
            new[] { "scotty" }, new[] { new ManifestRule("stack.yaml") });
        yield return EcosystemProfileFactory.Framework("servant", "Servant", EcosystemCategory.BackendFramework,
            new[] { "servant" }, new[] { new ManifestRule("stack.yaml") });

        // --- Lua / R / Julia / Perl ---
        yield return EcosystemProfileFactory.Framework("openresty", "OpenResty", EcosystemCategory.BackendFramework,
            new[] { "openresty", "ngx_lua" });
        yield return EcosystemProfileFactory.Framework("lapis", "Lapis", EcosystemCategory.BackendFramework,
            new[] { "lapis" });
        yield return EcosystemProfileFactory.Framework("shiny", "Shiny", EcosystemCategory.FrontendFramework,
            new[] { "shiny" }, new[] { new ManifestRule("DESCRIPTION") },
            new[] { new EntryPointRule(new[] { ".r", ".R" }, new[] { "shinyApp(", "fluidPage(" }, new[] { "app.R" }, 30) });
        yield return EcosystemProfileFactory.Framework("plumber", "Plumber", EcosystemCategory.BackendFramework,
            new[] { "plumber" }, new[] { new ManifestRule("DESCRIPTION") },
            new[] { new EntryPointRule(new[] { ".r", ".R" }, new[] { "#* @get", "plumber::" }, new[] { "plumber.R" }, 30) });
        yield return EcosystemProfileFactory.Framework("genie", "Genie", EcosystemCategory.BackendFramework,
            new[] { "genie", "genieframework" }, new[] { new ManifestRule("Project.toml") });
        yield return EcosystemProfileFactory.Framework("oxygen", "Oxygen", EcosystemCategory.BackendFramework,
            new[] { "oxygen" }, new[] { new ManifestRule("DESCRIPTION") });
        yield return EcosystemProfileFactory.Framework("mojolicious", "Mojolicious", EcosystemCategory.BackendFramework,
            new[] { "mojolicious" }, new[] { new ManifestRule("cpanfile"), new ManifestRule("Makefile.PL") });
        yield return EcosystemProfileFactory.Framework("dancer", "Dancer", EcosystemCategory.BackendFramework,
            new[] { "dancer", "dancer2" }, new[] { new ManifestRule("cpanfile") });

        // --- Clojure / Lisp / Scheme ---
        yield return EcosystemProfileFactory.Framework("ring", "Ring", EcosystemCategory.BackendFramework,
            new[] { "ring", "ring-clojure" }, new[] { new ManifestRule("deps.edn"), new ManifestRule("project.clj") });
        yield return EcosystemProfileFactory.Framework("reitit", "Reitit", EcosystemCategory.BackendFramework,
            new[] { "reitit" }, new[] { new ManifestRule("deps.edn") });
        yield return EcosystemProfileFactory.Framework("pedestal", "Pedestal", EcosystemCategory.BackendFramework,
            new[] { "pedestal" }, new[] { new ManifestRule("deps.edn") });
        yield return EcosystemProfileFactory.Framework("caveman", "Caveman", EcosystemCategory.BackendFramework,
            new[] { "caveman" }, new[] { new ManifestRule("project.clj") });
        yield return EcosystemProfileFactory.Framework("hunchentoot", "Hunchentoot", EcosystemCategory.BackendFramework,
            new[] { "hunchentoot" }, new[] { new ManifestRule("project.clj"), new ManifestRule("asd") });
        yield return EcosystemProfileFactory.Framework("racket-web", "Racket Web Server", EcosystemCategory.BackendFramework,
            new[] { "racket web", "web-server" }, new[] { new ManifestRule("info.rkt") });

        // --- Fortran / COBOL / Ada / Prolog ---
        yield return EcosystemProfileFactory.Framework("fpm", "fpm (Fortran)", EcosystemCategory.BackendFramework,
            new[] { "fpm", "fortran fpm" }, new[] { new ManifestRule("fpm.toml") });
        yield return EcosystemProfileFactory.Framework("gnucobol", "GnuCOBOL", EcosystemCategory.BackendFramework,
            new[] { "gnucobol", "gnu cobol" });
        yield return EcosystemProfileFactory.Framework("gnat", "GNAT", EcosystemCategory.BackendFramework,
            new[] { "gnat", "ada gnat" });
        yield return EcosystemProfileFactory.Framework("ada-web-server", "Ada Web Server", EcosystemCategory.BackendFramework,
            new[] { "ada web server", "aws ada" });
        yield return EcosystemProfileFactory.Framework("swi-prolog-http", "SWI-Prolog HTTP", EcosystemCategory.BackendFramework,
            new[] { "swi-prolog", "swi prolog http" });

        // --- Enterprise / blockchain ---
        yield return EcosystemProfileFactory.Framework("salesforce-apex", "Salesforce Apex", EcosystemCategory.BackendFramework,
            new[] { "salesforce apex", "apex triggers" }, extensions: new[] { ".cls", ".trigger" });
        yield return EcosystemProfileFactory.Framework("sap-rap", "SAP RAP", EcosystemCategory.BackendFramework,
            new[] { "sap rap", "restful abap" });
        yield return EcosystemProfileFactory.Framework("sap-fiori", "SAP Fiori", EcosystemCategory.FrontendFramework,
            new[] { "sap fiori", "fiori elements" });
        yield return EcosystemProfileFactory.Framework("hardhat", "Hardhat", EcosystemCategory.BackendFramework,
            new[] { "hardhat" }, new[] { new ManifestRule("package.json"), new ManifestRule("hardhat.config.ts") });
        yield return EcosystemProfileFactory.Framework("foundry", "Foundry", EcosystemCategory.BackendFramework,
            new[] { "foundry", "forge" }, new[] { new ManifestRule("foundry.toml") });

        // --- V / D / Elm / Reason / Coffee / PowerShell / Bash / MATLAB / Wolfram ---
        yield return EcosystemProfileFactory.Framework("vweb", "VWeb", EcosystemCategory.BackendFramework,
            new[] { "vweb", "v web" }, new[] { new ManifestRule("v.mod") });
        yield return EcosystemProfileFactory.Framework("vibe-d", "vibe.d", EcosystemCategory.BackendFramework,
            new[] { "vibe.d", "vibe-d" }, new[] { new ManifestRule("dub.json"), new ManifestRule("dub.sdl") });
        yield return EcosystemProfileFactory.Framework("elm-ui", "Elm UI", EcosystemCategory.FrontendFramework,
            new[] { "elm ui", "elm-ui" }, new[] { new ManifestRule("elm.json") });
        yield return EcosystemProfileFactory.Framework("rescript-react", "ReScript React", EcosystemCategory.FrontendFramework,
            new[] { "rescript react", "@rescript/react" }, new[] { new ManifestRule("package.json"), new ManifestRule("rescript.json") });
        yield return EcosystemProfileFactory.Framework("pode", "Pode", EcosystemCategory.BackendFramework,
            new[] { "pode" }, new[] { new ManifestRule("server.psd1") },
            new[] { new EntryPointRule(new[] { ".ps1" }, new[] { "Start-PodeServer", "Add-PodeRoute" }, new[] { "server.ps1" }, 30) });
        yield return EcosystemProfileFactory.Framework("universal-dashboard", "Universal Dashboard", EcosystemCategory.FrontendFramework,
            new[] { "universal dashboard", "powershell universal" });
        yield return EcosystemProfileFactory.Framework("matlab-app-designer", "MATLAB App Designer", EcosystemCategory.FrontendFramework,
            new[] { "app designer", "matlab app" }, new[] { new ManifestRule(".mlapp") });
        yield return EcosystemProfileFactory.Framework("wolfram-web", "Wolfram Web Framework", EcosystemCategory.FullStack,
            new[] { "wolfram web", "wolfram cloud" });
    }
}
