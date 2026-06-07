namespace Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Delegation;

public static class HumanReadableIdGenerator
{
    private static readonly string[] Adjectives =
    [
        "brisk", "calm", "bright", "swift", "bold", "keen", "warm", "cool"
    ];

    private static readonly string[] Colors =
    [
        "blue", "red", "green", "amber", "violet", "teal", "silver", "gold"
    ];

    private static readonly string[] Animals =
    [
        "fox", "owl", "lynx", "hawk", "wolf", "bear", "deer", "crow"
    ];

    public static string Create() =>
        $"{Pick(Adjectives)}-{Pick(Colors)}-{Pick(Animals)}";

    private static string Pick(string[] values) =>
        values[Random.Shared.Next(values.Length)];
}
