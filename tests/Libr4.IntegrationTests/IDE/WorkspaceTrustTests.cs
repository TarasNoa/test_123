using FluentAssertions;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Permissions;
using Libr4.IDE.Application.AutonomousAppGeneration.WorkspaceTrust;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Libr4.IntegrationTests.IDE;

public sealed class WorkspaceTrustTests
{
    [Fact]
    public void Hasher_SamePath_ProducesStableHash()
    {
        var a = WorkspaceTrustHasher.Compute(@"D:\projects\demo", null, "fp1");
        var b = WorkspaceTrustHasher.Compute(@"D:\projects\demo", null, "fp2");
        a.Should().Be(b);
    }

    [Fact]
    public async Task Store_RemembersDecision_PerWorkspaceHash()
    {
        var path = CreateTempDbPath();
        try
        {
            var store = CreateStore(path);
            await store.EnsureSchemaAsync();
            var hash = WorkspaceTrustHasher.Compute(null, "tenant-a", "fingerprint");

            await store.UpsertAsync(new WorkspaceTrustRecord(
                hash,
                WorkspaceSandboxPolicy.Strict,
                WorkspaceHostMode.LocalOnly,
                DateTime.UtcNow));

            var loaded = await store.GetAsync(hash);
            loaded.Should().NotBeNull();
            loaded!.SandboxPolicy.Should().Be(WorkspaceSandboxPolicy.Strict);
            loaded.HostMode.Should().Be(WorkspaceHostMode.LocalOnly);
        }
        finally
        {
            TryDeleteFile(path);
        }
    }

    [Fact]
    public async Task RunGate_FirstRunPrompt_ThenResolve_AppliesPermissionMode()
    {
        var path = CreateTempDbPath();
        try
        {
            var store = CreateStore(path);
            await store.EnsureSchemaAsync();
            var permissions = new AgentRunPermissionStore();
            var service = new WorkspaceTrustService(
                store,
                Options.Create(new WorkspaceTrustOptions { Enabled = true }),
                NullLogger<WorkspaceTrustService>.Instance);
            var gate = new WorkspaceTrustRunGate(
                service,
                permissions,
                Options.Create(new WorkspaceTrustOptions { Enabled = true }),
                NullLogger<WorkspaceTrustRunGate>.Instance);

            var runId = Guid.NewGuid();
            var hash = WorkspaceTrustHasher.Compute(null, "tenant-b", "fp");

            var state = await gate.BeginRunAsync(runId, hash);
            state.AwaitingPrompt.Should().BeTrue();
            state.PendingPrompt.Should().NotBeNull();

            var resolve = Task.Run(async () =>
            {
                await Task.Delay(50);
                await gate.ResolveAsync(
                    runId,
                    new WorkspaceTrustResolveRequest(
                        state.PendingPrompt!.PromptId,
                        WorkspaceSandboxPolicy.Strict,
                        WorkspaceHostMode.LocalOnly,
                        RememberChoice: true));
            });

            await gate.WaitForDecisionAsync(runId);
            await resolve;

            permissions.Get(runId).Should().Be(AgentPermissionMode.Plan);
            gate.DenyCloudInference(runId).Should().BeTrue();

            var remembered = await store.GetAsync(hash);
            remembered.Should().NotBeNull();
            remembered!.HostMode.Should().Be(WorkspaceHostMode.LocalOnly);
        }
        finally
        {
            TryDeleteFile(path);
        }
    }

    [Fact]
    public async Task TrustService_ConfigOverride_BypassesStore()
    {
        var path = CreateTempDbPath();
        try
        {
            var store = CreateStore(path);
            await store.EnsureSchemaAsync();
            var service = new WorkspaceTrustService(
                store,
                Options.Create(new WorkspaceTrustOptions
                {
                    Enabled = true,
                    ForceSandboxPolicy = "Permissive",
                    ForceHostMode = "CloudAllowed"
                }),
                NullLogger<WorkspaceTrustService>.Instance);

            var resolution = await service.ResolveAsync("any-hash");
            resolution.NeedsFirstRunPrompt.Should().BeFalse();
            resolution.Decision!.FromConfigOverride.Should().BeTrue();
            resolution.Decision.SandboxPolicy.Should().Be(WorkspaceSandboxPolicy.Permissive);
        }
        finally
        {
            TryDeleteFile(path);
        }
    }

    private static SqliteWorkspaceTrustStore CreateStore(string dbPath) =>
        new(Options.Create(new WorkspaceTrustOptions { DbPath = dbPath }), NullLogger<SqliteWorkspaceTrustStore>.Instance);

    private static string CreateTempDbPath() =>
        Path.Combine(Path.GetTempPath(), "libr4-workspace-trust-" + Guid.NewGuid().ToString("N") + ".db");

    private static void TryDeleteFile(string path)
    {
        try
        {
            if (File.Exists(path))
                File.Delete(path);
        }
        catch
        {
            // best effort
        }
    }
}
