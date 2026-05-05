using System.Diagnostics;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Neo4j.Driver;

namespace Libr4.IDE.Infrastructure.Memory;

/// <summary>
/// Semantic Blame - Understanding WHY code exists, not just WHERE
/// Integrates Git history with Neo4j for temporal code analysis
/// </summary>
public interface ISemanticBlameService
{
    /// <summary>
    /// Enrich Neo4j with Git commit history
    /// </summary>
    Task IngestGitHistoryAsync(string repositoryPath, string branch = "main", CancellationToken ct = default);
    
    /// <summary>
    /// Get semantic context for a file/line - why was this code added
    /// </summary>
    Task<SemanticBlameContext?> GetSemanticContextAsync(string filePath, int lineNumber, CancellationToken ct = default);
    
    /// <summary>
    /// Check if modification is safe based on historical context
    /// </summary>
    Task<ModificationRisk> AssessModificationRiskAsync(string filePath, string proposedChange, CancellationToken ct = default);
    
    /// <summary>
    /// Find related changes - what else changed when this was added
    /// </summary>
    Task<IReadOnlyList<RelatedChange>> FindRelatedChangesAsync(string filePath, int lineNumber, CancellationToken ct = default);
    
    /// <summary>
    /// Get temporal evolution of a code block
    /// </summary>
    Task<CodeEvolution> GetCodeEvolutionAsync(string filePath, int startLine, int endLine, CancellationToken ct = default);
}

/// <summary>
/// Implementation integrating Git + Neo4j
/// </summary>
public class SemanticBlameService : ISemanticBlameService
{
    private readonly IDriver _neo4jDriver;
    private readonly ILogger<SemanticBlameService> _logger;
    private readonly IGitCommandRunner _gitRunner;

    public SemanticBlameService(
        IDriver neo4jDriver,
        IGitCommandRunner gitRunner,
        ILogger<SemanticBlameService> logger)
    {
        _neo4jDriver = neo4jDriver;
        _gitRunner = gitRunner;
        _logger = logger;
    }

    public async Task IngestGitHistoryAsync(string repositoryPath, string branch = "main", CancellationToken ct = default)
    {
        _logger.LogInformation("Ingesting Git history for {Repo} on branch {Branch}", repositoryPath, branch);
        
        // Get commit log with full metadata
        var logOutput = await _gitRunner.RunCommandAsync(
            repositoryPath, 
            $"log {branch} --pretty=format:'%H|%an|%ae|%at|%s|%b|---END---' --reverse",
            ct);

        var commits = ParseCommits(logOutput);
        
        foreach (var commit in commits)
        {
            // Get files changed in this commit
            var diffOutput = await _gitRunner.RunCommandAsync(
                repositoryPath,
                $"show {commit.Hash} --name-only --format=''",
                ct);
            
            var files = diffOutput.Split('\n', StringSplitOptions.RemoveEmptyEntries)
                .Where(f => !string.IsNullOrWhiteSpace(f))
                .ToList();

            // Get detailed diff for each file
            foreach (var file in files.Take(50)) // Limit to prevent overload
            {
                var fileDiff = await _gitRunner.RunCommandAsync(
                    repositoryPath,
                    $"show {commit.Hash} -- {file}",
                    ct);
                
                var changes = ParseDiff(fileDiff, file);
                
                // Store in Neo4j with temporal relationships
                await StoreCommitInNeo4jAsync(commit, file, changes, ct);
            }
        }
        
        _logger.LogInformation("Ingested {Count} commits into Neo4j", commits.Count);
    }

