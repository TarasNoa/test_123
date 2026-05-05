using System.Text;
using System.Text.RegularExpressions;

namespace Libr4.AI.Infrastructure.Parsing;

/// <summary>
/// Parser for AGENTS.md files - standard context files for AI coding assistants.
/// Extracts project overview, repository structure, development setup, and code patterns.
/// </summary>
public sealed class AgentsMdParser
{
    private static readonly Regex SectionRegex = new(
        @"^##\s+(.+)$",
        RegexOptions.Multiline | RegexOptions.Compiled);
    
    private static readonly Regex SubSectionRegex = new(
        @"^###\s+(.+)$",
        RegexOptions.Multiline | RegexOptions.Compiled);
    
    private static readonly Regex TableRegex = new(
        @"\|(.+)\|\s*\r?\n\|[-:\s|]+\|\s*\r?\n((?:\|[^\n]*\|\s*\r?\n)+)",
        RegexOptions.Compiled);
    
    private static readonly Regex CodeBlockRegex = new(
        @"```(\w+)?\s*\r?\n(.*?)```",
        RegexOptions.Singleline | RegexOptions.Compiled);
    
    private static readonly Regex ListItemRegex = new(
        @"^[-*]\s+(.+)$",
        RegexOptions.Multiline | RegexOptions.Compiled);

    /// <summary>
    /// Parse AGENTS.md content into structured format.
    /// </summary>
    public AgentsMdDocument Parse(string content, string? filePath = null)
    {
        var doc = new AgentsMdDocument
        {
            SourceFile = filePath,
            ParsedAt = DateTime.UtcNow,
            RawContent = content
        };

        // Extract title (first # heading)
        var titleMatch = Regex.Match(content, @"^#\s+(.+)$", RegexOptions.Multiline);
        if (titleMatch.Success)
        {
            doc.Title = titleMatch.Groups[1].Value.Trim();
        }

        // Parse sections
        var sections = ParseSections(content);
        doc.Sections = sections;

        // Extract specific well-known sections
        doc.ProjectOverview = ExtractSection(sections, "Project Overview", "Overview");
        doc.RepositoryStructure = ExtractRepositoryStructure(sections);
        doc.DevelopmentSetup = ExtractDevelopmentSetup(sections);
        doc.BuildCommands = ExtractBuildCommands(sections);
        doc.ArchitectureDecisions = ExtractArchitectureDecisions(sections);
        doc.CodePatterns = ExtractCodePatterns(sections);

        // Extract all code examples
        doc.CodeExamples = ExtractCodeExamples(content);

        return doc;
    }

    /// <summary>
    /// Parse AGENTS.md file from disk.
    /// </summary>
    public async Task<AgentsMdDocument> ParseFileAsync(string filePath, CancellationToken ct = default)
    {
        var content = await File.ReadAllTextAsync(filePath, ct);
        return Parse(content, filePath);
    }

    /// <summary>
    /// Scan directory for AGENTS.md files (including subdirectories).
    /// </summary>
    public async Task<IReadOnlyList<AgentsMdDocument>> ScanDirectoryAsync(
        string directoryPath,
        CancellationToken ct = default)
    {
        var results = new List<AgentsMdDocument>();
        
        if (!Directory.Exists(directoryPath))
            return results;

        var files = Directory.GetFiles(directoryPath, "AGENTS.md", SearchOption.AllDirectories);
        
        foreach (var file in files)
        {
            try
            {
                var doc = await ParseFileAsync(file, ct);
                results.Add(doc);
            }
            catch (Exception ex)
            {
                // Log but continue
                System.Diagnostics.Debug.WriteLine($"Failed to parse {file}: {ex.Message}");
            }
        }

        return results.OrderBy(d => d.SourceFile).ToList();
    }

