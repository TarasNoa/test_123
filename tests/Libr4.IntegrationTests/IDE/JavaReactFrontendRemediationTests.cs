using FluentAssertions;
using Libr4.IDE.Application.AutonomousAppGeneration.Infrastructure;
using Libr4.IDE.Domain.AutonomousAppGeneration;
using Xunit;

namespace Libr4.IntegrationTests.IDE;

public sealed class ReactFrontendRemediationTests
{
    [Fact]
    public void Apply_AddsNamedExportApp_AndAlignsClientTestFromExports()
    {
        var files = new List<GeneratedFile>
        {
            new("frontend/src/main.tsx", "typescript", """
                import { createRoot } from 'react-dom/client';
                import { App } from './App';
                createRoot(document.getElementById('root')!).render(<App />);
                """),
            new("frontend/src/api/client.ts", "typescript", """
                export async function fetchItems() { return []; }
                export async function createItem() { return {}; }
                export async function deleteItem() { return {}; }
                """),
            new("frontend/src/api/client.test.ts", "typescript", """
                import { apiClient } from './client';
                describe('x', () => { it('y', () => expect(apiClient).toBeDefined()); });
                """)
        };

        var changed = ReactFrontendRemediation.Apply(files, JavaReactPlan(), Array.Empty<ErrorReport>());

        changed.Should().BeGreaterThan(0);
        files.Should().Contain(f => f.RelativePath.Equals("frontend/src/App.tsx", StringComparison.OrdinalIgnoreCase));
        files.Single(f => f.RelativePath.EndsWith("App.tsx")).Content
            .Should().Contain("export function App");
        files.Single(f => f.RelativePath.EndsWith("client.test.ts")).Content
            .Should().Contain("fetchItems")
            .And.Contain("createItem")
            .And.NotContain("apiClient");
    }

    private static GenerationPlan JavaReactPlan() =>
        new(
            "InventoryApp",
            "Java Spring Boot backend + React TypeScript frontend",
            new TechStack(
                ["Java", "TypeScript"],
                ["Spring Boot", "React"],
                ["PostgreSQL"],
                [],
                "fullstack"),
            [],
            [],
            "eclipse-temurin:21-jdk",
            ["cd backend && mvn -q package"],
            [],
            6);
}
