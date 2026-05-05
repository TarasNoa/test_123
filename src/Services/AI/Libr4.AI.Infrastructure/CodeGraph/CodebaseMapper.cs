using Microsoft.Extensions.Logging;

namespace Libr4.AI.Infrastructure.CodeGraph;

/// <summary>
/// Implementation of codebase mapper
/// </summary>
public class CodebaseMapper : ICodebaseMapper
{
    private readonly ILogger<CodebaseMapper> _logger;
    private readonly CodeExtractor _codeExtractor;

    public CodebaseMapper(ILogger<CodebaseMapper> logger, CodeExtractor codeExtractor)
    {
        _logger = logger;
        _codeExtractor = codeExtractor;
    }

    public async Task<CodebaseMap> GenerateMapAsync(string projectPath, CancellationToken cancellationToken = default)
    {
        var map = new CodebaseMap { ProjectPath = projectPath };
        
        try
        {
            // Scan directories
            var directories = Directory.GetDirectories(projectPath, "*", SearchOption.AllDirectories)
                .Where(d => !IsExcludedDirectory(d))
                .ToList();

            foreach (var dir in directories)
            {
                var relativePath = Path.GetRelativePath(projectPath, dir);
                map.Directories.Add(new CodeDirectory
                {
                    Path = relativePath,
                    Files = Directory.GetFiles(dir).Select(f => Path.GetFileName(f)).ToList()
                });
            }

            // Scan files
            var files = Directory.GetFiles(projectPath, "*", SearchOption.AllDirectories)
                .Where(f => !IsExcludedFile(f) && IsCodeFile(f))
                .ToList();

            foreach (var file in files)
            {
                var relativePath = Path.GetRelativePath(projectPath, file);
                var language = DetectLanguage(file);
                
                try
                {
                    var content = await File.ReadAllTextAsync(file, cancellationToken);
                    var imports = ExtractImports(content, language);
                    var exports = ExtractExports(content, language);
                    
                    map.Files.Add(new CodeFile
                    {
                        Path = relativePath,
                        Language = language,
                        LineCount = content.Split('\n').Length,
                        Imports = imports,
                        Exports = exports
                    });
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to analyze file {File}", file);
                }
            }

            // Build dependency graph
            BuildDependencyGraph(map);

            map.GeneratedAt = DateTimeOffset.UtcNow;
            _logger.LogInformation("Generated codebase map with {FileCount} files", map.Files.Count);
            
            return map;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate codebase map");
            throw;
        }
    }

    public async Task<string> GetFormattedMapAsync(string projectPath, CancellationToken cancellationToken = default)
    {
        var map = await GenerateMapAsync(projectPath, cancellationToken);
        var formatted = new System.Text.StringBuilder();

        formatted.AppendLine("=== CODEBASE MAP ===");
        formatted.AppendLine($"Project: {projectPath}");
        formatted.AppendLine($"Files: {map.Files.Count}");
        formatted.AppendLine($"Directories: {map.Directories.Count}");
        formatted.AppendLine();

        formatted.AppendLine("=== STRUCTURE ===");
        foreach (var dir in map.Directories.OrderBy(d => d.Path))
        {
            formatted.AppendLine($"📁 {dir.Path}/");
            foreach (var file in dir.Files)
            {
                formatted.AppendLine($"  📄 {file}");
            }
        }

        formatted.AppendLine();
        formatted.AppendLine("=== FILES ===");
        foreach (var file in map.Files.OrderBy(f => f.Path))
        {
            formatted.AppendLine($"📄 {file.Path} ({file.Language})");
            if (file.Imports.Any())
                formatted.AppendLine($"   Imports: {string.Join(", ", file.Imports)}");
            if (file.Exports.Any())
                formatted.AppendLine($"   Exports: {string.Join(", ", file.Exports)}");
        }

        formatted.AppendLine();
        formatted.AppendLine("=== DEPENDENCIES ===");
        foreach (var dep in map.Dependencies)
        {
            formatted.AppendLine($"{dep.Key} → {string.Join(", ", dep.Value)}");
        }

        return formatted.ToString();
    }

