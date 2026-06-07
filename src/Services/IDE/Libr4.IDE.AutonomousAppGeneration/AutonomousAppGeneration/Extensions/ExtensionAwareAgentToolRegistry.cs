using System.Text;
using System.Text.Json;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Abstractions;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Core;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Models;
using Libr4.IDE.Domain.AutonomousAppGeneration;

namespace Libr4.IDE.Application.AutonomousAppGeneration.Extensions;

public sealed class ExtensionScriptTool : IAgentTool
{
    private readonly ExtensionToolBinding _binding;
    private readonly ISandboxedExtensionRunner _runner;

    public ExtensionScriptTool(ExtensionToolBinding binding, ISandboxedExtensionRunner runner)
    {
        _binding = binding;
        _runner = runner;
    }

    public string Name => _binding.Definition.Name;
    public string Description => _binding.Definition.Description;
    public bool IsReadOnly => _binding.Definition.ReadOnly;
    public bool IsConcurrencySafe(JsonElement input) => IsReadOnly;

    public async Task<ToolExecutionResult> ExecuteAsync(JsonElement input, ToolContext context, CancellationToken ct)
    {
        var result = await _runner.RunToolAsync(_binding, input, context, ct).ConfigureAwait(false);
        return new ToolExecutionResult(
            Name,
            result.Success,
            result.Output,
            Array.Empty<GeneratedFile>());
    }
}

public sealed class ExtensionAwareAgentToolRegistry : IAgentToolRegistry
{
    private readonly AgentToolRegistry _inner;
    private readonly IExtensionHost _host;
    private readonly ISandboxedExtensionRunner _runner;
    private readonly object _lock = new();
    private Dictionary<string, IAgentTool>? _extensionTools;

    public ExtensionAwareAgentToolRegistry(
        IEnumerable<IAgentTool> tools,
        IExtensionHost host,
        ISandboxedExtensionRunner runner)
    {
        _inner = new AgentToolRegistry(tools);
        _host = host;
        _runner = runner;
    }

    public IReadOnlyList<IAgentTool> All
    {
        get
        {
            var list = new List<IAgentTool>(_inner.All);
            list.AddRange(GetExtensionTools().Values);
            return list.OrderBy(t => t.Name, StringComparer.OrdinalIgnoreCase).ToList();
        }
    }

    public IAgentTool? TryGet(string name)
    {
        var builtIn = _inner.TryGet(name);
        if (builtIn is not null)
            return builtIn;

        return GetExtensionTools().TryGetValue(name, out var tool) ? tool : null;
    }

    public string BuildToolCatalog()
    {
        var sb = new StringBuilder(_inner.BuildToolCatalog());
        foreach (var tool in GetExtensionTools().Values.OrderBy(t => t.Name, StringComparer.OrdinalIgnoreCase))
        {
            sb.AppendLine();
            sb.Append("- ").Append(tool.Name);
            if (tool.IsReadOnly)
                sb.Append(" [read-only]");
            sb.Append(": ").Append(tool.Description);
        }

        return sb.ToString().TrimEnd();
    }

    private Dictionary<string, IAgentTool> GetExtensionTools()
    {
        lock (_lock)
        {
            if (_extensionTools is not null)
                return _extensionTools;

            _extensionTools = _host.Tools
                .GroupBy(t => t.Definition.Name, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(
                    g => g.Key,
                    g => (IAgentTool)new ExtensionScriptTool(g.First(), _runner),
                    StringComparer.OrdinalIgnoreCase);
            return _extensionTools;
        }
    }
}
