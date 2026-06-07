using System.Text;
using System.Text.Json;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Abstractions;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Models;
using Microsoft.Extensions.Options;

namespace Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Tools;

public sealed class BashTool : IAgentTool
{
    private readonly AgentRuntimeOptions _options;

    public BashTool(IOptions<AgentRuntimeOptions> options) => _options = options.Value;

    public string Name => "bash";
    public string Description => "Run a shell command in the shadow workspace. Input: { \"command\": \"...\" }";
    public bool IsReadOnly => true;

    public bool IsConcurrencySafe(JsonElement input) => false;

    public async Task<ToolExecutionResult> ExecuteAsync(JsonElement input, ToolContext context, CancellationToken ct)
    {
        if (!input.TryGetProperty("command", out var cmdEl) || cmdEl.ValueKind != JsonValueKind.String)
            return Fail("command is required");

        var command = cmdEl.GetString() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(command))
            return Fail("command is empty");

        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeoutCts.CancelAfter(TimeSpan.FromSeconds(_options.BashTimeoutSeconds));

        var result = await context.Accessor.ExecAsync(context.Workspace.WorkspaceId, command, timeoutCts.Token)
            .ConfigureAwait(false);

        var output = string.Join("\n", result.Logs.Select(l => l.Message));
        var sb = new StringBuilder();
        sb.AppendLine($"exit_code={result.ExitCode}");
        sb.AppendLine($"duration_ms={(int)result.Duration.TotalMilliseconds}");
        sb.AppendLine("---");
        sb.Append(output);

        return new ToolExecutionResult(
            Name,
            result.Succeeded,
            sb.ToString().TrimEnd(),
            Array.Empty<Domain.AutonomousAppGeneration.GeneratedFile>());
    }

    private static ToolExecutionResult Fail(string message) =>
        new("bash", false, message, Array.Empty<Domain.AutonomousAppGeneration.GeneratedFile>());
}
