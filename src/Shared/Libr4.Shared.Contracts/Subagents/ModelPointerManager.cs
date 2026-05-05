namespace Libr4.Shared.Contracts.Subagents;

/// <summary>
/// Model pointer types for different purposes.
/// </summary>
public enum ModelPointer
{
    /// <summary>
    /// Default model for main agent conversation.
    /// </summary>
    Main,

    /// <summary>
    /// Default model for subagent/task execution.
    /// </summary>
    Task,

    /// <summary>
    /// Model for automatic context compression.
    /// </summary>
    Compact,

    /// <summary>
    /// Fast model for simple operations and utilities.
    /// </summary>
    Quick
}

/// <summary>
/// Model profile configuration.
/// </summary>
public record ModelProfile
{
    /// <summary>
    /// Profile name.
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// Provider (e.g., "openai", "anthropic", "alibaba").
    /// </summary>
    public string Provider { get; init; } = string.Empty;

    /// <summary>
    /// Model name (e.g., "gpt-4o", "claude-sonnet-4").
    /// </summary>
    public string ModelName { get; init; } = string.Empty;

    /// <summary>
    /// Maximum tokens for requests.
    /// </summary>
    public int MaxTokens { get; init; } = 4096;

    /// <summary>
    /// Context window size.
    /// </summary>
    public int ContextLength { get; init; } = 128000;

    /// <summary>
    /// API key (can reference environment variable).
    /// </summary>
    public string? ApiKey { get; init; }

    /// <summary>
    /// Base URL for the API endpoint.
    /// </summary>
    public string? BaseUrl { get; init; }

    /// <summary>
    /// Whether this profile is active.
    /// </summary>
    public bool IsActive { get; init; } = true;

    /// <summary>
    /// Reasoning effort (for models that support it).
    /// </summary>
    public string? ReasoningEffort { get; init; }

    /// <summary>
    /// Temperature for generation.
    /// </summary>
    public double Temperature { get; init; } = 0.7;

    /// <summary>
    /// Cost per 1M input tokens (in USD).
    /// </summary>
    public double InputCostPerMillion { get; init; } = 0;

    /// <summary>
    /// Cost per 1M output tokens (in USD).
    /// </summary>
    public double OutputCostPerMillion { get; init; } = 0;
}

/// <summary>
/// Manages model pointers and model selection.
/// </summary>
public class ModelPointerManager
{
    private readonly Dictionary<string, ModelProfile> _profiles = new();
    private readonly Dictionary<ModelPointer, string> _pointers = new();

    public ModelPointerManager()
    {
        // Initialize default pointers
        InitializeDefaults();
    }

    private void InitializeDefaults()
    {
        // Default pointers (can be overridden)
        _pointers[ModelPointer.Main] = "main";
        _pointers[ModelPointer.Task] = "task";
        _pointers[ModelPointer.Compact] = "compact";
        _pointers[ModelPointer.Quick] = "quick";
    }

    /// <summary>
    /// Registers a model profile.
    /// </summary>
    public void RegisterProfile(ModelProfile profile)
    {
        if (string.IsNullOrEmpty(profile.Name))
        {
            throw new ArgumentException("Profile name cannot be null or empty", nameof(profile));
        }

        _profiles[profile.Name] = profile;
    }

    /// <summary>
    /// Gets a model profile by name.
    /// </summary>
    public ModelProfile? GetProfile(string name)
    {
        _profiles.TryGetValue(name, out var profile);
        return profile;
    }

    /// <summary>
    /// Gets all registered profiles.
    /// </summary>
    public IReadOnlyList<ModelProfile> GetAllProfiles()
    {
        return _profiles.Values.ToList().AsReadOnly();
    }

    /// <summary>
    /// Sets a model pointer.
    /// </summary>
    public void SetPointer(ModelPointer pointer, string profileName)
    {
        if (!_profiles.ContainsKey(profileName))
        {
            throw new ArgumentException($"Profile '{profileName}' not found", nameof(profileName));
        }

        _pointers[pointer] = profileName;
    }

    /// <summary>
    /// Gets a model pointer.
    /// </summary>
    public string? GetPointer(ModelPointer pointer)
    {
        _pointers.TryGetValue(pointer, out var profileName);
        return profileName;
    }

