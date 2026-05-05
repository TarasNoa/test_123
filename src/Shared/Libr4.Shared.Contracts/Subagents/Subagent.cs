namespace Libr4.Shared.Contracts.Subagents;

/// <summary>
/// Represents a specialized subagent definition.
/// Based on awesome-codex-subagents .toml format and Kode-Agent YAML format.
/// </summary>
public record SubagentDefinition
{
    /// <summary>
    /// Unique identifier for the subagent.
    /// </summary>
    public string Id { get; init; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Subagent name.
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// Description of when this agent should be invoked.
    /// </summary>
    public string Description { get; init; } = string.Empty;

    /// <summary>
    /// Model to use for this subagent.
    /// </summary>
    public string Model { get; init; } = string.Empty;

    /// <summary>
    /// Reasoning effort level (low, medium, high).
    /// </summary>
    public string ModelReasoningEffort { get; init; } = "medium";

    /// <summary>
    /// Sandbox mode (read-only, read-write, full-access).
    /// </summary>
    public SandboxMode SandboxMode { get; init; } = SandboxMode.ReadOnly;

    /// <summary>
    /// Instructions for the subagent.
    /// </summary>
    public string Instructions { get; init; } = string.Empty;

    /// <summary>
    /// Category of the subagent.
    /// </summary>
    public SubagentCategory Category { get; init; }

    /// <summary>
    /// Capabilities of the subagent.
    /// </summary>
    public List<string> Capabilities { get; init; } = new();

    /// <summary>
    /// Dependencies on other subagents.
    /// </summary>
    public List<string> Dependencies { get; init; } = new();

    /// <summary>
    /// Metadata about the subagent.
    /// </summary>
    public Dictionary<string, string> Metadata { get; init; } = new();

    /// <summary>
    /// Whether the subagent is enabled.
    /// </summary>
    public bool Enabled { get; init; } = true;

    /// <summary>
    /// Version of the subagent definition.
    /// </summary>
    public string Version { get; init; } = "1.0.0";

    /// <summary>
    /// Source of this agent configuration.
    /// </summary>
    public AgentSource Source { get; init; } = AgentSource.BuiltIn;

    /// <summary>
    /// Location of this agent configuration.
    /// </summary>
    public AgentLocation Location { get; init; } = AgentLocation.BuiltIn;

    /// <summary>
    /// Base directory for this agent.
    /// </summary>
    public string? BaseDir { get; init; }

    /// <summary>
    /// Filename of the configuration.
    /// </summary>
    public string? Filename { get; init; }

    /// <summary>
    /// Color for UI display.
    /// </summary>
    public string? Color { get; init; }

    /// <summary>
    /// Permission mode for this agent.
    /// </summary>
    public AgentPermissionMode? PermissionMode { get; init; }

    /// <summary>
    /// Whether to fork context from parent.
    /// </summary>
    public bool ForkContext { get; init; }

    /// <summary>
    /// Tools explicitly disallowed for this agent.
    /// </summary>
    public List<string>? DisallowedTools { get; init; }

    /// <summary>
    /// Skills available to this agent.
    /// </summary>
    public List<string>? Skills { get; init; }
}

/// <summary>
/// Sandbox mode for subagents.
/// </summary>
public enum SandboxMode
{
    ReadOnly,
    ReadWrite,
    FullAccess
}

/// <summary>
/// Categories of subagents based on awesome-codex-subagents.
/// </summary>
public enum SubagentCategory
{
    CoreDevelopment,
    LanguageSpecialists,
    Infrastructure,
    QualityAndSecurity,
    DataAndAI,
    DeveloperExperience,
    SpecializedDomains,
    BusinessAndProduct,
    MetaAndOrchestration,
    ResearchAndAnalysis
}

/// <summary>
/// Result of subagent execution.
/// </summary>
public record SubagentExecutionResult
{
    /// <summary>
    /// Subagent ID.
    /// </summary>
    public string SubagentId { get; init; } = string.Empty;

