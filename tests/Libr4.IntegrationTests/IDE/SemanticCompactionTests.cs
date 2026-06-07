using FluentAssertions;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Models;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Rollout;
using Libr4.IDE.Application.AutonomousAppGeneration.Context.Compaction;
using Microsoft.Extensions.Options;
using Xunit;

namespace Libr4.IntegrationTests.IDE;

public sealed class SemanticCompactionTests
{
    private static SemanticContextCompactor CreateCompactor(InMemoryCompactionRollout? rollout = null) =>
        new(
            new HeuristicSemanticCompactor(),
            Options.Create(new SemanticCompactionOptions
            {
                EnableSemanticCompaction = true,
                TriggerBudgetRatio = 0.80,
                PreserveLastToolResults = 3,
                MinTurnsBeforeCompaction = 8
            }),
            Options.Create(new AgentRuntimeOptions
            {
                ConversationCharBudget = 4_000,
                MaxToolResultChars = 500
            }),
            rollout: rollout);

    [Fact]
    public async Task CompactAsync_50TurnSession_ProducesSummaryAndPreservesTailTools()
    {
        var rollout = new InMemoryCompactionRollout();
        var compactor = CreateCompactor(rollout);
        var manifest = new[] { "backend/views.py", "backend/models.py" };
        var turns = BuildFiftyTurnSession(manifest);

        var beforeChars = turns.Sum(t => t.Content.Length);
        beforeChars.Should().BeGreaterThan(3_200);

        var compacted = await compactor.CompactAsync(
            turns,
            4_000,
            new CompactionRequest(Guid.NewGuid(), "session-1", manifest));

        compacted.Count.Should().BeLessThan(turns.Count);
        compacted.Sum(t => t.Content.Length).Should().BeLessOrEqualTo(4_000);

        var joined = string.Join('\n', compacted.Select(t => t.Content));
        joined.Should().Contain("SEMANTIC COMPACTION SUMMARY");
        joined.Should().Contain("decisions:");
        joined.Should().Contain("files_touched:");
        joined.Should().Contain("backend/views.py");
        joined.Should().Contain("tool_turn_49");
        joined.Should().Contain("tool_turn_48");
        joined.Should().Contain("tool_turn_47");

        rollout.Compactions.Should().HaveCount(1);
        rollout.Compactions[0].BeforeTurns.Should().Be(turns.Count);
        rollout.Compactions[0].AfterChars.Should().BeLessThan(rollout.Compactions[0].BeforeChars);
    }

    [Fact]
    public async Task HeuristicSummarizer_ExtractsSchemaFields()
    {
        var compactor = new HeuristicSemanticCompactor();
        var turns = new[]
        {
            new AgentConversationTurn("assistant", """{"action":"tool","tool":"write_file","input":{"path":"backend/views.py"}}""", DateTime.UtcNow),
            new AgentConversationTurn("tool", "[write_file] success=true applied backend/views.py", DateTime.UtcNow),
            new AgentConversationTurn("system", "ModuleNotFoundError: django failed", DateTime.UtcNow)
        };

        var summary = await compactor.SummarizeAsync(turns, new[] { "backend/views.py" });

        summary.FilesTouched.Should().Contain("backend/views.py");
        summary.OpenIssues.Should().NotBeEmpty();
        summary.NextActions.Should().NotBeEmpty();
        summary.Decisions.Should().Contain(d => d.Contains("manifest_paths_preserved"));
    }

    [Fact]
    public async Task CompactAsync_UnderBudget_ReturnsWithoutSummary()
    {
        var compactor = CreateCompactor();
        var turns = new List<AgentConversationTurn>
        {
            new("user", "fix build", DateTime.UtcNow),
            new("assistant", """{"action":"tool","tool":"read_file"}""", DateTime.UtcNow),
            new("tool", "small output", DateTime.UtcNow)
        };

        var compacted = await compactor.CompactAsync(turns, 10_000);
        compacted.Should().HaveCount(3);
        compacted.Should().NotContain(t => t.Content.Contains("SEMANTIC COMPACTION SUMMARY"));
    }

