using Libr4.IDE.Application.AutonomousAppGeneration.Infrastructure;
using Libr4.IDE.Domain.AutonomousAppGeneration;

namespace Libr4.IDE.Application.AutonomousAppGeneration.Services.StackStrategy;

public sealed class DotNetStackStrategy : IStackStrategy
{
    public string StackId => "dotnet";
    public StackKind Kind => StackKind.DotNet;
    public bool Matches(GenerationPlan plan) => StackPlanHeuristics.IsAspNetCore(plan);
    public string PreferredRuntimeImage => "mcr.microsoft.com/dotnet/sdk:8.0";
    public IReadOnlyList<string> DefaultBuildCommands { get; } =
        new[] { "dotnet restore", "dotnet build --configuration Release" };
    public IReadOnlyList<string> DefaultTestCommands { get; } =
        new[] { "dotnet test --configuration Release" };

    public bool IsTestPath(string relativePath) =>
        Libr4.IDE.Application.AutonomousAppGeneration.Infrastructure.GenerationPathHeuristics
            .LooksLikeDotNetTestPath(relativePath);
}

public sealed class PythonStackStrategy : IStackStrategy
{
    public string StackId => "python";
    public StackKind Kind => StackKind.Python;
    public bool Matches(GenerationPlan plan) => StackPlanHeuristics.IsPython(plan);
    public string PreferredRuntimeImage => "python:3.12-slim";
    public IReadOnlyList<string> DefaultBuildCommands { get; } =
        new[] { "python -m pip install -r requirements.txt" };
    public IReadOnlyList<string> DefaultTestCommands { get; } =
        new[] { "python -m pytest -q" };

    public bool IsTestPath(string relativePath) =>
        Libr4.IDE.Application.AutonomousAppGeneration.Infrastructure.GenerationPathHeuristics
            .LooksLikePythonTestPath(relativePath);
}

public sealed class NodeStackStrategy : IStackStrategy
{
    public string StackId => "node";
    public StackKind Kind => StackKind.Node;
    public bool Matches(GenerationPlan plan) => StackPlanHeuristics.IsNode(plan);
    public string PreferredRuntimeImage => "node:20-alpine";
    public IReadOnlyList<string> DefaultBuildCommands { get; } =
        new[] { "npm ci", "npm run build" };
    public IReadOnlyList<string> DefaultTestCommands { get; } =
        new[] { "npm test" };

    public bool IsTestPath(string relativePath) =>
        Libr4.IDE.Application.AutonomousAppGeneration.Infrastructure.GenerationPathHeuristics
            .LooksLikeNodeTestPath(relativePath);
}

public sealed class UnknownStackStrategy : IStackStrategy
{
    public string StackId => "unknown";
    public StackKind Kind => StackKind.Unknown;
    public bool Matches(GenerationPlan plan) => false;
    public string PreferredRuntimeImage => "alpine:3";
    public IReadOnlyList<string> DefaultBuildCommands { get; } = new[] { "echo no_build_command_configured" };
    public IReadOnlyList<string> DefaultTestCommands { get; } = new[] { "echo no_test_command_configured" };

    public bool IsTestPath(string relativePath) =>
        relativePath.Contains("test", StringComparison.OrdinalIgnoreCase);
}
