using Libr4.AI.Infrastructure.AI;
using Microsoft.Extensions.Logging;

namespace Libr4.IDE.AutonomousAppGeneration.Agents;

/// <summary>
/// Database design agent with schema analysis, ERD generation, and index optimization
/// Inspired by claude-skills database-designer skill
/// </summary>
public class DatabaseDesignAgent : AgentSkillBase
{
    private readonly IAIService _aiService;
    private readonly ILogger _logger;

    public DatabaseDesignAgent(
        string skillPath,
        IAIService aiService,
        ILogger logger) : base(skillPath)
    {
        _aiService = aiService;
        _logger = logger;
    }

    public override async Task<AgentResult> ExecuteAsync(AgentContext context)
    {
        _logger.LogInformation("Executing DatabaseDesignAgent for application: {ApplicationName}", context.ApplicationName);

        var skillInstructions = GetSkillInstructions();
        var prompt = BuildPrompt(context, skillInstructions);

        var response = await _aiService.GenerateCompletionAsync(prompt, skillInstructions);

        var design = ParseDatabaseDesign(response);

        _logger.LogInformation("Database design completed with {TableCount} tables", design.Tables.Count);

        return new AgentResult
        {
            IsSuccess = true,
            DatabaseDesign = design,
            Content = response
        };
    }

    private string BuildPrompt(AgentContext context, string skillInstructions)
    {
        var sb = new System.Text.StringBuilder();
        
        sb.AppendLine("Design a database schema for the following application:");
        sb.AppendLine();
        sb.AppendLine($"Application Name: {context.ApplicationName}");
        sb.AppendLine($"Description: {context.Description}");
        sb.AppendLine($"Tech Stack: {context.TechStack}");
        sb.AppendLine();
        
        if (context.GeneratedFiles != null && context.GeneratedFiles.Any())
        {
            sb.AppendLine("Existing domain models:");
            foreach (var file in context.GeneratedFiles.Where(f => 
                f.RelativePath.Contains("Model") || 
                f.RelativePath.Contains("Entity") ||
                f.RelativePath.Contains("Domain")))
            {
                sb.AppendLine($"- {file.RelativePath}");
                sb.AppendLine(file.Content.Substring(0, Math.Min(500, file.Content.Length)));
                sb.AppendLine();
            }
        }

        sb.AppendLine();
        sb.AppendLine("Please provide:");
        sb.AppendLine("1. Complete database schema with tables, columns, data types, and constraints");
        sb.AppendLine("2. Entity relationships (one-to-one, one-to-many, many-to-many)");
        sb.AppendLine("3. Recommended indexes for query optimization");
        sb.AppendLine("4. ERD description or Mermaid diagram");
        sb.AppendLine("5. Migration considerations");

        return sb.ToString();
    }

    private DatabaseDesign ParseDatabaseDesign(string content)
    {
        // Parse the AI response into structured database design
        var design = new DatabaseDesign
        {
            Tables = new List<DatabaseTable>(),
            Relationships = new List<DatabaseRelationship>(),
            Indexes = new List<DatabaseIndex>()
        };

        // Simple parsing - in production, use more sophisticated parser
        var lines = content.Split('\n');
        var currentTable = new DatabaseTable();

        foreach (var line in lines)
        {
            var trimmed = line.Trim();
            
            if (trimmed.StartsWith("CREATE TABLE", StringComparison.OrdinalIgnoreCase))
            {
                if (!string.IsNullOrEmpty(currentTable.Name))
                {
                    design.Tables.Add(currentTable);
                }
                currentTable = new DatabaseTable();
                // Extract table name
                var match = System.Text.RegularExpressions.Regex.Match(trimmed, @"CREATE TABLE\s+(\w+)", 
                    System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                if (match.Success)
                {
                    currentTable.Name = match.Groups[1].Value;
                }
            }
            else if (trimmed.StartsWith("PRIMARY KEY", StringComparison.OrdinalIgnoreCase))
            {
                currentTable.PrimaryKey = trimmed;
            }
            else if (trimmed.Contains("FOREIGN KEY", StringComparison.OrdinalIgnoreCase))
            {
                var relationship = new DatabaseRelationship
                {
                    Description = trimmed
                };
                design.Relationships.Add(relationship);
            }
            else if (trimmed.Contains("INDEX", StringComparison.OrdinalIgnoreCase) || 
                     trimmed.Contains("CREATE INDEX", StringComparison.OrdinalIgnoreCase))
            {
                var index = new DatabaseIndex
                {
                    Definition = trimmed
                };
                design.Indexes.Add(index);
            }
        }

        if (!string.IsNullOrEmpty(currentTable.Name))
        {
            design.Tables.Add(currentTable);
        }

        return design;
    }
}

/// <summary>
/// Database design structure
/// </summary>
public class DatabaseDesign
{
    public List<DatabaseTable> Tables { get; set; } = new();
    public List<DatabaseRelationship> Relationships { get; set; } = new();
    public List<DatabaseIndex> Indexes { get; set; } = new();
    public string? ERDDiagram { get; set; }
    public List<string> MigrationNotes { get; set; } = new();
}

/// <summary>
/// Database table definition
/// </summary>
public class DatabaseTable
{
    public string Name { get; set; } = string.Empty;
    public List<DatabaseColumn> Columns { get; set; } = new();
    public string? PrimaryKey { get; set; }
    public string? Description { get; set; }
}

/// <summary>
/// Database column definition
/// </summary>
public class DatabaseColumn
{
    public string Name { get; set; } = string.Empty;
    public string DataType { get; set; } = string.Empty;
    public bool IsNullable { get; set; }
    public bool IsPrimaryKey { get; set; }
    public bool IsForeignKey { get; set; }
    public string? DefaultValue { get; set; }
    public string? Description { get; set; }
}

/// <summary>
/// Database relationship definition
/// </summary>
public class DatabaseRelationship
{
    public string FromTable { get; set; } = string.Empty;
    public string ToTable { get; set; } = string.Empty;
    public string? Type { get; set; } // one-to-one, one-to-many, many-to-many
    public string? ForeignKeyColumn { get; set; }
    public string? Description { get; set; }
}

/// <summary>
/// Database index definition
/// </summary>
public class DatabaseIndex
{
    public string Name { get; set; } = string.Empty;
    public string TableName { get; set; } = string.Empty;
    public List<string> Columns { get; set; } = new();
    public bool IsUnique { get; set; }
    public string? Definition { get; set; }
    public string? Reason { get; set; }
}
