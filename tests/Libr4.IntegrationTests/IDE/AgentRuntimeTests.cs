using FluentAssertions;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Abstractions;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Core;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Events;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.ExecPolicy;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Models;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Patching;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Pathing;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Persistence;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Prompting;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Reasoning;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Schema;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.DMail;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Delegation;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.SlashCommands;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Subagents;
using Libr4.IDE.Application.AutonomousAppGeneration.Tooling.Flow;
using Libr4.IDE.Application.AutonomousAppGeneration.Infrastructure;
using Libr4.IDE.Application.AutonomousAppGeneration.Runtime;
using Libr4.IDE.Domain.AutonomousAppGeneration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Libr4.IntegrationTests.IDE;

public sealed class AgentRuntimeTests
{
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
    [Theory]
    [InlineData("""{"action":"tool","tool":"read_file","input":{"path":"backend/app.py"}}""", AgentTurnAction.Tool)]
    [InlineData("""{"action":"done","summary":"fixed imports"}""", AgentTurnAction.Done)]
    [InlineData("not json", AgentTurnAction.Invalid)]
    public void Parse_RecognizesAgentActions(string raw, AgentTurnAction expected)
    {
        var parsed = AgentResponseParser.Parse(raw);
        parsed.Action.Should().Be(expected);
    }

    [Fact]
    public void Parse_ExtractsToolCallFromFencedJson()
    {
        var raw = """
            ```json
            {"action":"tool","tool":"bash","input":{"command":"python -m pip install -r requirements.txt"}}
            ```
            """;

        var parsed = AgentResponseParser.Parse(raw);
        parsed.Action.Should().Be(AgentTurnAction.Tool);
        parsed.ToolCall.Should().NotBeNull();
        parsed.ToolCall!.Name.Should().Be("bash");
    }

    [Fact]
    public void BuildGenerationObjective_IncludesTargetPaths()
    {
        var registry = new Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Core.AgentToolRegistry(
            Array.Empty<Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Abstractions.IAgentTool>());

        var plan = new GenerationPlan(
            applicationName: "CalorieVision",
            applicationDescription: "Calorie calculator",
            techStack: new TechStack(
                new[] { "Python", "TypeScript" },
                new[] { "Django", "SolidJS" },
                Array.Empty<string>(),
                Array.Empty<string>(),
                "django+solidjs"),
            phases: Array.Empty<GenerationPhase>(),
            requiredAgents: Array.Empty<string>(),
            runtimeImage: "python:3.12-slim",
            buildCommands: new List<string> { "cd backend && python -m pip install -r requirements.txt" },
            testCommands: new List<string> { "cd backend && python -m pytest -q" },
            maxIterations: 20);

        var objective = Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Core.AgentPromptBuilder
            .BuildGenerationObjective(
                "Create Django settings module",
                plan,
                new[] { "backend/config/settings.py" },
                registry);

        objective.Should().Contain("backend/config/settings.py");
        objective.Should().Contain("write_file");
    }

    [Theory]
    [InlineData("pip: command not found", BuildErrorCategory.Environment)]
    [InlineData("ModuleNotFoundError: django", BuildErrorCategory.Dependency)]
    [InlineData("SyntaxError: invalid syntax", BuildErrorCategory.Compilation)]
    public void Classify_MapsBuildLogToCategory(string snippet, BuildErrorCategory expected)
    {
        var (category, _) = BuildErrorCategoryClassifier.Classify(snippet);
        category.Should().Be(expected);
    }