    public async Task<SemanticBlameContext?> GetSemanticContextAsync(string filePath, int lineNumber, CancellationToken ct = default)
    {
        var query = @"
            MATCH (f:File {path: $filePath})-[:CONTAINS]->(l:Line {number: $lineNumber})
            MATCH (l)-[:ADDED_IN]->(c:Commit)
            OPTIONAL MATCH (c)-[:FIXES]->(b:Bug)
            OPTIONAL MATCH (c)-[:AUTHORED_BY]->(a:Author)
            OPTIONAL MATCH (c)-[:TOUCHES]->(related:File)
            RETURN c.hash as commitHash,
                   c.message as message,
                   c.timestamp as timestamp,
                   a.name as author,
                   b.id as bugId,
                   b.description as bugDescription,
                   collect(DISTINCT related.path) as relatedFiles
            ORDER BY c.timestamp DESC
            LIMIT 1";

        var session = _neo4jDriver.AsyncSession();
        try
        {
            var result = await session.RunAsync(query, new { filePath, lineNumber });
            var record = await result.SingleAsync();
            
            if (record == null) return null;

            return new SemanticBlameContext
            {
                FilePath = filePath,
                LineNumber = lineNumber,
                CommitHash = record["commitHash"].As<string>(),
                CommitMessage = record["message"].As<string>(),
                Author = record["author"].As<string>(),
                Timestamp = DateTimeOffset.FromUnixTimeSeconds(record["timestamp"].As<long>()).DateTime,
                RelatedBug = record["bugId"]?.As<string>() is string bugId 
                    ? new BugReference { Id = bugId, Description = record["bugDescription"].As<string>() }
                    : null,
                RelatedFiles = record["relatedFiles"].As<List<string>>(),
                ContextExplanation = GenerateContextExplanation(record)
            };
        }
        finally
        {
            await session.CloseAsync();
        }
    }

    public async Task<ModificationRisk> AssessModificationRiskAsync(string filePath, string proposedChange, CancellationToken ct = default)
    {
        var session = _neo4jDriver.AsyncSession();
        try
        {
            // Check if this file has stability-related commits
            var stabilityQuery = @"
                MATCH (f:File {path: $filePath})
                MATCH (f)-[:CONTAINS]->(l:Line)-[:ADDED_IN]->(c:Commit)
                WHERE c.message CONTAINS 'fix' 
                   OR c.message CONTAINS 'stability'
                   OR c.message CONTAINS 'critical'
                   OR c.message CONTAINS 'security'
                   OR c.message CONTAINS 'payment'
                RETURN count(DISTINCT c) as stabilityCommits,
                       collect(DISTINCT c.message)[0..5] as recentStabilityMessages
                ORDER BY stabilityCommits DESC";

            var stabilityResult = await session.RunAsync(stabilityQuery, new { filePath });
            var stabilityRecord = await stabilityResult.SingleAsync();
            var stabilityCommits = stabilityRecord["stabilityCommits"].As<int>();
            var messages = stabilityRecord["recentStabilityMessages"].As<List<string>>();

            // Check if proposed change touches critical lines
            var criticalQuery = @"
                MATCH (f:File {path: $filePath})-[:CONTAINS]->(l:Line)-[:ADDED_IN]->(c:Commit)
                WHERE c.isCritical = true
                RETURN count(l) as criticalLines";
            
            var criticalResult = await session.RunAsync(criticalQuery, new { filePath });
            var criticalRecord = await criticalResult.SingleAsync();
            var criticalLines = criticalRecord["criticalLines"].As<int>();

            // Calculate risk level
            var riskLevel = CalculateRiskLevel(stabilityCommits, criticalLines, messages);
            
            return new ModificationRisk
            {
                FilePath = filePath,
                RiskLevel = riskLevel,
                StabilityCommits = stabilityCommits,
                CriticalLinesCount = criticalLines,
                WarningMessage = GenerateWarningMessage(riskLevel, messages),
                RelatedCommits = messages.Take(3).ToList(),
                SuggestedReviewers = await FindDomainExpertsAsync(filePath, session, ct)
            };
        }
        finally
        {
            await session.CloseAsync();
        }
    }

    public async Task<IReadOnlyList<RelatedChange>> FindRelatedChangesAsync(string filePath, int lineNumber, CancellationToken ct = default)
    {
        var query = @"
            MATCH (f:File {path: $filePath})-[:CONTAINS]->(l:Line {number: $lineNumber})-[:ADDED_IN]->(c:Commit)
            MATCH (c)-[:TOUCHES]->(related:File)-[:CONTAINS]->(rl:Line)
            WHERE related.path <> $filePath
            RETURN DISTINCT related.path as filePath,
                   c.timestamp as timestamp,
                   c.message as commitMessage,
                   c.hash as commitHash
            ORDER BY c.timestamp DESC
            LIMIT 10";

        var session = _neo4jDriver.AsyncSession();
        try
        {
            var result = await session.RunAsync(query, new { filePath, lineNumber });
            var records = await result.ToListAsync();
            
            return records.Select(r => new RelatedChange
            {
                FilePath = r["filePath"].As<string>(),
                Timestamp = DateTimeOffset.FromUnixTimeSeconds(r["timestamp"].As<long>()).DateTime,
                CommitMessage = r["commitMessage"].As<string>(),
                CommitHash = r["commitHash"].As<string>(),
                RelationshipType = "SameCommit"  // These files changed together
            }).ToList();
        }
        finally
        {
            await session.CloseAsync();
        }
    }

