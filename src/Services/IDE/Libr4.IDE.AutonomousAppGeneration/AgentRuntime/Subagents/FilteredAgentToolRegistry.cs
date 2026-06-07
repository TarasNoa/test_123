using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Abstractions;

namespace Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Subagents;

public sealed class FilteredAgentToolRegistry : IAgentToolRegistry
{
    private readonly IAgentToolRegistry _inner;
    private readonly HashSet<string> _allowed;

    public FilteredAgentToolRegistry(IAgentToolRegistry inner, IReadOnlyList<string> toolset)
    {
        _inner = inner;
        _allowed = toolset.Count == 0
            ? inner.All.Select(t => t.Name).ToHashSet(StringComparer.OrdinalIgnoreCase)
            : toolset.ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    public IReadOnlyList<IAgentTool> All =>
        _inner.All.Where(t => _allowed.Contains(t.Name)).ToList();

    public IAgentTool? TryGet(string name) =>
        _allowed.Contains(name) ? _inner.TryGet(name) : null;

    public string BuildToolCatalog()
    {
        var sb = new System.Text.StringBuilder();
        foreach (var tool in All.OrderBy(t => t.Name, StringComparer.OrdinalIgnoreCase))
        {
            sb.Append("- ").Append(tool.Name);
            if (tool.IsReadOnly)
                sb.Append(" [read-only]");
            sb.Append(": ").AppendLine(tool.Description);
        }

        return sb.ToString().TrimEnd();
    }
}
