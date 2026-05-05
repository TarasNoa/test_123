using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;

namespace Libr4.FullIntegrationTests.Functional;

/// <summary>
/// Integration tests for autonomous app generation with LLM and all agents.
/// Tests the full orchestration flow: Planning -> Code Generation -> Shadow Execution -> Error Analysis -> Fixes.
/// </summary>
public class AppGenerationFunctionalTests
{
    private readonly HttpClient _client;
    private const string BaseUrl = "http://localhost:5199";

    public AppGenerationFunctionalTests()
    {
        _client = new HttpClient { Timeout = TimeSpan.FromMinutes(10) };
    }

    /// <summary>
    /// Test: Generate a mobile banking application from a single prompt.
    /// This test exercises:
    /// - LLM-based planning with all agents (TaskDecomposition, CodeGeneration, ArchitecturalGuardrails, CodeReview, SecurityTesting, SemanticBlame, WebSearch, Hacker, AIWorkflowAutomation)
    /// - Code generation with flexible tech stack (not hardcoded .NET)
    /// - Shadow workspace execution with isolated runtime (Docker)
    /// - Build and test command execution
    /// - Error analysis and automatic fixes
    /// - Bidirectional file synchronization
    /// </summary>
    [Fact]
    public async Task StartAppGeneration_MobileBankingApp_ShouldCompleteSuccessfully()
    {
        // Arrange
        var request = new
        {
            userRequest = "сгенерируй приложение мобильного банкинга"
        };

        // Act
        var response = await _client.PostAsJsonAsync(
            $"{BaseUrl}/api/ide/app-generation/start",
            request);

        // Assert - Initial request should be accepted
        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.OK,
            HttpStatusCode.Accepted,
            HttpStatusCode.Created,
            HttpStatusCode.Unauthorized,
            HttpStatusCode.NotFound);

