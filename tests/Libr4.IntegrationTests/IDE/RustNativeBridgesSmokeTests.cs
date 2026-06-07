using FluentAssertions;
using Libr4.Gateway.Infrastructure.Rust;
using Libr4.IDE.Application.AutonomousAppGeneration.Infrastructure.Rust;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Libr4.IntegrationTests.IDE;

/// <summary>Wave 3 native bridge smoke tests — skip positive paths when cdylib unavailable (CI builds them).</summary>
public sealed class RustNativeBridgesSmokeTests : IDisposable
{
    private readonly string _workspaceRoot;
    private readonly string _rolloutPath;

    public RustNativeBridgesSmokeTests()
    {
        _workspaceRoot = Path.Combine(Path.GetTempPath(), $"rust-bridges-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_workspaceRoot);
        File.WriteAllText(
            Path.Combine(_workspaceRoot, "models.py"),
            "# libr4_rust_bridge_smoke_token\nclass Marker: pass\n");

        _rolloutPath = Path.Combine(_workspaceRoot, "rollout.jsonl");
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
    public void AllBridges_IsAvailable_DoesNotThrow()
    {
        var act = () =>
        {
            _ = RustSandboxExecutorBridge.IsAvailable;
            _ = RustFastContextBridge.IsAvailable;
            _ = RustRolloutWriterBridge.IsAvailable;
            _ = RustDelegationWorkerBridge.IsAvailable;
            _ = RustGatewayCoreBridge.IsAvailable;
        };

        act.Should().NotThrow();
    }

    [Fact]
    public void FastContext_WhenNativePresent_BuildsWorkspaceManifest()
    {
        if (!RustFastContextBridge.IsAvailable)
            return;

        var ok = RustFastContextBridge.TryBuildManifest(
            _workspaceRoot,
            NullLogger.Instance,
            out var manifest);

        ok.Should().BeTrue();
        manifest.Should().NotBeNull();
        manifest!.WorkspaceRoot.Replace('\\', '/').Should().EndWith(Path.GetFileName(_workspaceRoot));
        manifest.WorkspaceHash.Should().NotBeNullOrWhiteSpace();
        manifest.FileCount.Should().BeGreaterOrEqualTo(1);
    }

    [Fact]
    public void RolloutWriter_WhenNativePresent_AppendsNdjsonLine()
    {
        if (!RustRolloutWriterBridge.IsAvailable)
            return;

        const string line = "{\"type\":\"rust_smoke\",\"stepNumber\":1}";
        var ok = RustRolloutWriterBridge.TryAppendLine(_rolloutPath, line, NullLogger.Instance);

        ok.Should().BeTrue();
        File.Exists(_rolloutPath).Should().BeTrue();
        File.ReadAllText(_rolloutPath).Should().Contain("rust_smoke");
    }

    [Fact]
    public void DelegationWorker_WhenNativePresent_ReturnsStructuredErrorForMissingJob()
    {
        if (!RustDelegationWorkerBridge.IsAvailable)
            return;

        var ok = RustDelegationWorkerBridge.TryRunWorker(
            Path.Combine(_workspaceRoot, "missing-job.json"),
            "dotnet",
            _workspaceRoot,
            timeoutMinutes: 1,
            memoryLimitMb: 256,
            maxRestartAttempts: 0,
            NullLogger.Instance,
            out var succeeded,
            out _,
            out var error,
            out var timedOut);

        ok.Should().BeTrue("native bridge should return structured JSON even on failure");
        succeeded.Should().BeFalse();
        timedOut.Should().BeFalse();
        error.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void GatewayCore_WhenNativePresent_ExecutesCircuitAndRateLimit()
    {
        if (!RustGatewayCoreBridge.IsAvailable)
            return;

        var key = $"smoke-{Guid.NewGuid():N}";
        RustGatewayCoreBridge.IsCircuitOpen(key).Should().BeFalse();
        RustGatewayCoreBridge.AllowRequest(key, capacity: 5, refillPerSec: 1).Should().BeTrue();

        RustGatewayCoreBridge.RecordCircuitFailure(key);
        RustGatewayCoreBridge.RecordCircuitSuccess(key);

        var decision = RustGatewayCoreBridge.EvaluateRisk(new RustRiskFeatures(
            RequestCount: 12,
            ErrorRate: 0.05f,
            UniquePaths: 4,
            TimeWindow: 60,
            Burstiness: 0.2f,
            RecentViolations: 0));

        decision.Should().NotBeNull();
        decision!.RiskScore.Should().BeGreaterOrEqualTo(0);
        decision.Action.Should().NotBeNullOrWhiteSpace();
    }
}
