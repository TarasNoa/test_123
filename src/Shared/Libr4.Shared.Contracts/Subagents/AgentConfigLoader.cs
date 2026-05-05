using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using System.Text.Json.Nodes;

namespace Libr4.Shared.Contracts.Subagents;

/// <summary>
/// Source of agent configuration.
/// </summary>
public enum AgentSource
{
    /// <summary>
    /// Built-in agents provided by the system.
    /// </summary>
    BuiltIn,

    /// <summary>
    /// Agents from plugins.
    /// </summary>
    Plugin,

    /// <summary>
    /// User-level agents from ~/.libr4/agents/.
    /// </summary>
    UserSettings,

    /// <summary>
    /// Project-level agents from ./.libr4/agents/.
    /// </summary>
    ProjectSettings,

    /// <summary>
    /// Agents from CLI flags.
    /// </summary>
    FlagSettings,

    /// <summary>
    /// Agents from policy directory.
    /// </summary>
    PolicySettings
}

/// <summary>
/// Location of agent configuration.
/// </summary>
public enum AgentLocation
{
    /// <summary>
    /// Built-in location.
    /// </summary>
    BuiltIn,

    /// <summary>
    /// Plugin location.
    /// </summary>
    Plugin,

    /// <summary>
    /// User directory.
    /// </summary>
    User,

    /// <summary>
    /// User settings directory.
    /// </summary>
    UserSettings,

    /// <summary>
    /// Project directory.
    /// </summary>
    Project,

    /// <summary>
    /// Project settings directory.
    /// </summary>
    ProjectSettings
}

/// <summary>
/// Permission mode for agents.
/// </summary>
public enum AgentPermissionMode
{
    /// <summary>
    /// Safe mode with permission checks.
    /// </summary>
    Safe,

    /// <summary>
    /// Default permission checks.
    /// </summary>
    Default,

    /// <summary>
    /// Accept file edits without prompts.
    /// </summary>
    AcceptEdits,

    /// <summary>
    /// Read-only planning mode.
    /// </summary>
    Plan,

    /// <summary>
    /// Skip all permission checks (YOLO mode).
    /// </summary>
    BypassPermissions,

    /// <summary>
    /// Don't ask for confirmations.
    /// </summary>
    DontAsk,

    /// <summary>
    /// Delegate to parent's permission mode.
    /// </summary>
    Delegate
}

/// <summary>
/// Agent configuration loaded from file.
/// </summary>
public class AgentConfig
{
    /// <summary>
    /// Agent type/name.
    /// </summary>
    public string AgentType { get; set; } = string.Empty;

    /// <summary>
    /// Description of when to use this agent.
    /// </summary>
    public string WhenToUse { get; set; } = string.Empty;

    /// <summary>
    /// Tools allowed for this agent (or "*" for all).
    /// </summary>
    public List<string> Tools { get; set; } = new();

    /// <summary>
    /// Tools explicitly disallowed for this agent.
    /// </summary>
    public List<string>? DisallowedTools { get; set; }

    /// <summary>
    /// Skills available to this agent.
    /// </summary>
    public List<string>? Skills { get; set; }

    /// <summary>
    /// System prompt for the agent.
    /// </summary>
    public string SystemPrompt { get; set; } = string.Empty;

    /// <summary>
    /// Source of this configuration.
    /// </summary>
    public AgentSource Source { get; set; }

    /// <summary>
    /// Location of this configuration.
    /// </summary>
    public AgentLocation Location { get; set; }

    /// <summary>
    /// Base directory for this agent.
    /// </summary>
    public string? BaseDir { get; set; }

    /// <summary>
    /// Filename of the configuration.
    /// </summary>
    public string? Filename { get; set; }

    /// <summary>
    /// Color for UI display.
    /// </summary>
    public string? Color { get; set; }

    /// <summary>
    /// Model to use for this agent.
    /// </summary>
    public string? Model { get; set; }

