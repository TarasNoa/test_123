/*
using MediatR;
using Libr4.IDE.Application.GitHubBootstrap.Commands;
using Libr4.IDE.Application.GitHubBootstrap.DTOs;
using Libr4.AI.Infrastructure.AI;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace Libr4.IDE.Application.GitHubBootstrap.Handlers;

/// <summary>
/// Handler for BootstrapProjectCommand - Clone and setup GitHub projects
/// </summary>
public class BootstrapProjectCommandHandler : IRequestHandler<BootstrapProjectCommand, BootstrapProjectDto>
{
    private readonly IAIService _aiService;
    private readonly ILogger<BootstrapProjectCommandHandler> _logger;

    public BootstrapProjectCommandHandler(IAIService aiService, ILogger<BootstrapProjectCommandHandler> logger)
    {
        _aiService = aiService;
        _logger = logger;
    }

    public async Task<BootstrapProjectDto> Handle(BootstrapProjectCommand request, CancellationToken ct)
    {
        _logger.LogInformation("Bootstrapping project from {RepoUrl} to workspace {WorkspaceId}",
            request.RepoUrl, request.WorkspaceId);

        var bootstrapId = Guid.NewGuid();
        var startedAt = DateTime.UtcNow;
        var repoName = ExtractRepoName(request.RepoUrl);
        var targetPath = Path.Combine(Path.GetTempPath(), request.WorkspaceId, repoName);

        try
        {
            // Clone repository
            var cloned = await CloneRepositoryAsync(request.RepoUrl, targetPath, ct);
            
            // Analyze project structure
            var repoDto = await AnalyzeRepositoryAsync(targetPath, request.RepoUrl, ct);
            
            // AI-powered setup recommendations
            var recommendations = await GenerateSetupRecommendationsAsync(repoDto, ct);
            
            _logger.LogInformation("Project {RepoName} bootstrapped successfully", repoName);

            return new BootstrapProjectDto
            {
                Id = bootstrapId,
                RepoUrl = request.RepoUrl,
                Repo = repoDto,
                Status = cloned ? "Completed" : "Partial",
                SetupRecommendations = recommendations,
                CreatedAt = startedAt,
                CompletedAt = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to bootstrap project {RepoUrl}", request.RepoUrl);
            
            return new BootstrapProjectDto
            {
                Id = bootstrapId,
                RepoUrl = request.RepoUrl,
                Status = "Failed",
                ErrorMessage = ex.Message,
                CreatedAt = startedAt,
                CompletedAt = DateTime.UtcNow
            };
        }
    }

    private string ExtractRepoName(string repoUrl)
    {
        // Extract repo name from URL
        var uri = new Uri(repoUrl);
        var segments = uri.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
        return segments.Length >= 2 ? segments[1].Replace(".git", "") : "project";
    }

    private async Task<bool> CloneRepositoryAsync(string repoUrl, string targetPath, CancellationToken ct)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(targetPath)!);
            
            var psi = new ProcessStartInfo
            {
                FileName = "git",
                Arguments = $"clone --depth 1 \"{repoUrl}\" \"{targetPath}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = new Process { StartInfo = psi };
            process.Start();
            await Task.Run(() => process.WaitForExit(60000), ct);

            return process.ExitCode == 0;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Git clone failed for {RepoUrl}", repoUrl);
            return false;
        }
    }

    private async Task<GitHubRepoDto> AnalyzeRepositoryAsync(string path, string repoUrl, CancellationToken ct)
    {
        var repoName = ExtractRepoName(repoUrl);
        var language = DetectPrimaryLanguage(path);
        
        // Get last commit info
        var (commitHash, branch) = await GetGitInfoAsync(path, ct);

        return new GitHubRepoDto
        {
            Id = Guid.NewGuid(),
            Owner = repoUrl.Split('/').Skip(3).FirstOrDefault() ?? "unknown",
            Name = repoName,
            CloneUrl = repoUrl,
            Branch = branch,
            CommitHash = commitHash,
            PrimaryLanguage = language,
            ClonedAt = DateTime.UtcNow,
            LocalPath = path
        };
    }

    private string DetectPrimaryLanguage(string path)
    {
        var extensions = Directory.GetFiles(path, "*.*", SearchOption.AllDirectories)
            .Select(f => Path.GetExtension(f).ToLower())
            .Where(ext => !string.IsNullOrEmpty(ext))
            .GroupBy(ext => ext)
            .OrderByDescending(g => g.Count())
            .FirstOrDefault()?.Key;

        return extensions switch
        {
            ".cs" => "C#",
            ".js" => "JavaScript",
            ".ts" => "TypeScript",
            ".py" => "Python",
            ".java" => "Java",
            ".go" => "Go",
            ".rs" => "Rust",
            _ => "Unknown"
        };
    }

    private async Task<(string hash, string branch)> GetGitInfoAsync(string path, CancellationToken ct)
    {
        try
        {
            // Get commit hash
            var hashPsi = new ProcessStartInfo
            {
                FileName = "git",
                Arguments = "rev-parse HEAD",
                WorkingDirectory = path,
                RedirectStandardOutput = true,
                UseShellExecute = false
            };

            using var hashProcess = new Process { StartInfo = hashPsi };
            hashProcess.Start();
            var hash = await hashProcess.StandardOutput.ReadToEndAsync(ct);
            await Task.Run(() => hashProcess.WaitForExit(5000), ct);

            // Get branch
            var branchPsi = new ProcessStartInfo
            {
                FileName = "git",
                Arguments = "branch --show-current",
                WorkingDirectory = path,
                RedirectStandardOutput = true,
                UseShellExecute = false
            };

            using var branchProcess = new Process { StartInfo = branchPsi };
            branchProcess.Start();
            var branch = await branchProcess.StandardOutput.ReadToEndAsync(ct);
            await Task.Run(() => branchProcess.WaitForExit(5000), ct);

            return (hash.Trim(), branch.Trim());
        }
        catch
        {
            return ("unknown", "main");
        }
    }

    private async Task<List<string>> GenerateSetupRecommendationsAsync(GitHubRepoDto repo, CancellationToken ct)
    {
        var prompt = $@"
Project: {repo.Name}
Language: {repo.PrimaryLanguage}

Generate 3-5 setup steps for this project type.
Format: Step number - Action description
Example: 1 - Run 'dotnet restore' to restore packages";

        try
        {
            var response = await _aiService.GenerateCompletionAsync(prompt, cancellationToken: ct);
            return response.Split('\n').Where(l => !string.IsNullOrWhiteSpace(l)).Take(5).ToList();
        }
        catch
        {
            return new List<string> { "1 - Review project README", "2 - Install dependencies", "3 - Build project" };
        }
    }
}
*/
