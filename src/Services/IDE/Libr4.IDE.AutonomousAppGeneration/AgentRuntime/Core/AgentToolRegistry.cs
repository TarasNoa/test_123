using System.Text;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Abstractions;

namespace Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Core;

public sealed class AgentToolRegistry : IAgentToolRegistry
{
    private readonly Dictionary<string, IAgentTool> _tools;

    public AgentToolRegistry(IEnumerable<IAgentTool> tools)
    {
        _tools = tools.ToDictionary(t => t.Name, t => t, StringComparer.OrdinalIgnoreCase);
    }

    public IReadOnlyList<IAgentTool> All => _tools.Values.ToList();

    public IAgentTool? TryGet(string name) =>
        _tools.TryGetValue(name, out var tool) ? tool : null;

    public string BuildToolCatalog()
    {
        var sb = new StringBuilder();
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