    public async Task<CodeEvolution> GetCodeEvolutionAsync(string filePath, int startLine, int endLine, CancellationToken ct = default)
    {
        var query = @"
            MATCH (f:File {path: $filePath})-[:CONTAINS]->(l:Line)
            WHERE l.number >= $startLine AND l.number <= $endLine
            MATCH (l)-[r:ADDED_IN|MODIFIED_IN|DELETED_IN]->(c:Commit)
            RETURN c.timestamp as timestamp,
                   c.hash as commitHash,
                   c.message as message,
                   c.author as author,
                   type(r) as changeType,
                   l.number as lineNumber,
                   l.content as content
            ORDER BY c.timestamp, l.number";

        var session = _neo4jDriver.AsyncSession();
        try
        {
            var result = await session.RunAsync(query, new { filePath, startLine, endLine });
            var records = await result.ToListAsync();

            var evolutionSteps = records
                .GroupBy(r => r["commitHash"].As<string>())
                .Select(g => new EvolutionStep
                {
                    CommitHash = g.Key,
                    Timestamp = DateTimeOffset.FromUnixTimeSeconds(g.First()["timestamp"].As<long>()).DateTime,
                    Author = g.First()["author"].As<string>(),
                    Message = g.First()["message"].As<string>(),
                    Changes = g.Select(r => new LineChange
                    {
                        LineNumber = r["lineNumber"].As<int>(),
                        ChangeType = r["changeType"].As<string>(),
                        Content = r["content"].As<string>()
                    }).ToList()
                })
                .OrderBy(e => e.Timestamp)
                .ToList();

            return new CodeEvolution
            {
                FilePath = filePath,
                StartLine = startLine,
                EndLine = endLine,
                EvolutionSteps = evolutionSteps,
                TotalModifications = evolutionSteps.Count,
                StabilityScore = CalculateStabilityScore(evolutionSteps)
            };
        }
        finally
        {
            await session.CloseAsync();
        }
    }

    // Private helper methods
    private List<GitCommit> ParseCommits(string logOutput)
    {
        var commits = new List<GitCommit>();
        var entries = logOutput.Split("---END---", StringSplitOptions.RemoveEmptyEntries);
        
        foreach (var entry in entries)
        {
            var parts = entry.Split('|');
            if (parts.Length >= 5)
            {
                commits.Add(new GitCommit
                {
                    Hash = parts[0].Trim(),
                    Author = parts[1].Trim(),
                    Email = parts[2].Trim(),
                    Timestamp = long.Parse(parts[3].Trim()),
                    Subject = parts[4].Trim(),
                    Body = parts.Length > 5 ? parts[5].Trim() : ""
                });
            }
        }
        
        return commits;
    }

    private List<FileChange> ParseDiff(string diffOutput, string filePath)
    {
        var changes = new List<FileChange>();
        var lines = diffOutput.Split('\n');
        int currentLine = 0;
        
        foreach (var line in lines)
        {
            if (line.StartsWith("@@"))
            {
                // Parse hunk header: @@ -1,3 +1,5 @@
                var match = Regex.Match(line, @"@@ -(\d+)(?:,(\d+))? \+(\d+)(?:,(\d+))? @@");
                if (match.Success)
                {
                    currentLine = int.Parse(match.Groups[3].Value);
                }
            }
            else if (line.StartsWith("+"))
            {
                changes.Add(new FileChange
                {
                    Type = ChangeType.Added,
                    LineNumber = currentLine,
                    Content = line.Substring(1)
                });
                currentLine++;
            }
            else if (line.StartsWith("-"))
            {
                changes.Add(new FileChange
                {
                    Type = ChangeType.Deleted,
                    LineNumber = currentLine,
                    Content = line.Substring(1)
                });
            }
            else if (!line.StartsWith("\\") && !string.IsNullOrWhiteSpace(line))
            {
                currentLine++;
            }
        }
        
        return changes;
    }

