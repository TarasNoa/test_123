using FluentAssertions;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentBackends;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Abstractions;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Models;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Subagents;
using Libr4.IDE.Application.AutonomousAppGeneration.Runtime;
using Libr4.IDE.Domain.AutonomousAppGeneration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Libr4.IntegrationTests.IDE;

public sealed class AgentBackendTests
{
    [Theory]
    [InlineData("libr4-native", AgentBackendKind.Libr4Native)]
    [InlineData("cursor-sdk", AgentBackendKind.CursorSdk)]
    [InlineData("codex-cli", AgentBackendKind.CodexCli)]
    public void Descriptor_Parse_MapsKnownSlugs(string slug, AgentBackendKind expected)
    {
        AgentBackendDescriptor.Parse(slug).Kind.Should().Be(expected);
    }

    [Fact]
    public void EventMapper_EmitsNdjsonWithTypeField()
    {
        var evt = AgentBackendEventMapper.CreateStatusEvent(
            Guid.NewGuid(),
            "instance-1",
            "running",
            3);

        var line = AgentBackendEventMapper.ToNdjsonLine(evt);
        line.Should().Contain("\"type\":\"status\"");
        line.Should().Contain("\"stage\":\"running\"");
    }

    [Fact]
    public void AgentSpecLoader_ResolvesBackendFromYaml()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"agent-spec-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        var path = Path.Combine(tempDir, "custom.agent.yaml");
        File.WriteAllText(path, """
            name: custom
            backend: libr4-native
            backendConfig:
              profile: default
            maxTurns: 4
            instruction: test
            """);