    /// <summary>
    /// Find AGENTS.md in project hierarchy (starting from path up to root).
    /// </summary>
    public async Task<AgentsMdDocument?> FindNearestAsync(
        string startPath,
        CancellationToken ct = default)
    {
        var dir = File.Exists(startPath) ? Path.GetDirectoryName(startPath) : startPath;
        
        while (!string.IsNullOrEmpty(dir))
        {
            var agentsMdPath = Path.Combine(dir, "AGENTS.md");
            if (File.Exists(agentsMdPath))
            {
                return await ParseFileAsync(agentsMdPath, ct);
            }

            var parent = Path.GetDirectoryName(dir);
            if (parent == dir) break; // At root
            dir = parent;
        }

        return null;
    }

    private List<AgentsMdSection> ParseSections(string content)
    {
        var sections = new List<AgentsMdSection>();
        var matches = SectionRegex.Matches(content);

        for (int i = 0; i < matches.Count; i++)
        {
            var match = matches[i];
            var title = match.Groups[1].Value.Trim();
            var startIndex = match.Index;
            var endIndex = i < matches.Count - 1 ? matches[i + 1].Index : content.Length;
            var sectionContent = content.Substring(startIndex, endIndex - startIndex).Trim();
            var bodyContent = sectionContent.Substring(match.Length).Trim();

            // Parse subsections
            var subsections = ParseSubSections(bodyContent);

            sections.Add(new AgentsMdSection
            {
                Title = title,
                Level = 2,
                Content = bodyContent,
                Subsections = subsections,
                Tables = ParseTables(bodyContent),
                Lists = ParseLists(bodyContent)
            });
        }

        return sections;
    }

    private List<AgentsMdSection> ParseSubSections(string content)
    {
        var subsections = new List<AgentsMdSection>();
        var matches = SubSectionRegex.Matches(content);

        for (int i = 0; i < matches.Count; i++)
        {
            var match = matches[i];
            var title = match.Groups[1].Value.Trim();
            var startIndex = match.Index;
            var endIndex = i < matches.Count - 1 ? matches[i + 1].Index : content.Length;
            var sectionContent = content.Substring(startIndex, endIndex - startIndex).Trim();
            var bodyContent = sectionContent.Substring(match.Length).Trim();

            subsections.Add(new AgentsMdSection
            {
                Title = title,
                Level = 3,
                Content = bodyContent,
                Tables = ParseTables(bodyContent),
                Lists = ParseLists(bodyContent)
            });
        }

        return subsections;
    }

    private List<AgentsMdTable> ParseTables(string content)
    {
        var tables = new List<AgentsMdTable>();
        var matches = TableRegex.Matches(content);

        foreach (Match match in matches)
        {
            var headers = match.Groups[1].Value
                .Split('|')
                .Select(h => h.Trim())
                .Where(h => !string.IsNullOrEmpty(h))
                .ToList();

            var rows = match.Groups[2].Value
                .Split('\n')
                .Where(l => l.Trim().StartsWith("|"))
                .Select(l => l.Split('|')
                    .Select(c => c.Trim())
                    .Where(c => !string.IsNullOrEmpty(c))
                    .ToList())
                .Where(r => r.Count > 0)
                .ToList();

            tables.Add(new AgentsMdTable
            {
                Headers = headers,
                Rows = rows
            });
        }

        return tables;
    }

    private List<AgentsMdList> ParseLists(string content)
    {
        var lists = new List<AgentsMdList>();
        var matches = ListItemRegex.Matches(content);

        if (matches.Count > 0)
        {
            var items = matches.Select(m => m.Groups[1].Value.Trim()).ToList();
            lists.Add(new AgentsMdList { Items = items });
        }

        return lists;
    }

    private string? ExtractSection(List<AgentsMdSection> sections, params string[] possibleTitles)
    {
        foreach (var title in possibleTitles)
        {
            var section = sections.FirstOrDefault(s => 
                s.Title.Equals(title, StringComparison.OrdinalIgnoreCase));
            if (section != null)
            {
                return section.Content;
            }
        }
        return null;
    }

