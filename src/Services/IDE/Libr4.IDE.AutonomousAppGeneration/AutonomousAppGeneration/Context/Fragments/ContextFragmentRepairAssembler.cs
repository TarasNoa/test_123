using System.Text;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Core;
using Libr4.IDE.Application.GitAutomation;
using Libr4.IDE.Domain.AutonomousAppGeneration;

namespace Libr4.IDE.Application.AutonomousAppGeneration.Context.Fragments;

public sealed class ContextFragmentRepairAssembler : IContextFragmentRepairAssembler
{
    private readonly IContextFragmentManager _manager;

    public ContextFragmentRepairAssembler(IContextFragmentManager manager) =>
        _manager = manager;

    public string Assemble(RepairFragmentInput input)
    {
        _manager.Clear();
        Populate(_manager, input);
        return _manager.Assemble();
    }

    public static void Populate(IContextFragmentManager manager, RepairFragmentInput input)
    {
        if (!string.IsNullOrWhiteSpace(input.BuildLog))
        {
            manager.Add(new ContextFragment(
                ContextFragmentType.BuildLog,
                TailBuildLog(input.BuildLog, 200),
                ContextFragmentManager.DefaultPriority(ContextFragmentType.BuildLog),
                new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    ["attempt"] = input.RepairAttempt.ToString()
                }));
        }

