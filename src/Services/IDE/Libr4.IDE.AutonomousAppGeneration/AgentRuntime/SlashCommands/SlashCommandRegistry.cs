namespace Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.SlashCommands;

public interface ISlashCommandRegistry
{
    bool TryGet(string command, out SlashCommandDefinition definition);
    IReadOnlyList<SlashCommandDefinition> All { get; }
}

public sealed record SlashCommandDefinition(string Name, string Description, bool RequiresActiveRun);

public sealed class SlashCommandRegistry : ISlashCommandRegistry
{
    private static readonly SlashCommandDefinition[] Commands =
    [
        new("verify", "Run verify subagent on current workspace", true),
        new("compact", "Compact agent session context", true),
        new("rewind", "Rewind agent session to checkpoint", true),
        new("flow", "Start named YAML flow (/flow:name)", false),
        new("delegate", "Start background explore delegation", true),
        new("memory-search", "Search consolidated run memory", true)
    ];

    private readonly Dictionary<string, SlashCommandDefinition> _byName =
        Commands.ToDictionary(c => c.Name, c => c, StringComparer.OrdinalIgnoreCase);

    public bool TryGet(string command, out SlashCommandDefinition definition)
    {
        var key = command.Trim().TrimStart('/');
        var colon = key.IndexOf(':');
        if (colon > 0)
            key = key[..colon];
        return _byName.TryGetValue(key, out definition!);
    }

    public IReadOnlyList<SlashCommandDefinition> All => Commands;
}