    private RepositoryStructure? ExtractRepositoryStructure(List<AgentsMdSection> sections)
    {
        var section = sections.FirstOrDefault(s =>
            s.Title.Contains("Repository Structure", StringComparison.OrdinalIgnoreCase) ||
            s.Title.Contains("Structure", StringComparison.OrdinalIgnoreCase) ||
            s.Title.Contains("Directories", StringComparison.OrdinalIgnoreCase));

        if (section == null) return null;

        var structure = new RepositoryStructure();

        // Try to extract from tables
        foreach (var table in section.Tables)
        {
            if (table.Headers.Any(h => h.Contains("Directory", StringComparison.OrdinalIgnoreCase)) ||
                table.Headers.Any(h => h.Contains("Path", StringComparison.OrdinalIgnoreCase)))
            {
                foreach (var row in table.Rows)
                {
                    if (row.Count >= 2)
                    {
                        structure.Directories.Add(new DirectoryInfo
                        {
                            Path = row[0],
                            Description = row[1]
                        });
                    }
                }
            }
        }

        // Also check subsections
        foreach (var subsection in section.Subsections)
        {
            if (subsection.Title.Contains("Key Directories", StringComparison.OrdinalIgnoreCase))
            {
                foreach (var table in subsection.Tables)
                {
                    foreach (var row in table.Rows)
                    {
                        if (row.Count >= 2)
                        {
                            structure.Directories.Add(new DirectoryInfo
                            {
                                Path = row[0].Trim('`', ' '),
                                Description = row[1]
                            });
                        }
                    }
                }
            }
        }

        return structure;
    }

    private DevelopmentSetup? ExtractDevelopmentSetup(List<AgentsMdSection> sections)
    {
        var section = sections.FirstOrDefault(s =>
            s.Title.Contains("Development Setup", StringComparison.OrdinalIgnoreCase) ||
            s.Title.Contains("Setup", StringComparison.OrdinalIgnoreCase) ||
            s.Title.Contains("Requirements", StringComparison.OrdinalIgnoreCase));

        if (section == null) return null;

        var setup = new DevelopmentSetup
        {
            RawContent = section.Content
        };

        // Extract requirements from lists or text
        foreach (var list in section.Lists)
        {
            foreach (var item in list.Items)
            {
                if (item.Contains("Python", StringComparison.OrdinalIgnoreCase))
                    setup.Requirements.Add(new Requirement { Name = "Python", Version = ExtractVersion(item) });
                else if (item.Contains("Node", StringComparison.OrdinalIgnoreCase))
                    setup.Requirements.Add(new Requirement { Name = "Node.js", Version = ExtractVersion(item) });
                else if (item.Contains("Docker", StringComparison.OrdinalIgnoreCase))
                    setup.Requirements.Add(new Requirement { Name = "Docker", Version = ExtractVersion(item) });
                else
                    setup.Requirements.Add(new Requirement { Name = item, Version = null });
            }
        }

        // Look for setup commands in code blocks
        foreach (var subsection in section.Subsections)
        {
            if (subsection.Title.Contains("Initial Setup", StringComparison.OrdinalIgnoreCase) ||
                subsection.Title.Contains("Getting Started", StringComparison.OrdinalIgnoreCase))
            {
                setup.SetupCommands = ExtractCodeExamples(subsection.Content)
                    .Where(c => c.Language == "bash" || c.Language is null)
                    .Select(c => c.Code)
                    .ToList();
            }
        }

        return setup;
    }