        if (input.Errors.Count > 0)
        {
            manager.Add(new ContextFragment(
                ContextFragmentType.ErrorReport,
                FormatErrors(input.Errors),
                ContextFragmentManager.DefaultPriority(ContextFragmentType.ErrorReport),
                new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    ["count"] = input.Errors.Count.ToString(),
                    ["attempt"] = input.RepairAttempt.ToString()
                }));
        }

        foreach (var excerpt in BuildFileExcerpts(input.Errors, input.WorkingFiles).Take(3))
            manager.Add(excerpt);

        var verify = input.VerifyEvidence
                     ?? BuildErrorCategoryClassifier.FormatForAgent(input.BuildLog, input.Errors.FirstOrDefault()?.Message);
        if (!string.IsNullOrWhiteSpace(verify))
        {
            manager.Add(new ContextFragment(
                ContextFragmentType.VerifyEvidence,
                verify,
                ContextFragmentManager.DefaultPriority(ContextFragmentType.VerifyEvidence),
                new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    ["attempt"] = input.RepairAttempt.ToString()
                }));
        }

        if (!string.IsNullOrWhiteSpace(input.DesignArtifactJson))
        {
            manager.Add(new ContextFragment(
                ContextFragmentType.DesignArtifact,
                input.DesignArtifactJson,
                ContextFragmentManager.DefaultPriority(ContextFragmentType.DesignArtifact),
                new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    ["attempt"] = input.RepairAttempt.ToString()
                }));
        }

        if (!string.IsNullOrWhiteSpace(input.OrchestratorJitHint))
        {
            manager.Add(new ContextFragment(
                ContextFragmentType.OrchestratorJit,
                input.OrchestratorJitHint,
                90,
                new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    ["source"] = "orchestrator_jit",
                    ["attempt"] = input.RepairAttempt.ToString()
                }));
        }

        if (!string.IsNullOrWhiteSpace(input.PlaybookHint))
        {
            manager.Add(new ContextFragment(
                ContextFragmentType.VerifyEvidence,
                $"PLAYBOOK_HINT: {input.PlaybookHint}",
                75,
                new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    ["source"] = "playbook",
                    ["attempt"] = input.RepairAttempt.ToString()
                }));
        }

        if (!string.IsNullOrWhiteSpace(input.LspDiagnostics))
        {
            manager.Add(new ContextFragment(
                ContextFragmentType.LspDiagnostics,
                input.LspDiagnostics,
                ContextFragmentManager.DefaultPriority(ContextFragmentType.LspDiagnostics),
                new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    ["attempt"] = input.RepairAttempt.ToString()
                }));
        }

        if (!string.IsNullOrWhiteSpace(input.GitDiffEvidence))
        {
            manager.Add(new ContextFragment(
                ContextFragmentType.GitDiff,
                input.GitDiffEvidence,
                ContextFragmentManager.DefaultPriority(ContextFragmentType.GitDiff),
                new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    ["attempt"] = input.RepairAttempt.ToString(),
                    ["tag"] = IShadowGitCheckpointService.RepairTagName(input.RepairAttempt)
                }));
        }

        if (!string.IsNullOrWhiteSpace(input.FastContextEvidence))
        {
            manager.Add(new ContextFragment(
                ContextFragmentType.FastContext,
                input.FastContextEvidence,
                ContextFragmentManager.DefaultPriority(ContextFragmentType.FastContext),
                new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    ["attempt"] = input.RepairAttempt.ToString(),
                    ["source"] = "prefetch"
                }));
        }
    }

    private static IEnumerable<ContextFragment> BuildFileExcerpts(
        IReadOnlyList<ErrorReport> errors,
        IReadOnlyList<GeneratedFile> workingFiles)
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var error in errors.Where(e => !string.IsNullOrWhiteSpace(e.FilePath)).Take(4))
        {
            var path = error.FilePath!.Replace('\\', '/');
            if (!seen.Add(path))
                continue;

            var file = workingFiles.FirstOrDefault(f =>
                f.RelativePath.Equals(path, StringComparison.OrdinalIgnoreCase)
                || f.RelativePath.Replace('\\', '/').EndsWith(path, StringComparison.OrdinalIgnoreCase));
            if (file is null || string.IsNullOrWhiteSpace(file.Content))
                continue;

            var excerpt = ExtractExcerpt(file.Content, error.LineNumber);
            if (string.IsNullOrWhiteSpace(excerpt))
                continue;

            var startLine = error.LineNumber is int ln && ln > 0 ? Math.Max(1, ln - 15) : 1;
            var endLine = error.LineNumber is int line && line > 0 ? line + 15 : Math.Min(40, file.Content.Split('\n').Length);

            yield return new ContextFragment(
                ContextFragmentType.FileExcerpt,
                excerpt,
                ContextFragmentManager.DefaultPriority(ContextFragmentType.FileExcerpt),
                new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    ["path"] = path,
                    ["lines"] = $"{startLine}-{endLine}"
                });
        }
    }

    private static string FormatErrors(IReadOnlyList<ErrorReport> errors)
    {
        var sb = new StringBuilder();
        foreach (var error in errors.Take(16))
        {
            sb.Append("- ");
            if (!string.IsNullOrWhiteSpace(error.FilePath))
                sb.Append(error.FilePath).Append(':');
            if (error.LineNumber is int line)
                sb.Append(line).Append(' ');
            sb.Append('[').Append(error.ErrorType).Append("] ").AppendLine(error.Message);
            if (!string.IsNullOrWhiteSpace(error.SuggestedFix))
                sb.AppendLine($"  fix: {error.SuggestedFix}");
        }

        if (errors.Count > 16)
            sb.AppendLine($"... and {errors.Count - 16} more");

        return sb.ToString().TrimEnd();
    }

    private static string ExtractExcerpt(string content, int? lineNumber, int radius = 15)
    {
        var lines = content.Replace("\r\n", "\n").Split('\n');
        if (lines.Length == 0)
            return string.Empty;

        int start;
        int end;
        if (lineNumber is int ln && ln > 0)
        {
            start = Math.Max(0, ln - 1 - radius);
            end = Math.Min(lines.Length, ln - 1 + radius + 1);
        }
        else
        {
            start = 0;
            end = Math.Min(lines.Length, 40);
        }

        return string.Join('\n', lines[start..end].Select((line, i) => $"{start + i + 1,4}| {line}"));
    }

    private static string TailBuildLog(string buildLog, int tailLines)
    {
        var lines = buildLog.Replace("\r\n", "\n").Split('\n');
        if (lines.Length <= tailLines)
            return buildLog;
        return string.Join('\n', lines[^tailLines..]);
    }
}
