using Microsoft.Extensions.Logging;

namespace Libr4.AI.Infrastructure.Workbench;

/// <summary>
/// Implementation of workbench manager for cross-project knowledge
/// Structure:
/// workbench/
/// ├── projects/<name>/ - project-specific docs
/// ├── domains/ - cross-project knowledge
/// │   ├── platform/
/// │   ├── infrastructure/
/// │   └── engineering/
/// ├── tools/ - validation scripts
/// ├── workspaces/ - .code-workspace files
/// └── .cursor/rules/ - global cursor rules
/// </summary>
public class WorkbenchManager : IWorkbenchManager
{
    private readonly ILogger<WorkbenchManager> _logger;
    private readonly string _workbenchPath;

    public WorkbenchManager(ILogger<WorkbenchManager> logger)
    {
        _logger = logger;
        _workbenchPath = Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..", "..", "..", "..", "workbench");
    }

    public string GetWorkbenchPath()
    {
        return Path.GetFullPath(_workbenchPath);
    }

    public async Task InitializeAsync()
    {
        var fullPath = GetWorkbenchPath();
        
        // Create directory structure
        var directories = new[]
        {
            Path.Combine(fullPath, "projects"),
            Path.Combine(fullPath, "domains", "platform"),
            Path.Combine(fullPath, "domains", "infrastructure"),
            Path.Combine(fullPath, "domains", "engineering"),
            Path.Combine(fullPath, "tools"),
            Path.Combine(fullPath, "workspaces"),
            Path.Combine(fullPath, ".cursor", "rules"),
            Path.Combine(fullPath, "skills")
        };

        foreach (var dir in directories)
        {
            Directory.CreateDirectory(dir);
        }

        // Create README
        var readmePath = Path.Combine(fullPath, "README.md");
        if (!File.Exists(readmePath))
        {
            var readme = @"# Workbench

Cross-project knowledge base for LLM agents.

## Structure

- `projects/<name>/` - Project-specific documentation
- `domains/` - Cross-project knowledge
  - `platform/` - Registry, topology, CI, decisions
  - `infrastructure/` - Hardware, models, incidents
  - `engineering/` - Development practices
- `tools/` - Validation scripts
- `workspaces/` - .code-workspace files
- `.cursor/rules/` - Global cursor rules

## Usage

This workbench should be added as a second root in your IDE workspace.
";
            await File.WriteAllTextAsync(readmePath, readme);
        }

        // Create example cursor rules
        var cursorRulesPath = Path.Combine(fullPath, ".cursor", "rules", "global.md");
        if (!File.Exists(cursorRulesPath))
        {
            var rules = @"# Global Cursor Rules

These rules apply across all projects in the workbench.

## Code Style
- Use C# conventions for C# projects
- Use snake_case for database fields
- Use camelCase for JavaScript/TypeScript

## Architecture
- Prefer dependency injection
- Keep services stateless when possible
- Use async/await for I/O operations

## Documentation
- Document public APIs
- Keep ADRs for architectural decisions
- Update README when adding new features
";
            await File.WriteAllTextAsync(cursorRulesPath, rules);
        }

        _logger.LogInformation("Workbench initialized at {Path}", fullPath);
    }

    public async Task<string> GetProjectDocAsync(string projectName, string docName)
    {
        var docPath = Path.Combine(GetWorkbenchPath(), "projects", projectName, $"{docName}.md");
        
        if (!File.Exists(docPath))
            return string.Empty;
        
        return await File.ReadAllTextAsync(docPath);
    }

    public async Task<string> GetDomainDocAsync(string domainName, string docName)
    {
        var docPath = Path.Combine(GetWorkbenchPath(), "domains", domainName, $"{docName}.md");
        
        if (!File.Exists(docPath))
            return string.Empty;
        
        return await File.ReadAllTextAsync(docPath);
    }

    public async Task<string> GetADRAsync(string adrId)
    {
        var adrPath = Path.Combine(GetWorkbenchPath(), "domains", "platform", "adr", $"{adrId}.md");
        
        if (!File.Exists(adrPath))
            return string.Empty;
        
        return await File.ReadAllTextAsync(adrPath);
    }