    private static List<AgentConversationTurn> BuildFiftyTurnSession(IReadOnlyList<string> manifest)
    {
        var turns = new List<AgentConversationTurn>
        {
            new("user", $"OBJECTIVE fix {string.Join(", ", manifest)}", DateTime.UtcNow)
        };

        for (var i = 1; i < 46; i++)
        {
            if (i % 2 == 0)
            {
                turns.Add(new AgentConversationTurn(
                    "tool",
                    $"[read_file] tool_turn_{i} path=backend/views.py content={new string('a', 180)}",
                    DateTime.UtcNow));
            }
            else
            {
                turns.Add(new AgentConversationTurn(
                    "assistant",
                    "{\"action\":\"tool\",\"tool\":\"read_file\",\"input\":{\"path\":\"backend/views.py\",\"turn\":" + i + "}}",
                    DateTime.UtcNow));
            }
        }

        turns.Add(new AgentConversationTurn("tool", "[grep] tool_turn_47 match error failed", DateTime.UtcNow));
        turns.Add(new AgentConversationTurn("assistant", """{"action":"tool","tool":"edit_file"}""", DateTime.UtcNow));
        turns.Add(new AgentConversationTurn("tool", "[edit_file] tool_turn_48 success=true", DateTime.UtcNow));
        turns.Add(new AgentConversationTurn("assistant", """{"action":"tool","tool":"run_build"}""", DateTime.UtcNow));
        turns.Add(new AgentConversationTurn("tool", "[run_build] tool_turn_49 build ok", DateTime.UtcNow));

        return turns;
    }

    private sealed class InMemoryCompactionRollout : IRolloutRecorder, IRolloutReplayService
    {
        public List<(int BeforeChars, int AfterChars, int BeforeTurns, int AfterTurns, string SummaryJson)> Compactions { get; } = new();

        public Task RecordCompactionAsync(
            Guid runId, string sessionId, int beforeChars, int afterChars, int beforeTurns, int afterTurns, string summaryJson, CancellationToken ct = default)
        {
            Compactions.Add((beforeChars, afterChars, beforeTurns, afterTurns, summaryJson));
            return Task.CompletedTask;
        }

        public Task RecordStepStartAsync(Guid runId, string sessionId, int stepNumber, CancellationToken ct = default) => Task.CompletedTask;
        public Task RecordTextAsync(Guid runId, string sessionId, int stepNumber, string text, CancellationToken ct = default) => Task.CompletedTask;
        public Task RecordToolUseAsync(Guid runId, string sessionId, int stepNumber, string toolName, string inputJson, string outputJson, bool success, long durationMs, IReadOnlyList<RolloutMediaAttachment>? media = null, CancellationToken ct = default) => Task.CompletedTask;
        public Task RecordStepFinishAsync(Guid runId, string sessionId, int stepNumber, string finishReason, RolloutUsage? usage = null, CancellationToken ct = default) => Task.CompletedTask;
        public Task RecordErrorAsync(Guid runId, string sessionId, string message, CancellationToken ct = default) => Task.CompletedTask;
        public Task RecordPermissionDecisionAsync(Guid runId, string toolName, string decision, string? reason, CancellationToken ct = default) => Task.CompletedTask;
        public Task RecordSkillActivationAsync(Guid runId, string sessionId, string skillName, bool firstActivation, bool consentGranted, int contentChars, CancellationToken ct = default) => Task.CompletedTask;
        public Task RecordMemoryOperationAsync(Guid runId, string sessionId, string operation, string scope, string? key, string? kind, int resultCount, CancellationToken ct = default) => Task.CompletedTask;
        public Task<IReadOnlyList<RolloutEntry>> GetRolloutAsync(Guid runId, CancellationToken ct = default) => Task.FromResult<IReadOnlyList<RolloutEntry>>(Array.Empty<RolloutEntry>());
        public Task<IReadOnlyList<RolloutEntry>> ReplayAsync(Guid runId, CancellationToken ct = default) => GetRolloutAsync(runId, ct);
        public Task<IReadOnlyList<RolloutSearchHit>> SearchAsync(string query, int limit = 25, CancellationToken ct = default) => Task.FromResult<IReadOnlyList<RolloutSearchHit>>(Array.Empty<RolloutSearchHit>());
    }
}