    public async Task<CodebaseMap> GetPartialMapAsync(string projectPath, List<string> paths, CancellationToken cancellationToken = default)
    {
        var fullMap = await GenerateMapAsync(projectPath, cancellationToken);
        var partialMap = new CodebaseMap
        {
            ProjectPath = projectPath,
            GeneratedAt = fullMap.GeneratedAt
        };

        // Filter files based on paths
        foreach (var path in paths)
        {
            var matchingFiles = fullMap.Files.Where(f => f.Path.StartsWith(path, StringComparison.OrdinalIgnoreCase)).ToList();
            partialMap.Files.AddRange(matchingFiles);

            var matchingDirs = fullMap.Directories.Where(d => d.Path.StartsWith(path, StringComparison.OrdinalIgnoreCase)).ToList();
            partialMap.Directories.AddRange(matchingDirs);
        }

        // Rebuild dependencies for partial map
        BuildDependencyGraph(partialMap);

        return partialMap;
    }

    private void BuildDependencyGraph(CodebaseMap map)
    {
        map.Dependencies = new Dictionary<string, List<string>>();

        foreach (var file in map.Files)
        {
            var dependencies = new List<string>();
            
            foreach (var import in file.Imports)
            {
                // Find files that export this import
                var providers = map.Files
                    .Where(f => f.Exports.Any(e => e.Equals(import, StringComparison.OrdinalIgnoreCase)))
                    .Select(f => f.Path)
                    .ToList();
                
                dependencies.AddRange(providers);
            }

            if (dependencies.Any())
            {
                map.Dependencies[file.Path] = dependencies;
            }
        }
    }

    private bool IsExcludedDirectory(string dir)
    {
        var excluded = new[] { "bin", "obj", "node_modules", ".git", "dist", "build", ".vs", ".idea" };
        var dirName = new DirectoryInfo(dir).Name;
        return excluded.Any(e => dirName.Equals(e, StringComparison.OrdinalIgnoreCase));
    }

    private bool IsExcludedFile(string file)
    {
        var excluded = new[] { ".dll", ".exe", ".pdb", ".json", ".xml", ".md", ".txt", ".gitignore", ".gitattributes" };
        return excluded.Any(e => file.EndsWith(e, StringComparison.OrdinalIgnoreCase));
    }

    private bool IsCodeFile(string file)
    {
        var extensions = new[] { ".cs", ".ts", ".js", ".py", ".java", ".go", ".rs", ".cpp", ".c", ".h", ".jsx", ".tsx", ".vue", ".php", ".rb" };
        return extensions.Any(e => file.EndsWith(e, StringComparison.OrdinalIgnoreCase));
    }

    private string DetectLanguage(string file)
    {
        var ext = Path.GetExtension(file).ToLowerInvariant();
        return ext switch
        {
            ".cs" => "C#",
            ".ts" => "TypeScript",
            ".js" => "JavaScript",
            ".jsx" => "React",
            ".tsx" => "React TypeScript",
            ".vue" => "Vue",
            ".py" => "Python",
            ".java" => "Java",
            ".go" => "Go",
            ".rs" => "Rust",
            ".cpp" or ".c" or ".h" => "C++",
            ".php" => "PHP",
            ".rb" => "Ruby",
            _ => "Unknown"
        };
    }

    private List<string> ExtractImports(string content, string language)
    {
        var imports = new List<string>();
        
        // Simple extraction based on language
        if (language == "C#")
        {
            var matches = System.Text.RegularExpressions.Regex.Matches(content, @"using\s+([\w.]+);");
            imports.AddRange(matches.Select(m => m.Groups[1].Value));
        }
        else if (language == "TypeScript" || language == "JavaScript" || language == "React" || language == "React TypeScript")
        {
            var matches = System.Text.RegularExpressions.Regex.Matches(content, @"import\s+.*from\s+['""]([^'""]+)['""]");
            imports.AddRange(matches.Select(m => m.Groups[1].Value));
        }
        else if (language == "Python")
        {
            var matches = System.Text.RegularExpressions.Regex.Matches(content, @"import\s+([\w.]+)");
            imports.AddRange(matches.Select(m => m.Groups[1].Value));
        }

        return imports.Distinct().ToList();
    }

    private List<string> ExtractExports(string content, string language)
    {
        var exports = new List<string>();
        
        // Simple extraction based on language
        if (language == "C#")
        {
            var matches = System.Text.RegularExpressions.Regex.Matches(content, @"public\s+(?:class|interface|struct|enum)\s+(\w+)");
            exports.AddRange(matches.Select(m => m.Groups[1].Value));
        }
        else if (language == "TypeScript" || language == "JavaScript")
        {
            var matches = System.Text.RegularExpressions.Regex.Matches(content, @"export\s+(?:default\s+)?(?:class|function|const|let)\s+(\w+)");
            exports.AddRange(matches.Select(m => m.Groups[1].Value));
        }

        return exports.Distinct().ToList();
    }
}