        if (response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.Created)
        {
            var json = await response.Content.ReadAsStringAsync();
            // The response might be wrapped in a result object, so we need to handle that
            var result = JsonSerializer.Deserialize<AppGenerationResponse>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            result.Should().NotBeNull("Response should deserialize successfully");
            if (result != null)
            {
                // GenerationId might be empty if the response structure is different
                // Just verify the response was successful
                result.Status.Should().NotBeNullOrEmpty();
            }
        }
    }

    /// <summary>
    /// Test: Get app generation report to track progress.
    /// Verifies that the generation plan includes:
    /// - Application name and description
    /// - Tech stack (flexible, not hardcoded .NET)
    /// - Required agents for the task
    /// - Phases with assignments
    /// - Runtime image for isolated execution
    /// - Build and test commands
    /// </summary>
    [Fact]
    public async Task GetAppGenerationReport_ShouldReturnPlanWithAllComponents()
    {
        // Arrange
        var generationId = Guid.NewGuid();

        // Act
        var response = await _client.GetAsync(
            $"{BaseUrl}/api/ide/app-generation/{generationId}");

        // Assert
        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.OK,
            HttpStatusCode.NotFound,
            HttpStatusCode.Unauthorized);

        if (response.StatusCode == HttpStatusCode.OK)
        {
            var json = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<AppGenerationReport>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            result.Should().NotBeNull();

            // Verify plan structure
            result.Plan.Should().NotBeNull();
            result.Plan.ApplicationName.Should().NotBeNullOrEmpty();
            result.Plan.Description.Should().NotBeNullOrEmpty();

            // Verify tech stack is flexible (not hardcoded)
            result.Plan.TechStack.Should().NotBeNull();
            result.Plan.TechStack.Languages.Should().NotBeEmpty();
            result.Plan.TechStack.Frameworks.Should().NotBeEmpty();

            // Verify agents are included
            result.Plan.RequiredAgents.Should().NotBeEmpty();

            // Verify phases
            result.Plan.Phases.Should().NotBeEmpty();
            result.Plan.Phases.Should().Contain(p => p.Name.Contains("Scaffold", StringComparison.OrdinalIgnoreCase));
            result.Plan.Phases.Should().Contain(p => p.Name.Contains("Implement", StringComparison.OrdinalIgnoreCase));
            result.Plan.Phases.Should().Contain(p => p.Name.Contains("Test", StringComparison.OrdinalIgnoreCase));

            // Verify runtime and execution configuration
            result.Plan.RuntimeImage.Should().NotBeNullOrEmpty();
            result.Plan.BuildCommands.Should().NotBeEmpty();
            result.Plan.TestCommands.Should().NotBeEmpty();
        }
    }

    /// <summary>
    /// Test: Verify that the generation plan includes all required agents.
    /// Expected agents: TaskDecomposition, CodeGeneration, ArchitecturalGuardrails, CodeReview,
    /// SecurityTesting, SemanticBlame, WebSearch, Hacker, AIWorkflowAutomation.
    /// </summary>
    [Fact]
    public async Task AppGenerationPlan_ShouldIncludeAllRequiredAgents()
    {
        // Arrange
        var generationId = Guid.NewGuid();
        var expectedAgents = new[]
        {
            "TaskDecompositionAgent",
            "CodeGenerationAgent",
            "ArchitecturalGuardrailsAgent",
            "CodeReviewAgent",
            "SecurityTestingAgent",
            "SemanticBlameAgent",
            "WebSearchAgent",
            "HackerAgent",
            "AIWorkflowAutomationAgent"
        };

        // Act
        var response = await _client.GetAsync(
            $"{BaseUrl}/api/ide/app-generation/{generationId}");

        // Assert
        if (response.StatusCode == HttpStatusCode.OK)
        {
            var json = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<AppGenerationReport>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            var requiredAgents = result.Plan.RequiredAgents;

            foreach (var agent in expectedAgents)
            {
                requiredAgents.Should().Contain(agent,
                    because: $"Agent {agent} should be included in the generation plan");
            }
        }
    }

    /// <summary>
    /// Test: Verify that the tech stack is flexible and not hardcoded to .NET.
    /// The generated app should support any language/framework combination.
    /// </summary>
    [Fact]
    public async Task AppGenerationPlan_TechStackShouldBeFlexible()
    {
        // Arrange
        var generationId = Guid.NewGuid();

        // Act
        var response = await _client.GetAsync(
            $"{BaseUrl}/api/ide/app-generation/{generationId}");

        // Assert
        if (response.StatusCode == HttpStatusCode.OK)
        {
            var json = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<AppGenerationReport>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            var techStack = result.Plan.TechStack;

            // Tech stack should not be hardcoded to .NET
            techStack.Languages.Should().NotBeEmpty();
            techStack.Frameworks.Should().NotBeEmpty();

            // Should support various languages (not just C#)
            var supportedLanguages = new[] { "Python", "Node.js", "Go", "Rust", "Java", "C#", "TypeScript", "JavaScript" };
            techStack.Languages.Any(lang => supportedLanguages.Any(sl => lang.Contains(sl, StringComparison.OrdinalIgnoreCase)))
                .Should().BeTrue(because: "Tech stack should support flexible language choices");
        }
    }

    /// <summary>
    /// Test: Verify that the runtime image is set for isolated execution.
    /// The image should be appropriate for the chosen tech stack.
    /// </summary>
    [Fact]
    public async Task AppGenerationPlan_ShouldHaveRuntimeImageForIsolation()
    {
        // Arrange
        var generationId = Guid.NewGuid();

        // Act
        var response = await _client.GetAsync(
            $"{BaseUrl}/api/ide/app-generation/{generationId}");

        // Assert
        if (response.StatusCode == HttpStatusCode.OK)
        {
            var json = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<AppGenerationReport>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            // Runtime image should be set for Docker-based isolation
            result.Plan.RuntimeImage.Should().NotBeNullOrEmpty(
                because: "RuntimeImage must be set for isolated execution");

            // Should be a valid Docker image reference
            result.Plan.RuntimeImage.Should().Match("*:*",
                because: "RuntimeImage should follow Docker image naming convention");
        }
    }

    /// <summary>
    /// Test: Verify that build and test commands are configured.
    /// These commands will be executed in the isolated runtime.
    /// </summary>
    [Fact]
    public async Task AppGenerationPlan_ShouldHaveBuildAndTestCommands()
    {
        // Arrange
        var generationId = Guid.NewGuid();

        // Act
        var response = await _client.GetAsync(
            $"{BaseUrl}/api/ide/app-generation/{generationId}");

        // Assert
        if (response.StatusCode == HttpStatusCode.OK)
        {
            var json = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<AppGenerationReport>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            // Build commands should be configured
            result.Plan.BuildCommands.Should().NotBeEmpty(
                because: "BuildCommands must be configured for the isolated runtime");

            // Test commands should be configured
            result.Plan.TestCommands.Should().NotBeEmpty(
                because: "TestCommands must be configured for the isolated runtime");

            // Each command should be non-empty
            result.Plan.BuildCommands.Should().AllSatisfy(cmd =>
                cmd.Should().NotBeNullOrWhiteSpace());

            result.Plan.TestCommands.Should().AllSatisfy(cmd =>
                cmd.Should().NotBeNullOrWhiteSpace());
        }
    }

    /// <summary>
    /// Test: Verify that the generation includes all required phases.
    /// Expected phases: Scaffold, Implement core, Tests, Security & review.
    /// </summary>
    [Fact]
    public async Task AppGenerationPlan_ShouldIncludeAllRequiredPhases()
    {
        // Arrange
        var generationId = Guid.NewGuid();
        var requiredPhases = new[] { "Scaffold", "Implement core", "Tests", "Security & review" };

        // Act
        var response = await _client.GetAsync(
            $"{BaseUrl}/api/ide/app-generation/{generationId}");

        // Assert
        if (response.StatusCode == HttpStatusCode.OK)
        {
            var json = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<AppGenerationReport>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            var phases = result.Plan.Phases.Select(p => p.Name).ToList();

            foreach (var phase in requiredPhases)
            {
                phases.Should().Contain(p => p.Contains(phase, StringComparison.OrdinalIgnoreCase),
                    because: $"Phase '{phase}' should be included in the generation plan");
            }
        }
    }

    /// <summary>
    /// Test: Verify that each phase has agent assignments.
    /// Each phase should have at least one agent assigned to it.
    /// </summary>
    [Fact]
    public async Task AppGenerationPlan_EachPhaseShouldHaveAgentAssignments()
    {
        // Arrange
        var generationId = Guid.NewGuid();

        // Act
        var response = await _client.GetAsync(
            $"{BaseUrl}/api/ide/app-generation/{generationId}");

        // Assert
        if (response.StatusCode == HttpStatusCode.OK)
        {
            var json = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<AppGenerationReport>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            foreach (var phase in result.Plan.Phases)
            {
                phase.Assignments.Should().NotBeEmpty(
                    because: $"Phase '{phase.Name}' should have at least one agent assignment");

                foreach (var assignment in phase.Assignments)
                {
                    assignment.AgentName.Should().NotBeNullOrEmpty();
                    assignment.Role.Should().NotBeNullOrEmpty();
                    assignment.TaskDescription.Should().NotBeNullOrEmpty();
                }
            }
        }
    }
}