        try
        {
            var doc = AgentSpecLoader.LoadFromFile(path);
            var spec = AgentSpecLoader.Resolve(doc, new Dictionary<string, AgentSpecDocument>());
            spec.Backend.Kind.Should().Be(AgentBackendKind.Libr4Native);
            spec.Backend.Config.Should().ContainKey("profile");
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public void Registry_ResolvesNativeBackend()
    {
        var native = new Libr4NativeAgentBackend(
            new StubScopeFactory(),
            new StubRolloutRecorder(),
            Microsoft.Extensions.Logging.Abstractions.NullLogger<Libr4NativeAgentBackend>.Instance);

        var registry = new AgentBackendRegistry([native, new CodexCliAgentBackend(
            Microsoft.Extensions.Options.Options.Create(new ExternalAgentBackendOptions()),
            Microsoft.Extensions.Logging.Abstractions.NullLogger<CodexCliAgentBackend>.Instance)]);

        registry.SupportedKinds.Should().Contain(AgentBackendKind.Libr4Native);
        registry.SupportedKinds.Should().Contain(AgentBackendKind.CodexCli);
        registry.Resolve(AgentBackendDescriptor.Native).Kind.Should().Be(AgentBackendKind.Libr4Native);
    }

    [Fact]
    public async Task MetadataStore_WritesAndReadsBackendKind()
    {
        var root = Path.Combine(Path.GetTempPath(), $"backend-meta-{Guid.NewGuid():N}");
        var runId = Guid.NewGuid();
        try
        {
            await AgentBackendRunMetadataStore.WriteAsync(root, runId, AgentBackendKind.CodexCli, "inst-1");
            AgentBackendRunMetadataStore.TryReadKind(root, runId).Should().Be(AgentBackendKind.CodexCli);
        }
        finally
        {
            if (Directory.Exists(root))
                Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void Registry_ResolvesCodexBackend()
    {
        var registry = new AgentBackendRegistry([
            new CodexCliAgentBackend(
                Microsoft.Extensions.Options.Options.Create(new ExternalAgentBackendOptions()),
                Microsoft.Extensions.Logging.Abstractions.NullLogger<CodexCliAgentBackend>.Instance)]);

        registry.Resolve(AgentBackendDescriptor.Parse("codex-cli")).Kind.Should().Be(AgentBackendKind.CodexCli);
    }

    [Fact]
    public void Registry_ResolvesCursorSdkBackend()
    {
        var registry = new AgentBackendRegistry([
            new CursorSdkAgentBackend(
                Microsoft.Extensions.Options.Options.Create(new ExternalAgentBackendOptions()),
                Microsoft.Extensions.Logging.Abstractions.NullLogger<CursorSdkAgentBackend>.Instance)]);

        registry.Resolve(AgentBackendDescriptor.Parse("cursor-sdk")).Kind.Should().Be(AgentBackendKind.CursorSdk);
    }

    [Fact]
    public void Registry_ResolvesExternalAcpBackend()
    {
        var registry = new AgentBackendRegistry([
            new ExternalAcpAgentBackend(
                Microsoft.Extensions.Options.Options.Create(new ExternalAgentBackendOptions()),
                Microsoft.Extensions.Logging.Abstractions.NullLogger<ExternalAcpAgentBackend>.Instance)]);

        registry.Resolve(AgentBackendDescriptor.Parse("external-acp")).Kind.Should().Be(AgentBackendKind.ExternalAcp);
    }

    [Fact]
    public async Task ExternalAcpBackend_CompletesWithMockNodeAgent()
    {
        var script = ResolveRepoPath("scripts/acp-mock-agent.mjs");
        File.Exists(script).Should().BeTrue("mock ACP script must exist");

        var backend = new ExternalAcpAgentBackend(
            Options.Create(new ExternalAgentBackendOptions()),
            NullLogger<ExternalAcpAgentBackend>.Instance);

        var config = new Dictionary<string, string>
        {
            ["executable"] = "node",
            ["args"] = script,
            ["timeoutSeconds"] = "30"
        };

        var handle = await backend.SpawnAsync(new AgentBackendSpawnRequest(
            Guid.NewGuid(),
            "implementer",
            new AgentBackendDescriptor(AgentBackendKind.ExternalAcp, config),
            InitialMessage: "say hello"));

        var events = new List<AgentBackendEvent>();
        using var streamCts = new CancellationTokenSource(TimeSpan.FromSeconds(20));
        var streamTask = Task.Run(async () =>
        {
            try
            {
                await foreach (var evt in backend.StreamEventsAsync(handle.BackendInstanceId, streamCts.Token))
                    events.Add(evt);
            }
            catch (OperationCanceledException)
            {
            }
        });

        var result = await PollUntilComplete(backend, handle.BackendInstanceId, TimeSpan.FromSeconds(15));
        await streamTask;

        result.Succeeded.Should().BeTrue();
        events.Should().Contain(e => e.PayloadJson.Contains("mock-acp-done", StringComparison.OrdinalIgnoreCase));
    }

    private static string ResolveRepoPath(string relative)
    {
        var candidates = new[]
        {
            Path.GetFullPath(relative),
            Path.Combine(Directory.GetCurrentDirectory(), relative),
            Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", relative)
        };

        return candidates.FirstOrDefault(File.Exists) ?? Path.GetFullPath(relative);
    }

    private static async Task<AgentSessionResult> PollUntilComplete(
        IAgentBackend backend,
        string instanceId,
        TimeSpan timeout)
    {
        var deadline = DateTime.UtcNow + timeout;
        while (DateTime.UtcNow < deadline)
        {
            var status = await backend.GetStatusAsync(instanceId);
            if (status.Status is AgentBackendRunStatus.Completed or AgentBackendRunStatus.Failed or AgentBackendRunStatus.Cancelled)
                break;
            await Task.Delay(200);
        }

        var final = await backend.GetStatusAsync(instanceId);
        return new AgentSessionResult(
            final.Status == AgentBackendRunStatus.Completed,
            final.Error ?? final.Stage ?? final.Status.ToString(),
            Array.Empty<GeneratedFile>(),
            final.StepNumber ?? 0,
            Array.Empty<string>());
    }

    [Fact]
    public async Task MetadataStore_WritesFallbackMetadata()
    {
        var root = Path.Combine(Path.GetTempPath(), $"backend-meta-{Guid.NewGuid():N}");
        var runId = Guid.NewGuid();
        try
        {
            await AgentBackendRunMetadataStore.WriteAsync(
                root,
                runId,
                AgentBackendKind.Libr4Native,
                "inst-2",
                fallbackFrom: AgentBackendKind.CodexCli,
                fallbackReason: "cli_failed");

            var meta = AgentBackendRunMetadataStore.TryRead(root, runId);
            meta.Should().NotBeNull();
            meta!.Backend.Should().Be(AgentBackendKind.Libr4Native);
            meta.FallbackFrom.Should().Be(AgentBackendKind.CodexCli);
            meta.FallbackReason.Should().Be("cli_failed");
        }
        finally
        {
            if (Directory.Exists(root))
                Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void Coordinator_RejectsDisallowedBackend()
    {
        var registry = new AgentBackendRegistry([
            new CodexCliAgentBackend(
                Microsoft.Extensions.Options.Options.Create(new ExternalAgentBackendOptions()),
                Microsoft.Extensions.Logging.Abstractions.NullLogger<CodexCliAgentBackend>.Instance)]);

        var coordinator = new AgentBackendCoordinator(
            registry,
            Microsoft.Extensions.Options.Options.Create(new AgentRuntimeOptions { RunsRoot = Path.GetTempPath() }),
            Microsoft.Extensions.Options.Options.Create(new ExternalAgentBackendOptions
            {
                AllowedBackends = ["Libr4Native"]
            }),
            Microsoft.Extensions.Logging.Abstractions.NullLogger<AgentBackendCoordinator>.Instance);

        var act = () => coordinator.RunSessionAsync(new AgentBackendSpawnRequest(
            Guid.NewGuid(),
            "implementer",
            AgentBackendDescriptor.Parse("codex-cli"),
            InitialMessage: "test"));

        act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*agent_backend_not_allowed*");
    }

    [Fact]
    public async Task IsolatedRunner_ExecutesCommandInWorkspaceMount()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"iso-runner-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        try
        {
            var runtime = new ProcessIsolatedRuntime(NullLogger<ProcessIsolatedRuntime>.Instance);
            var runner = new IsolatedExternalBackendRunner(
                runtime,
                new ExternalAgentBackendOptions(),
                NullLogger<IsolatedExternalBackendRunner>.Instance);

            var command = OperatingSystem.IsWindows()
                ? "cmd /c echo hello-isolated"
                : "echo hello-isolated";

            var (outcome, session) = await runner.RunAsync(
                tempDir,
                command,
                environmentVariables: null,
                TimeSpan.FromSeconds(30),
                CancellationToken.None);

            await using (session)
            {
                outcome.ExitCode.Should().Be(0);
                string.Join(' ', outcome.StdoutLines).Should().Contain("hello-isolated");
            }
        }
        finally
        {
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public async Task CodexBackend_IsolatedMode_CapturesStdout()
    {
        var runtime = new ProcessIsolatedRuntime(NullLogger<ProcessIsolatedRuntime>.Instance);
        var runner = new IsolatedExternalBackendRunner(
            runtime,
            new ExternalAgentBackendOptions(),
            NullLogger<IsolatedExternalBackendRunner>.Instance);
        var backend = new CodexCliAgentBackend(
            Options.Create(new ExternalAgentBackendOptions()),
            NullLogger<CodexCliAgentBackend>.Instance,
            runner);

        var config = new Dictionary<string, string>
        {
            ["isolate"] = "true",
            ["timeoutSeconds"] = "30",
            ["executableOverride"] = OperatingSystem.IsWindows() ? "cmd" : "echo",
            ["args"] = OperatingSystem.IsWindows() ? "/c,echo,hello-isolated" : "hello-isolated"
        };

        var handle = await backend.SpawnAsync(new AgentBackendSpawnRequest(
            Guid.NewGuid(),
            "implementer",
            new AgentBackendDescriptor(AgentBackendKind.CodexCli, config),
            InitialMessage: "ignored"));

        var result = await backend.WaitForCompletionAsync(handle.BackendInstanceId, CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        result.Trace.Should().Contain(l => l.Contains("hello-isolated", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task NativeBackend_RunSession_CompletesWithStubAgentSession()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"native-backend-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        var runId = Guid.NewGuid();
        try
        {
            var expected = new AgentSessionResult(
                true,
                "native_done",
                Array.Empty<GeneratedFile>(),
                2,
                Array.Empty<string>());

            var native = new Libr4NativeAgentBackend(
                new StubScopeFactoryWithSession(expected),
                new StubRolloutRecorder(),
                NullLogger<Libr4NativeAgentBackend>.Instance);

            var handle = await native.SpawnAsync(new AgentBackendSpawnRequest(
                runId,
                "implementer",
                AgentBackendDescriptor.Native,
                BuildMinimalSessionRequest(tempDir, runId)));

            var result = await native.WaitForCompletionAsync(handle.BackendInstanceId, CancellationToken.None);

            result.Succeeded.Should().BeTrue();
            result.Summary.Should().Be("native_done");
        }
        finally
        {
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public async Task CliBackend_Cancel_MarksCancelled()
    {
        var backend = new CodexCliAgentBackend(
            Options.Create(new ExternalAgentBackendOptions { DefaultTimeoutSeconds = 120 }),
            NullLogger<CodexCliAgentBackend>.Instance);

        var config = new Dictionary<string, string>
        {
            ["timeoutSeconds"] = "120",
            ["executableOverride"] = OperatingSystem.IsWindows() ? "cmd" : "sh",
            ["args"] = OperatingSystem.IsWindows() ? "/c,timeout,/t,60,/nobreak" : "-c,sleep 60"
        };

        var handle = await backend.SpawnAsync(new AgentBackendSpawnRequest(
            Guid.NewGuid(),
            "implementer",
            new AgentBackendDescriptor(AgentBackendKind.CodexCli, config),
            InitialMessage: "ignored"));

        await Task.Delay(300);
        await backend.CancelAsync(handle.BackendInstanceId);
        await Task.Delay(300);

        var status = await backend.GetStatusAsync(handle.BackendInstanceId);
        status.Status.Should().Be(AgentBackendRunStatus.Cancelled);
    }

    [Fact]
    public void BuildShellCommand_QuotesArgumentsWithSpaces()
    {
        var command = IsolatedExternalBackendRunner.BuildShellCommand("node", ["run", "my script.mjs", "--prompt", "hello world"]);
        command.Should().Contain("\"my script.mjs\"");
        command.Should().Contain("\"hello world\"");
    }

    private static AgentSessionRunRequest BuildMinimalSessionRequest(string tempDir, Guid runId)
    {
        var plan = new GenerationPlan(
            applicationName: "TestApp",
            applicationDescription: "Test",
            techStack: new TechStack(
                new[] { "Python" },
                new[] { "Django" },
                Array.Empty<string>(),
                Array.Empty<string>(),
                "test"),
            phases: Array.Empty<GenerationPhase>(),
            requiredAgents: Array.Empty<string>(),
            runtimeImage: "python:3.12-slim",
            buildCommands: Array.Empty<string>(),
            testCommands: Array.Empty<string>());

        return new AgentSessionRunRequest(
            Objective: "test objective",
            Workspace: new ShadowWorkspaceContext(
                Guid.NewGuid(),
                tempDir,
                string.Empty,
                new StubRuntimeSession()),
            WorkingFiles: new List<GeneratedFile>(),
            Plan: plan,
            Accessor: null!,
            RunId: runId);
    }

    private sealed class StubRuntimeSession : IRuntimeSession
    {
        public string ProviderName => "stub";
        public string SessionId => "stub";
        public string HostMountPath => string.Empty;
        public string GuestMountPath => "/workspace";
        public string Image => "stub";

        public Task<ExecResult> ExecAsync(
            string command,
            string workingSubDirectory,
            IDictionary<string, string>? environmentVariables = null,
            TimeSpan? timeout = null,
            CancellationToken ct = default) =>
            Task.FromResult(new ExecResult(0, TimeSpan.Zero, Array.Empty<ConsoleLogEntry>()));

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }

    private sealed class StubScopeFactoryWithSession : IServiceScopeFactory
    {
        private readonly AgentSessionResult _result;

        public StubScopeFactoryWithSession(AgentSessionResult result) => _result = result;

        public IServiceScope CreateScope() => new StubScope(_result);

        private sealed class StubScope : IServiceScope
        {
            public StubScope(AgentSessionResult result) =>
                ServiceProvider = new StubProvider(result);

            public IServiceProvider ServiceProvider { get; }

            public void Dispose() { }

            private sealed class StubProvider : IServiceProvider
            {
                private readonly AgentSessionResult _result;

                public StubProvider(AgentSessionResult result) => _result = result;

                public object? GetService(Type serviceType) =>
                    serviceType == typeof(IAgentSession) ? new StubAgentSession(_result) : null;
            }
        }

        private sealed class StubAgentSession : IAgentSession
        {
            private readonly AgentSessionResult _result;

            public StubAgentSession(AgentSessionResult result) => _result = result;

            public Task<AgentSessionResult> RunAsync(AgentSessionRunRequest request, CancellationToken ct = default) =>
                Task.FromResult(_result);

            public Task<AgentSessionResult> RunAsync(
                string objective,
                ShadowWorkspaceContext workspace,
                IList<GeneratedFile> workingFiles,
                GenerationPlan plan,
                IShadowWorkspaceAccessor accessor,
                string? buildLog,
                CancellationToken ct = default) =>
                Task.FromResult(_result);

            public Task<AgentSessionResult> ResumeAsync(string sessionId, AgentSessionRunRequest request, CancellationToken ct = default) =>
                Task.FromResult(_result);

            public Task<string> CheckpointAsync(string sessionId, IReadOnlyList<AgentConversationTurn> turns, CancellationToken ct = default) =>
                Task.FromResult(sessionId);

            public Task<IReadOnlyList<AgentConversationTurn>> RewindAsync(string sessionId, string checkpointId, CancellationToken ct = default) =>
                Task.FromResult<IReadOnlyList<AgentConversationTurn>>(Array.Empty<AgentConversationTurn>());
        }
    }

    private sealed class StubScopeFactory : IServiceScopeFactory
    {
        public IServiceScope CreateScope() => new StubScope();

        private sealed class StubScope : IServiceScope
        {
            public IServiceProvider ServiceProvider { get; } = new StubProvider();
            public void Dispose() { }

            private sealed class StubProvider : IServiceProvider
            {
                public object? GetService(Type serviceType) => null;
            }
        }
    }

    private sealed class StubRolloutRecorder : Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Rollout.IRolloutRecorder
    {
        public Task RecordStepStartAsync(Guid runId, string sessionId, int stepNumber, CancellationToken ct = default) => Task.CompletedTask;
        public Task RecordTextAsync(Guid runId, string sessionId, int stepNumber, string text, CancellationToken ct = default) => Task.CompletedTask;
        public Task RecordToolUseAsync(Guid runId, string sessionId, int stepNumber, string toolName, string inputJson, string outputJson, bool success, long durationMs, IReadOnlyList<Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Rollout.RolloutMediaAttachment>? media = null, CancellationToken ct = default) => Task.CompletedTask;
        public Task RecordStepFinishAsync(Guid runId, string sessionId, int stepNumber, string finishReason, Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Rollout.RolloutUsage? usage = null, CancellationToken ct = default) => Task.CompletedTask;
        public Task RecordErrorAsync(Guid runId, string sessionId, string message, CancellationToken ct = default) => Task.CompletedTask;
        public Task RecordPermissionDecisionAsync(Guid runId, string toolName, string decision, string? reason, CancellationToken ct = default) => Task.CompletedTask;
        public Task RecordSkillActivationAsync(Guid runId, string sessionId, string skillName, bool firstActivation, bool consentGranted, int contentChars, CancellationToken ct = default) => Task.CompletedTask;
        public Task RecordCompactionAsync(Guid runId, string sessionId, int beforeChars, int afterChars, int beforeTurns, int afterTurns, string summaryJson, CancellationToken ct = default) => Task.CompletedTask;
        public Task RecordMemoryOperationAsync(Guid runId, string sessionId, string operation, string scope, string? key, string? kind, int resultCount, CancellationToken ct = default) => Task.CompletedTask;
        public Task<IReadOnlyList<Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Rollout.RolloutEntry>> GetRolloutAsync(Guid runId, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Rollout.RolloutEntry>>(Array.Empty<Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Rollout.RolloutEntry>());
        public Task<IReadOnlyList<Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Rollout.RolloutSearchHit>> SearchAsync(string query, int limit = 25, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Rollout.RolloutSearchHit>>(Array.Empty<Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Rollout.RolloutSearchHit>());
    }
}