    private async Task StoreCommitInNeo4jAsync(GitCommit commit, string filePath, List<FileChange> changes, CancellationToken ct)
    {
        var session = _neo4jDriver.AsyncSession();
        try
        {
            var query = @"
                MERGE (c:Commit {hash: $hash})
                SET c.message = $message,
                    c.timestamp = $timestamp,
                    c.author = $author,
                    c.isCritical = $isCritical
                MERGE (a:Author {email: $email})
                SET a.name = $author
                MERGE (c)-[:AUTHORED_BY]->(a)
                MERGE (f:File {path: $filePath})
                MERGE (c)-[:TOUCHES]->(f)
                WITH c, f
                UNWIND $changes as change
                MERGE (l:Line {file: $filePath, number: change.lineNumber})
                SET l.content = change.content,
                    l.lastModified = $timestamp
                FOREACH (ignoreMe IN CASE WHEN change.type = 'Added' THEN [1] ELSE [] END |
                    MERGE (l)-[:ADDED_IN]->(c)
                )
                FOREACH (ignoreMe IN CASE WHEN change.type = 'Deleted' THEN [1] ELSE [] END |
                    MERGE (l)-[:DELETED_IN]->(c)
                )";

            var isCritical = IsCriticalCommit(commit.Subject);
            
            await session.RunAsync(query, new
            {
                hash = commit.Hash,
                message = commit.Subject,
                timestamp = commit.Timestamp,
                author = commit.Author,
                email = commit.Email,
                filePath,
                changes = changes.Select(c => new
                {
                    type = c.Type.ToString(),
                    lineNumber = c.LineNumber,
                    content = c.Content
                }).ToList(),
                isCritical
            });
        }
        finally
        {
            await session.CloseAsync();
        }
    }

    private bool IsCriticalCommit(string message)
    {
        var criticalKeywords = new[] { "fix", "security", "critical", "payment", "escrow", "auth", "vulnerability" };
        return criticalKeywords.Any(k => message.Contains(k, StringComparison.OrdinalIgnoreCase));
    }

    private string GenerateContextExplanation(IRecord record)
    {
        var message = record["message"].As<string>();
        var author = record["author"].As<string>();
        var bugId = record["bugId"]?.As<string>();
        
        if (!string.IsNullOrEmpty(bugId))
        {
            return $"This code was added by {author} to fix bug #{bugId}. Original commit message: {message}";
        }
        
        if (message.Contains("fix", StringComparison.OrdinalIgnoreCase))
        {
            return $"This code was added by {author} as a fix. Message: {message}";
        }
        
        return $"This code was added by {author}. Message: {message}";
    }

    private RiskLevel CalculateRiskLevel(int stabilityCommits, int criticalLines, List<string> messages)
    {
        var score = 0;
        
        if (stabilityCommits > 5) score += 3;
        else if (stabilityCommits > 2) score += 2;
        else if (stabilityCommits > 0) score += 1;
        
        if (criticalLines > 10) score += 3;
        else if (criticalLines > 5) score += 2;
        else if (criticalLines > 0) score += 1;
        
        if (messages.Any(m => m.Contains("payment", StringComparison.OrdinalIgnoreCase))) score += 2;
        if (messages.Any(m => m.Contains("security", StringComparison.OrdinalIgnoreCase))) score += 3;
        
        return score switch
        {
            >= 6 => RiskLevel.Critical,
            >= 4 => RiskLevel.High,
            >= 2 => RiskLevel.Medium,
            _ => RiskLevel.Low
        };
    }

