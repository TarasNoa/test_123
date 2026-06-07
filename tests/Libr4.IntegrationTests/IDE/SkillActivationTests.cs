using System.Text.Json;
using FluentAssertions;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Abstractions;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Core;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Models;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Prompting;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Rollout;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Skills;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Tools;
using Libr4.IDE.Application.AutonomousAppGeneration.Runtime;
using Libr4.IDE.Domain.AutonomousAppGeneration;
using Microsoft.Extensions.Options;
using Xunit;

namespace Libr4.IntegrationTests.IDE;

public sealed class SkillActivationTests
{
    private readonly string _skillsRoot = Path.Combine(AppContext.BaseDirectory, "Agents", "Skills");

    private FileSkillManifestRegistry CreateRegistry() =>
        new(Options.Create(new SkillActivationOptions { SkillsRoot = _skillsRoot }));

    [Fact]
    public void Manifest_IndexesPythonDjango_WithOneLiner()
    {
        var registry = CreateRegistry();
        var entries = registry.List();

        entries.Should().NotBeEmpty();
        entries.Should().Contain(e => e.Id.Equals("python-django", StringComparison.OrdinalIgnoreCase));

        var manifest = registry.FormatManifest();
        manifest.Should().Contain("python-django:");
        manifest.Should().NotContain("## When to Use");
    }

    [Fact]
    public void BuildSystemPrompt_IncludesManifest_NotFullSkillBodies()
    {
        var registry = CreateRegistry();
        var resolver = new BuiltinPromptVarResolver(registry);
        var context = new BuiltinPromptVarContext();

        var prompt = AgentPromptBuilder.BuildSystemPrompt(
            isGeneration: false,
            BuiltinPromptStage.Repairing,
            resolver,
            context);

        prompt.Should().Contain("Skills manifest");
        prompt.Should().Contain("python-django:");
        prompt.Should().NotContain("You are a senior Django engineer");
    }

