using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;

namespace Libr4.AI.Application.Mentions;

/// <summary>
/// Service for handling @ mentions in chat/input with fuzzy matching.
/// Supports mentions of: files, agents, symbols, context, directories.
/// Inspired by Roo-Code and Kode-Agent implementations.
/// </summary>
public interface IMentionService
{
    /// <summary>
    /// Parse input text and extract all mentions.
    /// </summary>
    ParsedMentions ParseMentions(string input);

    /// <summary>
    /// Get completions for mention query (for autocompletion UI).
    /// </summary>
    Task<IReadOnlyList<MentionCompletion>> GetCompletionsAsync(
        string query,
        MentionContext context,
        int maxResults = 10,
        CancellationToken ct = default);

    /// <summary>
    /// Resolve a mention to actual content/context.
    /// </summary>
    Task<ResolvedMention?> ResolveAsync(
        Mention mention,
        MentionContext context,
        CancellationToken ct = default);

    /// <summary>
    /// Expand mentions in input text with resolved content.
    /// </summary>
    Task<string> ExpandMentionsAsync(
        string input,
        MentionContext context,
        CancellationToken ct = default);
}

/// <summary>
/// Main implementation of mention service with fuzzy matching.
/// </summary>
public sealed class MentionService : IMentionService
{
    private readonly IFuzzyMatcher _fuzzyMatcher;
    private readonly IFileResolver _fileResolver;
    private readonly IAgentResolver _agentResolver;
    private readonly ISymbolResolver _symbolResolver;
    private readonly ILogger<MentionService> _logger;