    public async Task<string> GetContextAsync(string? currentProject = null)
    {
        var context = new System.Text.StringBuilder();
        var workbenchPath = GetWorkbenchPath();

        context.AppendLine("# WORKBENCH CONTEXT");
        context.AppendLine();

        // Global cursor rules
        var cursorRulesPath = Path.Combine(workbenchPath, ".cursor", "rules", "global.md");
        if (File.Exists(cursorRulesPath))
        {
            context.AppendLine("## Global Rules");
            context.AppendLine(await File.ReadAllTextAsync(cursorRulesPath));
            context.AppendLine();
        }

        // Platform documentation
        var platformPath = Path.Combine(workbenchPath, "domains", "platform");
        if (Directory.Exists(platformPath))
        {
            var files = Directory.GetFiles(platformPath, "*.md", SearchOption.AllDirectories);
            if (files.Any())
            {
                context.AppendLine("## Platform Knowledge");
                foreach (var file in files.Take(5)) // Limit to prevent context overflow
                {
                    context.AppendLine($"### {Path.GetFileNameWithoutExtension(file)}");
                    context.AppendLine(await File.ReadAllTextAsync(file));
                    context.AppendLine();
                }
            }
        }

        // Current project documentation
        if (!string.IsNullOrEmpty(currentProject))
        {
            var projectPath = Path.Combine(workbenchPath, "projects", currentProject);
            if (Directory.Exists(projectPath))
            {
                var files = Directory.GetFiles(projectPath, "*.md");
                if (files.Any())
                {
                    context.AppendLine($"## Project: {currentProject}");
                    foreach (var file in files)
                    {
                        context.AppendLine($"### {Path.GetFileNameWithoutExtension(file)}");
                        context.AppendLine(await File.ReadAllTextAsync(file));
                        context.AppendLine();
                    }
                }
            }
        }

        return context.ToString();
    }

    public async Task SetProjectDocAsync(string projectName, string docName, string content)
    {
        var projectPath = Path.Combine(GetWorkbenchPath(), "projects", projectName);
        Directory.CreateDirectory(projectPath);
        
        var docPath = Path.Combine(projectPath, $"{docName}.md");
        await File.WriteAllTextAsync(docPath, content);
        
        _logger.LogInformation("Created project doc: {Project}/{Doc}", projectName, docName);
    }

    public async Task SetDomainDocAsync(string domainName, string docName, string content)
    {
        var domainPath = Path.Combine(GetWorkbenchPath(), "domains", domainName);
        Directory.CreateDirectory(domainPath);
        
        var docPath = Path.Combine(domainPath, $"{docName}.md");
        await File.WriteAllTextAsync(docPath, content);
        
        _logger.LogInformation("Created domain doc: {Domain}/{Doc}", domainName, docName);
    }

    public async Task CreateADRAsync(string title, string status, string context, string decision, string consequences)
    {
        var adrPath = Path.Combine(GetWorkbenchPath(), "domains", "platform", "adr");
        Directory.CreateDirectory(adrPath);
        
        var adrId = DateTime.Now.ToString("yyyyMMdd-HHmmss");
        var adrContent = $@"# ADR-{adrId}: {title}

## Status
{status}

## Context
{context}

## Decision
{decision}

## Consequences
{consequences}
";
        
        var filePath = Path.Combine(adrPath, $"{adrId}.md");
        await File.WriteAllTextAsync(filePath, adrContent);
        
        _logger.LogInformation("Created ADR: {Id} - {Title}", adrId, title);
    }

    public async Task<string?> GetSkillAsync(string skillName)
    {
        var skillPath = Path.Combine(GetWorkbenchPath(), "skills", $"{skillName}.md");
        
        if (!File.Exists(skillPath))
            return null;
        
        return await File.ReadAllTextAsync(skillPath);
    }

    public async Task<List<string>> ListSkillsAsync()
    {
        var skillsPath = Path.Combine(GetWorkbenchPath(), "skills");
        
        if (!Directory.Exists(skillsPath))
            return new List<string>();
        
        var files = Directory.GetFiles(skillsPath, "*.md");
        return files.Select(f => Path.GetFileNameWithoutExtension(f)).ToList();
    }

    public async Task SetSkillAsync(string skillName, string content)
    {
        var skillsPath = Path.Combine(GetWorkbenchPath(), "skills");
        Directory.CreateDirectory(skillsPath);
        
        var skillPath = Path.Combine(skillsPath, $"{skillName}.md");
        await File.WriteAllTextAsync(skillPath, content);
        
        _logger.LogInformation("Created/updated skill: {Skill}", skillName);
    }
}
