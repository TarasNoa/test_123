using System.Text.Json;
using System.Text.Json.Serialization;

namespace Libr4.IDE.Domain.AutonomousAppGeneration;

public static class AgentTaskGraphPersistence
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    public static string Serialize(IReadOnlyList<AgentTaskGraphEntry> entries)
    {
        var dto = entries.Select(e => new EntryDto(
            e.TaskId,
            e.Title,
            e.BlockedByTaskIds.ToList(),
            e.State.ToString(),
            e.EvidencePaths.ToList(),
            e.Notes)).ToList();
        return JsonSerializer.Serialize(dto, Options);
    }

    public static IReadOnlyList<AgentTaskGraphEntry> Deserialize(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return Array.Empty<AgentTaskGraphEntry>();

        try
        {
            var dto = JsonSerializer.Deserialize<List<EntryDto>>(json, Options);
            if (dto is null || dto.Count == 0)
                return Array.Empty<AgentTaskGraphEntry>();

            return dto.Select(d => new AgentTaskGraphEntry(
                d.TaskId ?? string.Empty,
                d.Title ?? string.Empty,
                d.BlockedByTaskIds ?? new List<string>(),
                Enum.TryParse<AgentTaskState>(d.State, true, out var state) ? state : AgentTaskState.Pending,
                d.EvidencePaths ?? new List<string>(),
                d.Notes)).ToList();
        }
        catch (JsonException)
        {
            return Array.Empty<AgentTaskGraphEntry>();
        }
    }

    private sealed record EntryDto(
        string? TaskId,
        string? Title,
        List<string>? BlockedByTaskIds,
        string? State,
        List<string>? EvidencePaths,
        string? Notes);
}