    /// <summary>
    /// Gets the model profile for a pointer.
    /// </summary>
    public ModelProfile? GetProfileForPointer(ModelPointer pointer)
    {
        var profileName = GetPointer(pointer);
        if (string.IsNullOrEmpty(profileName))
            return null;

        return GetProfile(profileName);
    }

    /// <summary>
    /// Resolves a model name to a profile.
    /// Handles special values like "inherit", "main", "task", etc.
    /// </summary>
    public ModelProfile? ResolveModel(string? modelName)
    {
        if (string.IsNullOrEmpty(modelName))
            return null;

        // Handle special values
        switch (modelName.ToLowerInvariant())
        {
            case "inherit":
                // Inherit from parent - return null to signal inheritance
                return null;
            case "main":
                return GetProfileForPointer(ModelPointer.Main);
            case "task":
                return GetProfileForPointer(ModelPointer.Task);
            case "compact":
                return GetProfileForPointer(ModelPointer.Compact);
            case "quick":
                return GetProfileForPointer(ModelPointer.Quick);
            case "sonnet":
                // Map to task pointer by default
                return GetProfileForPointer(ModelPointer.Task);
            case "haiku":
                // Map to quick pointer by default
                return GetProfileForPointer(ModelPointer.Quick);
            case "opus":
                // Map to main pointer by default
                return GetProfileForPointer(ModelPointer.Main);
            default:
                // Direct profile name lookup
                return GetProfile(modelName);
        }
    }

    /// <summary>
    /// Gets the default profile for main conversation.
    /// </summary>
    public ModelProfile? GetMainProfile()
    {
        return GetProfileForPointer(ModelPointer.Main);
    }

    /// <summary>
    /// Gets the default profile for subagent execution.
    /// </summary>
    public ModelProfile? GetTaskProfile()
    {
        return GetProfileForPointer(ModelPointer.Task);
    }

    /// <summary>
    /// Gets the default profile for context compression.
    /// </summary>
    public ModelProfile? GetCompactProfile()
    {
        return GetProfileForPointer(ModelPointer.Compact);
    }

    /// <summary>
    /// Gets the default profile for quick operations.
    /// </summary>
    public ModelProfile? GetQuickProfile()
    {
        return GetProfileForPointer(ModelPointer.Quick);
    }

    /// <summary>
    /// Clears all profiles and pointers.
    /// </summary>
    public void Clear()
    {
        _profiles.Clear();
        _pointers.Clear();
        InitializeDefaults();
    }

    /// <summary>
    /// Loads profiles from configuration.
    /// </summary>
    public void LoadFromConfiguration(ModelPointerConfiguration config)
    {
        Clear();

        // Register profiles
        foreach (var profile in config.Profiles)
        {
            RegisterProfile(profile);
        }

        // Set pointers
        if (config.Pointers != null)
        {
            foreach (var (pointer, profileName) in config.Pointers)
            {
                SetPointer(pointer, profileName);
            }
        }
    }
}

/// <summary>
/// Configuration for model pointers.
/// </summary>
public record ModelPointerConfiguration
{
    /// <summary>
    /// Model profiles.
    /// </summary>
    public List<ModelProfile> Profiles { get; init; } = new();

    /// <summary>
    /// Model pointers mapping.
    /// </summary>
    public Dictionary<ModelPointer, string>? Pointers { get; init; }
}

/// <summary>
/// Intelligent work allocation strategy based on model pointers.
/// </summary>
public static class ModelWorkAllocationStrategy
{
    /// <summary>
    /// Suggests the appropriate model pointer for a given task type.
    /// </summary>
    public static ModelPointer SuggestModelPointer(string taskType)
    {
        return taskType.ToLowerInvariant() switch
        {
            "architecture" or "design" or "planning" => ModelPointer.Main,
            "exploration" or "search" or "analysis" => ModelPointer.Quick,
            "compression" or "summarization" => ModelPointer.Compact,
            "implementation" or "coding" or "refactoring" => ModelPointer.Task,
            "debugging" or "testing" => ModelPointer.Task,
            _ => ModelPointer.Main
        };
    }

    /// <summary>
    /// Suggests the appropriate model for a specific task.
    /// </summary>
    public static ModelProfile? SuggestModelForTask(
        ModelPointerManager manager,
        string taskType)
    {
        var pointer = SuggestModelPointer(taskType);
        return manager.GetProfileForPointer(pointer);
    }
}
