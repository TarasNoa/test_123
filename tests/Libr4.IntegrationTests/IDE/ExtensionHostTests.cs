using FluentAssertions;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Hooks;
using Libr4.IDE.Application.AutonomousAppGeneration.Extensions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Libr4.IntegrationTests.IDE;

public sealed class ExtensionHostTests
{
    [Fact]
    public void ManifestLoader_ParsesHooksToolsSkills()
    {
        var dir = CreateTempExtensionDir();
        try
        {
            File.WriteAllText(Path.Combine(dir, "extension.yaml"), """
                name: demo-ext
                version: 1.2.3
                description: Demo extension
                hooks:
                  - kind: PreToolUse
                    script: hooks/pre.ps1
                tools:
                  - name: ext_ping
                    description: Ping tool
                    script: tools/ping.ps1
                    readOnly: true
                skills:
                  - id: ext-skill
                    description: Extension skill
                    path: skills/demo.md
                """);

            var manifest = ExtensionManifestLoader.LoadFromFile(Path.Combine(dir, "extension.yaml"));
            manifest.Name.Should().Be("demo-ext");
            manifest.Version.Should().Be("1.2.3");
            manifest.Hooks.Should().HaveCount(1);
            manifest.Tools.Should().ContainSingle(t => t.Name == "ext_ping");
            manifest.Skills.Should().ContainSingle(s => s.Id == "ext-skill");
        }
        finally
        {
            TryDeleteDir(dir);
        }
    }

    [Fact]
    public void ResolveRelativePath_BlocksEscape()
    {
        var root = CreateTempExtensionDir();
        try
        {
            var act = () => ExtensionManifestLoader.ResolveRelativePath(root, "..\\secret.txt");
            act.Should().Throw<InvalidOperationException>();
        }
        finally
        {
            TryDeleteDir(root);
        }
    }

    [Fact]
    public async Task Host_LoadsProjectExtensions_WithBindings()
    {
        var root = CreateTempExtensionsRoot();
        var extDir = Path.Combine(root, "hello-ext");
        Directory.CreateDirectory(Path.Combine(extDir, "tools"));
        Directory.CreateDirectory(Path.Combine(extDir, "skills"));
        File.WriteAllText(Path.Combine(extDir, "extension.yaml"), """
            name: hello-ext
            version: 0.1.0
            tools:
              - name: ext_hello
                description: Says hello
                script: tools/hello.cmd
                readOnly: true
            skills:
              - id: hello-skill
                path: skills/hello.md
            """);
        File.WriteAllText(Path.Combine(extDir, "tools", "hello.cmd"), "@echo hello-from-extension");
        File.WriteAllText(Path.Combine(extDir, "skills", "hello.md"), "# Hello skill");

        var host = new ExtensionHost(
            Options.Create(new ExtensionHostOptions
            {
                Enabled = true,
                ProjectExtensionsRoot = root,
                UserExtensionsRoot = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"))
            }),
            NullLogger<ExtensionHost>.Instance);

        await host.RefreshAsync(root);
        host.Extensions.Should().ContainSingle(e => e.Id == "hello-ext");
        host.Tools.Should().ContainSingle(t => t.Definition.Name == "ext_hello");
        host.Skills.Should().ContainSingle(s => s.Definition.Id == "hello-skill");
    }

    [Fact]
    public async Task SandboxRunner_ExecutesHookScript()
    {
        var extDir = CreateTempExtensionDir();
        var hooksDir = Path.Combine(extDir, "hooks");
        Directory.CreateDirectory(hooksDir);
        var scriptPath = Path.Combine(hooksDir, "ping.cmd");
        File.WriteAllText(scriptPath, "@echo extension-ok");

        var extension = new LoadedExtension(
            "demo",
            extDir,
            ExtensionSource.Project,
            new ExtensionManifestDocument { Name = "demo" },
            Path.Combine(extDir, "extension.yaml"));

        var binding = new ExtensionHookBinding(
            extension,
            new ExtensionHookDefinition { Kind = "SessionStart", Script = "hooks/ping.cmd" },
            scriptPath);

        var runner = new SandboxedExtensionRunner(
            Options.Create(new ExtensionHostOptions()),
            NullLogger<SandboxedExtensionRunner>.Instance);

        var result = await runner.RunHookAsync(
            binding,
            new HookContext { RunId = Guid.NewGuid(), SessionId = "sess" },
            CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Output.Should().Contain("extension-ok");
    }

    private static string CreateTempExtensionsRoot()
    {
        var path = Path.Combine(Path.GetTempPath(), "libr4-ext-root-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }

    private static string CreateTempExtensionDir()
    {
        var path = Path.Combine(Path.GetTempPath(), "libr4-ext-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }

    private static void TryDeleteDir(string path)
    {
        try
        {
            if (Directory.Exists(path))
                Directory.Delete(path, recursive: true);
        }
        catch
        {
            // best effort
        }
    }
}