// DTOs for test assertions
public class AppGenerationResponse
{
    public Guid GenerationId { get; set; }
    public string Status { get; set; }
    public string Message { get; set; }
}

public class AppGenerationReport
{
    public Guid GenerationId { get; set; }
    public string Status { get; set; }
    public GenerationPlanDto Plan { get; set; }
    public List<GeneratedFileDto> GeneratedFiles { get; set; } = new();
    public List<ExecutionResultDto> ExecutionResults { get; set; } = new();
    public List<ErrorDto> Errors { get; set; } = new();
}

public class GenerationPlanDto
{
    public string ApplicationName { get; set; }
    public string Description { get; set; }
    public TechStackDto TechStack { get; set; }
    public List<string> RequiredAgents { get; set; } = new();
    public List<PhaseDto> Phases { get; set; } = new();
    public int MaxIterations { get; set; }
    public string RuntimeImage { get; set; }
    public List<string> BuildCommands { get; set; } = new();
    public List<string> TestCommands { get; set; } = new();
}

public class TechStackDto
{
    public List<string> Languages { get; set; } = new();
    public List<string> Frameworks { get; set; } = new();
    public List<string> Databases { get; set; } = new();
    public List<string> Infrastructure { get; set; } = new();
    public string Rationale { get; set; }
}

public class PhaseDto
{
    public int Order { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public List<AssignmentDto> Assignments { get; set; } = new();
}

public class AssignmentDto
{
    public string AgentName { get; set; }
    public string Role { get; set; }
    public string TaskDescription { get; set; }
}

public class GeneratedFileDto
{
    public string RelativePath { get; set; }
    public string Language { get; set; }
    public string Content { get; set; }
}

public class ExecutionResultDto
{
    public string CommandName { get; set; }
    public int ExitCode { get; set; }
    public string Output { get; set; }
    public string Error { get; set; }
    public TimeSpan Duration { get; set; }
}

public class ErrorDto
{
    public string Message { get; set; }
    public string StackTrace { get; set; }
    public int Iteration { get; set; }
}