    [Fact]
    public async Task ActivateSkill_MidRun_LoadsPythonDjangoContent()
    {
        var runId = Guid.NewGuid();
        var registry = CreateRegistry();
        var consent = new InMemorySkillConsentGate();
        var rollout = new InMemoryRolloutRecorder();
        var tool = new ActivateSkillTool(
            registry,
            consent,
            Options.Create(new SkillActivationOptions { SkillsRoot = _skillsRoot }),
            rollout);

        var session = new AgentSessionState { RunId = runId };
        var context = BuildToolContext(session);
        var input = JsonDocument.Parse("""{"name":"python-django"}""").RootElement;

        var result = await tool.ExecuteAsync(input, context, CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Output.Should().Contain("status=activated");
        result.Output.Should().Contain("Django REST Framework");
        session.ActivatedSkills.Should().Contain("python-django");

        var audit = rollout.Entries.Last(e => e.Type == "skill_activation");
        audit.PayloadJson.Should().Contain("python-django");
    }

    [Fact]
    public async Task ActivateSkill_DjangoRestFrameworkAlias_ResolvesToPythonDjango()
    {
        var registry = CreateRegistry();
        var tool = new ActivateSkillTool(
            registry,
            new InMemorySkillConsentGate(),
            Options.Create(new SkillActivationOptions { SkillsRoot = _skillsRoot }));

        var session = new AgentSessionState { RunId = Guid.NewGuid() };
        var context = BuildToolContext(session);
        var input = JsonDocument.Parse("""{"name":"django-rest-framework"}""").RootElement;

        var result = await tool.ExecuteAsync(input, context, CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Output.Should().Contain("skill=python-django");
        session.ActivatedSkills.Should().Contain("python-django");
    }

    [Fact]
    public async Task ActivateSkill_SecondCall_DoesNotReInjectFullContent()
    {
        var registry = CreateRegistry();
        var tool = new ActivateSkillTool(
            registry,
            new InMemorySkillConsentGate(),
            Options.Create(new SkillActivationOptions { SkillsRoot = _skillsRoot }));

        var session = new AgentSessionState { RunId = Guid.NewGuid() };
        var context = BuildToolContext(session);
        var input = JsonDocument.Parse("""{"name":"python-django"}""").RootElement;

        var first = await tool.ExecuteAsync(input, context, CancellationToken.None);
        first.Success.Should().BeTrue();

        var second = await tool.ExecuteAsync(input, context, CancellationToken.None);
        second.Success.Should().BeTrue();
        second.Output.Should().Contain("already_active");
        second.Output.Should().NotContain("You are a senior Django engineer");
    }

    [Fact]
    public async Task ActivateSkill_RequiresConsent_WhenAutoApproveDisabled()
    {
        var registry = CreateRegistry();
        var tool = new ActivateSkillTool(
            registry,
            new InMemorySkillConsentGate(),
            Options.Create(new SkillActivationOptions
            {
                SkillsRoot = _skillsRoot,
                AutoApproveFirstActivation = false
            }));

        var session = new AgentSessionState { RunId = Guid.NewGuid() };
        var context = BuildToolContext(session);
        var input = JsonDocument.Parse("""{"name":"python-django"}""").RootElement;

        var result = await tool.ExecuteAsync(input, context, CancellationToken.None);

        result.Success.Should().BeFalse();
        result.Output.Should().Contain("consent");
    }

    [Fact]
    public void Manifest_IsMuchSmallerThanFullSkillCorpus()
    {
        var registry = CreateRegistry();
        var manifest = registry.FormatManifest();
        var fullChars = registry.List().Sum(e =>
        {
            if (!File.Exists(e.FilePath))
                return 0;
            return File.ReadAllText(e.FilePath).Length;
        });

        fullChars.Should().BeGreaterThan(manifest.Length * 4);
    }

    private static ToolContext BuildToolContext(AgentSessionState session)
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "libr4-skill-activation-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);
        return new ToolContext
        {
            Workspace = new ShadowWorkspaceContext(Guid.NewGuid(), tempDir, string.Empty, new StubRuntimeSession()),
            Accessor = null!,
            WorkingFiles = new List<GeneratedFile>(),
            FileState = new FileStateCache(),
            Session = session,
            ToolInput = JsonDocument.Parse("{}").RootElement
        };
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

    private sealed class InMemoryRolloutRecorder : IRolloutRecorder, IRolloutReplayService
    {
        public List<RolloutEntry> Entries { get; } = new();

        public Task RecordStepStartAsync(Guid runId, string sessionId, int stepNumber, CancellationToken ct = default) =>
            Task.CompletedTask;

        public Task RecordTextAsync(Guid runId, string sessionId, int stepNumber, string text, CancellationToken ct = default) =>
            Task.CompletedTask;

        public Task RecordToolUseAsync(
            Guid runId, string sessionId, int stepNumber, string toolName, string inputJson, string outputJson,
            bool success, long durationMs, IReadOnlyList<RolloutMediaAttachment>? media = null, CancellationToken ct = default) =>
            Task.CompletedTask;

        public Task RecordStepFinishAsync(
            Guid runId, string sessionId, int stepNumber, string finishReason, RolloutUsage? usage = null, CancellationToken ct = default) =>
            Task.CompletedTask;

        public Task RecordErrorAsync(Guid runId, string sessionId, string message, CancellationToken ct = default) =>
            Task.CompletedTask;

        public Task RecordPermissionDecisionAsync(Guid runId, string toolName, string decision, string? reason, CancellationToken ct = default) =>
            Task.CompletedTask;

        public Task RecordSkillActivationAsync(
            Guid runId, string sessionId, string skillName, bool firstActivation, bool consentGranted, int contentChars, CancellationToken ct = default)
        {
            Entries.Add(new RolloutEntry(
                "skill_activation",
                runId,
                sessionId,
                0,
                DateTime.UtcNow,
                JsonSerializer.Serialize(new { skillName, firstActivation, consentGranted, contentChars })));
            return Task.CompletedTask;
        }

        public Task RecordCompactionAsync(
            Guid runId, string sessionId, int beforeChars, int afterChars, int beforeTurns, int afterTurns, string summaryJson, CancellationToken ct = default) =>
            Task.CompletedTask;

        public Task RecordMemoryOperationAsync(
            Guid runId, string sessionId, string operation, string scope, string? key, string? kind, int resultCount, CancellationToken ct = default) =>
            Task.CompletedTask;

        public Task<IReadOnlyList<RolloutEntry>> GetRolloutAsync(Guid runId, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<RolloutEntry>>(Entries);

        public Task<IReadOnlyList<RolloutEntry>> ReplayAsync(Guid runId, CancellationToken ct = default) =>
            GetRolloutAsync(runId, ct);

        public Task<IReadOnlyList<RolloutSearchHit>> SearchAsync(string query, int limit = 25, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<RolloutSearchHit>>(Array.Empty<RolloutSearchHit>());
    }
}
