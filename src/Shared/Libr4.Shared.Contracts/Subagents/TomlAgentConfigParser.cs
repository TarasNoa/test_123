using System.Text;
using System.Text.RegularExpressions;

namespace Libr4.Shared.Contracts.Subagents;

/// <summary>
/// TOML parser for agent configuration files.
/// Supports Kode-Agent compatible TOML format.
/// </summary>
public sealed class TomlAgentConfigParser
{
    private static readonly Regex SectionRegex = new(
        @"^\s*\[\s*(\w+(?:\.\w+)?)\s*\]\s*$",
        RegexOptions.Compiled);
    
    private static readonly Regex KeyValueRegex = new(
        @"^\s*(\w+)\s*=\s*(.+?)\s*$",
        RegexOptions.Compiled);
    
    private static readonly Regex ArrayRegex = new(
        @"^\s*\[\s*(.*?)\s*\]\s*$",
        RegexOptions.Compiled);
    
    private static readonly Regex StringRegex = new(
        @"^(?:""([^""]*)""|'([^']*)')$",
        RegexOptions.Compiled);

    /// <summary>
    /// Parse TOML content into agent configuration.
    /// </summary>
    public TomlAgentConfig Parse(string tomlContent)
    {
        var config = new TomlAgentConfig();
        var lines = tomlContent.Split('\n');
        string? currentSection = null;
        var currentArray = new List<string>();
        string? currentArrayKey = null;
        bool inArray = false;

        for (int i = 0; i < lines.Length; i++)
        {
            var line = lines[i].Trim();

            // Skip empty lines and comments
            if (string.IsNullOrWhiteSpace(line) || line.StartsWith('#'))
                continue;

            // Check for section header
            var sectionMatch = SectionRegex.Match(line);
            if (sectionMatch.Success)
            {
                // Save current array if any
                if (inArray && currentArrayKey != null)
                {
                    SetArrayValue(config, currentSection, currentArrayKey, currentArray);
                    currentArray.Clear();
                    inArray = false;
                }

                currentSection = sectionMatch.Groups[1].Value;
                continue;
            }

            // Check for key-value pair
            var kvMatch = KeyValueRegex.Match(line);
            if (kvMatch.Success)
            {
                // Save current array if any
                if (inArray && currentArrayKey != null)
                {
                    SetArrayValue(config, currentSection, currentArrayKey, currentArray);
                    currentArray.Clear();
                    inArray = false;
                }

                var key = kvMatch.Groups[1].Value;
                var value = kvMatch.Groups[2].Value.Trim();

                // Handle arrays
                if (value.StartsWith('[') && !value.EndsWith(']'))
                {
                    // Multi-line array
                    inArray = true;
                    currentArrayKey = key;
                    value = value.TrimStart('[').Trim();
                    if (!string.IsNullOrEmpty(value))
                        currentArray.Add(ParseString(value.TrimEnd(',')));
                }
                else if (value.StartsWith('[') && value.EndsWith(']'))
                {
                    // Single-line array
                    var arrayContent = value.TrimStart('[').TrimEnd(']').Trim();
                    var items = SplitArrayItems(arrayContent);
                    SetArrayValue(config, currentSection, key, items);
                }
                else
                {
                    // Single value
                    SetValue(config, currentSection, key, ParseString(value));
                }

                continue;
            }

            // Handle array continuation
            if (inArray)
            {
                if (line.EndsWith(']'))
                {
                    // End of array
                    var item = line.TrimEnd(']').Trim().TrimEnd(',');
                    if (!string.IsNullOrEmpty(item))
                        currentArray.Add(ParseString(item));
                    
                    if (currentArrayKey != null)
                        SetArrayValue(config, currentSection, currentArrayKey, currentArray);
                    
                    currentArray.Clear();
                    inArray = false;
                    currentArrayKey = null;
                }
                else
                {
                    // Array item
                    var item = line.TrimEnd(',').Trim();
                    if (!string.IsNullOrEmpty(item))
                        currentArray.Add(ParseString(item));
                }
            }
        }

        // Handle end of file
        if (inArray && currentArrayKey != null)
        {
            SetArrayValue(config, currentSection, currentArrayKey, currentArray);
        }

        return config;
    }

    /// <summary>
    /// Parse TOML file into agent configuration.
    /// </summary>
    public static TomlAgentConfig Parse(string tomlContent, string filePath, AgentSource source, string baseDir)
    {
        var config = new TomlAgentConfig
        {
            Source = source,
            Location = AgentLocation.ProjectSettings,
            BaseDir = baseDir,
            Filename = Path.GetFileName(filePath)
        };
        config.BaseDir = Path.GetDirectoryName(filePath);
        
        return config;
    }

    private static string ParseString(string value)
    {
        if (string.IsNullOrEmpty(value))
            return value;

        // Remove quotes
        if ((value.StartsWith('"') && value.EndsWith('"')) ||
            (value.StartsWith('\'') && value.EndsWith('\'')))
        {
            return value.Substring(1, value.Length - 2);
        }

        return value;
    }

    private static List<string> SplitArrayItems(string content)
    {
        var items = new List<string>();
        var current = new StringBuilder();
        int depth = 0;
        bool inString = false;
        char stringChar = '\0';

        for (int i = 0; i < content.Length; i++)
        {
            var c = content[i];

            if (!inString && (c == '"' || c == '\''))
            {
                inString = true;
                stringChar = c;
            }
            else if (inString && c == stringChar)
            {
                inString = false;
            }
            else if (!inString)
            {
                if (c == '[')
                    depth++;
                else if (c == ']')
                    depth--;
                else if (c == ',' && depth == 0)
                {
                    items.Add(current.ToString().Trim());
                    current.Clear();
                    continue;
                }
            }

            current.Append(c);
        }

        if (current.Length > 0)
            items.Add(current.ToString().Trim());

        return items.Select(ParseString).ToList();
    }

