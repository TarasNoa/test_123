using System.Text.RegularExpressions;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Playbook;
using Libr4.IDE.Domain.AutonomousAppGeneration;

namespace Libr4.IDE.Application.AutonomousAppGeneration.Services.PlatformUtilization;

public static class PlatformJitPlaybookCatalog
{
    public static PlatformJitPlaybookMatch? TryMatch(
        IReadOnlyList<ErrorReport> errors,
        string? buildLog,
        GenerationPlan? plan)
    {
        var blob = string.Join('\n', errors.Select(e => e.Message))
                   + '\n'
                   + (buildLog ?? string.Empty);
        var stack = PlatformStackProfile.FromBlob(
            plan is null
                ? string.Empty
                : string.Join(' ', plan.TechStack.Languages.Concat(plan.TechStack.Frameworks)));

        if (TryPytestImport(errors, blob, stack, out var pytest))
            return pytest;

        if (TryDependencySync(blob, stack, out var deps))
            return deps;

        if (TryFastApiLayout(errors, blob, stack, out var fastApi))
            return fastApi;

        if (TryCompileSymbol(errors, blob, stack, out var compile))
            return compile;

        return null;
    }

    private static bool TryPytestImport(
        IReadOnlyList<ErrorReport> errors,
        string blob,
        PlatformStackProfile stack,
        out PlatformJitPlaybookMatch match)
    {
        match = null!;
        if (!stack.IsPython && !stack.UsesPytest)
            return false;

        var hasImport = blob.Contains("ModuleNotFoundError", StringComparison.OrdinalIgnoreCase)
                      || blob.Contains("ImportError", StringComparison.OrdinalIgnoreCase)
                      || errors.Any(e => e.Message.Contains("cannot import", StringComparison.OrdinalIgnoreCase));
        if (!hasImport)
            return false;

        var module = ExtractQuotedModule(blob) ?? "main";
        match = new PlatformJitPlaybookMatch(
            "pytest_import_remediation",
            $"ModuleNotFoundError({module})",
            $"""
            ORCHESTRATOR JIT — pytest import remediation
            Error signature: ModuleNotFoundError({module})
            Actions (in order — do NOT delegate/browser/subagent):
            1. read_file the failing test and the imported module path
            2. align layout: src/ package + tests importing app package (not bare main.py at repo root)
            3. add missing __init__.py under package dirs
            4. apply_patch imports to match src/ layout; bash: python -m pip install -r requirements.txt
            5. run_tests with plan test command
            SKIP: memory_search, tool_search, subagent unless steps 1–5 fail twice
            """);
        return true;
    }

    private static bool TryDependencySync(
        string blob,
        PlatformStackProfile stack,
        out PlatformJitPlaybookMatch match)
    {
        match = null!;
        if (!stack.IsPython)
            return false;

        if (!blob.Contains("No module named", StringComparison.OrdinalIgnoreCase))
            return false;

        var pkg = ExtractQuotedModule(blob);
        if (pkg is null || pkg is "main" or "app" or "src")
            return false;

        match = new PlatformJitPlaybookMatch(
            "dependency_sync",
            $"ModuleNotFoundError({pkg})",
            $"""
            ORCHESTRATOR JIT — dependency sync
            Missing package: {pkg}
            Actions:
            1. bash: python -m pip install {pkg} OR add {pkg} to requirements.txt then pip install -r requirements.txt
            2. run_build / run_tests
            SKIP: browser_research, delegate, subagent
            """);
        return true;
    }

    private static bool TryFastApiLayout(
        IReadOnlyList<ErrorReport> errors,
        string blob,
        PlatformStackProfile stack,
        out PlatformJitPlaybookMatch match)
    {
        match = null!;
        if (!stack.IsFastApi)
            return false;

        if (!blob.Contains("ImportError", StringComparison.OrdinalIgnoreCase)
            && !blob.Contains("ModuleNotFoundError", StringComparison.OrdinalIgnoreCase)
            && !errors.Any(e => e.FilePath?.Contains("test", StringComparison.OrdinalIgnoreCase) == true))
            return false;

        match = new PlatformJitPlaybookMatch(
            "fastapi_layout_skill",
            "fastapi_import_layout",
            """
            ORCHESTRATOR JIT — FastAPI layout
            Recommended: activate_skill python-fastapi if not already loaded.
            Typical layout: app/main.py or src/<pkg>/main.py; tests/test_api.py with TestClient.
            Fix import paths before adding new files. run_tests after patch.
            SKIP: browser, delegate, MCP for import/layout issues
            """);
        return true;
    }

    private static bool TryCompileSymbol(
        IReadOnlyList<ErrorReport> errors,
        string blob,
        PlatformStackProfile stack,
        out PlatformJitPlaybookMatch match)
    {
        match = null!;
        var compileHint = errors.FirstOrDefault(e =>
            e.ErrorType.Contains("compile", StringComparison.OrdinalIgnoreCase)
            || e.Message.Contains("cannot find symbol", StringComparison.OrdinalIgnoreCase)
            || e.Message.Contains("CS", StringComparison.OrdinalIgnoreCase));
        if (compileHint is null
            && !blob.Contains("cannot find symbol", StringComparison.OrdinalIgnoreCase)
            && !blob.Contains("error CS", StringComparison.OrdinalIgnoreCase))
            return false;

        match = new PlatformJitPlaybookMatch(
            "compile_symbol_fix",
            RepairPlaybookSignature.FromErrors(errors, blob, null).Signature,
            """
            ORCHESTRATOR JIT — compile symbol recovery
            Orchestrator already tried deterministic compile remediation.
            Actions: read_file at error line; apply_patch missing import/using/type; run_build.
            SKIP: subagent, browser, memory_search for single-symbol errors
            """);
        return true;
    }

    private static string? ExtractQuotedModule(string blob)
    {
        var match = Regex.Match(
            blob,
            @"No module named ['""]([^'""]+)['""]",
            RegexOptions.IgnoreCase);
        return match.Success ? match.Groups[1].Value : null;
    }
}
