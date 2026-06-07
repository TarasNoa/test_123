using System.Text.Json;

namespace Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Schema;

public sealed record ToolPropertySchema(string Name, string Type, bool Required = true);

public sealed record ToolInputSchema(string ToolName, IReadOnlyList<ToolPropertySchema> Properties);

public static class ToolSchemaRegistry
{
    private static readonly Dictionary<string, ToolInputSchema> Schemas = new(StringComparer.OrdinalIgnoreCase)
    {
        ["read_file"] = new("read_file", [new("path", "string")]),
        ["write_file"] = new("write_file", [new("path", "string"), new("content", "string")]),
        ["edit_file"] = new("edit_file", [new("path", "string"), new("search", "string"), new("replace", "string")]),
        ["apply_patch"] = new("apply_patch", [new("path", "string"), new("patch", "string")]),
        ["bash"] = new("bash", [new("command", "string")]),
        ["grep"] = new("grep", [new("pattern", "string")]),
        ["glob"] = new("glob", [new("pattern", "string")]),
        ["list_directory"] = new("list_directory", [new("path", "string", false), new("depth", "number", false)]),
        ["run_build"] = new("run_build", []),
        ["run_tests"] = new("run_tests", []),
        ["agent"] = new("agent", [new("role", "string"), new("task", "string")]),
        ["mcp"] = new("mcp", [new("tool", "string")]),
        ["skill"] = new("skill", [new("name", "string")]),
        ["activate_skill"] = new("activate_skill", [new("name", "string")]),
    };

    public static ToolInputSchema? TryGet(string toolName) =>
        Schemas.TryGetValue(toolName, out var schema) ? schema : null;

    public static ToolValidationResult Validate(string toolName, JsonElement input)
    {
        var schema = TryGet(toolName);
        if (schema is null)
            return ToolValidationResult.Valid();

        var missing = new List<string>();
        foreach (var prop in schema.Properties.Where(p => p.Required))
        {
            if (!input.TryGetProperty(prop.Name, out var el) || el.ValueKind == JsonValueKind.Null)
                missing.Add(prop.Name);
            else if (prop.Type == "string" && el.ValueKind != JsonValueKind.String)
                missing.Add($"{prop.Name}:expected_string");
            else if (prop.Type == "number" && el.ValueKind != JsonValueKind.Number)
                missing.Add($"{prop.Name}:expected_number");
        }

        return missing.Count == 0
            ? ToolValidationResult.Valid()
            : ToolValidationResult.Invalid($"missing_or_invalid_fields: {string.Join(", ", missing)}");
    }
}

public sealed record ToolValidationResult(bool IsValid, string? Error)
{
    public static ToolValidationResult Valid() => new(true, null);
    public static ToolValidationResult Invalid(string error) => new(false, error);
}

public static class ToolInputValidator
{
    public static ToolValidationResult ValidateBeforeExecute(string toolName, JsonElement input) =>
        ToolSchemaRegistry.Validate(toolName, input);
}
