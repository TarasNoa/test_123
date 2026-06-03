using Libr4.IDE.AutonomousAppGeneration.Agents;
using Xunit;

namespace Libr4.IntegrationTests.IDE;

public sealed class MultiAgentArtifactCollectorTests
{
    [Fact]
    public void CollectFiles_MergesNestedSubtaskJson()
    {
        var phase = new OrchestrationResult
        {
            Results = new List<TaskResult>
            {
                new()
                {
                    TaskId = "parent",
                    IsSuccess = true,
                    Result = new AgentResult { IsSuccess = true, Content = "ignored" },
                    NestedResults = new List<TaskResult>
                    {
                        new()
                        {
                            TaskId = "child",
                            IsSuccess = true,
                            Result = new AgentResult
                            {
                                IsSuccess = true,
                                Content = """{"files":[{"relativePath":"backend/pom.xml","content":"<project/>"}]}"""
                            }
                        }
                    }
                }
            }
        };

        var files = MultiAgentArtifactCollector.CollectFiles(phase);

        Assert.Single(files);
        Assert.Equal("backend/pom.xml", files[0].RelativePath);
    }
}
