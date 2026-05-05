using MediatR;
using Libr4.IDE.Application.SemanticBlame.Commands;
using Libr4.IDE.Application.SemanticBlame.DTOs;
using Libr4.AI.Infrastructure.AI;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;

namespace Libr4.IDE.Application.SemanticBlame.Handlers;

/// <summary>
/// Handler for RunBlameCommand - AI-powered semantic git blame analysis
/// </summary>
public class RunBlameCommandHandler : IRequestHandler<RunBlameCommand, SemanticBlameDto>
{
    private readonly IAIService _aiService;
    private readonly ILogger<RunBlameCommandHandler> _logger;

    public RunBlameCommandHandler(
        IAIService aiService,
        ILogger<RunBlameCommandHandler> logger)
    {
        _aiService = aiService;
        _logger = logger;
    }

    public async Task<SemanticBlameDto> Handle(RunBlameCommand request, CancellationToken ct)
    {
        _logger.LogInformation(
            "Starting semantic blame analysis for {FilePath} in {WorkspacePath}",
            request.FilePath, request.WorkspacePath);

        var blameId = Guid.NewGuid().ToString("N")[..8];
        var startedAt = DateTime.UtcNow;

        try
        {
            // Get git blame information
            var blameEntries = await GetGitBlameAsync(request.WorkspacePath, request.FilePath, ct);
            
            // Enhance with AI semantic analysis
            var enhancedEntries = await EnhanceWithSemanticAnalysisAsync(blameEntries, request.FilePath, ct);
            
            // Analyze code evolution
            var evolution = await AnalyzeCodeEvolutionAsync(request.WorkspacePath, request.FilePath, ct);
            
            // AI summary of the file's history and ownership
            var aiSummary = await GenerateAISummaryAsync(enhancedEntries, evolution, ct);

            _logger.LogInformation(
                "Semantic blame analysis {BlameId} completed. Entries: {EntryCount}, Contributors: {ContributorCount}",
                blameId, enhancedEntries.Count, evolution?.ContributorStats?.Count ?? 0);

            return new SemanticBlameDto
            {
                Id = Guid.NewGuid(),
                BlameId = blameId,
                FilePath = request.FilePath,
                Entries = enhancedEntries,
                Evolution = evolution,
                Status = "Completed",
                CreatedAt = startedAt,
                CompletedAt = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Semantic blame analysis {BlameId} failed", blameId);
            
            return new SemanticBlameDto
            {
                Id = Guid.NewGuid(),
                BlameId = blameId,
                FilePath = request.FilePath,
                Entries = new List<BlameEntryDto>(),
                Status = "Failed",
                CreatedAt = startedAt,
                CompletedAt = DateTime.UtcNow
            };
        }
    }

    private async Task<List<BlameEntryDto>> GetGitBlameAsync(
        string workspacePath,
        string filePath,
        CancellationToken ct)
    {
        var entries = new List<BlameEntryDto>();
        var fullPath = Path.Combine(workspacePath, filePath);
        
        if (!File.Exists(fullPath))
        {
            _logger.LogWarning("File not found: {FullPath}", fullPath);
            return entries;
        }

        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "git",
                Arguments = $"blame --line-porcelain \"{filePath}\"",
                WorkingDirectory = workspacePath,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = new Process { StartInfo = psi };
            var output = new StringBuilder();
            
            process.OutputDataReceived += (sender, e) =>
            {
                if (e.Data != null) output.AppendLine(e.Data);
            };

            process.Start();
            process.BeginOutputReadLine();
            await Task.Run(() => process.WaitForExit(30000), ct);

            if (process.ExitCode == 0)
            {
                entries = ParseBlameOutput(output.ToString(), filePath);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Git blame failed for {FilePath}", filePath);
        }

        return entries;
    }

    private List<BlameEntryDto> ParseBlameOutput(string blameOutput, string filePath)
    {
        var entries = new List<BlameEntryDto>();
        var lines = blameOutput.Split('\n');

        string? hash = null;
        string author = string.Empty;
        DateTime commitDate = default;
        string commitMessage = string.Empty;
        int lineNumber = 0;

        void FlushEntry()
        {
            if (hash == null) return;
            entries.Add(new BlameEntryDto
            {
                Id = Guid.NewGuid(),
                FilePath = filePath,
                CommitHash = hash,
                LineNumber = lineNumber,
                Author = author,
                CommitDate = commitDate,
                CommitMessage = commitMessage
            });
        }

        foreach (var line in lines)
        {
            if (Regex.IsMatch(line, @"^[a-f0-9]{40}"))
            {
                FlushEntry();
                var parts = line.Split(' ');
                hash = parts[0];
                author = string.Empty;
                commitDate = default;
                commitMessage = string.Empty;
                lineNumber++;
            }
            else if (hash != null)
            {
                if (line.StartsWith("author "))
                    author = line.Substring(7).Trim();
                else if (line.StartsWith("author-time ") &&
                         long.TryParse(line.Substring(12).Trim(), out var ts))
                    commitDate = DateTimeOffset.FromUnixTimeSeconds(ts).DateTime;
                else if (line.StartsWith("summary "))
                    commitMessage = line.Substring(8).Trim();
            }
        }

        FlushEntry();
        return entries;
    }

    private async Task<List<BlameEntryDto>> EnhanceWithSemanticAnalysisAsync(
        List<BlameEntryDto> entries,
        string filePath,
        CancellationToken ct)
    {
        if (entries.Count == 0) return entries;

        try
        {
            // Group by author for AI analysis
            var authorGroups = entries.GroupBy(e => e.Author)
                .Select(g => new { Author = g.Key, Count = g.Count(), Lines = g.Select(l => l.LineNumber).ToList() })
                .ToList();

            var prompt = $@"
Analyze the code ownership pattern for {filePath}:

Contributors:
{string.Join("\n", authorGroups.Select(a => $"- {a.Author}: {a.Count} lines"))}

Total lines: {entries.Count}

Provide insights on:
1. Primary maintainers (authors with >50% of code)
2. Knowledge distribution (concentrated vs distributed)
3. Recent vs legacy contributors
4. Ownership recommendations for code reviews

Keep response under 200 words.";

            var analysis = await _aiService.GenerateCompletionAsync(prompt);
            
            // Add analysis as metadata to first entry
            if (entries.Count > 0)
            {
                var e0 = entries[0];
                entries[0] = new BlameEntryDto
                {
                    Id = e0.Id,
                    FilePath = e0.FilePath,
                    CommitHash = e0.CommitHash,
                    LineNumber = e0.LineNumber,
                    Author = e0.Author,
                    CommitDate = e0.CommitDate,
                    CommitMessage = e0.CommitMessage + $"\n[AI_ANALYSIS] {analysis}"
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Semantic analysis failed for {FilePath}", filePath);
        }

        return entries;
    }

    private async Task<CodeEvolutionDto?> AnalyzeCodeEvolutionAsync(
        string workspacePath,
        string filePath,
        CancellationToken ct)
    {
        try
        {
            // Get commit history
            var psi = new ProcessStartInfo
            {
                FileName = "git",
                Arguments = $"log --format=\"%H|%an|%at|%s\" -- \"{filePath}\"",
                WorkingDirectory = workspacePath,
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = new Process { StartInfo = psi };
            var output = new StringBuilder();
            
            process.OutputDataReceived += (sender, e) =>
            {
                if (e.Data != null) output.AppendLine(e.Data);
            };

            process.Start();
            process.BeginOutputReadLine();
            await Task.Run(() => process.WaitForExit(30000), ct);

            var commits = new List<GitCommitDto>();
            var contributorStats = new Dictionary<string, int>();

            foreach (var line in output.ToString().Split('\n', StringSplitOptions.RemoveEmptyEntries))
            {
                var parts = line.Split('|');
                if (parts.Length >= 4)
                {
                    if (long.TryParse(parts[2], out var timestamp))
                    {
                        commits.Add(new GitCommitDto
                        {
                            CommitHash = parts[0],
                            Author = parts[1],
                            CommitDate = DateTimeOffset.FromUnixTimeSeconds(timestamp).DateTime,
                            Message = parts[3]
                        });

                        contributorStats[parts[1]] = contributorStats.GetValueOrDefault(parts[1]) + 1;
                    }
                }
            }

            return new CodeEvolutionDto
            {
                Id = Guid.NewGuid(),
                FilePath = filePath,
                Commits = commits,
                ContributorStats = contributorStats.ToDictionary(kvp => kvp.Key, kvp => kvp.Value)
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Code evolution analysis failed for {FilePath}", filePath);
            return null;
        }
    }

    private async Task<string> GenerateAISummaryAsync(
        List<BlameEntryDto> entries,
        CodeEvolutionDto? evolution,
        CancellationToken ct)
    {
        if (entries.Count == 0) return "No data available";

        var topContributors = evolution?.ContributorStats?.OrderByDescending(x => x.Value).Take(3)
            .Select(x => $"{x.Key}: {x.Value} commits").ToList() ?? new List<string>();

        var prompt = $@"
Summarize code ownership for {entries.First().FilePath}:

Total lines: {entries.Count}
Unique contributors: {entries.Select(e => e.Author).Distinct().Count()}
Top contributors: {string.Join(", ", topContributors)}

Generate a one-line summary for who to contact for questions about this file.";

        try
        {
            return await _aiService.GenerateCompletionAsync(prompt);
        }
        catch
        {
            return "AI summary unavailable";
        }
    }
}
