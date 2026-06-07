using FluentAssertions;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentIntegration.LspBridge;
using Libr4.IDE.Domain.AutonomousAppGeneration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Libr4.IntegrationTests.IDE;

public sealed class LspBridgeTests
{
    [Fact]
    public void ResolveProfileKey_TypeScriptFile_ReturnsTypescript()
    {
        LspStackProfileResolver.ResolveProfileKey(null, "src/App.tsx")
            .Should().Be("typescript");
    }

    [Fact]
    public async Task LspBridge_MapsCompilerErrors_ToDiagnostics()
    {
        var bridge = new LspBridge(
            Options.Create(new LspBridgeOptions { Enabled = true, EnableProcessServers = false }),
            new ProcessLspClient(Options.Create(new LspBridgeOptions()), NullLogger<ProcessLspClient>.Instance),
            NullLogger<LspBridge>.Instance);

        var ctx = await bridge.GetWorkspaceContextAsync(
            [new GeneratedFile("Program.cs", "csharp", "class X {}")],
            null,
            [new ErrorReport("CS1002", "; expected", "", "Program.cs", 1)],
            ["Program.cs"]);

        ctx.Diagnostics.Should().Contain(d => d.Message.Contains("expected"));
        ctx.FormatForContextPack().Should().Contain("lsp_diagnostics");
    }
}
