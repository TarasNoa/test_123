using Libr4.IDE.AutonomousAppGeneration.Agents;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Libr4.IntegrationTests.IDE;

public sealed class SubagentOrchestratorNestedTests
{
    private sealed class PassThroughImplementerAgent : IAgent
    {
        public Task<AgentResult> ExecuteAsync(AgentContext context)
        {
            var result = new AgentResult
            {
                IsSuccess = true,
                Content = $"implemented:{context.Task?.Id ?? "unknown"}"
            };
            return Task.FromResult(result);
        }
    }

    private sealed class ApprovingReviewerAgent : IAgent
    {
        public Task<AgentResult> ExecuteAsync(AgentContext context)
            => Task.FromResult(new AgentResult { IsSuccess = true, Content = "approved" });
    }

    private sealed class RejectingReviewerAgent : IAgent
    {
        public Task<AgentResult> ExecuteAsync(AgentContext context)
            => Task.FromResult(new AgentResult { IsSuccess = true, Content = "NEEDS_FIX: rejected" });
    }

    private sealed class JsonImplementerAgent : IAgent
    {
        public Task<AgentResult> ExecuteAsync(AgentContext context) =>
            Task.FromResult(new AgentResult
            {
                IsSuccess = true,
                Content = """{"files":[{"relativePath":"backend/App.java","content":"class App{}"}]}"""
            });
    }

    [Fact]
    public async Task FastPath_AcceptsParseableJson_WithoutSpecReviewer()
    {
        var implementer = new JsonImplementerAgent();
        var orchestrator = new SubagentOrchestrator(
            implementer,
            new RejectingReviewerAgent(),
            new RejectingReviewerAgent(),
            NullLogger.Instance,
            options: new AgentOrchestrationOptions
            {
                SkipLlmReviewWhenParseableFiles = true,
                MaxLlmReviewRounds = 0
            });

        var result = await orchestrator.ExecuteParallelAsync(new List<AgentTask>
        {
            new()
            {
                Id = "json-task",
                Description = "emit files",
                Context = new AgentContext()
            }
        });

        Assert.Equal(1, result.SuccessCount);
        Assert.True(result.Results[0].IsSuccess);
    }

    [Fact]
    public async Task ExecuteParallelAsync_ShouldExecuteNestedSubtasks_WhenProvidedInTask()
    {
        var orchestrator = new SubagentOrchestrator(
            implementerAgent: new PassThroughImplementerAgent(),
            specReviewerAgent: new ApprovingReviewerAgent(),
            codeQualityReviewerAgent: new ApprovingReviewerAgent(),
            logger: NullLogger.Instance,
            maxConcurrency: 2,
            maxSubtaskDepth: 2,
            options: new AgentOrchestrationOptions { MaxLlmReviewRounds = 2 });

        var task = new AgentTask
        {
            Id = "parent-1",
            Description = "Parent task",
            Context = new AgentContext(),
            Subtasks = new List<AgentTask>
            {
                new()
                {
                    Id = "child-1",
                    Description = "Child task",
                    Context = new AgentContext()
                }
            }
        };
        task.Context.Task = task;
        task.Subtasks[0].Context.Task = task.Subtasks[0];

        var result = await orchestrator.ExecuteParallelAsync(new List<AgentTask> { task });

        Assert.Single(result.Results);
        var parent = result.Results[0];
        Assert.True(parent.IsSuccess);
        Assert.Single(parent.NestedResults);
        Assert.Equal("child-1", parent.NestedResults[0].TaskId);
        Assert.Equal("parent-1", parent.NestedResults[0].ParentTaskId);
    }
}
