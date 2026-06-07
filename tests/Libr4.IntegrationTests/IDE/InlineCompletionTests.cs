using FluentAssertions;
using Libr4.AI.Application.Abstractions;
using Libr4.IDE.Application.AutonomousAppGeneration.InlineCompletion;
using Libr4.IDE.Application.AutonomousAppGeneration.ModelRouting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Libr4.IntegrationTests.IDE;

public sealed class InlineCompletionTests
{
    [Fact]
    public async Task CompleteAsync_ReturnsSuppressed_WhenDisabled()
    {
        var service = new InlineCompletionService(
            new StubAiService("ignored"),
            new StubModelRouter(),
            Options.Create(new InlineCompletionOptions { Enabled = false }),
            NullLogger<InlineCompletionService>.Instance);

        var result = await service.CompleteAsync(new InlineCompletionRequest(
            "src/App.cs",
            "csharp",
            "class App {\n  void M() {\n    \n  }\n}",
            3,
            5));

        result.Suppressed.Should().BeTrue();
        result.SuppressReason.Should().Be("disabled");
    }

    [Fact]
    public async Task CompleteAsync_ReturnsGhostText_WhenModelResponds()
    {
        var service = new InlineCompletionService(
            new StubAiService("return x;"),
            new StubModelRouter(),
            Options.Create(new InlineCompletionOptions { Enabled = true, MaxLatencyMs = 5000 }),
            NullLogger<InlineCompletionService>.Instance);

        var result = await service.CompleteAsync(new InlineCompletionRequest(
            "src/App.cs",
            "csharp",
            "class App {\n  void M() {\n    \n  }\n}",
            3,
            5));

        result.Suppressed.Should().BeFalse();
        result.Text.Should().Be("return x;");
    }

    private sealed class StubAiService(string response) : IAIService
    {
        public Task<string> GenerateCompletionAsync(string prompt, string? systemPrompt = null, string? model = null) =>
            Task.FromResult(response);

        public Task<string> GenerateEmbeddingAsync(string text, string? model = null) =>
            Task.FromResult("[]");

        public Task<string> AnalyzeTextAsync(string text, string analysisType, string? model = null) =>
            Task.FromResult(string.Empty);

        public Task<string> ChatAsync(string message, string? systemPrompt = null, string? model = null) =>
            Task.FromResult(response);
    }

    private sealed class StubModelRouter : IAgentModelRouter
    {
        public AgentModelRouteDecision Route(string role, string? yamlModelOverride = null) =>
            new(role, "test-model", Array.Empty<string>(), AgentModelProfile.OpenRouter, "test");

        public bool IsRoleModelCircuitOpen(string role, string model) => false;

        public void RecordRoleModelSuccess(string role, string model) { }

        public void RecordRoleModelFailure(string role, string model) { }
    }
}
