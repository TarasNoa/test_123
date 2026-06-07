using FluentAssertions;
using Libr4.IDE.Application.AutonomousAppGeneration.Infrastructure;
using Libr4.IDE.Domain.AutonomousAppGeneration;
using Xunit;

namespace Libr4.IntegrationTests.IDE;

public sealed class SurgicalPatchEngineTests
{
    [Fact]
    public void Apply_ReplacesExactSearch_AndCreatesNewFile()
    {
        var files = new List<GeneratedFile>
        {
            new("backend/src/main/java/com/app/web/AuthController.java", "java", """
                public class AuthController {
                    public Object login() { return authService.authenticate(u, p); }
                }
                """)
        };

        var result = SurgicalPatchEngine.Apply(
            files,
            new[]
            {
                new SurgicalPatchEngine.SurgicalEdit(
                    "backend/src/main/java/com/app/web/AuthController.java",
                    "authService.authenticate(u, p)",
                    "authService.authenticate(request)")
            },
            new[]
            {
                new SurgicalPatchEngine.NewFile(
                    "backend/src/main/java/com/app/dto/AuthTokenRequest.java",
                    "package com.app.dto; public record AuthTokenRequest(String username, String password) {}")
            });

        result.AppliedEdits.Should().Be(2);
        result.Patches.Should().HaveCount(2);
        result.Patches.Single(f => f.RelativePath.Contains("AuthController")).Content
            .Should().Contain("authService.authenticate(request)");
        result.Patches.Should().Contain(f => f.RelativePath.Contains("AuthTokenRequest"));
    }

    [Fact]
    public void Parse_ExtractsEditsFromJson()
    {
        var raw = """
            {
              "edits": [
                { "relativePath": "frontend/src/App.tsx", "search": "apiClient", "replace": "fetchItems" }
              ],
              "newFiles": []
            }
            """;

        var parsed = SurgicalFixerOutputParser.Parse(raw);
        parsed.Edits.Should().HaveCount(1);
        parsed.Edits[0].RelativePath.Should().Contain("App.tsx");
    }
}