    /// <summary>
    /// Permission mode for this agent.
    /// </summary>
    public AgentPermissionMode? PermissionMode { get; set; }

    /// <summary>
    /// Whether to fork context from parent.
    /// </summary>
    public bool ForkContext { get; set; }

    /// <summary>
    /// Converts to SubagentDefinition.
    /// </summary>
    public SubagentDefinition ToSubagentDefinition()
    {
        return new SubagentDefinition
        {
            Id = AgentType,
            Name = AgentType,
            Description = WhenToUse,
            Model = Model ?? string.Empty,
            Instructions = SystemPrompt,
            Capabilities = Tools,
            Enabled = true
        };
    }
}

/// <summary>
/// YAML frontmatter for agent configuration.
/// </summary>
public class AgentFrontmatter
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<string>? Tools { get; set; }
    public List<string>? DisallowedTools { get; set; }
    public List<string>? Disallowed_tools { get; set; }
    public List<string>? Disallowed_Tools { get; set; }
    public List<string>? Skills { get; set; }
    public string? Model { get; set; }
    public string? Model_name { get; set; }
    public string? Color { get; set; }
    public string? PermissionMode { get; set; }
    public string? ForkContext { get; set; }
    public string? Fork_context { get; set; }
}

/// <summary>
/// Loader for agent configuration files.
/// </summary>
public class AgentConfigLoader
{
    private readonly IDeserializer _yamlDeserializer;

    public AgentConfigLoader()
    {
        _yamlDeserializer = new DeserializerBuilder()
            .WithNamingConvention(UnderscoredNamingConvention.Instance)
            .IgnoreUnmatchedProperties()
            .Build();
    }

