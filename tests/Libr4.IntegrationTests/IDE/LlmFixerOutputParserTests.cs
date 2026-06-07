using FluentAssertions;
using Libr4.IDE.Application.AutonomousAppGeneration.Infrastructure;
using Libr4.IDE.Domain.AutonomousAppGeneration;
using Xunit;

namespace Libr4.IntegrationTests.IDE;

public sealed class LlmFixerOutputParserTests
{
    [Fact]
    public void Parse_AcceptsAlternatePathAndContentKeys()
    {
        const string raw = """
            {
              "files": [
                { "path": "backend/src/Foo.java", "body": "package x;\npublic class Foo {}" }
              ]
            }
            """;

        var current = Array.Empty<GeneratedFile>();
        var parsed = LlmFixerOutputParser.Parse(raw, current);

        parsed.Should().ContainSingle();
        parsed[0].RelativePath.Should().Be("backend/src/Foo.java");
        parsed[0].Content.Should().Contain("class Foo");
    }

    [Fact]
    public void Parse_LenientContract_SkipsPathOnlyEntriesButKeepsValidOnes()
    {
        const string raw = """
            {
              "files": [
                { "relativePath": "backend/pom.xml" },
                { "relativePath": "backend/src/Bar.java", "content": "package y;\npublic class Bar {}" }
              ]
            }
            """;

        var parsed = LlmFixerOutputParser.Parse(raw, Array.Empty<GeneratedFile>());

        parsed.Should().ContainSingle();
        parsed[0].RelativePath.Should().EndWith("Bar.java");
    }

    [Fact]
    public void Parse_RecoversMarkdownCodeFences()
    {
        const string raw = """
            Here are fixes:
            --- backend/src/main/java/com/generated/banking/security/JwtTokenProvider.java ---
            ```java
            package com.generated.banking.security;
            public class JwtTokenProvider {}
            ```
            """;

        var parsed = LlmFixerOutputParser.Parse(raw, Array.Empty<GeneratedFile>());

        parsed.Should().ContainSingle();
        parsed[0].RelativePath.Should().Contain("JwtTokenProvider.java");
        parsed[0].Content.Should().Contain("class JwtTokenProvider");
    }

    [Fact]
    public void Parse_SynthesizesAppTsx_FromSuggestedFixHints()
    {
        const string raw = """
            MissingType frontend/src/App.tsx: module does not export 'App' as imported by src/main.tsx.
            fix: create frontend/src/App.tsx with a named export of a react component named App.
            """;

        var parsed = LlmFixerOutputParser.Parse(raw, Array.Empty<GeneratedFile>());

        parsed.Should().ContainSingle();
        parsed[0].RelativePath.Should().Be("frontend/src/App.tsx");
        parsed[0].Content.Should().Contain("export function App");
    }

    [Fact]
    public void PromptPipelinePolicy_Fixing_AllowsPartialFilesEnvelope()
    {
        const string payload = """
            {"files":[{"relativePath":"backend/a.java","content":"ok"},{"relativePath":"backend/b.java"}]}
            """;

        var ok = PromptPipelinePolicy.ValidateOutputContract("fixing", payload, out var reason);

        ok.Should().BeTrue();
        reason.Should().BeEmpty();
    }
}
