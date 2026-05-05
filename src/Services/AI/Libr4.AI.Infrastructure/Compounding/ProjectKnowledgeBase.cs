using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace Libr4.AI.Infrastructure.Compounding;

/// <summary>
/// File-based project knowledge base
/// Stores knowledge in .knowledge/ directory next to the project
/// </summary>
public class ProjectKnowledgeBase : IProjectKnowledgeBase
{
    private readonly ILogger<ProjectKnowledgeBase> _logger;
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public ProjectKnowledgeBase(ILogger<ProjectKnowledgeBase> logger)
    {
        _logger = logger;
    }

    public async Task<string> GetProjectKnowledgeAsync(string projectPath)
    {
        var knowledgePath = GetKnowledgePath(projectPath);
        if (!Directory.Exists(knowledgePath))
        {
            return string.Empty;
        }

        var knowledgeFile = Path.Combine(knowledgePath, "knowledge.json");
        if (!File.Exists(knowledgeFile))
        {
            return string.Empty;
        }

        var json = await File.ReadAllTextAsync(knowledgeFile);
        return json;
    }

    public async Task AddLessonAsync(string projectPath, KnowledgeLesson lesson)
    {
        var knowledge = await LoadKnowledgeAsync(projectPath);
        knowledge.Lessons.Add(lesson);
        await SaveKnowledgeAsync(projectPath, knowledge);
        _logger.LogInformation("Added lesson: {Title}", lesson.Title);
    }

    public async Task AddConventionAsync(string projectPath, ProjectConvention convention)
    {
        var knowledge = await LoadKnowledgeAsync(projectPath);
        
        // Update existing or add new
        var existing = knowledge.Conventions.FirstOrDefault(c => c.Name == convention.Name);
        if (existing != null)
        {
            knowledge.Conventions.Remove(existing);
        }
        knowledge.Conventions.Add(convention);
        
        await SaveKnowledgeAsync(projectPath, knowledge);
        _logger.LogInformation("Added convention: {Name}", convention.Name);
    }

    public async Task AddAntiPatternAsync(string projectPath, AntiPattern antiPattern)
    {
        var knowledge = await LoadKnowledgeAsync(projectPath);
        
        var existing = knowledge.AntiPatterns.FirstOrDefault(a => a.Name == antiPattern.Name);
        if (existing != null)
        {
            knowledge.AntiPatterns.Remove(existing);
        }
        knowledge.AntiPatterns.Add(antiPattern);
        
        await SaveKnowledgeAsync(projectPath, knowledge);
        _logger.LogInformation("Added anti-pattern: {Name}", antiPattern.Name);
    }

    public async Task<string> GetFormattedKnowledgeAsync(string projectPath)
    {
        var knowledge = await LoadKnowledgeAsync(projectPath);
        
        var md = new System.Text.StringBuilder();
        
        md.AppendLine("# Project Knowledge Base");
        md.AppendLine();
        
        // Conventions
        if (knowledge.Conventions.Any())
        {
            md.AppendLine("## Project Conventions");
            md.AppendLine();
            
            foreach (var conv in knowledge.Conventions.Where(c => c.IsRequired))
            {
                md.AppendLine($"### {conv.Name} (Required)");
                md.AppendLine(conv.Description);
                if (!string.IsNullOrEmpty(conv.Example))
                {
                    md.AppendLine($"```");
                    md.AppendLine(conv.Example);
                    md.AppendLine($"```");
                }
                md.AppendLine();
            }
            
            foreach (var conv in knowledge.Conventions.Where(c => !c.IsRequired))
            {
                md.AppendLine($"### {conv.Name}");
                md.AppendLine(conv.Description);
                md.AppendLine();
            }
        }
        
        // Anti-patterns
        if (knowledge.AntiPatterns.Any())
        {
            md.AppendLine("## Anti-Patterns (Do NOT use)");
            md.AppendLine();
            
            foreach (var anti in knowledge.AntiPatterns)
            {
                md.AppendLine($"### {anti.Name}");
                md.AppendLine(anti.Description);
                md.AppendLine($"**Why avoid:** {anti.WhyAvoid}");
                if (!string.IsNullOrEmpty(anti.Alternative))
                {
                    md.AppendLine($"**Alternative:** {anti.Alternative}");
                }
                md.AppendLine();
            }
        }
        
        // Learned lessons
        if (knowledge.Lessons.Any())
        {
            md.AppendLine("## Learned Lessons");
            md.AppendLine();
            
            foreach (var lesson in knowledge.Lessons.OrderByDescending(l => l.LearnedAt).Take(20))
            {
                md.AppendLine($"### {lesson.Title} ({lesson.LearnedAt:yyyy-MM-dd})");
                md.AppendLine($"**Problem:** {lesson.Problem}");
                md.AppendLine($"**Solution:** {lesson.Solution}");
                if (!string.IsNullOrEmpty(lesson.CodeExample))
                {
                    md.AppendLine($"```");
                    md.AppendLine(lesson.CodeExample);
                    md.AppendLine($"```");
                }
                if (lesson.Tags.Any())
                {
                    md.AppendLine($"Tags: {string.Join(", ", lesson.Tags)}");
                }
                md.AppendLine();
            }
        }
        
        return md.ToString();
    }

    public async Task<string?> GetDesignAsync(string projectPath)
    {
        var designPath = Path.Combine(projectPath, "DESIGN.md");
        
        if (!File.Exists(designPath))
            return null;
        
        return await File.ReadAllTextAsync(designPath);
    }

    public async Task SetDesignAsync(string projectPath, string content)
    {
        var designPath = Path.Combine(projectPath, "DESIGN.md");
        await File.WriteAllTextAsync(designPath, content);
        _logger.LogInformation("Created/updated DESIGN.md for project");
    }

    private async Task<ProjectKnowledge> LoadKnowledgeAsync(string projectPath)
    {
        var knowledgePath = GetKnowledgePath(projectPath);
        Directory.CreateDirectory(knowledgePath);
        
        var knowledgeFile = Path.Combine(knowledgePath, "knowledge.json");
        if (!File.Exists(knowledgeFile))
        {
            return new ProjectKnowledge();
        }
        
        var json = await File.ReadAllTextAsync(knowledgeFile);
        return JsonSerializer.Deserialize<ProjectKnowledge>(json, _jsonOptions)
            ?? new ProjectKnowledge();
    }

    private async Task SaveKnowledgeAsync(string projectPath, ProjectKnowledge knowledge)
    {
        var knowledgePath = GetKnowledgePath(projectPath);
        Directory.CreateDirectory(knowledgePath);
        
        var knowledgeFile = Path.Combine(knowledgePath, "knowledge.json");
        var json = JsonSerializer.Serialize(knowledge, _jsonOptions);
        await File.WriteAllTextAsync(knowledgeFile, json);
    }

    private string GetKnowledgePath(string projectPath)
    {
        return Path.Combine(projectPath, ".knowledge");
    }
}

public class ProjectKnowledge
{
    public List<ProjectConvention> Conventions { get; set; } = new();
    public List<AntiPattern> AntiPatterns { get; set; } = new();
    public List<KnowledgeLesson> Lessons { get; set; } = new();
}
