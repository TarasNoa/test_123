using Libr4.IDE.Domain.AutonomousAppGeneration;
using static Libr4.IDE.Application.AutonomousAppGeneration.Infrastructure.CompileErrorAnalyzer;

namespace Libr4.IDE.Application.AutonomousAppGeneration.Infrastructure;

/// <summary>
/// Deterministic fixes for Python compile/import analysis (local modules, pytest paths).
/// </summary>
public static class PythonCompileSymbolRemediation
{
    public static int Apply(
        IList<GeneratedFile> files,
        GenerationPlan plan,
        CompileErrorAnalysis analysis,
        string? buildLog)
    {
        if (analysis.Kind != CompileFixKind.MissingImport)
            return 0;

        var changed = PythonDependencyGraphNormalizer.Normalize(files, buildLog);
        if (changed > 0)
            return changed;

        if (string.Equals(analysis.SymbolCategory, "local_module", StringComparison.Ordinal)
            || PythonPytestImportRemediation.IsLocalPythonModule(analysis.SymbolName, files))
            return PythonPytestImportRemediation.Apply(files, buildLog);

        return 0;
    }
}
