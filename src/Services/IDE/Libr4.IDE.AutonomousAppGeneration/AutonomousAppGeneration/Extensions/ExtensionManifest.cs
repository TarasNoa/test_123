namespace Libr4.IDE.Application.AutonomousAppGeneration.Extensions;

public sealed class ExtensionManifestDocument
{
    public string Name { get; set; } = string.Empty;
    public string Version { get; set; } = "0.0.0";
    public string? Description { get; set; }
    public List<ExtensionHookDefinition> Hooks { get; set; } = [];
    public List<ExtensionToolDefinition> Tools { get; set; } = [];
    public List<ExtensionSkillDefinition> Skills { get; set; } = [];
}

public sealed class ExtensionHookDefinition
{
    public string Kind { get; set; } = "PreToolUse";
    public string Script { get; set; } = string.Empty;
    public string OnFailure { get; set; } = "log";
    public int? TimeoutMs { get; set; }
}

public sealed class ExtensionToolDefinition
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Script { get; set; } = string.Empty;
    public bool ReadOnly { get; set; }
    public int? TimeoutMs { get; set; }
}

public sealed class ExtensionSkillDefinition
{
    public string Id { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Path { get; set; } = string.Empty;
}

public sealed record LoadedExtension(
    string Id,
    string RootPath,
    ExtensionSource Source,
    ExtensionManifestDocument Manifest,
    string ManifestPath);

public enum ExtensionSource
{
    Project,
    User
}

public sealed record ExtensionHookBinding(
    LoadedExtension Extension,
    ExtensionHookDefinition Definition,
    string ScriptPath);

public sealed record ExtensionToolBinding(
    LoadedExtension Extension,
    ExtensionToolDefinition Definition,
    string ScriptPath);

public sealed record ExtensionSkillBinding(
    LoadedExtension Extension,
    ExtensionSkillDefinition Definition,
    string SkillFilePath);
