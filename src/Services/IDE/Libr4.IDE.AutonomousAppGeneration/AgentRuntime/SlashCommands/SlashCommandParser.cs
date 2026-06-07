using System.Text.RegularExpressions;

namespace Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.SlashCommands;

public static class SlashCommandParser
{
    private static readonly Regex FlowCommand = new(@"(?:^|\s)/flow:([A-Za-z0-9_-]+)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex VerifyCommand = new(@"(?:^|\s)/verify\b", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex CompactCommand = new(@"(?:^|\s)/compact\b", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex RewindCommand = new(@"(?:^|\s)/rewind\b", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex DelegateCommand = new(@"(?:^|\s)/delegate\b", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public static string? TryParseFlow(string? userRequest)
    {
        if (string.IsNullOrWhiteSpace(userRequest))
            return null;
        var match = FlowCommand.Match(userRequest);
        return match.Success ? match.Groups[1].Value : null;
    }

    public static IReadOnlyList<string> ParseCommands(string? userRequest)
    {
        if (string.IsNullOrWhiteSpace(userRequest))
            return Array.Empty<string>();

        var commands = new List<string>();
        if (FlowCommand.IsMatch(userRequest))
            commands.Add("flow");
        if (VerifyCommand.IsMatch(userRequest))
            commands.Add("verify");
        if (CompactCommand.IsMatch(userRequest))
            commands.Add("compact");
        if (RewindCommand.IsMatch(userRequest))
            commands.Add("rewind");
        if (DelegateCommand.IsMatch(userRequest))
            commands.Add("delegate");
        return commands;
    }

    public static string StripCommandPrefixes(string userRequest)
    {
        var stripped = FlowCommand.Replace(userRequest, " ");
        stripped = VerifyCommand.Replace(stripped, " ");
        stripped = CompactCommand.Replace(stripped, " ");
        stripped = RewindCommand.Replace(stripped, " ");
        stripped = DelegateCommand.Replace(stripped, " ");
        return stripped.Trim();
    }
}