    // Regex patterns for different mention types
    private static readonly Regex FileMentionRegex = new(
        @"@([^\s,;]+\.(?:cs|fs|rs|js|ts|jsx|tsx|py|java|go|rb|php|cpp|c|h|hpp|json|xml|yaml|yml|md|txt|sql|dockerfile|sh|ps1|bat))",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex AgentMentionRegex = new(
        @"@agent/([a-zA-Z0-9_-]+)",
        RegexOptions.Compiled);

    private static readonly Regex SymbolMentionRegex = new(
        @"@symbol/([a-zA-Z0-9_\.]+)",
        RegexOptions.Compiled);

    private static readonly Regex ContextMentionRegex = new(
        @"@context/([a-zA-Z0-9_-]+)",
        RegexOptions.Compiled);

    private static readonly Regex DirectoryMentionRegex = new(
        @"@dir/([a-zA-Z0-9_/-]+)",
        RegexOptions.Compiled);

    private static readonly Regex GenericMentionRegex = new(
        @"@([a-zA-Z0-9_./-]+)",
        RegexOptions.Compiled);

    public MentionService(
        IFuzzyMatcher fuzzyMatcher,
        IFileResolver fileResolver,
        IAgentResolver agentResolver,
        ISymbolResolver symbolResolver,
        ILogger<MentionService> logger)
    {
        _fuzzyMatcher = fuzzyMatcher;
        _fileResolver = fileResolver;
        _agentResolver = agentResolver;
        _symbolResolver = symbolResolver;
        _logger = logger;
    }

    public ParsedMentions ParseMentions(string input)
    {
        var mentions = new List<Mention>();
        var textWithoutMentions = input;

        // Extract file mentions
        foreach (Match match in FileMentionRegex.Matches(input))
        {
            mentions.Add(new Mention
            {
                Type = MentionType.File,
                RawText = match.Value,
                Identifier = match.Groups[1].Value,
                Index = match.Index,
                Length = match.Length
            });
        }

        // Extract agent mentions
        foreach (Match match in AgentMentionRegex.Matches(input))
        {
            mentions.Add(new Mention
            {
                Type = MentionType.Agent,
                RawText = match.Value,
                Identifier = match.Groups[1].Value,
                Index = match.Index,
                Length = match.Length
            });
        }

        // Extract symbol mentions
        foreach (Match match in SymbolMentionRegex.Matches(input))
        {
            mentions.Add(new Mention
            {
                Type = MentionType.Symbol,
                RawText = match.Value,
                Identifier = match.Groups[1].Value,
                Index = match.Index,
                Length = match.Length
            });
        }

        // Extract context mentions
        foreach (Match match in ContextMentionRegex.Matches(input))
        {
            mentions.Add(new Mention
            {
                Type = MentionType.Context,
                RawText = match.Value,
                Identifier = match.Groups[1].Value,
                Index = match.Index,
                Length = match.Length
            });
        }

        // Extract directory mentions
        foreach (Match match in DirectoryMentionRegex.Matches(input))
        {
            mentions.Add(new Mention
            {
                Type = MentionType.Directory,
                RawText = match.Value,
                Identifier = match.Groups[1].Value,
                Index = match.Index,
                Length = match.Length
            });
        }

        // Generic mentions (catch-all for other patterns)
        foreach (Match match in GenericMentionRegex.Matches(input))
        {
            // Skip if already matched by more specific pattern
            if (!mentions.Any(m => m.Index == match.Index))
            {
                mentions.Add(new Mention
                {
                    Type = MentionType.Generic,
                    RawText = match.Value,
                    Identifier = match.Groups[1].Value,
                    Index = match.Index,
                    Length = match.Length
                });
            }
        }

        // Sort by position
        mentions = mentions.OrderBy(m => m.Index).ToList();

        // Build text without mentions for context
        if (mentions.Count > 0)
        {
            var sb = new StringBuilder(input);
            // Remove mentions from text (process in reverse to preserve indices)
            foreach (var mention in mentions.OrderByDescending(m => m.Index))
            {
                sb.Remove(mention.Index, mention.Length);
                sb.Insert(mention.Index, $"[{mention.Type}:{mention.Identifier}]");
            }
            textWithoutMentions = sb.ToString();
        }

        return new ParsedMentions
        {
            OriginalText = input,
            TextWithoutMentions = textWithoutMentions,
            Mentions = mentions
        };
    }

    public async Task<IReadOnlyList<MentionCompletion>> GetCompletionsAsync(
        string query,
        MentionContext context,
        int maxResults = 10,
        CancellationToken ct = default)
    {
        var completions = new List<MentionCompletion>();
        var queryLower = query.ToLowerInvariant();

        // Get file completions
        if (context.AvailableFiles != null)
        {
            var fileMatches = _fuzzyMatcher.Match(queryLower, context.AvailableFiles, maxResults / 2);
            completions.AddRange(fileMatches.Select(m => new MentionCompletion
            {
                Type = MentionType.File,
                DisplayText = m.Value,
                InsertText = $"@{m.Value}",
                Description = "File",
                Score = m.Score,
                Icon = "📄"
            }));
        }

        // Get agent completions
        if (context.AvailableAgents != null)
        {
            var agentMatches = _fuzzyMatcher.Match(queryLower, context.AvailableAgents.Select(a => a.Id), maxResults / 3);
            completions.AddRange(agentMatches.Select(m => new MentionCompletion
            {
                Type = MentionType.Agent,
                DisplayText = context.AvailableAgents.First(a => a.Id == m.Value).Name,
                InsertText = $"@agent/{m.Value}",
                Description = context.AvailableAgents.First(a => a.Id == m.Value).Description,
                Score = m.Score,
                Icon = "🤖"
            }));
        }

        // Get symbol completions from current file
        if (context.CurrentFileSymbols != null)
        {
            var symbolMatches = _fuzzyMatcher.Match(queryLower, context.CurrentFileSymbols.Select(s => s.Name), maxResults / 4);
            completions.AddRange(symbolMatches.Select(m => new MentionCompletion
            {
                Type = MentionType.Symbol,
                DisplayText = m.Value,
                InsertText = $"@symbol/{m.Value}",
                Description = context.CurrentFileSymbols.First(s => s.Name == m.Value).Type,
                Score = m.Score,
                Icon = "🔣"
            }));
        }

        // Get directory completions
        if (context.AvailableDirectories != null)
        {
            var dirMatches = _fuzzyMatcher.Match(queryLower, context.AvailableDirectories, maxResults / 4);
            completions.AddRange(dirMatches.Select(m => new MentionCompletion
            {
                Type = MentionType.Directory,
                DisplayText = m.Value,
                InsertText = $"@dir/{m.Value}",
                Description = "Directory",
                Score = m.Score,
                Icon = "📁"
            }));
        }

        // Sort by score and return top results
        return completions
            .OrderByDescending(c => c.Score)
            .Take(maxResults)
            .ToList();
    }

    public async Task<ResolvedMention?> ResolveAsync(
        Mention mention,
        MentionContext context,
        CancellationToken ct = default)
    {
        try
        {
            return mention.Type switch
            {
                MentionType.File => await ResolveFileAsync(mention, context, ct),
                MentionType.Agent => await ResolveAgentAsync(mention, context, ct),
                MentionType.Symbol => await ResolveSymbolAsync(mention, context, ct),
                MentionType.Context => await ResolveContextAsync(mention, context, ct),
                MentionType.Directory => await ResolveDirectoryAsync(mention, context, ct),
                _ => null
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to resolve mention {Mention}", mention.RawText);
            return null;
        }
    }

    public async Task<string> ExpandMentionsAsync(
        string input,
        MentionContext context,
        CancellationToken ct = default)
    {
        var parsed = ParseMentions(input);
        if (parsed.Mentions.Count == 0)
        {
            return input;
        }

        var sb = new StringBuilder();
        var lastIndex = 0;

        foreach (var mention in parsed.Mentions.OrderBy(m => m.Index))
        {
            // Add text before mention
            sb.Append(input.Substring(lastIndex, mention.Index - lastIndex));

            // Resolve and expand mention
            var resolved = await ResolveAsync(mention, context, ct);
            if (resolved != null)
            {
                sb.AppendLine();
                sb.AppendLine($"<!-- BEGIN {mention.Type} mention: {mention.Identifier} -->");
                sb.AppendLine(resolved.Content);
                sb.AppendLine($"<!-- END {mention.Type} mention -->");
                sb.AppendLine();
            }
            else
            {
                // Keep original if resolution failed
                sb.Append(mention.RawText);
            }

            lastIndex = mention.Index + mention.Length;
        }

        // Add remaining text
        if (lastIndex < input.Length)
        {
            sb.Append(input.Substring(lastIndex));
        }

        return sb.ToString();
    }

    private async Task<ResolvedMention?> ResolveFileAsync(Mention mention, MentionContext context, CancellationToken ct)
    {
        var content = await _fileResolver.ResolveAsync(mention.Identifier, context.WorkspacePath, ct);
        if (content == null) return null;

        return new ResolvedMention
        {
            Type = MentionType.File,
            Identifier = mention.Identifier,
            Content = $"File: {mention.Identifier}\n```\n{content}\n```",
            Metadata = new Dictionary<string, object>
            {
                ["file_path"] = mention.Identifier,
                ["content_length"] = content.Length
            }
        };
    }

    private async Task<ResolvedMention?> ResolveAgentAsync(Mention mention, MentionContext context, CancellationToken ct)
    {
        var agent = await _agentResolver.ResolveAsync(mention.Identifier, ct);
        if (agent == null) return null;

        return new ResolvedMention
        {
            Type = MentionType.Agent,
            Identifier = mention.Identifier,
            Content = $"Agent: {agent.Name}\nDescription: {agent.Description}\nCapabilities: {string.Join(", ", agent.Capabilities)}",
            Metadata = new Dictionary<string, object>
            {
                ["agent_id"] = agent.Id,
                ["agent_name"] = agent.Name
            }
        };
    }

    private async Task<ResolvedMention?> ResolveSymbolAsync(Mention mention, MentionContext context, CancellationToken ct)
    {
        var symbol = await _symbolResolver.ResolveAsync(mention.Identifier, context.CurrentFilePath, ct);
        if (symbol == null) return null;

        return new ResolvedMention
        {
            Type = MentionType.Symbol,
            Identifier = mention.Identifier,
            Content = $"Symbol: {symbol.Name} ({symbol.Type})\n```\n{symbol.Definition}\n```",
            Metadata = new Dictionary<string, object>
            {
                ["symbol_name"] = symbol.Name,
                ["symbol_type"] = symbol.Type,
                ["file_path"] = symbol.FilePath
            }
        };
    }

    private Task<ResolvedMention> ResolveContextAsync(Mention mention, MentionContext context, CancellationToken ct)
    {
        var contextContent = mention.Identifier.ToLowerInvariant() switch
        {
            "workspace" => $"Workspace: {context.WorkspacePath}",
            "current_file" => $"Current file: {context.CurrentFilePath}",
            "selection" => $"Selection: {context.CurrentSelection ?? "None"}",
            "git_status" => "Git status: (would show git diff stats)",
            "errors" => "Current errors: (would show diagnostics)",
            _ => $"Context: {mention.Identifier}"
        };

        return Task.FromResult(new ResolvedMention
        {
            Type = MentionType.Context,
            Identifier = mention.Identifier,
            Content = contextContent,
            Metadata = new Dictionary<string, object> { ["context_type"] = mention.Identifier }
        });
    }

    private async Task<ResolvedMention?> ResolveDirectoryAsync(Mention mention, MentionContext context, CancellationToken ct)
    {
        var files = await _fileResolver.ListFilesAsync(mention.Identifier, context.WorkspacePath, ct);
        if (files == null) return null;

        var fileList = string.Join("\n", files.Take(20).Select(f => $"- {f}"));
        if (files.Count > 20)
        {
            fileList += $"\n... and {files.Count - 20} more files";
        }

        return new ResolvedMention
        {
            Type = MentionType.Directory,
            Identifier = mention.Identifier,
            Content = $"Directory: {mention.Identifier}\nFiles:\n{fileList}",
            Metadata = new Dictionary<string, object>
            {
                ["directory"] = mention.Identifier,
                ["file_count"] = files.Count
            }
        };
    }
}

/// <summary>
/// Fuzzy matcher implementation using fzf algorithm.
/// </summary>
public interface IFuzzyMatcher
{
    IReadOnlyList<FuzzyMatch> Match(string query, IEnumerable<string> candidates, int maxResults);
}

public sealed class FuzzyMatcher : IFuzzyMatcher
{
    public IReadOnlyList<FuzzyMatch> Match(string query, IEnumerable<string> candidates, int maxResults)
    {
        var matches = new List<FuzzyMatch>();
        var queryChars = query.ToCharArray();

        foreach (var candidate in candidates)
        {
            var candidateLower = candidate.ToLowerInvariant();
            var score = CalculateScore(queryChars, candidateLower);
            
            if (score > 0)
            {
                matches.Add(new FuzzyMatch { Value = candidate, Score = score });
            }
        }

        return matches
            .OrderByDescending(m => m.Score)
            .Take(maxResults)
            .ToList();
    }

    private int CalculateScore(char[] queryChars, string candidate)
    {
        int score = 0;
        int queryIndex = 0;
        int consecutiveBonus = 0;
        bool isStartOfWord = true;

        for (int i = 0; i < candidate.Length && queryIndex < queryChars.Length; i++)
        {
            char c = candidate[i];
            
            if (c == queryChars[queryIndex])
            {
                // Base match
                score += 10;
                
                // Consecutive character bonus
                if (i > 0 && queryIndex > 0)
                {
                    consecutiveBonus += 5;
                    score += consecutiveBonus;
                }
                else
                {
                    consecutiveBonus = 0;
                }
                
                // Start of word bonus
                if (isStartOfWord || i == 0)
                {
                    score += 15;
                }
                
                // Exact case match bonus
                if (c == queryChars[queryIndex])
                {
                    score += 2;
                }

                queryIndex++;
                isStartOfWord = false;
            }
            else
            {
                consecutiveBonus = 0;
                isStartOfWord = !char.IsLetterOrDigit(c);
            }
        }

        // Penalty for length difference
        if (queryIndex == queryChars.Length)
        {
            score -= (candidate.Length - queryChars.Length) * 2;
            return Math.Max(1, score);
        }

        return 0; // Not all query chars matched
    }
}

// Supporting interfaces and types

public interface IFileResolver
{
    Task<string?> ResolveAsync(string filePath, string? basePath, CancellationToken ct);
    Task<IReadOnlyList<string>?> ListFilesAsync(string directory, string? basePath, CancellationToken ct);
}

public interface IAgentResolver
{
    Task<AgentInfo?> ResolveAsync(string agentId, CancellationToken ct);
}

public interface ISymbolResolver
{
    Task<SymbolInfo?> ResolveAsync(string symbolName, string? filePath, CancellationToken ct);
}

public enum MentionType
{
    File,
    Agent,
    Symbol,
    Context,
    Directory,
    Generic
}

public sealed class Mention
{
    public MentionType Type { get; set; }
    public string RawText { get; set; } = "";
    public string Identifier { get; set; } = "";
    public int Index { get; set; }
    public int Length { get; set; }
}

public sealed class ParsedMentions
{
    public string OriginalText { get; set; } = "";
    public string TextWithoutMentions { get; set; } = "";
    public IReadOnlyList<Mention> Mentions { get; set; } = new List<Mention>();
}

public sealed class MentionCompletion
{
    public MentionType Type { get; set; }
    public string DisplayText { get; set; } = "";
    public string InsertText { get; set; } = "";
    public string Description { get; set; } = "";
    public int Score { get; set; }
    public string Icon { get; set; } = "";
}

public sealed class ResolvedMention
{
    public MentionType Type { get; set; }
    public string Identifier { get; set; } = "";
    public string Content { get; set; } = "";
    public Dictionary<string, object> Metadata { get; set; } = new();
}

public sealed class MentionContext
{
    public string? WorkspacePath { get; set; }
    public string? CurrentFilePath { get; set; }
    public string? CurrentSelection { get; set; }
    public IEnumerable<string>? AvailableFiles { get; set; }
    public IEnumerable<AgentInfo>? AvailableAgents { get; set; }
    public IEnumerable<SymbolInfo>? CurrentFileSymbols { get; set; }
    public IEnumerable<string>? AvailableDirectories { get; set; }
}

public sealed class AgentInfo
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public List<string> Capabilities { get; set; } = new();
}

public sealed class SymbolInfo
{
    public string Name { get; set; } = "";
    public string Type { get; set; } = "";
    public string Definition { get; set; } = "";
    public string? FilePath { get; set; }
}

public sealed class FuzzyMatch
{
    public string Value { get; set; } = "";
    public int Score { get; set; }
}
