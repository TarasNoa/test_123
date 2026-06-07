namespace Libr4.IDE.Application.AutonomousAppGeneration.Infrastructure.StackRecovery.Ecosystems;

/// <summary>Popular backend and frontend frameworks (JS/TS ecosystem heavily covered).</summary>
public static partial class DeveloperEcosystemCatalog
{
    private static IEnumerable<EcosystemProfile> BuildFrameworkProfiles()
    {
        // --- Java backend ---
        yield return EcosystemProfileFactory.Framework("spring-boot", "Spring Boot", EcosystemCategory.BackendFramework,
            new[] { "spring boot", "springboot", "spring framework" },
            new[] { new ManifestRule("pom.xml"), new ManifestRule("build.gradle") },
            new[] { new EntryPointRule(new[] { ".java" }, new[] { "@SpringBootApplication" }, new[] { "Application.java" }, 40) },
            new[] { "AuthController", "UserRepository" });
        yield return EcosystemProfileFactory.Framework("quarkus", "Quarkus", EcosystemCategory.BackendFramework,
            new[] { "quarkus" }, new[] { new ManifestRule("pom.xml") });
        yield return EcosystemProfileFactory.Framework("micronaut", "Micronaut", EcosystemCategory.BackendFramework,
            new[] { "micronaut" }, new[] { new ManifestRule("pom.xml") });

        // --- .NET ---
        yield return EcosystemProfileFactory.Framework("aspnet-core", "ASP.NET Core", EcosystemCategory.BackendFramework,
            new[] { "asp.net", "aspnet", "asp.net core" },
            new[] { new ManifestRule(".csproj") },
            new[] { new EntryPointRule(new[] { ".cs" }, new[] { "WebApplication.CreateBuilder" }, new[] { "Program.cs" }, 40) });
        yield return EcosystemProfileFactory.Framework("blazor", "Blazor", EcosystemCategory.FrontendFramework,
            new[] { "blazor" }, new[] { new ManifestRule(".csproj") },
            entryPoints: new[] { new EntryPointRule(new[] { ".razor", ".cs" }, new[] { "Router", "Routes" }, new[] { "App.razor" }, 25) });

        // --- Python ---
        yield return EcosystemProfileFactory.Framework("fastapi", "FastAPI", EcosystemCategory.BackendFramework,
            new[] { "fastapi" }, new[] { new ManifestRule("requirements.txt"), new ManifestRule("pyproject.toml") },
            new[] { new EntryPointRule(new[] { ".py" }, new[] { "FastAPI(" }, new[] { "main.py" }, 35) });
        yield return EcosystemProfileFactory.Framework("django", "Django", EcosystemCategory.BackendFramework,
            new[] { "django" }, duplicateTypes: new[] { "settings", "urls" });
        yield return EcosystemProfileFactory.Framework("flask", "Flask", EcosystemCategory.BackendFramework,
            new[] { "flask" }, entryPoints: new[] { new EntryPointRule(new[] { ".py" }, new[] { "Flask(__name__)" }, new[] { "app.py" }, 30) });

        // --- Node HTTP ---
        yield return EcosystemProfileFactory.Framework("express", "Express.js", EcosystemCategory.BackendFramework,
            new[] { "express", "express.js" }, new[] { new ManifestRule("package.json") },
            new[] { new EntryPointRule(new[] { ".js", ".ts" }, new[] { "express()" }, new[] { "server", "index", "app" }, 35) });
        yield return EcosystemProfileFactory.Framework("fastify", "Fastify", EcosystemCategory.BackendFramework,
            new[] { "fastify" }, entryPoints: new[] { new EntryPointRule(new[] { ".ts", ".js" }, new[] { "fastify(" }, new[] { "server" }, 30) });
        yield return EcosystemProfileFactory.Framework("nestjs", "NestJS", EcosystemCategory.BackendFramework,
            new[] { "nestjs", "nest.js" }, new[] { new ManifestRule("package.json"), new ManifestRule("nest-cli.json") },
            new[] { new EntryPointRule(new[] { ".ts" }, new[] { "NestFactory.create" }, new[] { "main.ts" }, 35) });
        yield return EcosystemProfileFactory.Framework("koa", "Koa", EcosystemCategory.BackendFramework, new[] { "koa" });
        yield return EcosystemProfileFactory.Framework("hono", "Hono", EcosystemCategory.BackendFramework, new[] { "hono" });

        // --- Frontend meta / UI ---
        yield return EcosystemProfileFactory.Framework("react", "React", EcosystemCategory.FrontendFramework,
            new[] { "react", "reactjs", "react.js" }, new[] { new ManifestRule("package.json") },
            new[] { new EntryPointRule(new[] { ".tsx", ".jsx" }, new[] { "from \"react\"", "from 'react'", "createRoot(" }, new[] { "main.tsx", "App.tsx" }, 35) },
            extensions: new[] { ".tsx", ".jsx" }, basePriority: 25);
        yield return EcosystemProfileFactory.Framework("vue", "Vue.js", EcosystemCategory.FrontendFramework,
            new[] { "vue", "vuejs", "vue.js" }, new[] { new ManifestRule("package.json"), new ManifestRule("vite.config.ts") },
            new[] { new EntryPointRule(new[] { ".vue" }, new[] { "createApp(", "defineComponent" }, new[] { "App.vue", "main.ts" }, 35) },
            extensions: new[] { ".vue" });
        yield return EcosystemProfileFactory.Framework("angular", "Angular", EcosystemCategory.FrontendFramework,
            new[] { "angular" }, new[] { new ManifestRule("package.json"), new ManifestRule("angular.json") },
            new[] { new EntryPointRule(new[] { ".ts" }, new[] { "@Component", "platformBrowser" }, new[] { "app.component.ts", "main.ts" }, 35) });
        yield return EcosystemProfileFactory.Framework("svelte", "Svelte", EcosystemCategory.FrontendFramework,
            new[] { "svelte" }, new[] { new ManifestRule("package.json"), new ManifestRule("svelte.config.js") },
            new[] { new EntryPointRule(new[] { ".svelte" }, new[] { "<script" }, new[] { "App.svelte", "+page.svelte" }, 30) },
            extensions: new[] { ".svelte" });
        yield return EcosystemProfileFactory.Framework("solidjs", "SolidJS", EcosystemCategory.FrontendFramework,
            new[] { "solid", "solidjs", "solid.js" }, new[] { new ManifestRule("package.json") },
            new[] { new EntryPointRule(new[] { ".tsx" }, new[] { "solid-js", "render(" }, new[] { "index.tsx", "App.tsx" }, 35) });
        yield return EcosystemProfileFactory.Framework("nextjs", "Next.js", EcosystemCategory.FullStack,
            new[] { "next", "next.js", "nextjs" },
            new[] { new ManifestRule("package.json"), new ManifestRule("next.config.js"), new ManifestRule("next.config.ts"), new ManifestRule("next.config.mjs") },
            new[] { new EntryPointRule(new[] { ".tsx", ".ts" }, new[] { "next/", "export default function", "metadata:" }, new[] { "app/page.tsx", "pages/index.tsx" }, 45) },
            basePriority: 35);
        yield return EcosystemProfileFactory.Framework("nuxt", "Nuxt", EcosystemCategory.FullStack,
            new[] { "nuxt", "nuxt.js", "nuxt3" },
            new[] { new ManifestRule("package.json"), new ManifestRule("nuxt.config.ts"), new ManifestRule("nuxt.config.js") },
            new[] { new EntryPointRule(new[] { ".vue" }, new[] { "defineNuxtConfig", "<NuxtPage" }, new[] { "app.vue", "pages/index.vue" }, 45) });
        yield return EcosystemProfileFactory.Framework("remix", "Remix", EcosystemCategory.FullStack,
            new[] { "remix", "remix.run" }, new[] { new ManifestRule("package.json") },
            new[] { new EntryPointRule(new[] { ".tsx" }, new[] { "@remix-run", "LoaderFunctionArgs" }, new[] { "app/routes" }, 40) });
        yield return EcosystemProfileFactory.Framework("astro", "Astro", EcosystemCategory.FrontendFramework,
            new[] { "astro" }, new[] { new ManifestRule("package.json"), new ManifestRule("astro.config.mjs") },
            new[] { new EntryPointRule(new[] { ".astro" }, new[] { "---", "Astro." }, new[] { "src/pages" }, 35) },
            extensions: new[] { ".astro" });
        yield return EcosystemProfileFactory.Framework("qwik", "Qwik", EcosystemCategory.FrontendFramework,
            new[] { "qwik" }, new[] { new ManifestRule("package.json"), new ManifestRule("vite.config.ts") });
        yield return EcosystemProfileFactory.Framework("gatsby", "Gatsby", EcosystemCategory.FrontendFramework,
            new[] { "gatsby" }, new[] { new ManifestRule("package.json"), new ManifestRule("gatsby-config.js") });
        yield return EcosystemProfileFactory.Framework("vite", "Vite", EcosystemCategory.FrontendFramework,
            new[] { "vite" }, new[] { new ManifestRule("package.json"), new ManifestRule("vite.config.ts"), new ManifestRule("vite.config.js") });
        yield return EcosystemProfileFactory.Framework("electron", "Electron", EcosystemCategory.FullStack,
            new[] { "electron" }, new[] { new ManifestRule("package.json") },
            new[] { new EntryPointRule(new[] { ".js", ".ts" }, new[] { "electron", "BrowserWindow" }, new[] { "main.js", "electron" }, 30) });
        yield return EcosystemProfileFactory.Framework("react-native", "React Native", EcosystemCategory.FrontendFramework,
            new[] { "react native", "react-native" }, new[] { new ManifestRule("package.json") },
            new[] { new EntryPointRule(new[] { ".tsx" }, new[] { "AppRegistry.registerComponent" }, new[] { "App.tsx" }, 35) });
        yield return EcosystemProfileFactory.Framework("expo", "Expo", EcosystemCategory.FrontendFramework,
            new[] { "expo" }, new[] { new ManifestRule("package.json"), new ManifestRule("app.json") });

        // --- Go / Rust / Ruby / PHP frameworks ---
        yield return EcosystemProfileFactory.Framework("gin", "Gin", EcosystemCategory.BackendFramework,
            new[] { "gin", "gin-gonic" }, new[] { new ManifestRule("go.mod") },
            new[] { new EntryPointRule(new[] { ".go" }, new[] { "gin.Default()", "gin.New()" }, new[] { "main.go" }, 35) });
        yield return EcosystemProfileFactory.Framework("echo-go", "Echo (Go)", EcosystemCategory.BackendFramework,
            new[] { "echo" }, entryPoints: new[] { new EntryPointRule(new[] { ".go" }, new[] { "echo.New()" }, new[] { "main.go" }, 30) });
        yield return EcosystemProfileFactory.Framework("fiber-go", "Fiber (Go)", EcosystemCategory.BackendFramework,
            new[] { "fiber" }, entryPoints: new[] { new EntryPointRule(new[] { ".go" }, new[] { "fiber.New()" }, new[] { "main.go" }, 30) });
        yield return EcosystemProfileFactory.Framework("axum", "Axum", EcosystemCategory.BackendFramework,
            new[] { "axum" }, new[] { new ManifestRule("Cargo.toml") },
            new[] { new EntryPointRule(new[] { ".rs" }, new[] { "Router::new", "axum::" }, new[] { "main.rs" }, 35) });
        yield return EcosystemProfileFactory.Framework("actix", "Actix Web", EcosystemCategory.BackendFramework,
            new[] { "actix" }, new[] { new ManifestRule("Cargo.toml") });
        yield return EcosystemProfileFactory.Framework("rails", "Ruby on Rails", EcosystemCategory.FullStack,
            new[] { "rails", "ruby on rails" }, new[] { new ManifestRule("Gemfile") },
            duplicateTypes: new[] { "ApplicationController" });
        yield return EcosystemProfileFactory.Framework("laravel", "Laravel", EcosystemCategory.BackendFramework,
            new[] { "laravel" }, new[] { new ManifestRule("composer.json") },
            duplicateTypes: new[] { "AuthController" });
        yield return EcosystemProfileFactory.Framework("symfony", "Symfony", EcosystemCategory.BackendFramework,
            new[] { "symfony" }, new[] { new ManifestRule("composer.json") });
        yield return EcosystemProfileFactory.Framework("phoenix", "Phoenix", EcosystemCategory.FullStack,
            new[] { "phoenix" }, new[] { new ManifestRule("mix.exs") });

        // --- Mobile / cross-platform ---
        yield return EcosystemProfileFactory.Framework("flutter", "Flutter", EcosystemCategory.FrontendFramework,
            new[] { "flutter" }, new[] { new ManifestRule("pubspec.yaml") },
            new[] { new EntryPointRule(new[] { ".dart" }, new[] { "void main(", "runApp(" }, new[] { "main.dart" }, 40) });
    }
}