    private string GenerateWarningMessage(RiskLevel level, List<string> messages)
    {
        var relevantMessages = messages.Where(m => 
            m.Contains("fix", StringComparison.OrdinalIgnoreCase) ||
            m.Contains("stability", StringComparison.OrdinalIgnoreCase) ||
            m.Contains("critical", StringComparison.OrdinalIgnoreCase)
        ).Take(3);
        
        var baseMessage = level switch
        {
            RiskLevel.Critical => "⚠️ CRITICAL: You are attempting to modify logic that was added for payment stability or security. Proceed with extreme caution.",
            RiskLevel.High => "⚠️ WARNING: This file has multiple stability-related changes. Review commit history before modifying.",
            RiskLevel.Medium => "⚠️ CAUTION: This file has been modified for stability reasons. Consider impact before changing.",
            _ => "ℹ️ Low risk modification."
        };

        if (relevantMessages.Any())
        {
            baseMessage += $"\nRecent stability commits:\n{string.Join("\n", relevantMessages.Select(m => $"  - {m}"))}";
        }
        
        return baseMessage;
    }

    private async Task<List<string>> FindDomainExpertsAsync(string filePath, IAsyncSession session, CancellationToken ct)
    {
        var query = @"
            MATCH (f:File {path: $filePath})<-[:TOUCHES]-(c:Commit)-[:AUTHORED_BY]->(a:Author)
            RETURN a.email as email, count(c) as commitCount
            ORDER BY commitCount DESC
            LIMIT 3";

        var result = await session.RunAsync(query, new { filePath });
        var records = await result.ToListAsync();
        
        return records.Select(r => r["email"].As<string>()).ToList();
    }

    private double CalculateStabilityScore(List<EvolutionStep> steps)
    {
        if (steps.Count <= 1) return 1.0;
        
        // More modifications = less stable
        var modificationRate = steps.Count / (double)(steps.Count + 5);
        return Math.Max(0, 1.0 - modificationRate);
    }
}

// Supporting types
public class SemanticBlameContext
{
    public string FilePath { get; set; } = string.Empty;
    public int LineNumber { get; set; }
    public string CommitHash { get; set; } = string.Empty;
    public string CommitMessage { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public BugReference? RelatedBug { get; set; }
    public List<string> RelatedFiles { get; set; } = new();
    public string ContextExplanation { get; set; } = string.Empty;
}

public class BugReference
{
    public string Id { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

public class ModificationRisk
{
    public string FilePath { get; set; } = string.Empty;
    public RiskLevel RiskLevel { get; set; }
    public int StabilityCommits { get; set; }
    public int CriticalLinesCount { get; set; }
    public string WarningMessage { get; set; } = string.Empty;
    public List<string> RelatedCommits { get; set; } = new();
    public List<string> SuggestedReviewers { get; set; } = new();
}

public enum RiskLevel { Low, Medium, High, Critical }

public class RelatedChange
{
    public string FilePath { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public string CommitMessage { get; set; } = string.Empty;
    public string CommitHash { get; set; } = string.Empty;
    public string RelationshipType { get; set; } = string.Empty;
}

public class CodeEvolution
{
    public string FilePath { get; set; } = string.Empty;
    public int StartLine { get; set; }
    public int EndLine { get; set; }
    public List<EvolutionStep> EvolutionSteps { get; set; } = new();
    public int TotalModifications { get; set; }
    public double StabilityScore { get; set; }
}

public class EvolutionStep
{
    public string CommitHash { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public string Author { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public List<LineChange> Changes { get; set; } = new();
}

public class LineChange
{
    public int LineNumber { get; set; }
    public string ChangeType { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
}

public class GitCommit
{
    public string Hash { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public long Timestamp { get; set; }
    public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
}

public class FileChange
{
    public ChangeType Type { get; set; }
    public int LineNumber { get; set; }
    public string Content { get; set; } = string.Empty;
}

public enum ChangeType { Added, Deleted, Modified }

public interface IGitCommandRunner
{
    Task<string> RunCommandAsync(string workingDirectory, string arguments, CancellationToken ct);
}

public class ProcessGitCommandRunner : IGitCommandRunner
{
    public async Task<string> RunCommandAsync(string workingDirectory, string arguments, CancellationToken ct)
    {
        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "git",
                Arguments = arguments,
                WorkingDirectory = workingDirectory,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };

        process.Start();
        var output = await process.StandardOutput.ReadToEndAsync(ct);
        await process.WaitForExitAsync(ct);

        return output;
    }
}
