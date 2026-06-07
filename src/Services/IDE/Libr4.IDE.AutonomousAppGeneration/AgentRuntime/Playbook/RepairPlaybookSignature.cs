using Libr4.IDE.Application.AutonomousAppGeneration.Infrastructure.Algorithms;
using Libr4.IDE.Domain.AutonomousAppGeneration;

namespace Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Playbook;

public static class RepairPlaybookSignature
{
    public static RepairPlaybookSignatureResult FromErrors(
        IReadOnlyList<ErrorReport> errors,
        string? buildLog,
        GenerationPlan? plan)
    {
        var errorTuples = errors
            .Select(e => (e.ErrorType, e.FilePath, e.Message))
            .ToArray();

        var languages = plan?.TechStack.Languages.ToArray() ?? Array.Empty<string>();
        var frameworks = plan?.TechStack.Frameworks.ToArray() ?? Array.Empty<string>();

        var (signature, stackPattern) = FSharpAlgorithmsBridge.BuildPlaybookSignature(
            errorTuples,
            buildLog,
            plan?.ApplicationName,
            languages,
            frameworks);

        return new RepairPlaybookSignatureResult(signature, stackPattern);
    }

    public static string FromError(ErrorReport error) =>
        FSharpAlgorithmsBridge.BuildPlaybookSignature(
            new[] { (error.ErrorType, error.FilePath, error.Message) },
            null,
            null,
            Array.Empty<string>(),
            Array.Empty<string>()).Signature;

    public static string FromBuildLog(string buildLog) =>
        FSharpAlgorithmsBridge.BuildPlaybookSignature(
            Array.Empty<(string, string?, string)>(),
            buildLog,
            null,
            Array.Empty<string>(),
            Array.Empty<string>()).Signature;

    public static string BuildStackPattern(GenerationPlan? plan)
    {
        var languages = plan?.TechStack.Languages.ToArray() ?? Array.Empty<string>();
        var frameworks = plan?.TechStack.Frameworks.ToArray() ?? Array.Empty<string>();
        return FSharpAlgorithmsBridge.BuildPlaybookSignature(
            Array.Empty<(string, string?, string)>(),
            null,
            plan?.ApplicationName,
            languages,
            frameworks).StackPattern;
    }
}