    [Fact]
    public void RequiresReadBeforeWrite_AllowsNewFileWriteWithoutRead()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "libr4-agent-policy-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);
        try
        {
            var context = new ToolContext
            {
                Workspace = new ShadowWorkspaceContext(Guid.NewGuid(), tempDir, string.Empty, new StubRuntimeSession()),
                Accessor = null!,
                WorkingFiles = new List<GeneratedFile>(),
                FileState = new FileStateCache(),
                Session = new AgentSessionState()
            };

            AgentGenerationPolicy.RequiresReadBeforeWrite(context, "write_file", "backend/manage.py")
                .Should().BeFalse();
            AgentGenerationPolicy.RequiresReadBeforeWrite(context, "edit_file", "backend/manage.py")
                .Should().BeTrue();
        }
        finally
        {
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public void TargetsSatisfied_RequiresAllTargetPaths()
    {
        var patches = new Dictionary<string, GeneratedFile>(StringComparer.OrdinalIgnoreCase)
        {
            ["backend/a.py"] = new GeneratedFile("backend/a.py", "python", new string('x', 40))
        };

        AgentGenerationPolicy.TargetsSatisfied(patches, new[] { "backend/a.py", "backend/b.py" })
            .Should().BeFalse();
        AgentGenerationPolicy.TargetsSatisfied(patches, new[] { "backend/a.py" })
            .Should().BeTrue();
    }

    [Fact]
    public async Task RepairPlaybook_ReturnsHintAfterSuccessfulAttempts()
    {
        var dbPath = Path.Combine(Path.GetTempPath(), $"repair-playbook-unit-{Guid.NewGuid():N}.db");
        try
        {
            var store = new Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Playbook.SqliteRepairPlaybookStore(
                Microsoft.Extensions.Options.Options.Create(
                    new Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Playbook.RepairPlaybookOptions
                    {
                        DbPath = dbPath
                    }),
                Microsoft.Extensions.Logging.Abstractions.NullLogger<
                    Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Playbook.SqliteRepairPlaybookStore>.Instance);
            var playbook = new RepairPlaybookService(store);
            const string sig = "pip not found in shadow workspace";
            await playbook.RecordOutcomeAsync(sig, "bash:python -m pip install", succeeded: true);
            await playbook.RecordOutcomeAsync(sig, "bash:python -m pip install", succeeded: true);
            (await playbook.TryGetHintAsync(sig)).Should().Be("bash:python -m pip install");
        }
        finally
        {
            try
            {
                if (File.Exists(dbPath))
                    File.Delete(dbPath);
            }
            catch
            {
                // SQLite may still hold a transient lock on Windows
            }
        }
    }

    [Fact]
    public void TryCoerceWriteFileFromRaw_AcceptsFencedPython()
    {
        var raw = """
            ```python
            import os
            import django
            from django.core.management import execute_from_command_line

            def main():
                os.environ.setdefault('DJANGO_SETTINGS_MODULE', 'calorievisionapp.settings')
                execute_from_command_line(sys.argv)
            ```
            """;

        var call = AgentGenerationPolicy.TryCoerceWriteFileFromRaw(
            raw,
            new[] { "backend/manage.py" });

        call.Should().NotBeNull();
        call!.Name.Should().Be("write_file");
        call.Input.GetProperty("path").GetString().Should().Be("backend/manage.py");
        call.Input.GetProperty("content").GetString().Should().Contain("execute_from_command_line");
    }

    [Fact]
    public void TryCoerceWriteFileFromRaw_RejectsInvalidJsonAndShortText()
    {
        AgentGenerationPolicy.TryCoerceWriteFileFromRaw("not code", new[] { "backend/a.py" })
            .Should().BeNull();
        AgentGenerationPolicy.TryCoerceWriteFileFromRaw(
            """{"action":"tool","tool":"read_file","input":{"path":"x"}}""",
            new[] { "backend/a.py" })
            .Should().BeNull();
        AgentGenerationPolicy.TryCoerceWriteFileFromRaw("import x", new[] { "backend/a.py", "backend/b.py" })
            .Should().BeNull();
    }

    [Fact]
    public void BuildInvalidJsonNudge_IncludesTargetPathExample()
    {
        var nudge = AgentGenerationPolicy.BuildInvalidJsonNudge("backend/manage.py");
        nudge.Should().Contain("backend/manage.py");
        nudge.Should().Contain("write_file");
    }

    [Fact]
    public void BoilerplateRegistry_InfersSettingsModuleForManagePy()
    {
        var working = new List<GeneratedFile>
        {
            new("backend/calorievisionapp/settings.py", "python", "SECRET_KEY='x'")
        };

        var content = BoilerplateRegistry.TryGetContent("backend/manage.py", working, null);
        content.Should().Contain("calorievisionapp.settings");
        content.Should().Contain("execute_from_command_line");
    }

    [Fact]
    public void ToolCallRecovery_AppliesBoilerplateAfterRepeatedInvalidJson()
    {
        var working = new List<GeneratedFile>
        {
            new("backend/calorievisionapp/settings.py", "python", "SECRET_KEY='x'")
        };

        var result = ToolCallRecovery.Recover(
            rawResponse: "{broken",
            consecutiveInvalidTurns: 5,
            targetPaths: new[] { "backend/manage.py" },
            workingFiles: working,
            plan: null,
            enableRawCoercion: true,
            enableBoilerplateFallback: true);

        result.HasToolCall.Should().BeTrue();
        result.Stage.Should().Be(ToolCallRecoveryStage.BoilerplateFallback);
        result.ToolCall!.Name.Should().Be("write_file");
    }

    [Theory]
    [InlineData(3, ToolCallRecoveryStage.CompressedPrompt)]
    [InlineData(4, ToolCallRecoveryStage.StrictSchema)]
    public void ToolCallRecovery_EscalatesProtocolNudges(int invalidCount, ToolCallRecoveryStage expected)
    {
        var result = ToolCallRecovery.Recover(
            "{nope",
            invalidCount,
            new[] { "backend/meals/views.py" },
            Array.Empty<GeneratedFile>(),
            null,
            enableRawCoercion: false,
            enableBoilerplateFallback: false);

        result.HasToolCall.Should().BeFalse();
        result.Stage.Should().Be(expected);
        result.SystemNudge.Should().Contain("backend/meals/views.py");
    }

    [Fact]
    public void FeatureGrouper_BatchesMealsApiFilesTogether()
    {
        Libr4.IDE.AutonomousAppGeneration.Agents.FeatureDependencyGrouper.TryResolveFeatureBucket("backend/meals/models.py")
            .Should().Be("feature:django-app:meals");
        Libr4.IDE.AutonomousAppGeneration.Agents.FeatureDependencyGrouper.TryResolveFeatureBucket("backend/meals/views.py")
            .Should().Be("feature:django-app:meals");
        Libr4.IDE.AutonomousAppGeneration.Agents.FeatureDependencyGrouper.TryResolveFeatureBucket("backend/meals/services/openai_vision.py")
            .Should().Be("feature:django-app:meals");
        Libr4.IDE.AutonomousAppGeneration.Agents.FeatureDependencyGrouper.ShouldStayOutsideFeatureBatch("backend/meals/apps.py").Should().BeTrue();
        Libr4.IDE.AutonomousAppGeneration.Agents.FeatureDependencyGrouper.ShouldStayOutsideFeatureBatch("backend/calorievisionapp/urls.py").Should().BeTrue();
    }

    [Fact]
    public void IncrementalGrouper_GroupsMealsFeatureWhenEnabled()
    {
        var entries = new[]
        {
            new Libr4.IDE.AutonomousAppGeneration.Agents.PlannedFileEntry("backend/meals/models.py", Libr4.IDE.AutonomousAppGeneration.Agents.AgentPhase.Backend, "m", "python-django"),
            new Libr4.IDE.AutonomousAppGeneration.Agents.PlannedFileEntry("backend/meals/serializers.py", Libr4.IDE.AutonomousAppGeneration.Agents.AgentPhase.Backend, "s", "python-django"),
            new Libr4.IDE.AutonomousAppGeneration.Agents.PlannedFileEntry("backend/meals/views.py", Libr4.IDE.AutonomousAppGeneration.Agents.AgentPhase.Backend, "v", "python-django"),
            new Libr4.IDE.AutonomousAppGeneration.Agents.PlannedFileEntry("backend/meals/urls.py", Libr4.IDE.AutonomousAppGeneration.Agents.AgentPhase.Backend, "u", "python-django"),
            new Libr4.IDE.AutonomousAppGeneration.Agents.PlannedFileEntry("backend/meals/apps.py", Libr4.IDE.AutonomousAppGeneration.Agents.AgentPhase.Backend, "a", "python-django"),
        };

        var batches = Libr4.IDE.AutonomousAppGeneration.Agents.IncrementalFileBatchGrouper.GroupEntries(
            entries,
            maxFilesPerBatch: 1,
            phase: null,
            useFeatureScopedBatches: true);
        batches.Should().HaveCount(2);
        batches[0].Should().HaveCount(4);
        batches[1].Should().ContainSingle(e => e.Path.EndsWith("apps.py", StringComparison.OrdinalIgnoreCase));
    }

    [Theory]
    [InlineData("Traceback (most recent call last)", BuildErrorCategory.Runtime)]
    [InlineData("SyntaxError: invalid syntax", BuildErrorCategory.Compilation)]
    public void Classify_SeparatesRuntimeFromCompile(string snippet, BuildErrorCategory expected)
    {
        var (category, _) = BuildErrorCategoryClassifier.Classify(snippet);
        category.Should().Be(expected);
    }

    [Fact]
    public void SchemaValidation_RejectsMissingPathOnWriteFile()
    {
        using var doc = System.Text.Json.JsonDocument.Parse("""{"content":"x"}""");
        var result = ToolInputValidator.ValidateBeforeExecute("write_file", doc.RootElement);
        result.IsValid.Should().BeFalse();
        result.Error.Should().Contain("path");
    }

    [Fact]
    public void ToolCallRecovery_SchemaCoerceRecoversManagePyIntent()
    {
        var raw = """
            backend/manage.py
            ```python
            import os
            import django
            from django.core.management import execute_from_command_line

            def main():
                os.environ.setdefault('DJANGO_SETTINGS_MODULE', 'calorievisionapp.settings')
                execute_from_command_line([])
            ```
            """;
        var result = ToolCallRecovery.Recover(
            raw,
            consecutiveInvalidTurns: 4,
            targetPaths: new[] { "backend/manage.py" },
            workingFiles: new List<GeneratedFile>
            {
                new("backend/calorievisionapp/settings.py", "python", "SECRET_KEY='x'")
            },
            plan: null,
            enableRawCoercion: false,
            enableBoilerplateFallback: false);

        result.HasToolCall.Should().BeTrue();
        result.Stage.Should().Be(ToolCallRecoveryStage.SchemaCoerce);
    }

    [Fact]
    public void PathValidator_BlocksTraversalAndEtcPasswd()
    {
        var options = Options.Create(new AgentRuntimeOptions
        {
            DeniedPathPatterns = new[] { "/etc/*" }
        });
        var validator = new WorkspacePathValidator(options, new InMemoryPathAccessAudit());
        var root = Path.Combine(Path.GetTempPath(), "libr4-path-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            var ctx = new ToolContextPaths(root, Guid.NewGuid());
            validator.Validate("../secret.txt", ctx).Allowed.Should().BeFalse();
            validator.Validate("/etc/passwd", ctx).Allowed.Should().BeFalse();
            validator.Validate("backend/app.py", ctx).Allowed.Should().BeTrue();
        }
        finally
        {
            if (Directory.Exists(root))
                Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void ExecPolicy_ForbidsDestructiveBash()
    {
        var engine = new YamlExecPolicyEngine(
            Options.Create(new AgentRuntimeOptions { ExecPolicyPath = "missing.yaml" }),
            NullLogger<YamlExecPolicyEngine>.Instance);
        engine.EvaluateBash("rm -rf /").Decision.Should().Be(ExecPolicyDecision.Forbid);
        engine.EvaluateBash("python manage.py migrate").Decision.Should().Be(ExecPolicyDecision.Allow);
    }

    [Fact]
    public void PatchApplicator_AppliesUnifiedDiff()
    {
        var original = "line1\nline2\nline3\n";
        var patch = """
            @@ -1,3 +1,3 @@
             line1
            -line2
            +line2-fixed
             line3
            """;
        var diff = UnifiedDiffParser.Parse(patch, "app.py");
        var result = PatchApplicator.ApplyExact(original, diff);
        result.Success.Should().BeTrue();
        result.PatchedContent.Should().Contain("line2-fixed");
    }

    [Fact]
    public void SurgicalPatchEngine_AppliesJavaImportFix()
    {
        var files = new List<GeneratedFile>
        {
            new("backend/src/Main.java", "java", "public class Main {\n  Foo f;\n}\n")
        };
        var edits = new List<SurgicalPatchEngine.SurgicalEdit>
        {
            new("backend/src/Main.java", "public class Main {", "import Foo;\npublic class Main {")
        };
        var result = SurgicalPatchEngine.Apply(files, edits);
        result.AppliedEdits.Should().Be(1);
        result.Patches[0].Content.Should().Contain("import Foo;");
    }

    [Fact]
    public void ReasoningParser_SplitsThinkingFromJson()
    {
        var raw = """
            <thinking>inspect manage.py first</thinking>
            {"action":"tool","tool":"read_file","input":{"path":"backend/manage.py"}}
            """;
        var split = ReasoningChannelParser.Split(raw);
        split.ReasoningContent.Should().Contain("inspect manage.py");
        split.VisibleContent.Should().Contain("read_file");
    }

    [Fact]
    public async Task SessionStore_ResumeAfterFiveTurns()
    {
        var dbPath = Path.Combine(Path.GetTempPath(), "libr4-sess-" + Guid.NewGuid().ToString("N") + ".db");
        var options = Options.Create(new AgentRuntimeOptions { SessionDbPath = dbPath });
        var store = new SqliteAgentSessionStore(options, NullLogger<SqliteAgentSessionStore>.Instance);
        await store.EnsureSchemaAsync();

        var runId = Guid.NewGuid();
        var sessionId = Guid.NewGuid().ToString("D");
        await store.CreateSessionAsync(new AgentSessionRecord(
            sessionId, runId, null, null, "running", DateTime.UtcNow, DateTime.UtcNow, 0, 0, "BypassPermissions", 0));

        for (var step = 1; step <= 5; step++)
        {
            await store.SaveTurnAsync(
                sessionId,
                step,
                new AgentConversationTurn("assistant", $"turn-{step}", DateTime.UtcNow),
                null);
        }

        var bundle = await store.LoadResumeBundleAsync(sessionId);
        bundle.Should().NotBeNull();
        bundle!.Turns.Should().HaveCount(5);
        bundle.NextStepNumber.Should().Be(6);
    }

    [Fact]
    public void FileStateCacheRestorer_MarksReadPathsFromPersistedToolCalls()
    {
        var cache = new FileStateCache();
        var calls = new[]
        {
            new AgentToolCallRecord(1, "s1", "read_file", """{"path":"backend/app.py"}""", "print('x')", true, 10, DateTime.UtcNow),
            new AgentToolCallRecord(2, "s1", "bash", """{"command":"ls"}""", "ok", true, 5, DateTime.UtcNow),
        };

        FileStateCacheRestorer.RestoreFromToolCalls(cache, calls);
        cache.HasRead("backend/app.py").Should().BeTrue();
        cache.HasRead("backend/other.py").Should().BeFalse();
    }

    [Fact]
    public async Task NdjsonEventWriter_PublishesThreeTurnSequence()
    {
        var runId = Guid.NewGuid();
        var root = Path.Combine(Path.GetTempPath(), "libr4-events-" + Guid.NewGuid().ToString("N"));
        var published = new List<string>();
        var hub = new AgentRuntimeEventHub();
        hub.EventPublished += evt =>
        {
            published.Add(evt.EventType);
            return Task.CompletedTask;
        };

        var writer = new NdjsonEventWriter(
            Options.Create(new AgentRuntimeOptions { RunsRoot = root, EnableNdjsonEvents = true }),
            hub);

        await writer.WriteAsync(runId, new { type = "step_start", stepNumber = 1 });
        await writer.WriteAsync(runId, new { type = "tool_use", stepNumber = 1, toolName = "read_file" });
        await writer.WriteAsync(runId, new { type = "step_finish", stepNumber = 1, finishReason = "tool_calls" });

        published.Should().Equal("step_start", "tool_use", "step_finish");
        var path = Path.Combine(root, runId.ToString("D"), "events.jsonl");
        File.Exists(path).Should().BeTrue();
        File.ReadAllLines(path).Should().HaveCount(3);
    }

    [Fact]
    public void BuiltinPromptVars_InjectStageAndRunId()
    {
        var resolver = new BuiltinPromptVarResolver();
        var context = new BuiltinPromptVarContext
        {
            RunId = Guid.Parse("11111111-1111-1111-1111-111111111111"),
            Stage = BuiltinPromptStage.Repairing,
            RepairAttempt = 3,
            BuildLog = "line1\nline2\nfail",
            LastErrors = ["backend/app.py: ImportError"],
            ManifestFiles = ["backend/app.py"]
        };

        var prompt = PromptVariableSubstitutor.Apply(
            "run={{LIBR4_RUN_ID}} stage={{LIBR4_STAGE}} attempt={{LIBR4_REPAIR_ATTEMPT}} errors={{LIBR4_ERRORS}}",
            resolver,
            context);

        prompt.Should().Contain("11111111-1111-1111-1111-111111111111");
        prompt.Should().Contain("repairing");
        prompt.Should().Contain("3");
        prompt.Should().Contain("ImportError");
    }

    [Fact]
    public void AgentSpecLoader_LoadsVerifyWithRestrictedToolset()
    {
        var dir = Path.Combine(AppContext.BaseDirectory, "Agents", "Subagents");
        var docs = AgentSpecLoader.LoadDirectory(dir);
        docs.Should().NotBeEmpty();
        var verify = docs.First(d => d.Name.Equals("verify", StringComparison.OrdinalIgnoreCase));
        verify.Toolset.Should().NotContain("write_file");
        verify.Toolset.Should().NotContain("edit_file");
        verify.Toolset.Should().Contain("run_tests");
    }

    [Fact]
    public void FilteredRegistry_DeniesDisallowedTools()
    {
        var tools = new Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Abstractions.IAgentTool[]
        {
            new StubTool("read_file", true),
            new StubTool("write_file", false)
        };
        var registry = new AgentToolRegistry(tools);
        var filtered = new FilteredAgentToolRegistry(registry, new[] { "read_file", "grep" });
        filtered.TryGet("write_file").Should().BeNull();
        filtered.TryGet("read_file").Should().NotBeNull();
        filtered.BuildToolCatalog().Should().NotContain("write_file");
    }

    [Fact]
    public async Task SubagentStore_PersistsCreateAndComplete()
    {
        var root = Path.Combine(Path.GetTempPath(), "libr4-sub-" + Guid.NewGuid().ToString("N"));
        var store = new FileSubagentStore(Microsoft.Extensions.Options.Options.Create(
            new AgentRuntimeOptions { RunsRoot = root }));
        var runId = Guid.NewGuid();
        var created = await store.CreateAsync(runId, "verify", "check tests", new AgentSpec
        {
            Name = "verify",
            Toolset = new[] { "run_tests" }
        });
        await store.CompleteAsync(runId, created.Id, "all green");
        var list = await store.ListAsync(runId);
        list.Should().HaveCount(1);
        list[0].Status.Should().Be("completed");
        list[0].OutputPreview.Should().Contain("all green");
    }

    [Fact]
    public void MultiAgentManifest_ParsesSubagentDirective()
    {
        var name = Libr4.IDE.AutonomousAppGeneration.Agents.MultiAgentIncrementalManifest
            .TryParseSubagentSpecName("@subagent verify run smoke tests");
        name.Should().Be("verify");
    }

    private sealed class StubTool : Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Abstractions.IAgentTool
    {
        public StubTool(string name, bool readOnly)
        {
            Name = name;
            IsReadOnly = readOnly;
        }

        public string Name { get; }
        public string Description => Name;
        public bool IsReadOnly { get; }
        public bool IsConcurrencySafe(System.Text.Json.JsonElement input) => true;
        public Task<Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Models.ToolExecutionResult> ExecuteAsync(
            System.Text.Json.JsonElement input,
            Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Abstractions.ToolContext context,
            CancellationToken ct) =>
            Task.FromResult(new Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Models.ToolExecutionResult(
                Name, true, "ok", Array.Empty<GeneratedFile>()));
    }

    [Fact]
    public void SlashCommandParser_ExtractsFlowName()
    {
        SlashCommandParser.TryParseFlow("/flow:calorie-django-solidjs build calorie app")
            .Should().Be("calorie-django-solidjs");
        SlashCommandParser.ParseCommands("/flow:banking-java-react /verify")
            .Should().Contain(new[] { "flow", "verify" });
    }

    [Fact]
    public void FlowYamlLoader_LoadsCalorieFlow()
    {
        var dir = Path.Combine(AppContext.BaseDirectory, "Flows");
        var docs = FlowYamlLoader.LoadDirectory(dir);
        docs.Should().NotBeEmpty();
        var calorie = FlowYamlLoader.ToDefinition(docs.First(d => d.Name == "calorie-django-solidjs"));
        calorie.Nodes.Should().Contain(n => n.Id == "backend-gate" && n.Type == FlowNodeType.Gate);
        calorie.Edges.Should().Contain(e => e.Action == FlowFailureAction.Retry);
    }

    [Fact]
    public async Task FlowEngine_AdvancesPlanningToGeneration()
    {
        var root = Path.Combine(Path.GetTempPath(), "libr4-flow-" + Guid.NewGuid().ToString("N"));
        var options = Microsoft.Extensions.Options.Options.Create(new FlowEngineOptions
        {
            FlowsDirectory = Path.Combine(AppContext.BaseDirectory, "Flows"),
            RunsRoot = root
        });
        var registry = new FlowRegistry(options, NullLogger<FlowRegistry>.Instance);
        var store = new FileFlowProgressStore(options);
        var engine = new YamlFlowEngine(registry, store, options);
        var runId = Guid.NewGuid();
        engine.TryResolveFlowName("/flow:calorie-django-solidjs app", out var flowName).Should().BeTrue();
        await engine.InitializeAsync(runId, flowName);
        var advance = await engine.OnPhaseCompletedAsync(
            runId,
            "planning",
            true,
            new FlowRuntimeContext { WorkspaceFiles = Array.Empty<string>() });
        advance.NextNodeId.Should().Be("generate-backend");
    }

    [Fact]
    public void HumanReadableIdGenerator_ProducesTripleToken()
    {
        var id = HumanReadableIdGenerator.Create();
        id.Split('-').Should().HaveCount(3);
    }

    [Fact]
    public async Task DMailBus_TwoSubagentsExchangeContext()
    {
        var root = Path.Combine(Path.GetTempPath(), "libr4-dmail-" + Guid.NewGuid().ToString("N"));
        var bus = new FileDMailBus(Microsoft.Extensions.Options.Options.Create(new DMailOptions { RunsRoot = root }));
        var runId = Guid.NewGuid();

        var sent = await bus.SendAsync(runId, "backend", "frontend", "api base: /api/v1", ackRequired: true);
        var inbox = await bus.ReadAsync(runId, to: "frontend", from: "backend");
        inbox.Should().HaveCount(1);
        inbox[0].Payload.Should().Contain("/api/v1");

        (await bus.AckAsync(runId, sent.Id)).Should().BeTrue();
        var pending = await bus.ReadAsync(runId, to: "frontend", unackedOnly: true);
        pending.Should().BeEmpty();
    }

    [Fact]
    public async Task FeatureBatchHandoff_SendsBackendManifest()
    {
        var root = Path.Combine(Path.GetTempPath(), "libr4-handoff-" + Guid.NewGuid().ToString("N"));
        var bus = new FileDMailBus(Microsoft.Extensions.Options.Options.Create(new DMailOptions { RunsRoot = root }));
        var coordinator = new FeatureBatchHandoffCoordinator(bus);
        var runId = Guid.NewGuid();

        await coordinator.SendBackendToFrontendAsync(runId, new[]
        {
            new GeneratedFile("backend/manage.py", "python", "print('x')"),
            new GeneratedFile("backend/api/views.py", "python", "pass")
        });

        var prefix = await coordinator.BuildFrontendHandoffPrefixAsync(runId);
        prefix.Should().Contain("backend/manage.py");
        prefix.Should().Contain("DMail handoff");
    }

    [Fact]
    public void Compact_TruncatesLargeToolResults()
    {
        var compactor = new ToolResultBudgetCompactor(
            Microsoft.Extensions.Options.Options.Create(new Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.AgentRuntimeOptions
            {
                ConversationCharBudget = 200,
                MaxToolResultChars = 40
            }));

        var turns = new List<AgentConversationTurn>
        {
            new("user", "objective", DateTime.UtcNow),
            new("tool", new string('x', 500), DateTime.UtcNow)
        };

        var compacted = compactor.CompactAsync(turns, 200).GetAwaiter().GetResult();
        compacted.Should().NotBeEmpty();
        compacted.Last().Content.Should().Contain("truncated");
    }
}