    /// <summary>
    /// Parses agent configuration from YAML content with frontmatter.
    /// </summary>
    public AgentConfig? ParseFromYaml(string yamlContent, string filePath, AgentSource source, string baseDir)
    {
        try
        {
            // Split YAML into frontmatter and content
            var parts = yamlContent.Split("---", StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 2)
                return null;

            var frontmatterYaml = parts[0].Trim();
            var content = string.Join("---", parts.Skip(1)).Trim();

            var frontmatter = _yamlDeserializer.Deserialize<AgentFrontmatter>(frontmatterYaml);
            if (frontmatter == null || string.IsNullOrEmpty(frontmatter.Name) || string.IsNullOrEmpty(frontmatter.Description))
                return null;

            // Normalize model field
            var model = frontmatter.Model_name ?? frontmatter.Model;
            if (string.IsNullOrEmpty(model))
                model = null;

            // Normalize disallowed tools
            var disallowedTools = frontmatter.DisallowedTools ?? frontmatter.Disallowed_tools ?? frontmatter.Disallowed_Tools;

            // Normalize permission mode
            var permissionModeStr = frontmatter.PermissionMode ?? "Safe";
            AgentPermissionMode? permissionMode = null;
            if (!string.IsNullOrEmpty(permissionModeStr))
            {
                if (Enum.TryParse<AgentPermissionMode>(permissionModeStr, true, out var mode))
                    permissionMode = mode;
            }

            // Normalize fork context
            bool forkContext = false;
            if (!string.IsNullOrEmpty(frontmatter.ForkContext) || !string.IsNullOrEmpty(frontmatter.Fork_context))
            {
                var forkStr = frontmatter.ForkContext ?? frontmatter.Fork_context;
                if (forkStr == "true" || forkStr == "1")
                    forkContext = true;
            }

            // Validate fork context with model override
            if (forkContext && model != null && model != "inherit")
            {
                // Warning: forkContext with model override - set model to inherit
                model = "inherit";
            }

            // Normalize tools
            var tools = frontmatter.Tools ?? new List<string>();
            if (tools.Contains("*"))
                tools = new List<string> { "*" };

            var config = new AgentConfig
            {
                AgentType = frontmatter.Name,
                WhenToUse = frontmatter.Description.Replace("\\n", "\n"),
                Tools = tools,
                DisallowedTools = disallowedTools,
                Skills = frontmatter.Skills,
                SystemPrompt = content,
                Source = source,
                Location = SourceToLocation(source),
                BaseDir = baseDir,
                Filename = Path.GetFileName(filePath),
                Color = frontmatter.Color,
                Model = model,
                PermissionMode = permissionMode,
                ForkContext = forkContext
            };

            return config;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Parses agent configuration from JSON file.
    /// </summary>
    public AgentConfig? ParseFromJson(string jsonContent, string agentType, AgentSource source)
    {
        try
        {
            var data = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(jsonContent);
            if (data == null)
                return null;

            if (!data.ContainsKey("description") || !data.ContainsKey("prompt"))
                return null;

            var description = data["description"]?.ToString() ?? string.Empty;
            var prompt = data["prompt"]?.ToString() ?? string.Empty;

            var tools = new List<string>();
            if (data.ContainsKey("tools") && data["tools"] is JsonNode toolsNode)
            {
                if (toolsNode is JsonArray toolsArray)
                {
                    foreach (var tool in toolsArray)
                    {
                        tools.Add(tool.ToString() ?? string.Empty);
                    }
                }
            }

            var disallowedTools = new List<string>();
            if (data.ContainsKey("disallowedTools") && data["disallowedTools"] is JsonNode disallowedNode)
            {
                if (disallowedNode is JsonArray disallowedArray)
                {
                    foreach (var tool in disallowedArray)
                    {
                        disallowedTools.Add(tool.ToString() ?? string.Empty);
                    }
                }
            }

            var model = data.ContainsKey("model") ? data["model"]?.ToString() : null;

            AgentPermissionMode? permissionMode = null;
            if (data.ContainsKey("permissionMode"))
            {
                var pmStr = data["permissionMode"]?.ToString();
                if (!string.IsNullOrEmpty(pmStr) && Enum.TryParse<AgentPermissionMode>(pmStr, true, out var mode))
                    permissionMode = mode;
            }

            if (tools.Contains("*"))
                tools = new List<string> { "*" };

            var config = new AgentConfig
            {
                AgentType = agentType,
                WhenToUse = description,
                Tools = tools,
                DisallowedTools = disallowedTools.Count > 0 ? disallowedTools : null,
                SystemPrompt = prompt,
                Source = source,
                Location = SourceToLocation(source),
                Model = model,
                PermissionMode = permissionMode
            };

            return config;
        }
        catch
        {
            return null;
        }
    }

    private static AgentLocation SourceToLocation(AgentSource source)
    {
        return source switch
        {
            AgentSource.Plugin => AgentLocation.Plugin,
            AgentSource.UserSettings => AgentLocation.User,
            AgentSource.ProjectSettings => AgentLocation.Project,
            _ => AgentLocation.BuiltIn
        };
    }

    /// <summary>
    /// Load agent configuration from a file
    /// </summary>
    public async Task<AgentConfig?> LoadFromFileAsync(string filePath, CancellationToken cancellationToken = default)
    {
        try
        {
            var content = await File.ReadAllTextAsync(filePath, cancellationToken);
            var extension = Path.GetExtension(filePath).ToLowerInvariant();
            
            return extension switch
            {
                ".yaml" or ".yml" => ParseFromYaml(content, filePath, AgentSource.ProjectSettings, Path.GetDirectoryName(filePath) ?? string.Empty),
                ".json" => ParseFromJson(content, Path.GetFileNameWithoutExtension(filePath), AgentSource.ProjectSettings),
                ".toml" => TomlAgentConfigParser.Parse(content, filePath, AgentSource.ProjectSettings, Path.GetDirectoryName(filePath) ?? string.Empty).ToAgentConfig(),
                _ => null
            };
        }
        catch
        {
            return null;
        }
    }
}
