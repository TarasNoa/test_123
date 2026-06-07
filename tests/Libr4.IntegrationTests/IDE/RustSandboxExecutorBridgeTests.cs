using FluentAssertions;
using Libr4.IDE.Application.AutonomousAppGeneration.Infrastructure.Rust;
using Libr4.IDE.Application.AutonomousAppGeneration.Runtime;
using Xunit;

namespace Libr4.IntegrationTests.IDE;

public sealed class RustSandboxExecutorBridgeTests : IDisposable
{
    private readonly string _workspaceRoot;

    public RustSandboxExecutorBridgeTests()
    {
        _workspaceRoot = Path.Combine(Path.GetTempPath(), $"rust-sandbox-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_workspaceRoot);
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_workspaceRoot))
                Directory.Delete(_workspaceRoot, recursive: true);
        }
        catch
        {
            // best-effort
        }
    }

    [Fact]
    public void IsAvailable_WhenNativeLibraryMissing_DoesNotThrow()
    {
        var act = () => _ = RustSandboxExecutorBridge.IsAvailable;
        act.Should().NotThrow();
    }

    [Fact]
    public void TryExecute_WhenNativeLibraryMissing_ReturnsFalse()
    {
        if (RustSandboxExecutorBridge.IsAvailable)
            return;

        var ok = RustSandboxExecutorBridge.TryExecute(
            _workspaceRoot,
            "shell",
            OperatingSystem.IsWindows() ? "echo libr4-rust-sandbox" : "echo libr4-rust-sandbox",
            TimeSpan.FromSeconds(10),
            logger: null,
            out var result);

        ok.Should().BeFalse();
        result.Should().Be(SandboxExecutorBridgeResult.Empty);
    }

    [Fact]
    public void TryExecute_WhenNativeLibraryPresent_RunsSimpleEcho()
    {
        if (!RustSandboxExecutorBridge.IsAvailable)
            return;

        var command = OperatingSystem.IsWindows() ? "echo libr4-rust-sandbox" : "echo libr4-rust-sandbox";
        var ok = RustSandboxExecutorBridge.TryExecute(
            _workspaceRoot,
            "shell",
            command,
            TimeSpan.FromSeconds(15),
            logger: null,
            out var result);

        ok.Should().BeTrue();
        result.ExitCode.Should().Be(0);
        result.Stdout.Should().Contain("libr4-rust-sandbox");
    }

    [Fact]
    public void TryRunShell_WhenNativeLibraryMissing_ReturnsFalse()
    {
        if (RustSandboxExecutorBridge.IsAvailable)
            return;

        var ok = RustSandboxExecutorBridge.TryRunShell(
            _workspaceRoot,
            OperatingSystem.IsWindows() ? "echo libr4-rust-shell" : "echo libr4-rust-shell",
            TimeSpan.FromSeconds(10),
            logger: null,
            out var result);

        ok.Should().BeFalse();
        result.Should().Be(SandboxExecutorBridgeResult.Empty);
    }

    [Fact]
    public void TryRunShell_WhenNativeLibraryPresent_RunsSimpleEcho()
    {
        if (!RustSandboxExecutorBridge.IsAvailable)
            return;

        var command = OperatingSystem.IsWindows() ? "echo libr4-rust-shell" : "echo libr4-rust-shell";
        var ok = RustSandboxExecutorBridge.TryRunShell(
            _workspaceRoot,
            command,
            TimeSpan.FromSeconds(15),
            logger: null,
            out var result);

        ok.Should().BeTrue();
        result.ExitCode.Should().Be(0);
        result.Stdout.Should().Contain("libr4-rust-shell");
    }

    [Theory]
    [InlineData("python -m pytest -q", true)]
    [InlineData("npm test --silent", true)]
    [InlineData("bash ./build.sh", true)]
    [InlineData("dotnet build -c Release", false)]
    public void TryMapRustExecution_ClassifiesSupportedCommands(string command, bool expected)
    {
        var mapped = RustBackedIsolatedRuntime.TryMapRustExecution(command, out _, out _);
        mapped.Should().Be(expected);
    }
}
