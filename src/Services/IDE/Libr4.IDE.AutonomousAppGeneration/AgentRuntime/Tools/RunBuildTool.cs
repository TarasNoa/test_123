using System.Text;
using System.Text.Json;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Abstractions;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Models;
using Microsoft.Extensions.Options;

namespace Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Tools;

/// <summary>Claude Code-style verify step: run plan build/test commands inside workspace.</summary>
public sealed class RunBuildTool : IAgentTool
{
    private readonly AgentRuntimeOptions _options;

    public RunBuildTool(IOptions<AgentRuntimeOptions> options) => _options = options.Value;

    public string Name => "run_build";
    public string Description => "Run plan build+test commands. Input: { \"phase\": \"build\"|\"test\"|\"all\" }";
    public bool IsReadOnly => true;
    public bool IsConcurrencySafe(JsonElement input) => false;

    public async Task<ToolExecutionResult> ExecuteAsync(JsonElement input, ToolContext context, CancellationToken ct)
    {
        if (context.Mode == AgentSessionMode.Generation && !_options.AllowBashDuringGeneration)
            return Fail("run_build disabled during generation (enable AllowBashDuringGeneration)");

        var phase = input.TryGetProperty("phase", out var p) && p.ValueKind == JsonValueKind.String
            ? p.GetString()?.Trim().ToLowerInvariant() ?? "all"
            : "all";

        if (context.Plan is null)
            return Fail("no generation plan in context");

        var commands = phase switch
        {
            "build" => context.Plan.BuildCommands,
            "test" => context.Plan.TestCommands,
            _ => context.Plan.BuildCommands.Concat(context.Plan.TestCommands).ToList()
        };

        if (commands.Count == 0)
            return Fail("no commands for phase");

        var sb = new StringBuilder();
        var allOk = true;
        foreach (var cmd in commands)
        {
            ct.ThrowIfCancellationRequested();
            var result = await context.Accessor.ExecAsync(context.Workspace.WorkspaceId, cmd, ct).ConfigureAwait(false);
            sb.AppendLine($"$ {cmd}");
            sb.AppendLine($"exit_code={result.ExitCode}");
            sb.AppendLine(string.Join("\n", result.Logs.Select(l => l.Message)));
            sb.AppendLine("---");
            if (!result.Succeeded)
            {
                allOk = false;
                break;
            }
        }

        return new ToolExecutionResult(Name, allOk, sb.ToString().TrimEnd(), Array.Empty<Domain.AutonomousAppGeneration.GeneratedFile>());
    }

    private static ToolExecutionResult Fail(string msg) =>
        new("run_build", false, msg, Array.Empty<Domain.AutonomousAppGeneration.GeneratedFile>());
}
