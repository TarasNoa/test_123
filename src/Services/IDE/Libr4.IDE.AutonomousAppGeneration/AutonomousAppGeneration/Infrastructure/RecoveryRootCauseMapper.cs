using Libr4.IDE.Domain.AutonomousAppGeneration;
using static Libr4.IDE.Application.AutonomousAppGeneration.Infrastructure.CompileErrorAnalyzer;

namespace Libr4.IDE.Application.AutonomousAppGeneration.Infrastructure;

/// <summary>Maps classifier/planner/analyzer signals to framework-agnostic root cause categories.</summary>
public static class RecoveryRootCauseMapper
{
    public static RecoveryRootCauseCategory FromPlannerCategory(string? plannerCategory) =>
        plannerCategory?.Trim().ToLowerInvariant() switch
        {
            "manifest_pom" or "package" => RecoveryRootCauseCategory.Configuration,
            "missing_class" or "missing_interface" or "missing_field" => RecoveryRootCauseCategory.MissingType,
            "package_mismatch" => RecoveryRootCauseCategory.Configuration,
            "wrong_import" or "missing_import" or "missing_symbol" => RecoveryRootCauseCategory.Imports,
            "missing_bean" => RecoveryRootCauseCategory.RuntimeWiring,
            "frontend" => RecoveryRootCauseCategory.Configuration,
            "compile" => RecoveryRootCauseCategory.Imports,
            _ => RecoveryRootCauseCategory.Unknown
        };

    public static RecoveryRootCauseCategory FromCompileFixKind(CompileFixKind kind) => kind switch
    {
        CompileFixKind.MissingClass or CompileFixKind.MissingInterface or CompileFixKind.MissingField
            => RecoveryRootCauseCategory.MissingType,
        CompileFixKind.PackageMismatch => RecoveryRootCauseCategory.Configuration,
        CompileFixKind.WrongImport or CompileFixKind.MissingImport => RecoveryRootCauseCategory.Imports,
        CompileFixKind.MissingBean => RecoveryRootCauseCategory.RuntimeWiring,
        _ => RecoveryRootCauseCategory.Unknown
    };

    public static RecoveryRootCauseCategory FromClassifier(
        RepairErrorClassifier.RepairErrorClass errorClass,
        string? filePath,
        string? message,
        CompileErrorAnalysis? symbolAnalysis = null)
    {
        if (symbolAnalysis is not null && symbolAnalysis.Kind != CompileFixKind.Unknown)
            return FromCompileFixKind(symbolAnalysis.Kind);

        var signal = $"{filePath} {message}";
        if (errorClass == RepairErrorClassifier.RepairErrorClass.ArtifactContamination
            || signal.Contains("SpringBootApplication", StringComparison.OrdinalIgnoreCase)
            || signal.Contains("entry point", StringComparison.OrdinalIgnoreCase)
            || signal.Contains("main.ts", StringComparison.OrdinalIgnoreCase)
            || signal.Contains("Program.cs", StringComparison.OrdinalIgnoreCase))
            return RecoveryRootCauseCategory.EntryPoints;

        return errorClass switch
        {
            RepairErrorClassifier.RepairErrorClass.PomSyntax
                or RepairErrorClassifier.RepairErrorClass.CsprojSyntax
                or RepairErrorClassifier.RepairErrorClass.PackageJsonSyntax
                or RepairErrorClassifier.RepairErrorClass.YamlSyntax
                or RepairErrorClassifier.RepairErrorClass.RequirementsSyntax
                => RecoveryRootCauseCategory.Configuration,
            RepairErrorClassifier.RepairErrorClass.MissingDependency
                => RecoveryRootCauseCategory.Dependencies,
            RepairErrorClassifier.RepairErrorClass.CompileSymbol
                => RecoveryRootCauseCategory.Imports,
            RepairErrorClassifier.RepairErrorClass.RuntimeConfiguration
                or RepairErrorClassifier.RepairErrorClass.RuntimeDiFailure
                => RecoveryRootCauseCategory.RuntimeWiring,
            RepairErrorClassifier.RepairErrorClass.TestFailure
                => RecoveryRootCauseCategory.TestInfrastructure,
            RepairErrorClassifier.RepairErrorClass.ArtifactContamination
                => RecoveryRootCauseCategory.ArtifactContamination,
            _ => RecoveryRootCauseCategory.Unknown
        };
    }

    public static RecoveryRootCauseCategory Merge(
        RecoveryRootCauseCategory fromClassifier,
        RecoveryRootCauseCategory fromPlanner) =>
        fromClassifier != RecoveryRootCauseCategory.Unknown
            ? fromClassifier
            : fromPlanner;
}
