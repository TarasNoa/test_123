using Libr4.IDE.Domain.AutonomousAppGeneration;
using static Libr4.IDE.Application.AutonomousAppGeneration.Infrastructure.CompileErrorAnalyzer;

namespace Libr4.IDE.Application.AutonomousAppGeneration.Infrastructure;

/// <summary>
/// Deterministic fixes for TypeScript module/symbol resolution failures.
/// </summary>
public static class NodeTsCompileSymbolRemediation
{
    public static int Apply(
        IList<GeneratedFile> files,
        GenerationPlan plan,
        CompileErrorAnalysis analysis)
    {
        if (analysis.Kind != CompileFixKind.MissingImport
            || analysis.SymbolCategory != "module")
            return 0;

        var mod = analysis.SymbolName;
        if (string.IsNullOrWhiteSpace(mod))
            return 0;

        if (mod.StartsWith("@", StringComparison.Ordinal) || mod.StartsWith("node:", StringComparison.Ordinal))
            return 0;

        if (!mod.StartsWith("./", StringComparison.Ordinal) && !mod.StartsWith("../", StringComparison.Ordinal))
            return 0;

        var reporter = analysis.ReporterFilePath;
        if (string.IsNullOrWhiteSpace(reporter))
            return 0;

        var reporterDir = Path.GetDirectoryName(reporter.Replace('\\', '/')) ?? string.Empty;
        var targetRel = ResolveModulePath(reporterDir, mod);
        if (string.IsNullOrWhiteSpace(targetRel))
            return 0;

        if (files.Any(f => f.RelativePath.Equals(targetRel, StringComparison.OrdinalIgnoreCase)))
            return 0;

        var ext = targetRel.EndsWith(".tsx", StringComparison.OrdinalIgnoreCase) ? "tsx" : "ts";
        var exportName = SanitizeIdentifier(Path.GetFileNameWithoutExtension(targetRel));
        var reporterContent = files.FirstOrDefault(f =>
                f.RelativePath.Equals(reporter, StringComparison.OrdinalIgnoreCase))
            ?.Content ?? string.Empty;
        var wantsNamedExport = reporterContent.Contains($"{{ {exportName} }}", StringComparison.Ordinal)
                               || reporterContent.Contains($"{{{exportName}}}", StringComparison.Ordinal);

        var content = ext == "tsx"
            ? wantsNamedExport
                ? "export function " + exportName + "() {\n  return null;\n}\n"
                : "export default function " + exportName + "() {\n  return null;\n}\n"
            : "export const " + exportName + " = {};\n";

        files.Add(new GeneratedFile(targetRel, ext, content));
        return 1;
    }

    private static string ResolveModulePath(string reporterDir, string modulePath)
    {
        var combined = Path.Combine(reporterDir, modulePath).Replace('\\', '/');
        if (combined.EndsWith(".ts", StringComparison.OrdinalIgnoreCase)
            || combined.EndsWith(".tsx", StringComparison.OrdinalIgnoreCase))
            return combined;

        return combined + ".ts";
    }

    private static string SanitizeIdentifier(string name)
    {
        var chars = name.Where(c => char.IsLetter(c) || char.IsDigit(c) || c == '_').ToArray();
        if (chars.Length == 0 || char.IsDigit(chars[0]))
            return "GeneratedModule";
        return new string(chars);
    }
}