    private static void SetValue(TomlAgentConfig config, string? section, string key, string value)
    {
        var targetKey = section != null ? $"{section}.{key}" : key;

        switch (targetKey.ToLowerInvariant())
        {
            // Root-level fields
            case "name":
                config.Name = value;
                break;
            case "description":
                config.Description = value;
                break;
            case "model":
                config.Model = value;
                break;
            case "model_reasoning_effort":
            case "modelreasoningeffort":
                config.ModelReasoningEffort = value;
                break;
            case "color":
                config.Color = value;
                break;
            case "base_dir":
            case "basedir":
                config.BaseDir = value;
                break;
            case "instructions":
                config.Instructions = value;
                break;
            case "permission_mode":
            case "permissionmode":
                config.PermissionMode = Enum.TryParse<AgentPermissionMode>(value, true, out var mode) 
                    ? mode 
                    : AgentPermissionMode.Safe;
                break;
            case "sandbox_mode":
            case "sandboxmode":
                config.SandboxMode = Enum.TryParse<SandboxMode>(value, true, out var sandbox) 
                    ? sandbox 
                    : SandboxMode.ReadOnly;
                break;
            case "fork_context":
            case "forkcontext":
                config.ForkContext = value;
                break;
            case "source":
                config.Source = Enum.TryParse<AgentSource>(value, true, out var source) 
                    ? source 
                    : AgentSource.ProjectSettings;
                break;
            case "location":
                config.Location = Enum.TryParse<AgentLocation>(value, true, out var location) 
                    ? location 
                    : AgentLocation.ProjectSettings;
                break;

            // Section fields
            case "agent.name":
                config.Name = value;
                break;
            case "agent.description":
                config.Description = value;
                break;
            case "agent.model":
                config.Model = value;
                break;
            case "model.reasoning_effort":
            case "model.reasoningeffort":
                config.ModelReasoningEffort = value;
                break;
            case "config.base_dir":
            case "config.basedir":
                config.BaseDir = value;
                break;
            case "config.color":
                config.Color = value;
                break;
            case "config.permission_mode":
            case "config.permissionmode":
                config.PermissionMode = Enum.TryParse<AgentPermissionMode>(value, true, out var configMode) 
                    ? configMode 
                    : AgentPermissionMode.Safe;
                break;
            case "config.fork_context":
            case "config.forkcontext":
                config.ForkContext = value;
                break;

            default:
                // Store in metadata
                config.Metadata[targetKey] = value;
                break;
        }
    }

    private static void SetArrayValue(TomlAgentConfig config, string? section, string key, List<string> values)
    {
        var targetKey = section != null ? $"{section}.{key}" : key;

        switch (targetKey.ToLowerInvariant())
        {
            case "tools":
            case "agent.tools":
                config.Tools = values;
                break;
            case "disallowed_tools":
            case "disallowedtools":
            case "agent.disallowed_tools":
            case "agent.disallowedtools":
                config.DisallowedTools = values;
                break;
            case "capabilities":
            case "agent.capabilities":
                config.Capabilities = values;
                break;
            case "dependencies":
            case "agent.dependencies":
                config.Dependencies = values;
                break;
            case "skills":
            case "agent.skills":
                config.Skills = values;
                break;
            case "aliases":
            case "config.aliases":
                config.Aliases = values;
                break;

            default:
                // Store in metadata
                config.Metadata[$"{targetKey}_array"] = string.Join(",", values);
                break;
        }
    }
}

/// <summary>
/// Agent configuration model for TOML parsing.
/// </summary>
public sealed class TomlAgentConfig
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public string? Model { get; set; }
    public string? ModelReasoningEffort { get; set; } = "medium";
    public List<string> Tools { get; set; } = new();
    public List<string> DisallowedTools { get; set; } = new();
    public List<string> Capabilities { get; set; } = new();
    public List<string> Dependencies { get; set; } = new();
    public List<string> Skills { get; set; } = new();
    public List<string> Aliases { get; set; } = new();
    public string? Instructions { get; set; }
    public string? Color { get; set; }
    public string? BaseDir { get; set; }
    public string? Filename { get; set; }
    public AgentPermissionMode PermissionMode { get; set; } = AgentPermissionMode.Safe;
    public SandboxMode SandboxMode { get; set; } = SandboxMode.ReadOnly;
    public string? ForkContext { get; set; }
    public AgentSource Source { get; set; } = AgentSource.ProjectSettings;
    public AgentLocation Location { get; set; } = AgentLocation.ProjectSettings;
    public Dictionary<string, string> Metadata { get; set; } = new();

    public AgentConfig ToAgentConfig()
    {
        return new AgentConfig
        {
            AgentType = Name,
            WhenToUse = Description,
            Tools = Tools,
            DisallowedTools = DisallowedTools,
            Skills = Skills,
            SystemPrompt = Instructions ?? string.Empty,
            Source = Source,
            Location = Location,
            BaseDir = BaseDir,
            Filename = Filename,
            Color = Color,
            Model = Model,
            PermissionMode = PermissionMode,
            ForkContext = !string.IsNullOrEmpty(ForkContext)
        };
    }
}
