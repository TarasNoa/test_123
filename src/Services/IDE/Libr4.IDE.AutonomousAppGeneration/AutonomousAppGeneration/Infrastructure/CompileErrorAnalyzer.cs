using System.Text.RegularExpressions;
using Libr4.IDE.Domain.AutonomousAppGeneration;

namespace Libr4.IDE.Application.AutonomousAppGeneration.Infrastructure;

/// <summary>
/// Fine-grained compile error intelligence: resolves "cannot find symbol" and related
/// failures against the generated file tree before LLM guessing.
/// </summary>
public static class CompileErrorAnalyzer
{
    public enum CompileFixKind
    {
        Unknown,
        MissingClass,
        MissingInterface,
        MissingImport,
        WrongImport,
        PackageMismatch,
        MissingField,
        MissingBean
    }

    public sealed record CompileErrorAnalysis(
        CompileFixKind Kind,
        string SymbolName,
        string SymbolCategory,
        string? ExpectedPackage,
        string? ReporterFilePath,
        int? ReporterLine,
        string? TargetFilePath,
        string SuggestedFix,
        string Evidence);

    private static readonly Regex MavenCannotFindSymbol = new(
        @"\[ERROR\]\s+(?<path>[^\s:]+\.java):\[(?<line>\d+)[^\]]*\]\s+cannot find symbol",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex SymbolClass = new(
        @"symbol:\s+class\s+(?<name>[\w$]+)",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex SymbolInterface = new(
        @"symbol:\s+interface\s+(?<name>[\w$]+)",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex SymbolVariable = new(
        @"symbol:\s+(?:variable|method)\s+(?<name>[\w$]+)",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex LocationPackage = new(
        @"location:\s+package\s+(?<pkg>[\w.]+)",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex LocationClass = new(
        @"location:\s+class\s+(?<cls>[\w.]+)",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex InlineClassSymbol = new(
        @"cannot find symbol:?\s+class\s+(?<name>[\w$]+)",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex JavaPackageLine = new(
        @"^\s*package\s+([\w.]+)\s*;",
        RegexOptions.Multiline | RegexOptions.Compiled);

    private static readonly Regex JavaImportLine = new(
        @"^\s*import\s+([\w.]+)\s*;",
        RegexOptions.Multiline | RegexOptions.Compiled);

    public static CompileErrorAnalysis? Analyze(
        ErrorReport rootCause,
        string buildLog,
        IReadOnlyList<GeneratedFile> files,
        GenerationPlan plan)
    {
        var stack = StackPlanHeuristics.Classify(plan);
        if (stack is StackKind.Node or StackKind.Unknown)
            return AnalyzeTypeScript(rootCause, buildLog, files, plan);

        if (stack == StackKind.DotNet)
            return AnalyzeDotNet(rootCause, buildLog, files);

        if (stack == StackKind.Python)
            return AnalyzePython(rootCause, buildLog, files);

        if (stack == StackKind.JavaReactFullStack
            && IsTypeScriptSignal($"{rootCause.Message} {rootCause.FilePath} {buildLog}"))
            return AnalyzeTypeScript(rootCause, buildLog, files, plan);

        var signal = $"{rootCause.Message} {rootCause.FilePath} {buildLog}";
        if (!signal.Contains("cannot find symbol", StringComparison.OrdinalIgnoreCase)
            && !signal.Contains("package does not exist", StringComparison.OrdinalIgnoreCase))
            return null;

        var parsed = ParseJavaSymbolFailure(rootCause, buildLog, files);
        if (parsed is null)
            return null;

        return ResolveJavaSymbol(parsed, files, plan);
    }

    public static CompileErrorAnalysis? Analyze(
        IReadOnlyList<ErrorReport> errors,
        string buildLog,
        IReadOnlyList<GeneratedFile> files,
        GenerationPlan plan)
    {
        if (errors.Count == 0)
            return null;

        var root = errors.FirstOrDefault(e =>
                       e.Message?.Contains("cannot find symbol", StringComparison.OrdinalIgnoreCase) == true)
                   ?? errors.FirstOrDefault(e =>
                       e.Message?.Contains("package does not exist", StringComparison.OrdinalIgnoreCase) == true)
                   ?? errors[0];

        return Analyze(root, buildLog, files, plan);
    }

    private static ParsedJavaSymbol? ParseJavaSymbolFailure(
        ErrorReport rootCause,
        string buildLog,
        IReadOnlyList<GeneratedFile> files)
    {
        var blob = $"{rootCause.Message}\n{buildLog}";
        var reporterPath = NormalizeRepoPath(rootCause.FilePath, files);
        int? reporterLine = rootCause.LineNumber;

        string? symbolName = null;
        var symbolCategory = "class";
        string? expectedPackage = null;

        foreach (Match m in MavenCannotFindSymbol.Matches(blob))
        {
            reporterPath ??= NormalizeRepoPath(m.Groups["path"].Value, files);
            if (!reporterLine.HasValue && int.TryParse(m.Groups["line"].Value, out var ln))
                reporterLine = ln;
        }

        var classMatch = SymbolClass.Match(blob);
        if (classMatch.Success)
        {
            symbolName = classMatch.Groups["name"].Value;
            symbolCategory = "class";
        }

        var ifaceMatch = SymbolInterface.Match(blob);
        if (ifaceMatch.Success)
        {
            symbolName = ifaceMatch.Groups["name"].Value;
            symbolCategory = "interface";
        }

        var varMatch = SymbolVariable.Match(blob);
        if (varMatch.Success && symbolName is null)
        {
            symbolName = varMatch.Groups["name"].Value;
            symbolCategory = "variable";
        }

        var inline = InlineClassSymbol.Match(blob);
        if (inline.Success && symbolName is null)
            symbolName = inline.Groups["name"].Value;

        var pkgLoc = LocationPackage.Match(blob);
        if (pkgLoc.Success)
            expectedPackage = pkgLoc.Groups["pkg"].Value;

        var clsLoc = LocationClass.Match(blob);
        if (clsLoc.Success && expectedPackage is null)
        {
            var fq = clsLoc.Groups["cls"].Value;
            var lastDot = fq.LastIndexOf('.');
            expectedPackage = lastDot > 0 ? fq[..lastDot] : fq;
            symbolName ??= lastDot > 0 ? fq[(lastDot + 1)..] : fq;
        }

        if (string.IsNullOrWhiteSpace(symbolName))
        {
            var msg = rootCause.Message ?? string.Empty;
            var m2 = InlineClassSymbol.Match(msg);
            if (m2.Success)
                symbolName = m2.Groups["name"].Value;
        }

        if (string.IsNullOrWhiteSpace(symbolName))
            return null;

        if (expectedPackage is null && !string.IsNullOrWhiteSpace(reporterPath))
            expectedPackage = InferPackageFromReporterUsage(reporterPath, symbolName, files);

        return new ParsedJavaSymbol(
            symbolName,
            symbolCategory,
            expectedPackage,
            reporterPath,
            reporterLine);
    }

    private static CompileErrorAnalysis ResolveJavaSymbol(
        ParsedJavaSymbol parsed,
        IReadOnlyList<GeneratedFile> files,
        GenerationPlan plan)
    {
        var expectedPackage = parsed.ExpectedPackage
                              ?? InferDefaultServicePackage(files, plan);
        var targetPath = PackageToJavaPath(expectedPackage, parsed.SymbolName, parsed.SymbolCategory);

        var existingAtTarget = FindFile(files, targetPath);
        var existingByName = FindJavaType(files, parsed.SymbolName);

        if (parsed.SymbolCategory == "variable" && existingByName is null)
        {
            return new CompileErrorAnalysis(
                CompileFixKind.MissingField,
                parsed.SymbolName,
                parsed.SymbolCategory,
                expectedPackage,
                parsed.ReporterPath,
                parsed.ReporterLine,
                targetPath,
                $"Field or local '{parsed.SymbolName}' is missing; inspect {parsed.ReporterPath}.",
                "variable_symbol_unresolved");
        }

        if (existingAtTarget is null && existingByName is null)
        {
            var kind = parsed.SymbolCategory == "interface"
                ? CompileFixKind.MissingInterface
                : CompileFixKind.MissingClass;
            return new CompileErrorAnalysis(
                kind,
                parsed.SymbolName,
                parsed.SymbolCategory,
                expectedPackage,
                parsed.ReporterPath,
                parsed.ReporterLine,
                targetPath,
                $"Create {targetPath} in package {expectedPackage}.",
                "type_file_absent");
        }

        var candidate = existingAtTarget ?? existingByName!;
        var pathPackage = JavaPathToPackage(candidate.RelativePath);
        var declaredPackage = ExtractDeclaredPackage(candidate.Content);

        if (!string.IsNullOrWhiteSpace(pathPackage)
            && !string.IsNullOrWhiteSpace(declaredPackage)
            && !string.Equals(pathPackage, declaredPackage, StringComparison.Ordinal))
        {
            return new CompileErrorAnalysis(
                CompileFixKind.PackageMismatch,
                parsed.SymbolName,
                parsed.SymbolCategory,
                pathPackage,
                parsed.ReporterPath,
                parsed.ReporterLine,
                candidate.RelativePath,
                $"Align package declaration to {pathPackage} (was {declaredPackage}).",
                "package_declaration_mismatch");
        }

        if (!string.IsNullOrWhiteSpace(parsed.ReporterPath))
        {
            var reporter = FindFile(files, parsed.ReporterPath);
            if (reporter is not null)
            {
                var fqcn = $"{expectedPackage}.{parsed.SymbolName}";
                var importFix = AnalyzeReporterImports(reporter, fqcn, parsed.SymbolName, files);
                if (importFix is not null)
                    return importFix;
            }
        }

        if (parsed.SymbolCategory == "class" && parsed.SymbolName.EndsWith("Service", StringComparison.Ordinal))
        {
            return new CompileErrorAnalysis(
                CompileFixKind.MissingBean,
                parsed.SymbolName,
                parsed.SymbolCategory,
                expectedPackage,
                parsed.ReporterPath,
                parsed.ReporterLine,
                candidate.RelativePath,
                $"Ensure @Service on {parsed.SymbolName} and component scan includes {expectedPackage}.",
                "spring_bean_wiring");
        }

        return new CompileErrorAnalysis(
            CompileFixKind.Unknown,
            parsed.SymbolName,
            parsed.SymbolCategory,
            expectedPackage,
            parsed.ReporterPath,
            parsed.ReporterLine,
            targetPath,
            "Inspect imports and type visibility.",
            "unresolved_compile_symbol");
    }

    private static CompileErrorAnalysis? AnalyzeReporterImports(
        GeneratedFile reporter,
        string fqcn,
        string simpleName,
        IReadOnlyList<GeneratedFile> files)
    {
        var content = reporter.Content ?? string.Empty;
        var imports = JavaImportLine.Matches(content)
            .Select(m => m.Groups[1].Value)
            .ToList();

        var wrong = imports.FirstOrDefault(i =>
            i.EndsWith("." + simpleName, StringComparison.Ordinal)
            && !string.Equals(i, fqcn, StringComparison.Ordinal));

        if (wrong is not null)
        {
            return new CompileErrorAnalysis(
                CompileFixKind.WrongImport,
                simpleName,
                "class",
                fqcn[..fqcn.LastIndexOf('.')],
                reporter.RelativePath,
                null,
                reporter.RelativePath,
                $"Replace import {wrong} with {fqcn}.",
                "wrong_import_fqcn");
        }

        var usesType = content.Contains(simpleName, StringComparison.Ordinal);
        var hasCorrectImport = imports.Any(i => string.Equals(i, fqcn, StringComparison.Ordinal));
        if (usesType && !hasCorrectImport && files.Any(f =>
                f.RelativePath.EndsWith($"/{simpleName}.java", StringComparison.OrdinalIgnoreCase)
                || f.RelativePath.EndsWith($"\\{simpleName}.java", StringComparison.OrdinalIgnoreCase)))
        {
            return new CompileErrorAnalysis(
                CompileFixKind.MissingImport,
                simpleName,
                "class",
                fqcn[..fqcn.LastIndexOf('.')],
                reporter.RelativePath,
                null,
                reporter.RelativePath,
                $"Add import {fqcn}.",
                "missing_import_statement");
        }

        return null;
    }

    private static bool IsTypeScriptSignal(string signal) =>
        signal.Contains("Cannot find module", StringComparison.OrdinalIgnoreCase)
        || signal.Contains("Module not found", StringComparison.OrdinalIgnoreCase)
        || signal.Contains("TS2307", StringComparison.OrdinalIgnoreCase)
        || signal.Contains("TS2304", StringComparison.OrdinalIgnoreCase)
        || signal.Contains("frontend/src/", StringComparison.OrdinalIgnoreCase)
        || signal.Contains("vitest", StringComparison.OrdinalIgnoreCase)
        || signal.Contains("npm run build", StringComparison.OrdinalIgnoreCase);

    private static CompileErrorAnalysis? AnalyzeDotNet(
        ErrorReport rootCause,
        string buildLog,
        IReadOnlyList<GeneratedFile> files)
    {
        var signal = $"{rootCause.Message} {buildLog}";
        if (!signal.Contains("CS0246", StringComparison.OrdinalIgnoreCase)
            && !signal.Contains("CS0234", StringComparison.OrdinalIgnoreCase)
            && !signal.Contains("error CS", StringComparison.OrdinalIgnoreCase))
            return null;

        var typeMatch = Regex.Match(
            signal,
            @"(?:CS0246|CS0234)[^']*'(?<type>[^']+)'",
            RegexOptions.IgnoreCase);
        if (!typeMatch.Success)
            return null;

        var typeName = typeMatch.Groups["type"].Value;
        return new CompileErrorAnalysis(
            CompileFixKind.MissingImport,
            typeName,
            "type",
            null,
            rootCause.FilePath,
            rootCause.LineNumber,
            rootCause.FilePath,
            $"Resolve missing .NET type or package for '{typeName}'.",
            "dotnet_missing_type");
    }

    private static CompileErrorAnalysis? AnalyzePython(
        ErrorReport rootCause,
        string buildLog,
        IReadOnlyList<GeneratedFile> files)
    {
        var signal = $"{rootCause.Message} {buildLog}";
        if (!signal.Contains("ModuleNotFoundError", StringComparison.OrdinalIgnoreCase)
            && !signal.Contains("ImportError", StringComparison.OrdinalIgnoreCase)
            && !signal.Contains("No module named", StringComparison.OrdinalIgnoreCase))
            return null;

        var modMatch = Regex.Match(
            signal,
            @"(?:No module named|ModuleNotFoundError)[^'""]*['""](?<mod>[^'""]+)['""]",
            RegexOptions.IgnoreCase);
        if (!modMatch.Success)
            return null;

        var mod = modMatch.Groups["mod"].Value;
        if (PythonPytestImportRemediation.IsLocalPythonModule(mod, files))
        {
            var mainPath = files
                .Select(f => f.RelativePath.Replace('\\', '/'))
                .FirstOrDefault(p => p.EndsWith($"/{mod}.py", StringComparison.OrdinalIgnoreCase)
                                     || p.Equals($"{mod}.py", StringComparison.OrdinalIgnoreCase));
            return new CompileErrorAnalysis(
                CompileFixKind.MissingImport,
                mod,
                "local_module",
                null,
                rootCause.FilePath,
                rootCause.LineNumber,
                mainPath ?? rootCause.FilePath,
                $"Fix pytest import path for local module '{mod}'.",
                "python_local_module_import");
        }

        return new CompileErrorAnalysis(
            CompileFixKind.MissingImport,
            mod,
            "module",
            null,
            rootCause.FilePath,
            rootCause.LineNumber,
            files.FirstOrDefault(f => f.RelativePath.EndsWith("requirements.txt", StringComparison.OrdinalIgnoreCase))?.RelativePath,
            $"Add Python dependency '{mod}' to requirements.txt.",
            "python_missing_module");
    }

    private static CompileErrorAnalysis? AnalyzeTypeScript(
        ErrorReport rootCause,
        string buildLog,
        IReadOnlyList<GeneratedFile> files,
        GenerationPlan plan)
    {
        var signal = $"{rootCause.Message} {buildLog}";
        if (!signal.Contains("Cannot find module", StringComparison.OrdinalIgnoreCase)
            && !signal.Contains("Module not found", StringComparison.OrdinalIgnoreCase)
            && !signal.Contains("TS2307", StringComparison.OrdinalIgnoreCase)
            && !signal.Contains("TS2304", StringComparison.OrdinalIgnoreCase))
            return null;

        var moduleMatch = Regex.Match(
            signal,
            @"(?:Cannot find module|Module not found)[^'""]*['""](?<mod>[^'""]+)['""]",
            RegexOptions.IgnoreCase);
        if (!moduleMatch.Success)
            return null;

        var mod = moduleMatch.Groups["mod"].Value;
        return new CompileErrorAnalysis(
            CompileFixKind.MissingImport,
            mod,
            "module",
            null,
            rootCause.FilePath,
            rootCause.LineNumber,
            rootCause.FilePath,
            $"Resolve module '{mod}' (path alias or dependency).",
            "typescript_module_resolution");
    }

    private static string InferPackageFromReporterUsage(
        string reporterPath,
        string symbolName,
        IReadOnlyList<GeneratedFile> files)
    {
        var reporter = FindFile(files, reporterPath);
        if (reporter?.Content is null)
            return string.Empty;

        var import = JavaImportLine.Matches(reporter.Content)
            .Select(m => m.Groups[1].Value)
            .FirstOrDefault(i => i.EndsWith("." + symbolName, StringComparison.Ordinal));

        if (!string.IsNullOrWhiteSpace(import))
            return import[..import.LastIndexOf('.')];

        var declared = ExtractDeclaredPackage(reporter.Content);
        if (!string.IsNullOrWhiteSpace(declared))
        {
            var dir = Path.GetDirectoryName(reporterPath.Replace('\\', '/')) ?? string.Empty;
            if (dir.Contains("/service/", StringComparison.OrdinalIgnoreCase)
                || dir.Contains("/services/", StringComparison.OrdinalIgnoreCase))
                return declared.Contains(".service", StringComparison.Ordinal)
                    ? declared[..declared.LastIndexOf(".service")] + ".service"
                    : declared + ".service";
        }

        return string.Empty;
    }

    private static string InferDefaultServicePackage(IReadOnlyList<GeneratedFile> files, GenerationPlan plan)
    {
        var javaMain = files
            .Where(f => f.RelativePath.Contains("/src/main/java/", StringComparison.OrdinalIgnoreCase)
                        && f.RelativePath.EndsWith(".java", StringComparison.OrdinalIgnoreCase))
            .Select(f => JavaPathToPackage(f.RelativePath))
            .Where(p => !string.IsNullOrWhiteSpace(p))
            .GroupBy(p => p, StringComparer.OrdinalIgnoreCase)
            .OrderByDescending(g => g.Count())
            .Select(g => g.Key)
            .FirstOrDefault();

        if (!string.IsNullOrWhiteSpace(javaMain))
        {
            if (javaMain.Contains(".service", StringComparison.Ordinal))
                return javaMain[..javaMain.LastIndexOf(".service", StringComparison.Ordinal)] + ".service";
            return javaMain + ".service";
        }

        var slug = new string(plan.ApplicationName.Where(char.IsLetterOrDigit).ToArray()).ToLowerInvariant();
        if (string.IsNullOrEmpty(slug))
            slug = "generatedapp";
        return $"com.{slug}.service";
    }

    private static GeneratedFile? FindJavaType(IReadOnlyList<GeneratedFile> files, string simpleName) =>
        files.FirstOrDefault(f =>
            f.RelativePath.EndsWith($"/{simpleName}.java", StringComparison.OrdinalIgnoreCase)
            || f.RelativePath.EndsWith($"\\{simpleName}.java", StringComparison.OrdinalIgnoreCase));

    private static GeneratedFile? FindFile(IReadOnlyList<GeneratedFile> files, string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return null;
        return files.FirstOrDefault(f =>
            f.RelativePath.Equals(path, StringComparison.OrdinalIgnoreCase)
            || f.RelativePath.Replace('\\', '/').Equals(path.Replace('\\', '/'), StringComparison.OrdinalIgnoreCase));
    }

    private static string PackageToJavaPath(string packageName, string simpleName, string symbolCategory)
    {
        var suffix = symbolCategory == "interface" ? simpleName : simpleName;
        var rel = "backend/src/main/java/" + packageName.Replace('.', '/') + "/" + suffix + ".java";
        return rel.Replace('\\', '/');
    }

    private static string JavaPathToPackage(string relativePath)
    {
        var path = relativePath.Replace('\\', '/');
        const string marker = "/src/main/java/";
        var idx = path.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
        if (idx < 0)
            return string.Empty;

        var after = path[(idx + marker.Length)..];
        var file = Path.GetFileName(after);
        var dir = Path.GetDirectoryName(after)?.Replace('\\', '/').Replace('/', '.') ?? string.Empty;
        return string.IsNullOrWhiteSpace(dir) ? string.Empty : dir;
    }

    private static string ExtractDeclaredPackage(string? content)
    {
        if (string.IsNullOrWhiteSpace(content))
            return string.Empty;
        var m = JavaPackageLine.Match(content);
        return m.Success ? m.Groups[1].Value : string.Empty;
    }

    private static string? NormalizeRepoPath(string? raw, IReadOnlyList<GeneratedFile> files)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return null;

        var path = raw.Replace('\\', '/');
        if (path.Contains("/backend/", StringComparison.OrdinalIgnoreCase))
        {
            var idx = path.IndexOf("/backend/", StringComparison.OrdinalIgnoreCase);
            path = path[(idx + 1)..];
        }

        if (path.StartsWith("backend/", StringComparison.OrdinalIgnoreCase))
            return path;

        return files.FirstOrDefault(f =>
                path.EndsWith(f.RelativePath.Replace('\\', '/'), StringComparison.OrdinalIgnoreCase))
            ?.RelativePath;
    }

    private sealed record ParsedJavaSymbol(
        string SymbolName,
        string SymbolCategory,
        string? ExpectedPackage,
        string? ReporterPath,
        int? ReporterLine);
}