    /// <summary>
    /// Whether the execution succeeded.
    /// </summary>
    public bool Success { get; init; }

    /// <summary>
    /// Output from the subagent.
    /// </summary>
    public string Output { get; init; } = string.Empty;

    /// <summary>
    /// Error message if execution failed.
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// Execution duration.
    /// </summary>
    public TimeSpan Duration { get; init; }

    /// <summary>
    /// When execution started.
    /// </summary>
    public DateTime StartedAt { get; init; }

    /// <summary>
    /// When execution completed.
    /// </summary>
    public DateTime CompletedAt { get; init; }

    /// <summary>
    /// Tokens used.
    /// </summary>
    public int TokensUsed { get; init; }

    /// <summary>
    /// Additional metadata.
    /// </summary>
    public Dictionary<string, string> Metadata { get; init; } = new();
}

/// <summary>
/// Interface for subagent registry.
/// </summary>
public interface ISubagentRegistry
{
    /// <summary>
    /// Registers a subagent.
    /// </summary>
    /// <param name="subagent">Subagent to register.</param>
    void RegisterSubagent(SubagentDefinition subagent);

    /// <summary>
    /// Unregisters a subagent.
    /// </summary>
    /// <param name="subagentId">Subagent ID.</param>
    void UnregisterSubagent(string subagentId);

    /// <summary>
    /// Gets a subagent by ID.
    /// </summary>
    /// <param name="subagentId">Subagent ID.</param>
    /// <returns>The subagent, or null if not found.</returns>
    SubagentDefinition? GetSubagent(string subagentId);

    /// <summary>
    /// Gets all subagents.
    /// </summary>
    /// <returns>List of all subagents.</returns>
    IReadOnlyList<SubagentDefinition> GetAllSubagents();

    /// <summary>
    /// Gets subagents by category.
    /// </summary>
    /// <param name="category">Category to filter by.</param>
    /// <returns>List of subagents in the category.</returns>
    IReadOnlyList<SubagentDefinition> GetSubagentsByCategory(SubagentCategory category);

    /// <summary>
    /// Searches for subagents by capability.
    /// </summary>
    /// <param name="capability">Capability to search for.</param>
    /// <returns>List of subagents with the capability.</returns>
    IReadOnlyList<SubagentDefinition> SearchByCapability(string capability);