    private List<BuildCommand> ExtractBuildCommands(List<AgentsMdSection> sections)
    {
        var commands = new List<BuildCommand>();

        var section = sections.FirstOrDefault(s =>
            s.Title.Contains("Build", StringComparison.OrdinalIgnoreCase) ||
            s.Title.Contains("Test", StringComparison.OrdinalIgnoreCase) ||
            s.Title.Contains("Commands", StringComparison.OrdinalIgnoreCase) ||
            s.Title.Contains("Scripts", StringComparison.OrdinalIgnoreCase));

        if (section == null) return commands;

        // Extract from code blocks
        var codeExamples = ExtractCodeExamples(section.Content);
        foreach (var example in codeExamples)
        {
            if (example.Language == "bash" || example.Language is null)
            {
                var lines = example.Code.Split('\n')
                    .Where(l => l.Trim().StartsWith("make ") || 
                                l.Trim().StartsWith("npm ") ||
                                l.Trim().StartsWith("pnpm ") ||
                                l.Trim().StartsWith("python ") ||
                                l.Trim().StartsWith("dotnet "))
                    .ToList();

                foreach (var line in lines)
                {
                    var parts = line.Trim().Split(new[] { ' ', '\t' }, 2);
                    if (parts.Length >= 2)
                    {
                        commands.Add(new BuildCommand
                        {
                            Tool = parts[0],
                            Command = parts[1],
                            FullCommand = line.Trim()
                        });
                    }
                }
            }
        }

        return commands;
    }

    private List<ArchitectureDecision> ExtractArchitectureDecisions(List<AgentsMdSection> sections)
    {
        var decisions = new List<ArchitectureDecision>();

        var section = sections.FirstOrDefault(s =>
            s.Title.Contains("Architecture", StringComparison.OrdinalIgnoreCase) ||
            s.Title.Contains("Decisions", StringComparison.OrdinalIgnoreCase) ||
            s.Title.Contains("ADR", StringComparison.OrdinalIgnoreCase));

        if (section == null) return decisions;

        // Parse architecture content
        foreach (var list in section.Lists)
        {
            foreach (var item in list.Items)
            {
                decisions.Add(new ArchitectureDecision
                {
                    Title = item,
                    Description = item
                });
            }
        }

        return decisions;
    }

    private List<CodePattern> ExtractCodePatterns(List<AgentsMdSection> sections)
    {
        var patterns = new List<CodePattern>();

        var section = sections.FirstOrDefault(s =>
            s.Title.Contains("Patterns", StringComparison.OrdinalIgnoreCase) ||
            s.Title.Contains("Conventions", StringComparison.OrdinalIgnoreCase) ||
            s.Title.Contains("Style", StringComparison.OrdinalIgnoreCase) ||
            s.Title.Contains("Guidelines", StringComparison.OrdinalIgnoreCase));

        if (section == null) return patterns;

        foreach (var codeExample in ExtractCodeExamples(section.Content))
        {
            patterns.Add(new CodePattern
            {
                Language = codeExample.Language ?? "text",
                Example = codeExample.Code,
                Description = codeExample.PrecedingContext?.LastOrDefault()
            });
        }

        return patterns;
    }

    private List<CodeExample> ExtractCodeExamples(string content)
    {
        var examples = new List<CodeExample>();
        var matches = CodeBlockRegex.Matches(content);

        foreach (Match match in matches)
        {
            var language = match.Groups[1].Success ? match.Groups[1].Value : null;
            var code = match.Groups[2].Value.Trim();
            
            // Extract preceding context (last 3 lines before code block)
            var precedingText = content.Substring(0, match.Index);
            var lines = precedingText.Split('\n');
            var context = lines.TakeLast(3)
                .Select(l => l.Trim())
                .Where(l => !string.IsNullOrEmpty(l) && !l.StartsWith('#'))
                .ToList();

            examples.Add(new CodeExample
            {
                Language = language,
                Code = code,
                PrecedingContext = context
            });
        }

        return examples;
    }

    private static string? ExtractVersion(string text)
    {
        // Extract version patterns like "3.9+", "v18+", "1.2.3"
        var match = Regex.Match(text, @"(\d+\.\d+(?:\.\d+)?\+?)");
        return match.Success ? match.Groups[1].Value : null;
    }
}

// Data Models

public sealed class AgentsMdDocument
{
    public string? SourceFile { get; set; }
    public DateTime ParsedAt { get; set; }
    public string? Title { get; set; }
    public string RawContent { get; set; } = "";
    
