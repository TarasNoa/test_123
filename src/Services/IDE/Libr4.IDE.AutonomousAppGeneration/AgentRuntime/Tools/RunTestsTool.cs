using System.Text;
using System.Text.Json;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Abstractions;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Core;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Models;
using Microsoft.Extensions.Options;

namespace Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Tools;

/// <summary>Run plan test commands only — separates test failures from compile/build errors.</summary>
public sealed class RunTestsTool : IAgentTool
{
    private readonly AgentRuntimeOptions _options;

    public RunTestsTool(IOptions<AgentRuntimeOptions> options) => _options = options.Value;

    public string Name => "run_tests";
    public string Description => "Run plan test commands only. Input: {} or { \"filter\": \"optional substring\" }";
    public bool IsReadOnly => true;
    public bool IsConcurrencySafe(JsonElement input) => false;

    public async Task<ToolExecutionResult> ExecuteAsync(JsonElement input, ToolContext context, CancellationToken ct)
    {
        if (context.Mode == AgentSessionMode.Generation && !_options.AllowBashDuringGeneration)
            return Fail("run_tests disabled during generation (enable AllowBashDuringGeneration)");

        if (context.Plan is null)
            return Fail("no generation plan in context");

        var commands = context.Plan.TestCommands;
        if (commands.Count == 0)
            return Fail("no test commands in plan");

        var filter = input.TryGetProperty("filter", out var f) && f.ValueKind == JsonValueKind.String
            ? f.GetString()?.Trim()
            : null;

        var sb = new StringBuilder();
        sb.AppendLine(BuildErrorCategoryClassifier.FormatForAgent(null, "running tests"));
        sb.AppendLine();

        var allOk = true;
        foreach (var cmd in commands)
        {
            if (!string.IsNullOrWhiteSpace(filter)
                && !cmd.Contains(filter, StringComparison.OrdinalIgnoreCase))
                continue;

            ct.ThrowIfCancellationRequested();
            var result = await context.Accessor.ExecAsync(context.Workspace.WorkspaceId, cmd, ct).ConfigureAwait(false);
            sb.AppendLine($"$ {cmd}");
            sb.AppendLine($"exit_code={result.ExitCode}");
            sb.AppendLine(string.Join("\n", result.Logs.Select(l => l.Message)));
            sb.AppendLine("---");
            if (!result.Succeeded)
            {
                allOk = false;
                var (category, hint) = BuildErrorCategoryClassifier.Classify(sb.ToString());
                sb.AppendLine($"classified_as={category}");
                sb.AppendLine($"hint={hint}");
                break;
            }
        }

        return new ToolExecutionResult(Name, allOk, sb.ToString().TrimEnd(), Array.Empty<Domain.AutonomousAppGeneration.GeneratedFile>());
    }

    private static ToolExecutionResult Fail(string msg) =>
        new("run_tests", false, msg, Array.Empty<Domain.AutonomousAppGeneration.GeneratedFile>());
}
