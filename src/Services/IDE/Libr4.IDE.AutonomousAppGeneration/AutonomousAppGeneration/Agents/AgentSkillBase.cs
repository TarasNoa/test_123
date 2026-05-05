namespace Libr4.IDE.AutonomousAppGeneration.Agents;

/// <summary>
/// Base class for skill-based agents with SKILL.md support
/// Inspired by claude-skills framework (SKILL.md structure)
/// </summary>
public abstract class AgentSkillBase : IAgent
{
    protected string SkillName { get; }
    protected string SkillDescription { get; }
    protected string Version { get; }
    protected string[] AllowedTools { get; }
    protected SkillMetadata Metadata { get; }

    protected AgentSkillBase(string skillPath)
    {
        if (!File.Exists(skillPath))
        {
            throw new FileNotFoundException($"Skill file not found: {skillPath}");
        }

        var skillContent = File.ReadAllText(skillPath);
        Metadata = SkillParser.Parse(skillContent);
        
        SkillName = Metadata.Name;
        SkillDescription = Metadata.Description;
        Version = Metadata.Version;
        AllowedTools = Metadata.AllowedTools;
    }

    /// <summary>
    /// Execute the agent with the given context
    /// </summary>
    public abstract Task<AgentResult> ExecuteAsync(AgentContext context);

    /// <summary>
    /// Get the skill instructions for LLM context
    /// </summary>
    protected string GetSkillInstructions()
    {
        return Metadata.Instructions;
    }

    /// <summary>
    /// Check if a tool is allowed for this skill
    /// </summary>
    protected bool IsToolAllowed(string toolName)
    {
        return AllowedTools.Contains(toolName, StringComparer.OrdinalIgnoreCase);
    }
}

/// <summary>
/// Skill metadata parsed from SKILL.md frontmatter
/// </summary>
public class SkillMetadata
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Version { get; set; } = "1.0.0";
    public string[] AllowedTools { get; set; } = Array.Empty<string>();
    public string Instructions { get; set; } = string.Empty;
    public Dictionary<string, string> AdditionalMetadata { get; set; } = new();
}

/// <summary>
/// Parser for SKILL.md files with frontmatter
/// </summary>
public static class SkillParser
{
    public static SkillMetadata Parse(string skillContent)
    {
        var lines = skillContent.Split('\n');
        var metadata = new SkillMetadata();
        var inFrontmatter = false;
        var inInstructions = false;
        var instructionsBuilder = new System.Text.StringBuilder();

        foreach (var line in lines)
        {
            if (line.Trim() == "---")
            {
                if (!inFrontmatter)
                {
                    inFrontmatter = true;
                    continue;
                }
                else
                {
                    inFrontmatter = false;
                    inInstructions = true;
                    continue;
                }
            }

            if (inFrontmatter)
            {
                ParseFrontmatterLine(line, metadata);
            }
            else if (inInstructions)
            {
                instructionsBuilder.AppendLine(line);
            }
        }

        metadata.Instructions = instructionsBuilder.ToString();
        return metadata;
    }

    private static void ParseFrontmatterLine(string line, SkillMetadata metadata)
    {
        var trimmed = line.Trim();
        if (string.IsNullOrEmpty(trimmed) || !trimmed.Contains(':'))
            return;

        var parts = trimmed.Split(':', 2);
        if (parts.Length != 2) return;

        var key = parts[0].Trim();
        var value = parts[1].Trim();

        switch (key.ToLower())
        {
            case "name":
                metadata.Name = value;
                break;
            case "description":
                metadata.Description = value;
                break;
            case "version":
                metadata.Version = value;
                break;
            case "allowed-tools":
                metadata.AllowedTools = value
                    .Trim('[', ']', ' ')
                    .Split(',')
                    .Select(t => t.Trim().Trim('"'))
                    .ToArray();
                break;
            default:
                metadata.AdditionalMetadata[key] = value;
                break;
        }
    }
}
