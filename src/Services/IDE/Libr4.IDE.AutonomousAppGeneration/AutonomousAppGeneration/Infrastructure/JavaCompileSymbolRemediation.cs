using System.Text;
using System.Text.RegularExpressions;
using Libr4.IDE.Domain.AutonomousAppGeneration;
using static Libr4.IDE.Application.AutonomousAppGeneration.Infrastructure.CompileErrorAnalyzer;

namespace Libr4.IDE.Application.AutonomousAppGeneration.Infrastructure;

/// <summary>
/// Deterministic fixes for Java compile symbol failures identified by <see cref="CompileErrorAnalyzer"/>.
/// </summary>
public static class JavaCompileSymbolRemediation
{
    private static readonly Regex JavaPackageLine = new(
        @"^\s*package\s+[\w.]+\s*;",
        RegexOptions.Multiline | RegexOptions.Compiled);

    private static readonly Regex WrongImportLine = new(
        @"^\s*import\s+[\w.]+\s*;\s*$",
        RegexOptions.Multiline | RegexOptions.Compiled);

    public static int Apply(
        IList<GeneratedFile> files,
        GenerationPlan plan,
        CompileErrorAnalysis analysis)
    {
        if (StackPlanHeuristics.Classify(plan) != StackKind.JavaReactFullStack)
            return 0;

        return analysis.Kind switch
        {
            CompileFixKind.MissingClass => CreateMissingType(files, analysis, isInterface: false),
            CompileFixKind.MissingInterface => CreateMissingType(files, analysis, isInterface: true),
            CompileFixKind.PackageMismatch => FixPackageDeclaration(files, analysis),
            CompileFixKind.WrongImport => FixImport(files, analysis, replaceWrong: true),
            CompileFixKind.MissingImport => FixImport(files, analysis, replaceWrong: false),
            CompileFixKind.MissingBean => EnsureSpringServiceAnnotation(files, analysis),
            _ => 0
        };
    }

    private static int CreateMissingType(
        IList<GeneratedFile> files,
        CompileErrorAnalysis analysis,
        bool isInterface)
    {
        var path = analysis.TargetFilePath;
        var package = analysis.ExpectedPackage;
        if (string.IsNullOrWhiteSpace(path) || string.IsNullOrWhiteSpace(package))
            return 0;

        if (files.Any(f => f.RelativePath.Equals(path, StringComparison.OrdinalIgnoreCase)))
            return 0;

        var name = analysis.SymbolName;
        var sb = new StringBuilder();
        sb.AppendLine($"package {package};");
        sb.AppendLine();

        if (!isInterface && name.EndsWith("Service", StringComparison.Ordinal))
        {
            sb.AppendLine("import org.springframework.stereotype.Service;");
            sb.AppendLine();
            sb.AppendLine("@Service");
            sb.AppendLine($"public class {name} {{");
            sb.AppendLine("    // Auto-generated stub to satisfy compile references. Expand in later iterations.");
            sb.AppendLine("}");
        }
        else if (!isInterface && name.EndsWith("Controller", StringComparison.Ordinal))
        {
            sb.AppendLine("import org.springframework.web.bind.annotation.RestController;");
            sb.AppendLine();
            sb.AppendLine("@RestController");
            sb.AppendLine($"public class {name} {{");
            sb.AppendLine("}");
        }
        else if (!isInterface && name.EndsWith("Repository", StringComparison.Ordinal))
        {
            sb.AppendLine("import org.springframework.stereotype.Repository;");
            sb.AppendLine();
            sb.AppendLine("@Repository");
            sb.AppendLine($"public interface {name} {{");
            sb.AppendLine("}");
        }
        else if (isInterface)
        {
            sb.AppendLine($"public interface {name} {{");
            sb.AppendLine("}");
        }
        else
        {
            sb.AppendLine($"public class {name} {{");
            sb.AppendLine("}");
        }

        files.Add(new GeneratedFile(path, "java", sb.ToString()));
        return 1;
    }