    /// <summary>
    /// Loads subagents from a .toml file.
    /// </summary>
    /// <param name="tomlPath">Path to the .toml file.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Number of subagents loaded.</returns>
    Task<int> LoadFromTomlAsync(string tomlPath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Loads subagents from a directory.
    /// </summary>
    /// <param name="directoryPath">Path to the directory.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Number of subagents loaded.</returns>
    Task<int> LoadFromDirectoryAsync(string directoryPath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Loads subagents from YAML files in a directory.
    /// </summary>
    /// <param name="directoryPath">Path to the directory.</param>
    /// <param name="source">Source of the agents.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Number of subagents loaded.</returns>
    Task<int> LoadFromYamlDirectoryAsync(string directoryPath, AgentSource source, CancellationToken cancellationToken = default);

    /// <summary>
    /// Loads subagents from a YAML file.
    /// </summary>
    /// <param name="filePath">Path to the YAML file.</param>
    /// <param name="source">Source of the agent.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Number of subagents loaded (0 or 1).</returns>
    Task<int> LoadFromYamlFileAsync(string filePath, AgentSource source, CancellationToken cancellationToken = default);

    /// <summary>
    /// Reloads all agents from configured sources.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Number of subagents loaded.</returns>
    Task<int> ReloadAllAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Interface for subagent executor.
/// </summary>
public interface ISubagentExecutor
{
    /// <summary>
    /// Executes a subagent.
    /// </summary>
    /// <param name="subagentId">Subagent ID.</param>
    /// <param name="input">Input for the subagent.</param>
    /// <param name="context">Additional context.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Execution result.</returns>
    Task<SubagentExecutionResult> ExecuteAsync(
        string subagentId,
        string input,
        Dictionary<string, object>? context = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes multiple subagents in parallel.
    /// </summary>
    /// <param name="subagentIds">List of subagent IDs to execute.</param>
    /// <param name="input">Input for the subagents.</param>
    /// <param name="context">Additional context.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of execution results.</returns>
    Task<IReadOnlyList<SubagentExecutionResult>> ExecuteParallelAsync(
        List<string> subagentIds,
        string input,
        Dictionary<string, object>? context = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes subagents in a chain (output of one becomes input of next).
    /// </summary>
    /// <param name="subagentIds">List of subagent IDs to execute in order.</param>
    /// <param name="input">Initial input.</param>
    /// <param name="context">Additional context.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of execution results.</returns>
    Task<IReadOnlyList<SubagentExecutionResult>> ExecuteChainAsync(
        List<string> subagentIds,
        string input,
        Dictionary<string, object>? context = null,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// In-memory implementation of subagent registry.
/// </summary>
public class InMemorySubagentRegistry : ISubagentRegistry
{
    private readonly Dictionary<string, SubagentDefinition> _subagents = new();
    private readonly AgentConfigLoader _configLoader = new();
    private readonly HashSet<FileSystemWatcher> _watchers = new();

    public InMemorySubagentRegistry()
    {
        // Register built-in agents
        RegisterBuiltInAgents();
    }

    private void RegisterBuiltInAgents()
    {
        // General Purpose Agent
        RegisterSubagent(new SubagentDefinition
        {
            Id = "general-purpose",
            Name = "general-purpose",
            Description = "General-purpose agent for researching complex questions, searching for code, and executing multi-step tasks",
            Model = "task",
            Instructions = @"You are a general-purpose agent. Given the user's task, use the tools available to complete it efficiently and thoroughly.

When to use your capabilities:
- Searching for code, configurations, and patterns across large codebases
- Analyzing multiple files to understand system architecture
- Investigating complex questions that require exploring many files
- Performing multi-step research tasks

Guidelines:
- For file searches: Use Grep or Glob when you need to search broadly. Use FileRead when you know the specific file path.
- For analysis: Start broad and narrow down. Use multiple search strategies if the first doesn't yield results.
- Be thorough: Check multiple locations, consider different naming conventions, look for related files.
- Complete tasks directly using your capabilities.",
            Capabilities = new List<string> { "*" },
            Source = AgentSource.BuiltIn,
            Location = AgentLocation.BuiltIn,
            Category = SubagentCategory.MetaAndOrchestration
        });

        // Explore Agent (READ-ONLY)
        RegisterSubagent(new SubagentDefinition
        {
            Id = "Explore",
            Name = "Explore",
            Description = "Fast agent specialized for exploring codebases. Use this when you need to quickly find files by patterns (eg. src/components/**/*.tsx), search code for keywords (eg. API endpoints), or answer questions about the codebase (eg. how do API endpoints work?).",
            Model = "quick",
            Instructions = @"You are a file search specialist. You excel at thoroughly navigating and exploring codebases.

=== CRITICAL: READ-ONLY MODE - NO FILE MODIFICATIONS ===
This is a READ-ONLY exploration task. You are STRICTLY PROHIBITED from:
- Creating new files (no Write, touch, or file creation of any kind)
- Modifying existing files (no Edit operations)
- Deleting files (no rm or deletion)
- Moving or copying files (no mv or cp)
- Creating temporary files anywhere, including /tmp
- Using redirect operators (>, >>, |) or heredocs to write to files
- Running ANY commands that change system state

Your role is EXCLUSIVELY to search and analyze existing code. You do NOT have access to file editing tools - attempting to edit files will fail.

Your strengths:
- Rapidly finding files using glob patterns
- Searching code and text with powerful regex patterns
- Reading and analyzing file contents

Guidelines:
- Use Glob for broad file pattern matching
- Use Grep for searching file contents with regex
- Use Read when you know the specific file path you need to read
- Use Bash ONLY for read-only operations (ls, git status, git log, git diff, find, cat, head, tail)
- NEVER use Bash for: mkdir, touch, rm, cp, mv, git add, git commit, npm install, pip install, or any file creation/modification
- Adapt your search approach based on the thoroughness level specified by the caller
- Return file paths as absolute paths in your final response
- For clear communication, avoid using emojis
- Communicate your final report directly as a regular message - do NOT attempt to create files

NOTE: You are meant to be a fast agent that returns output as quickly as possible. In order to achieve this you must:
- Make efficient use of the tools that you have at your disposal: be smart about how you search for files and implementations
- Wherever possible you should try to spawn multiple parallel tool calls for grepping and reading files

Complete the user's search request efficiently and report your findings clearly.",
            Capabilities = new List<string> { "Glob", "Grep", "Read", "Bash" },
            DisallowedTools = new List<string> { "Task", "Edit", "Write", "NotebookEdit" },
            SandboxMode = SandboxMode.ReadOnly,
            Source = AgentSource.BuiltIn,
            Location = AgentLocation.BuiltIn,
            Category = SubagentCategory.MetaAndOrchestration,
            PermissionMode = AgentPermissionMode.Plan
        });

        // Plan Agent (READ-ONLY)
        RegisterSubagent(new SubagentDefinition
        {
            Id = "Plan",
            Name = "Plan",
            Description = "Software architect agent for designing implementation plans. Use this when you need to plan the implementation strategy for a task. Returns step-by-step plans, identifies critical files, and considers architectural trade-offs.",
            Model = "inherit",
            Instructions = @"You are a software architect and planning specialist. Your role is to explore the codebase and design implementation plans.

=== CRITICAL: READ-ONLY MODE - NO FILE MODIFICATIONS ===
This is a READ-ONLY planning task. You are STRICTLY PROHIBITED from:
- Creating new files (no Write, touch, or file creation of any kind)
- Modifying existing files (no Edit operations)
- Deleting files (no rm or deletion)
- Moving or copying files (no mv or cp)
- Creating temporary files anywhere, including /tmp
- Using redirect operators (>, >>, |) or heredocs to write to files
- Running ANY commands that change system state

Your role is EXCLUSIVELY to explore the codebase and design implementation plans. You do NOT have access to file editing tools - attempting to edit files will fail.

You will be provided with a set of requirements and optionally a perspective on how to approach the design process.

## Your Process

1. **Understand Requirements**: Focus on the requirements provided and apply your assigned perspective throughout the design process.

2. **Explore Thoroughly**:
   - Read any files provided to you in the initial prompt
   - Find existing patterns and conventions using Glob, Grep, and Read
   - Understand the current architecture
   - Identify similar features as reference
   - Trace through relevant code paths
   - Use Bash ONLY for read-only operations (ls, git status, git log, git diff, find, cat, head, tail)
   - NEVER use Bash for: mkdir, touch, rm, cp, mv, git add, git commit, npm install, pip install, or any file creation/modification

3. **Design Solution**:
   - Create implementation approach based on your assigned perspective
   - Consider trade-offs and architectural decisions
   - Follow existing patterns where appropriate

4. **Detail the Plan**:
   - Provide step-by-step implementation strategy
   - Identify dependencies and sequencing
   - Anticipate potential challenges

## Required Output

End your response with:

### Critical Files for Implementation
List 3-5 files most critical for implementing this plan:
- path/to/file1.cs - [Brief reason: e.g., ""Core logic to modify""]
- path/to/file2.cs - [Brief reason: e.g., ""Interfaces to implement""]
- path/to/file3.cs - [Brief reason: e.g., ""Pattern to follow""]

REMEMBER: You can ONLY explore and plan. You CANNOT and MUST NOT write, edit, or modify any files. You do NOT have access to file editing tools.",
            Capabilities = new List<string> { "Glob", "Grep", "Read", "Bash" },
            DisallowedTools = new List<string> { "Task", "Edit", "Write", "NotebookEdit" },
            SandboxMode = SandboxMode.ReadOnly,
            Source = AgentSource.BuiltIn,
            Location = AgentLocation.BuiltIn,
            Category = SubagentCategory.MetaAndOrchestration,
            PermissionMode = AgentPermissionMode.Plan
        });
    }

    public void RegisterSubagent(SubagentDefinition subagent)
    {
        if (string.IsNullOrEmpty(subagent.Id))
        {
            throw new ArgumentException("Subagent ID cannot be null or empty", nameof(subagent));
        }

        _subagents[subagent.Id] = subagent;
    }

    public void UnregisterSubagent(string subagentId)
    {
        _subagents.Remove(subagentId);
    }

    public SubagentDefinition? GetSubagent(string subagentId)
    {
        _subagents.TryGetValue(subagentId, out var subagent);
        return subagent;
    }

    public IReadOnlyList<SubagentDefinition> GetAllSubagents()
    {
        return _subagents.Values.ToList().AsReadOnly();
    }

    public IReadOnlyList<SubagentDefinition> GetSubagentsByCategory(SubagentCategory category)
    {
        return _subagents.Values
            .Where(s => s.Category == category)
            .ToList()
            .AsReadOnly();
    }

    public IReadOnlyList<SubagentDefinition> SearchByCapability(string capability)
    {
        var lowerCapability = capability.ToLowerInvariant();
        return _subagents.Values
            .Where(s => s.Capabilities.Any(c => c.ToLowerInvariant().Contains(lowerCapability)))
            .ToList()
            .AsReadOnly();
    }

    public async Task<int> LoadFromTomlAsync(string tomlPath, CancellationToken cancellationToken = default)
    {
        if (!File.Exists(tomlPath))
            return 0;

        try
        {
            var parser = new TomlAgentConfigParser();
            // var config = await parser.ParseFileAsync(tomlPath, cancellationToken);
            
            // TODO: Implement TOML parsing
            return 0;
            
            /*
            // Determine source from file location
            var userDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".libr4", "agents");
            config.Source = tomlPath.StartsWith(userDir) ? AgentSource.UserSettings : AgentSource.ProjectSettings;
            config.Location = tomlPath.StartsWith(userDir) ? AgentLocation.UserSettings : AgentLocation.ProjectSettings;

            var subagent = config.ToSubagentDefinition();
            subagent = subagent with
            {
                Source = config.Source,
                Location = config.Location,
                BaseDir = config.BaseDir,
                Filename = config.Filename,
                Color = config.Color,
                PermissionMode = config.PermissionMode,
                ForkContext = config.ForkContext,
                DisallowedTools = config.DisallowedTools,
                Skills = config.Skills
            };

            RegisterSubagent(subagent);
            return 1;
            */
        }
        catch (Exception ex)
        {
            // Log error but don't fail
            System.Diagnostics.Debug.WriteLine($"Failed to load TOML agent from {tomlPath}: {ex.Message}");
            return 0;
        }
    }

    public async Task<int> LoadFromDirectoryAsync(string directoryPath, AgentSource source, CancellationToken cancellationToken = default)
    {
        if (!Directory.Exists(directoryPath))
            return 0;

        var count = 0;
        
        // Find all TOML files
        var tomlFiles = Directory.GetFiles(directoryPath, "*.toml", SearchOption.AllDirectories);
        
        foreach (var file in tomlFiles)
        {
            try
            {
                var loaded = await LoadFromTomlAsync(file, cancellationToken);
                count += loaded;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to load TOML from {file}: {ex.Message}");
            }
        }

        // Also load any YAML files in the same directory (for compatibility)
        var yamlCount = await LoadFromYamlDirectoryAsync(directoryPath, source, cancellationToken);
        count += yamlCount;

        return count;
    }

    /// <summary>
    /// Loads agents from all configured directories (user + project).
    /// </summary>
    public async Task<int> LoadFromAllDirectoriesAsync(CancellationToken cancellationToken = default)
    {
        var total = 0;

        // User directory
        var userDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".libr4", "agents");
        if (Directory.Exists(userDir))
        {
            total += await LoadFromDirectoryAsync(userDir, AgentSource.UserSettings, cancellationToken);
        }

        // Project directory
        var projectDir = Path.Combine(Directory.GetCurrentDirectory(), ".libr4", "agents");
        if (Directory.Exists(projectDir))
        {
            total += await LoadFromDirectoryAsync(projectDir, AgentSource.ProjectSettings, cancellationToken);
        }

        return total;
    }

    public async Task<int> LoadFromYamlDirectoryAsync(string directoryPath, AgentSource source, CancellationToken cancellationToken = default)
    {
        if (!Directory.Exists(directoryPath))
            return 0;

        var count = 0;
        var yamlFiles = Directory.GetFiles(directoryPath, "*.yaml", SearchOption.AllDirectories)
            .Concat(Directory.GetFiles(directoryPath, "*.yml", SearchOption.AllDirectories));

        foreach (var file in yamlFiles)
        {
            count += await LoadFromYamlFileAsync(file, source, cancellationToken);
        }

        return count;
    }

    public async Task<int> LoadFromYamlFileAsync(string filePath, AgentSource source, CancellationToken cancellationToken = default)
    {
        if (!File.Exists(filePath))
            return 0;

        try
        {
            var content = await File.ReadAllTextAsync(filePath, cancellationToken);
            var baseDir = Path.GetDirectoryName(filePath) ?? string.Empty;
            var config = _configLoader.ParseFromYaml(content, filePath, source, baseDir);

            if (config == null)
                return 0;

            var subagent = config.ToSubagentDefinition();
            subagent = subagent with
            {
                Source = config.Source,
                Location = config.Location,
                BaseDir = config.BaseDir,
                Filename = config.Filename,
                Color = config.Color,
                PermissionMode = config.PermissionMode,
                ForkContext = config.ForkContext,
                DisallowedTools = config.DisallowedTools,
                Skills = config.Skills
            };

            RegisterSubagent(subagent);
            return 1;
        }
        catch
        {
            return 0;
        }
    }

    public async Task<int> ReloadAllAsync(CancellationToken cancellationToken = default)
    {
        // Clear current agents (except built-in)
        var builtInAgents = _subagents.Values.Where(s => s.Source == AgentSource.BuiltIn).ToList();
        _subagents.Clear();

        foreach (var agent in builtInAgents)
        {
            RegisterSubagent(agent);
        }

        // Load from all configured sources (supports both TOML and YAML)
        var total = await LoadFromAllDirectoriesAsync(cancellationToken);

        return total;
    }

    /// <summary>
    /// Gets active agents after merging by priority (project > user > built-in).
    /// </summary>
    public IReadOnlyList<SubagentDefinition> GetActiveAgents()
    {
        var grouped = _subagents.Values.GroupBy(s => s.Id);
        var merged = new List<SubagentDefinition>();

        foreach (var group in grouped)
        {
            // Priority: PolicySettings > FlagSettings > ProjectSettings > UserSettings > Plugin > BuiltIn
            var sorted = group.OrderByDescending(s => s.Source);
            merged.Add(sorted.First());
        }

        return merged.AsReadOnly();
    }

    /// <summary>
    /// Checks if a file is an agent configuration file.
    /// </summary>
    private static bool IsAgentConfigFile(string? filename)
    {
        if (string.IsNullOrEmpty(filename))
            return false;
            
        var ext = Path.GetExtension(filename).ToLowerInvariant();
        return ext == ".yml" || ext == ".yaml" || ext == ".toml";
    }

    /// <summary>
    /// Starts watching agent directories for changes.
    /// Supports YAML, YML, and TOML files.
    /// </summary>
    public void StartWatching(Action? onChange = null)
    {
        StopWatching();

        var directoriesToWatch = new List<string>
        {
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".libr4", "agents"),
            Path.Combine(Directory.GetCurrentDirectory(), ".libr4", "agents")
        };

        foreach (var dir in directoriesToWatch.Where(Directory.Exists))
        {
            try
            {
                // Use wildcard filter to catch all config file types
                var watcher = new FileSystemWatcher(dir)
                {
                    Filter = "*.*",
                    IncludeSubdirectories = true,
                    NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName
                };

                watcher.Changed += async (s, e) =>
                {
                    if (IsAgentConfigFile(e.Name))
                    {
                        await ReloadAllAsync();
                        onChange?.Invoke();
                    }
                };

                watcher.Created += async (s, e) =>
                {
                    if (IsAgentConfigFile(e.Name))
                    {
                        await ReloadAllAsync();
                        onChange?.Invoke();
                    }
                };

                watcher.Deleted += async (s, e) =>
                {
                    if (IsAgentConfigFile(e.Name))
                    {
                        await ReloadAllAsync();
                        onChange?.Invoke();
                    }
                };

                watcher.Renamed += async (s, e) =>
                {
                    if (IsAgentConfigFile(e.OldFullPath) || IsAgentConfigFile(e.FullPath))
                    {
                        await ReloadAllAsync();
                        onChange?.Invoke();
                    }
                };

                watcher.EnableRaisingEvents = true;
                _watchers.Add(watcher);
            }
            catch (Exception ex)
            {
                // Failed to setup file system watcher
            }
        }
    }

    /// <summary>
    /// Stops watching agent directories.
    /// </summary>
    public void StopWatching()
    {
        foreach (var watcher in _watchers)
        {
            try
            {
                watcher.EnableRaisingEvents = false;
                watcher.Dispose();
            }
            catch (Exception ex)
            {
                // Error disposing file system watcher
            }
        }
        _watchers.Clear();
    }

    /// <summary>
    /// Load subagents from a directory
    /// </summary>
    public async Task<int> LoadFromDirectoryAsync(string directoryPath, CancellationToken cancellationToken = default)
    {
        if (!Directory.Exists(directoryPath))
        {
            return 0;
        }

        var yamlFiles = Directory.GetFiles(directoryPath, "*.yaml", SearchOption.AllDirectories)
            .Concat(Directory.GetFiles(directoryPath, "*.yml", SearchOption.AllDirectories));

        int loadedCount = 0;
        foreach (var file in yamlFiles)
        {
            try
            {
                var config = await _configLoader.LoadFromFileAsync(file, cancellationToken);
                if (config != null)
                {
                    // Convert AgentConfig to SubagentDefinition
                    var subagentDef = new SubagentDefinition
                    {
                        Id = config.AgentType,
                        Name = config.AgentType,
                        Description = config.WhenToUse,
                        Capabilities = config.Tools,
                        Skills = config.Skills,
                        Instructions = config.SystemPrompt,
                        Source = config.Source,
                        Location = config.Location,
                        BaseDir = config.BaseDir,
                        Filename = config.Filename,
                        Color = config.Color,
                        Model = config.Model,
                        PermissionMode = config.PermissionMode,
                        ForkContext = config.ForkContext
                    };
                    RegisterSubagent(subagentDef);
                    loadedCount++;
                }
            }
            catch (Exception ex)
            {
                // Failed to load subagent from file
            }
        }
        
        return loadedCount;
    }
}
