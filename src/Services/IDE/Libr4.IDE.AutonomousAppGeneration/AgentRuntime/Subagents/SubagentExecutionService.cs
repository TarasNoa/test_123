using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Abstractions;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Models;
using Libr4.IDE.Application.AutonomousAppGeneration.Computer;
using Libr4.IDE.AutonomousAppGeneration.Agents;
using Microsoft.Extensions.DependencyInjection;

namespace Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Subagents;

public sealed class SubagentExecutionService
{
    private readonly IServiceScopeFactory _scopes;
    private readonly IAgentSpecRegistry _specs;
    private readonly ISubagentStore _store;

    public SubagentExecutionService(
        IServiceScopeFactory scopes,
        IAgentSpecRegistry specs,
        ISubagentStore store)
    {
        _scopes = scopes;
        _specs = specs;
        _store = store;
    }

    public async Task<ToolExecutionResult> ExecuteAsync(
        string toolName,
        string? specName,
        string? role,
        string task,
        ToolContext context,
        AgentRuntimeOptions options,
        CancellationToken ct)
    {
        if (context.Session.SubagentDepth >= options.MaxSubagentDepth)
            return Fail(toolName, $"subagent depth limit {options.MaxSubagentDepth} reached");

        var resolvedName = !string.IsNullOrWhiteSpace(specName) ? specName! : role!;
        _specs.TryGet(resolvedName, out var spec);

        var runId = context.Session.RunId ?? Guid.NewGuid();
        var record = await _store.CreateAsync(runId, resolvedName, task, spec, ct).ConfigureAwait(false);
        await _store.AppendMessageAsync(runId, record.Id, "user", task, ct).ConfigureAwait(false);

        context.Session.SubagentDepth++;
        try
        {
            string output;
            var success = false;

            if (spec is not null
                && string.Equals(resolvedName, "computer", StringComparison.OrdinalIgnoreCase))
            {
                using var scope = _scopes.CreateScope();
                var computer = scope.ServiceProvider.GetRequiredService<IComputerSubagentService>();
                var result = await computer.RunAsync(spec, task, context, ct).ConfigureAwait(false);
                output = result.Succeeded ? result.Summary : $"subagent_failed: {result.Summary}";
                success = result.Succeeded;
            }
            else if (spec is not null && AgentSpecReservedNames.All.Contains(resolvedName))
            {
                using var scope = _scopes.CreateScope();
                var runner = scope.ServiceProvider.GetRequiredService<IAgentSpecSubagentRunner>();
                var result = await runner.RunAsync(spec, task, context, ct).ConfigureAwait(false);
                output = result.Succeeded ? result.Summary ?? "done" : $"subagent_failed: {result.Summary}";
                success = result.Succeeded;
            }
            else
            {
                using var scope = _scopes.CreateScope();
                var spawner = scope.ServiceProvider.GetService<IAgentSpawner>();
                if (spawner is null)
                {
                    await _store.FailAsync(runId, record.Id, "IAgentSpawner not available", ct).ConfigureAwait(false);
                    return Fail(toolName, "IAgentSpawner not available");
                }

                var agentContext = new AgentContext
                {
                    ApplicationName = context.Plan?.ApplicationName ?? "app",
                    Description = spec is not null ? $"{spec.Instruction}\n\n{task}" : task,
                    TechStack = string.Join(", ", context.Plan?.TechStack.Frameworks ?? Array.Empty<string>()),
                    ScopedOutputOnly = true,
                    GeneratedFiles = context.WorkingFiles
                        .Take(8)
                        .Select(f => new GeneratedFile { RelativePath = f.RelativePath, Content = f.Content })
                        .ToArray()
                };

                var spawnRole = spec?.Name ?? role!;
                var result = await spawner.SpawnAndExecuteAsync(spawnRole, agentContext, ct).ConfigureAwait(false);
                output = result.IsSuccess ? result.Content : $"subagent_failed: {result.Feedback ?? "unknown"}";
                success = result.IsSuccess;
            }

            await _store.AppendMessageAsync(runId, record.Id, "assistant", output, ct).ConfigureAwait(false);
            if (success)
                await _store.CompleteAsync(runId, record.Id, output, ct).ConfigureAwait(false);
            else
                await _store.FailAsync(runId, record.Id, output, ct).ConfigureAwait(false);

            return new ToolExecutionResult(toolName, success, Truncate(output), Array.Empty<Domain.AutonomousAppGeneration.GeneratedFile>());
        }
        catch (Exception ex)
        {
            await _store.FailAsync(runId, record.Id, ex.Message, ct).ConfigureAwait(false);
            return Fail(toolName, ex.Message);
        }
        finally
        {
            context.Session.SubagentDepth--;
        }
    }

    private static string Truncate(string text) =>
        text.Length <= 12_000 ? text : text[..12_000] + "\n...[truncated]...";

    private static ToolExecutionResult Fail(string toolName, string msg) =>
        new(toolName, false, msg, Array.Empty<Domain.AutonomousAppGeneration.GeneratedFile>());
}