    public List<AgentsMdSection> Sections { get; set; } = new();
    public string? ProjectOverview { get; set; }
    public RepositoryStructure? RepositoryStructure { get; set; }
    public DevelopmentSetup? DevelopmentSetup { get; set; }
    public List<BuildCommand> BuildCommands { get; set; } = new();
    public List<ArchitectureDecision> ArchitectureDecisions { get; set; } = new();
    public List<CodePattern> CodePatterns { get; set; } = new();
    public List<CodeExample> CodeExamples { get; set; } = new();

    /// <summary>
    /// Generate context prompt for AI agent from this document.
    /// </summary>
    public string ToContextPrompt(int maxLength = 4000)
    {
        var sb = new StringBuilder();
        
        sb.AppendLine("# Project Context");
        sb.AppendLine();
        
        if (!string.IsNullOrEmpty(ProjectOverview))
        {
            sb.AppendLine("## Overview");
            sb.AppendLine(ProjectOverview[..Math.Min(ProjectOverview.Length, 1000)]);
            sb.AppendLine();
        }

        if (RepositoryStructure?.Directories.Count > 0)
        {
            sb.AppendLine("## Repository Structure");
            foreach (var dir in RepositoryStructure.Directories.Take(10))
            {
                sb.AppendLine($"- `{dir.Path}`: {dir.Description}");
            }
            sb.AppendLine();
        }

        if (DevelopmentSetup?.Requirements.Count > 0)
        {
            sb.AppendLine("## Requirements");
            foreach (var req in DevelopmentSetup.Requirements)
            {
                sb.AppendLine($"- {req.Name}" + (req.Version != null ? $" ({req.Version})" : ""));
            }
            sb.AppendLine();
        }

        if (BuildCommands.Count > 0)
        {
            sb.AppendLine("## Common Commands");
            foreach (var cmd in BuildCommands.Take(5))
            {
                sb.AppendLine($"- `{cmd.FullCommand}`");
            }
            sb.AppendLine();
        }

        var result = sb.ToString();
        
        // Truncate if needed
        if (result.Length > maxLength)
        {
            return result.Substring(0, maxLength) + "\n\n... (truncated)";
        }

        return result;
    }
}

public sealed class AgentsMdSection
{
    public string Title { get; set; } = "";
    public int Level { get; set; } // 2 for ##, 3 for ###
    public string Content { get; set; } = "";
    public List<AgentsMdSection> Subsections { get; set; } = new();
    public List<AgentsMdTable> Tables { get; set; } = new();
    public List<AgentsMdList> Lists { get; set; } = new();
}

public sealed class AgentsMdTable
{
    public List<string> Headers { get; set; } = new();
    public List<List<string>> Rows { get; set; } = new();
}

public sealed class AgentsMdList
{
    public List<string> Items { get; set; } = new();
}

public sealed class RepositoryStructure
{
    public List<DirectoryInfo> Directories { get; set; } = new();
}

public sealed class DirectoryInfo
{
    public string Path { get; set; } = "";
    public string Description { get; set; } = "";
}

public sealed class DevelopmentSetup
{
    public string? RawContent { get; set; }
    public List<Requirement> Requirements { get; set; } = new();
    public List<string> SetupCommands { get; set; } = new();
}

public sealed class Requirement
{
    public string Name { get; set; } = "";
    public string? Version { get; set; }
}

public sealed class BuildCommand
{
    public string Tool { get; set; } = "";
    public string Command { get; set; } = "";
    public string FullCommand { get; set; } = "";
}

public sealed class ArchitectureDecision
{
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
}

public sealed class CodePattern
{
    public string Language { get; set; } = "";
    public string Example { get; set; } = "";
    public string? Description { get; set; }
}

public sealed class CodeExample
{
    public string? Language { get; set; }
    public string Code { get; set; } = "";
    public List<string>? PrecedingContext { get; set; }
}
