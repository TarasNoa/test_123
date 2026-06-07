namespace Libr4.IDE.Application.AutonomousAppGeneration.Infrastructure.StackRecovery.Ecosystems;

/// <summary>Additional popular frameworks (JS/TS meta, mobile, JVM, Python, Rust desktop).</summary>
public static partial class DeveloperEcosystemCatalog
{
    private static IEnumerable<EcosystemProfile> BuildExtendedFrameworkProfiles()
    {
        // --- JS/TS meta-frameworks & routers ---
        yield return EcosystemProfileFactory.Framework("sveltekit", "SvelteKit", EcosystemCategory.FullStack,
            new[] { "sveltekit", "svelte kit" },
            new[] { new ManifestRule("package.json"), new ManifestRule("svelte.config.js"), new ManifestRule("vite.config.ts") },
            new[] { new EntryPointRule(new[] { ".svelte" }, new[] { "+page.svelte", "+layout.svelte" }, new[] { "routes", "src/routes" }, 40) },
            extensions: new[] { ".svelte" });
        yield return EcosystemProfileFactory.Framework("preact", "Preact", EcosystemCategory.FrontendFramework,
            new[] { "preact" }, new[] { new ManifestRule("package.json") },
            new[] { new EntryPointRule(new[] { ".tsx", ".jsx" }, new[] { "preact", "h(" }, new[] { "main.tsx", "App.tsx" }, 30) });
        yield return EcosystemProfileFactory.Framework("tanstack-router", "TanStack Router", EcosystemCategory.FrontendFramework,
            new[] { "tanstack router", "@tanstack/react-router", "tanstack-router" },
            new[] { new ManifestRule("package.json") },
            new[] { new EntryPointRule(new[] { ".tsx" }, new[] { "createRouter", "RouterProvider", "@tanstack/react-router" }, new[] { "routes", "__root" }, 35) });
        yield return EcosystemProfileFactory.Framework("tanstack-start", "TanStack Start", EcosystemCategory.FullStack,
            new[] { "tanstack start", "@tanstack/start", "tanstack-start" },
            new[] { new ManifestRule("package.json"), new ManifestRule("app.config.ts") },
            new[] { new EntryPointRule(new[] { ".tsx" }, new[] { "@tanstack/start", "createStart" }, new[] { "app/routes" }, 40) });
        yield return EcosystemProfileFactory.Framework("redwood", "RedwoodJS", EcosystemCategory.FullStack,
            new[] { "redwood", "redwoodjs" }, new[] { new ManifestRule("package.json"), new ManifestRule("redwood.toml") });
        yield return EcosystemProfileFactory.Framework("fresh", "Fresh (Deno)", EcosystemCategory.FullStack,
            new[] { "fresh", "deno fresh" }, new[] { new ManifestRule("deno.json"), new ManifestRule("deno.jsonc") },
            new[] { new EntryPointRule(new[] { ".tsx" }, new[] { "fresh", "$fresh" }, new[] { "routes", "main.tsx" }, 35) });
        yield return EcosystemProfileFactory.Framework("meteor", "Meteor", EcosystemCategory.FullStack,
            new[] { "meteor" }, new[] { new ManifestRule("package.json"), new ManifestRule(".meteor") });
        yield return EcosystemProfileFactory.Framework("ember", "Ember.js", EcosystemCategory.FrontendFramework,
            new[] { "ember", "ember.js" }, new[] { new ManifestRule("package.json"), new ManifestRule("ember-cli-build.js") });
        yield return EcosystemProfileFactory.Framework("lit", "Lit", EcosystemCategory.FrontendFramework,
            new[] { "lit", "lit-element" }, new[] { new ManifestRule("package.json") },
            new[] { new EntryPointRule(new[] { ".ts" }, new[] { "@customElement", "LitElement" }, new[] { "index.ts" }, 25) });
        yield return EcosystemProfileFactory.Framework("alpinejs", "Alpine.js", EcosystemCategory.FrontendFramework,
            new[] { "alpine", "alpine.js", "alpinejs" }, new[] { new ManifestRule("package.json") });
        yield return EcosystemProfileFactory.Framework("htmx", "HTMX", EcosystemCategory.FrontendFramework,
            new[] { "htmx" }, new[] { new ManifestRule("package.json") },
            new[] { new EntryPointRule(new[] { ".html" }, new[] { "hx-get", "hx-post", "htmx.org" }, new[] { "index.html" }, 25) });
        yield return EcosystemProfileFactory.Framework("backbone", "Backbone.js", EcosystemCategory.FrontendFramework,
            new[] { "backbone" }, new[] { new ManifestRule("package.json") });
        yield return EcosystemProfileFactory.Framework("jquery", "jQuery", EcosystemCategory.FrontendFramework,
            new[] { "jquery" }, new[] { new ManifestRule("package.json") });

        // --- Mobile / desktop JS ---
        yield return EcosystemProfileFactory.Framework("ionic", "Ionic", EcosystemCategory.FrontendFramework,
            new[] { "ionic", "@ionic/angular", "@ionic/react" },
            new[] { new ManifestRule("package.json"), new ManifestRule("ionic.config.json") });
        yield return EcosystemProfileFactory.Framework("capacitor", "Capacitor", EcosystemCategory.FrontendFramework,
            new[] { "capacitor", "@capacitor/core" }, new[] { new ManifestRule("package.json"), new ManifestRule("capacitor.config.ts") });
        yield return EcosystemProfileFactory.Framework("tauri", "Tauri", EcosystemCategory.FullStack,
            new[] { "tauri" }, new[] { new ManifestRule("package.json"), new ManifestRule("src-tauri/Cargo.toml") },
            new[] { new EntryPointRule(new[] { ".rs" }, new[] { "tauri::", "tauri::Builder" }, new[] { "main.rs" }, 35) });

        // --- API / data layer (Node) ---
        yield return EcosystemProfileFactory.Framework("trpc", "tRPC", EcosystemCategory.BackendFramework,
            new[] { "trpc", "@trpc/server" }, new[] { new ManifestRule("package.json") },
            new[] { new EntryPointRule(new[] { ".ts" }, new[] { "initTRPC", "router(" }, new[] { "trpc", "server" }, 30) });
        yield return EcosystemProfileFactory.Framework("apollo-graphql", "Apollo GraphQL", EcosystemCategory.BackendFramework,
            new[] { "apollo", "apollo server", "@apollo/server" }, new[] { new ManifestRule("package.json") });
        yield return EcosystemProfileFactory.Framework("prisma", "Prisma", EcosystemCategory.BackendFramework,
            new[] { "prisma" }, new[] { new ManifestRule("package.json"), new ManifestRule("prisma/schema.prisma") });

        // --- Monorepo / tooling (affects artifact layout) ---
        yield return EcosystemProfileFactory.Framework("turborepo", "Turborepo", EcosystemCategory.FrontendFramework,
            new[] { "turborepo", "turbo repo" }, new[] { new ManifestRule("package.json"), new ManifestRule("turbo.json") });
        yield return EcosystemProfileFactory.Framework("nx", "Nx", EcosystemCategory.FrontendFramework,
            new[] { "nx", "nrwl" }, new[] { new ManifestRule("package.json"), new ManifestRule("nx.json") });
        yield return EcosystemProfileFactory.Framework("lerna", "Lerna", EcosystemCategory.FrontendFramework,
            new[] { "lerna" }, new[] { new ManifestRule("package.json"), new ManifestRule("lerna.json") });

        // --- JVM / Kotlin extra ---
        yield return EcosystemProfileFactory.Framework("ktor", "Ktor", EcosystemCategory.BackendFramework,
            new[] { "ktor" }, new[] { new ManifestRule("build.gradle.kts"), new ManifestRule("pom.xml") },
            new[] { new EntryPointRule(new[] { ".kt" }, new[] { "embeddedServer", "routing {" }, new[] { "Application.kt" }, 35) });
        yield return EcosystemProfileFactory.Framework("dropwizard", "Dropwizard", EcosystemCategory.BackendFramework,
            new[] { "dropwizard" }, new[] { new ManifestRule("pom.xml") });
        yield return EcosystemProfileFactory.Framework("play-framework", "Play Framework", EcosystemCategory.BackendFramework,
            new[] { "play framework", "play-framework", "play scala" },
            new[] { new ManifestRule("build.sbt"), new ManifestRule("pom.xml") });

        // --- Rust web / desktop ---
        yield return EcosystemProfileFactory.Framework("rocket", "Rocket (Rust)", EcosystemCategory.BackendFramework,
            new[] { "rocket", "rocket.rs" }, new[] { new ManifestRule("Cargo.toml") },
            new[] { new EntryPointRule(new[] { ".rs" }, new[] { "#[launch]", "rocket::" }, new[] { "main.rs" }, 30) });
        yield return EcosystemProfileFactory.Framework("leptos", "Leptos", EcosystemCategory.FullStack,
            new[] { "leptos" }, new[] { new ManifestRule("Cargo.toml") });

        // --- Python data / ML UI ---
        yield return EcosystemProfileFactory.Framework("streamlit", "Streamlit", EcosystemCategory.FrontendFramework,
            new[] { "streamlit" }, new[] { new ManifestRule("requirements.txt"), new ManifestRule("pyproject.toml") },
            new[] { new EntryPointRule(new[] { ".py" }, new[] { "st.", "streamlit" }, new[] { "app.py", "main.py" }, 35) });
        yield return EcosystemProfileFactory.Framework("gradio", "Gradio", EcosystemCategory.FrontendFramework,
            new[] { "gradio" }, entryPoints: new[] { new EntryPointRule(new[] { ".py" }, new[] { "gr.Interface", "gr.Blocks" }, new[] { "app.py" }, 30) });

        // --- PHP / Ruby extras ---
        yield return EcosystemProfileFactory.Framework("wordpress", "WordPress", EcosystemCategory.FullStack,
            new[] { "wordpress", "wp" }, new[] { new ManifestRule("wp-config.php"), new ManifestRule("composer.json") });
        yield return EcosystemProfileFactory.Framework("sinatra", "Sinatra", EcosystemCategory.BackendFramework,
            new[] { "sinatra" }, new[] { new ManifestRule("Gemfile") });

        // --- .NET UI ---
        yield return EcosystemProfileFactory.Framework("maui", ".NET MAUI", EcosystemCategory.FrontendFramework,
            new[] { "maui", ".net maui" }, new[] { new ManifestRule(".csproj") });
        yield return EcosystemProfileFactory.Framework("wpf", "WPF", EcosystemCategory.FrontendFramework,
            new[] { "wpf" }, new[] { new ManifestRule(".csproj") });

        // --- CSS / component libs often co-generated with React ---
        yield return EcosystemProfileFactory.Framework("tailwindcss", "Tailwind CSS", EcosystemCategory.FrontendFramework,
            new[] { "tailwind", "tailwindcss" },
            new[] { new ManifestRule("package.json"), new ManifestRule("tailwind.config.js"), new ManifestRule("tailwind.config.ts") });
        yield return EcosystemProfileFactory.Framework("shadcn-ui", "shadcn/ui", EcosystemCategory.FrontendFramework,
            new[] { "shadcn", "shadcn/ui" }, new[] { new ManifestRule("package.json"), new ManifestRule("components.json") });
    }
}