    private static int FixPackageDeclaration(IList<GeneratedFile> files, CompileErrorAnalysis analysis)
    {
        var path = analysis.TargetFilePath;
        var expected = analysis.ExpectedPackage;
        if (string.IsNullOrWhiteSpace(path) || string.IsNullOrWhiteSpace(expected))
            return 0;

        var idx = files.ToList().FindIndex(f =>
            f.RelativePath.Equals(path, StringComparison.OrdinalIgnoreCase));
        if (idx < 0)
            return 0;

        var content = files[idx].Content ?? string.Empty;
        if (!JavaPackageLine.IsMatch(content))
            return 0;

        var updated = JavaPackageLine.Replace(content, $"package {expected};");
        if (string.Equals(updated, content, StringComparison.Ordinal))
            return 0;

        files[idx] = new GeneratedFile(files[idx].RelativePath, files[idx].Language, updated);
        return 1;
    }

    private static int FixImport(
        IList<GeneratedFile> files,
        CompileErrorAnalysis analysis,
        bool replaceWrong)
    {
        var path = analysis.ReporterFilePath ?? analysis.TargetFilePath;
        if (string.IsNullOrWhiteSpace(path))
            return 0;

        var fqcn = analysis.ExpectedPackage is not null
            ? $"{analysis.ExpectedPackage}.{analysis.SymbolName}"
            : analysis.SymbolName;

        var idx = files.ToList().FindIndex(f =>
            f.RelativePath.Equals(path, StringComparison.OrdinalIgnoreCase));
        if (idx < 0)
            return 0;

        var content = files[idx].Content ?? string.Empty;
        string updated;

        if (replaceWrong)
        {
            var wrongPattern = new Regex(
                $@"^\s*import\s+[\w.]*\.{Regex.Escape(analysis.SymbolName)}\s*;\s*$",
                RegexOptions.Multiline);
            if (!wrongPattern.IsMatch(content))
                return 0;
            updated = wrongPattern.Replace(content, $"import {fqcn};");
        }
        else
        {
            if (content.Contains($"import {fqcn};", StringComparison.Ordinal))
                return 0;

            var pkgMatch = JavaPackageLine.Match(content);
            if (!pkgMatch.Success)
                return 0;

            var insertAt = pkgMatch.Index + pkgMatch.Length;
            updated = content.Insert(insertAt, $"\n\nimport {fqcn};");
        }

        if (string.Equals(updated, content, StringComparison.Ordinal))
            return 0;

        files[idx] = new GeneratedFile(files[idx].RelativePath, files[idx].Language, updated);
        return 1;
    }

    private static int EnsureSpringServiceAnnotation(IList<GeneratedFile> files, CompileErrorAnalysis analysis)
    {
        var path = analysis.TargetFilePath;
        if (string.IsNullOrWhiteSpace(path))
            return 0;

        var idx = files.ToList().FindIndex(f =>
            f.RelativePath.Equals(path, StringComparison.OrdinalIgnoreCase));
        if (idx < 0)
            return 0;

        var content = files[idx].Content ?? string.Empty;
        if (content.Contains("@Service", StringComparison.Ordinal))
            return 0;

        var pkgMatch = JavaPackageLine.Match(content);
        var sb = new StringBuilder();
        if (!content.Contains("org.springframework.stereotype.Service", StringComparison.Ordinal))
        {
            if (pkgMatch.Success)
            {
                var insertAt = pkgMatch.Index + pkgMatch.Length;
                content = content.Insert(insertAt, "\n\nimport org.springframework.stereotype.Service;");
            }
            else
            {
                sb.AppendLine("import org.springframework.stereotype.Service;");
            }
        }

        var classPattern = new Regex(
            $@"(public\s+(?:class|interface)\s+{Regex.Escape(analysis.SymbolName)}\s*\{{)",
            RegexOptions.Compiled);
        if (!classPattern.IsMatch(content))
            return 0;

        content = classPattern.Replace(content, "@Service\n$1");
        files[idx] = new GeneratedFile(files[idx].RelativePath, files[idx].Language, content);
        return 1;
    }
}
