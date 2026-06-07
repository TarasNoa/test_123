using System.Text.Json;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Abstractions;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Models;

namespace Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Tools;

/// <summary>Claude Code TodoWriteTool — session task tracking.</summary>
public sealed class TodoWriteTool : IAgentTool
{
    public string Name => "todo_write";
    public string Description => "Update session todos. Input: { \"todos\": [{\"id\",\"content\",\"status\",\"activeForm\"}] }";
    public bool IsReadOnly => false;
    public bool IsConcurrencySafe(JsonElement input) => false;

    public Task<ToolExecutionResult> ExecuteAsync(JsonElement input, ToolContext context, CancellationToken ct)
    {
        if (!input.TryGetProperty("todos", out var arr) || arr.ValueKind != JsonValueKind.Array)
            return Task.FromResult(Fail("todos array required"));

        context.Session.Todos.Clear();
        foreach (var item in arr.EnumerateArray())
        {
            var id = item.TryGetProperty("id", out var idEl) ? idEl.GetString() : null;
            var content = item.TryGetProperty("content", out var cEl) ? cEl.GetString() : null;
            var status = item.TryGetProperty("status", out var sEl) ? sEl.GetString() : "pending";
            var active = item.TryGetProperty("activeForm", out var aEl) ? aEl.GetString() : null;
            if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(content))
                continue;
            context.Session.Todos.Add(new AgentTodoItem(id!, content!, status ?? "pending", active));
        }

        var summary = string.Join("\n", context.Session.Todos.Select(t => $"[{t.Status}] {t.Id}: {t.Content}"));
        return Task.FromResult(new ToolExecutionResult(Name, true, summary, Array.Empty<Domain.AutonomousAppGeneration.GeneratedFile>()));
    }

    private static ToolExecutionResult Fail(string msg) =>
        new("todo_write", false, msg, Array.Empty<Domain.AutonomousAppGeneration.GeneratedFile>());
}
