using System.Security.Cryptography;
using System.Text;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Playbook;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Skills;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Libr4.IDE.Application.AutonomousAppGeneration.Memory.Crystallization;

public sealed class FileSkillCrystallizer : ISkillCrystallizer
{
    private readonly SkillCrystallizationOptions _options;
    private readonly ISkillManifestRegistryRefresh? _registryRefresh;
    private readonly ILogger<FileSkillCrystallizer> _logger;

    public FileSkillCrystallizer(
        IOptions<SkillCrystallizationOptions> options,
        ILogger<FileSkillCrystallizer> logger,
        ISkillManifestRegistry? registry = null)
    {
        _options = options.Value;
        _logger = logger;
        _registryRefresh = registry as ISkillManifestRegistryRefresh;
    }

    public Task<CrystallizedSkillResult?> TryCrystallizeAsync(RepairPlaybookEntry entry, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        if (!_options.Enabled
            || entry.SuccessCount < _options.CrystallizeAfterSuccessCount
            || string.IsNullOrWhiteSpace(entry.ErrorSignature))
        {
            return Task.FromResult<CrystallizedSkillResult?>(null);
        }

        var hash = ShortHash(entry.ErrorSignature);
        var skillId = $"crystallized-{hash}";
        var activePath = Path.Combine(_options.CrystallizedSkillsRoot, $"{hash}.md");
        var pendingPath = Path.Combine(_options.CrystallizedSkillsRoot, "pending", $"{hash}.md");

        if (File.Exists(activePath))
            return Task.FromResult<CrystallizedSkillResult?>(new CrystallizedSkillResult(skillId, activePath, false, false));

        if (_options.RequireHumanApproval)
        {
            if (File.Exists(pendingPath))
                return Task.FromResult<CrystallizedSkillResult?>(new CrystallizedSkillResult(skillId, pendingPath, true, false));

            Directory.CreateDirectory(Path.GetDirectoryName(pendingPath)!);
            var pendingContent = BuildSkillMarkdown(entry, skillId, approval: "pending");
            File.WriteAllText(pendingPath, pendingContent, Encoding.UTF8);
            _logger.LogInformation("Queued crystallized skill {SkillId} for approval at {Path}", skillId, pendingPath);
            return Task.FromResult<CrystallizedSkillResult?>(new CrystallizedSkillResult(skillId, pendingPath, true, true));
        }

        Directory.CreateDirectory(_options.CrystallizedSkillsRoot);
        var content = BuildSkillMarkdown(entry, skillId, approval: "active");
        File.WriteAllText(activePath, content, Encoding.UTF8);
        _registryRefresh?.Refresh();
        _logger.LogInformation("Crystallized skill {SkillId} at {Path}", skillId, activePath);
        return Task.FromResult<CrystallizedSkillResult?>(new CrystallizedSkillResult(skillId, activePath, false, true));
    }

    public Task<bool> ApprovePendingAsync(string errorSignature, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        var hash = ShortHash(errorSignature);
        var pendingPath = Path.Combine(_options.CrystallizedSkillsRoot, "pending", $"{hash}.md");
        var activePath = Path.Combine(_options.CrystallizedSkillsRoot, $"{hash}.md");

        if (!File.Exists(pendingPath))
            return Task.FromResult(false);

        var content = File.ReadAllText(pendingPath, Encoding.UTF8)
            .Replace("approval: pending", "approval: active", StringComparison.OrdinalIgnoreCase);
        Directory.CreateDirectory(_options.CrystallizedSkillsRoot);
        File.WriteAllText(activePath, content, Encoding.UTF8);
        File.Delete(pendingPath);
        _registryRefresh?.Refresh();
        _logger.LogInformation("Approved crystallized skill {Hash}", hash);
        return Task.FromResult(true);
    }

    internal static string BuildSkillMarkdown(RepairPlaybookEntry entry, string skillId, string approval)
    {
        var sb = new StringBuilder();
        sb.AppendLine("---");
        sb.AppendLine($"name: {skillId}");
        sb.AppendLine($"description: Crystallized repair pattern ({entry.SuccessCount} successes) for stack {entry.StackPattern}");
        sb.AppendLine("version: 1.0.0");
        sb.AppendLine("crystallized: true");
        sb.AppendLine($"error-signature: {entry.ErrorSignature}");
        sb.AppendLine($"approval: {approval}");
        sb.AppendLine("allowed-tools: [apply_patch, edit_file, write_file, bash, run_build, run_tests]");
        sb.AppendLine("---");
        sb.AppendLine();
        sb.AppendLine("# Crystallized Repair Skill");
        sb.AppendLine();
        sb.AppendLine("## Trigger Conditions");
        sb.AppendLine($"- Stack pattern: `{entry.StackPattern}`");
        sb.AppendLine($"- Error signature: `{entry.ErrorSignature}`");
        sb.AppendLine($"- Playbook score: {entry.Score:F2} (success={entry.SuccessCount}, fail={entry.FailCount})");
        sb.AppendLine();
        sb.AppendLine("## Fix Steps");
        sb.AppendLine("1. Reproduce using the error signature and recent build log.");
        sb.AppendLine($"2. Apply fix pattern: `{entry.FixPattern}`");
        sb.AppendLine("3. Verify with `run_build` then `run_tests` if applicable.");
        sb.AppendLine("4. Keep the diff minimal — patch only files implicated by the error.");
        sb.AppendLine();
        sb.AppendLine("## Example Diff");
        sb.AppendLine("```diff");
        sb.AppendLine($"# derived from successful pattern: {entry.FixPattern}");
        sb.AppendLine("# inspect rollout tool outputs for concrete patch hunks");
        sb.AppendLine("```");
        return sb.ToString().TrimEnd() + Environment.NewLine;
    }

    private static string ShortHash(string signature)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(signature));
        return Convert.ToHexString(bytes)[..16].ToLowerInvariant();
    }
}
