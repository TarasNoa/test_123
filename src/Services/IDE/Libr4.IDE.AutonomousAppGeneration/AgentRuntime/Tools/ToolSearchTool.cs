using System.Text;
using System.Text.Json;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Abstractions;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Models;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentIntegration;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Skills;
using Microsoft.Extensions.DependencyInjection;

namespace Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Tools;

/// <summary>Claude Code ToolSearchTool — discover available tools/skills/MCP.</summary>
public sealed class ToolSearchTool : IAgentTool
{
    private readonly IServiceScopeFactory _scopes;

    public ToolSearchTool(IServiceScopeFactory scopes) => _scopes = scopes;

    public string Name => "tool_search";
    public string Description => "List tools/skills/MCP. Input: { \"query\": \"optional filter\" }";
    public bool IsReadOnly => true;
    public bool IsConcurrencySafe(JsonElement input) => true;

    public Task<ToolExecutionResult> ExecuteAsync(JsonElement input, ToolContext context, CancellationToken ct)
    {
        var query = input.TryGetProperty("query", out var q) && q.ValueKind == JsonValueKind.String
            ? q.GetString()?.Trim().ToLowerInvariant()
            : null;

        using var scope = _scopes.CreateScope();
        var registry = scope.ServiceProvider.GetRequiredService<IAgentToolRegistry>();

        var sb = new StringBuilder();
        sb.AppendLine("DISCOVERY (call only when stuck):");
        sb.AppendLine("tool_search — find skills/MCP/subagents not in your current scoped briefing.");
        sb.AppendLine("Format: { \"query\": \"pytest|mcp|verify|...\" }");
        sb.AppendLine();
        sb.AppendLine("CORE TOOLS:");
        foreach (var tool in registry.All.OrderBy(t => t.Name, StringComparer.OrdinalIgnoreCase))
        {
            if (query is not null
                && !tool.Name.Contains(query, StringComparison.OrdinalIgnoreCase)
                && !tool.Description.Contains(query, StringComparison.OrdinalIgnoreCase))
                continue;
            sb.Append("- ").Append(tool.Name).Append(": ").AppendLine(tool.Description);
        }

        var manifest = scope.ServiceProvider.GetService<ISkillManifestRegistry>();
        if (manifest is not null)
        {
            sb.AppendLine("SKILLS (use activate_skill to load):");
            foreach (var skill in manifest.List().Take(42))
            {
                if (query is not null
                    && !skill.Id.Contains(query, StringComparison.OrdinalIgnoreCase)
                    && !skill.Description.Contains(query, StringComparison.OrdinalIgnoreCase))
                    continue;
                sb.Append("- ").Append(skill.Id).Append(": ").AppendLine(skill.Description);
            }
        }
        else
        {
            var skills = scope.ServiceProvider.GetService<ISkillRegistry>();
            if (skills is not null)
            {
                sb.AppendLine("SKILLS:");
                foreach (var skill in skills.List().Take(30))
                {
                    if (query is not null && !skill.Id.Contains(query, StringComparison.OrdinalIgnoreCase))
                        continue;
                    sb.Append("- skill:").Append(skill.Id).Append(" v").Append(skill.Version).AppendLine();
                }
            }
        }

        var mcp = scope.ServiceProvider.GetService<IMcpToolRegistry>();
        if (mcp is not null)
        {
            sb.AppendLine("MCP:");
            foreach (var tool in mcp.ListTools().Take(30))
            {
                if (query is not null && !tool.ToolName.Contains(query, StringComparison.OrdinalIgnoreCase))
                    continue;
                sb.Append("- mcp:").Append(tool.ToolName).AppendLine();
            }
        }

        return Task.FromResult(new ToolExecutionResult(Name, true, sb.ToString().TrimEnd(), Array.Empty<Domain.AutonomousAppGeneration.GeneratedFile>()));
    }
}
