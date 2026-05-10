using System;
using System.Threading.Tasks;

namespace Libr4.IDE.Domain.AgentExecution;

public interface ICodeRepairService
{
    Task<string?> RepairCodeAsync(string code, ErrorAnalysis errorAnalysis, string language);
    Task<(bool Success, string RepairedCode)> AttemptAutoFixAsync(string code, string errorMessage, string language);
}

public class CodeRepairService : ICodeRepairService
{
    private readonly ICodeErrorAnalyzer _errorAnalyzer;
    private readonly ILogger<CodeRepairService> _logger;

    public CodeRepairService(ICodeErrorAnalyzer errorAnalyzer, ILogger<CodeRepairService> logger)
    {
        _errorAnalyzer = errorAnalyzer;
        _logger = logger;
    }

    public async Task<string?> RepairCodeAsync(string code, ErrorAnalysis errorAnalysis, string language)
    {
        return errorAnalysis.ErrorType switch
        {
            "NullReferenceException" => RepairNullReference(code, errorAnalysis, language),
            "MissingReference" => RepairMissingReference(code, errorAnalysis, language),
            "TypeError" => RepairTypeError(code, errorAnalysis, language),
            "SyntaxError" => await RepairSyntaxErrorAsync(code, errorAnalysis, language),
            "ReferenceError" => RepairReferenceError(code, errorAnalysis, language),
            _ => null
        };
    }

    public async Task<(bool Success, string RepairedCode)> AttemptAutoFixAsync(string code, string errorMessage, string language)
    {
        try
        {
            var errors = _errorAnalyzer.AnalyzeMultipleErrors(errorMessage, code, language);
            var repairedCode = code;

            foreach (var error in errors.Where(e => e.Confidence > 0.75))
            {
                var fixed_code = await RepairCodeAsync(repairedCode, error, language);
                if (fixed_code != null)
                {
                    repairedCode = fixed_code;
                    _logger.LogInformation($"Applied fix for {error.ErrorType}");
                }
            }

            return (repairedCode != code, repairedCode);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error during auto-fix: {ex.Message}");
            return (false, code);
        }
    }

    private string? RepairNullReference(string code, ErrorAnalysis error, string language)
    {
        if (language is "csharp" or "cs")
        {
            // Add null coalescing or null check
            return code.Replace("?.", "?.") // Ensure null-conditional is used
                      .Replace(".", "?.");
        }
        return null;
    }

    private string? RepairMissingReference(string code, ErrorAnalysis error, string language)
    {
        if (language is "csharp" or "cs")
        {
            var match = Regex.Match(error.FixDescription ?? "", @"'(\w+)'");
            if (match.Success)
            {
                var name = match.Groups[1].Value;
                var usingStatement = $"using {GuessNamespace(name)};\n";
                return usingStatement + code;
            }
        }
        return null;
    }

    private string? RepairTypeError(string code, ErrorAnalysis error, string language)
    {
        // Generic type conversion attempt
        if (language is "typescript" or "ts")
        {
            return code.Replace("=", " as any ="); // Last resort casting
        }
        if (language is "csharp" or "cs")
        {
            return code.Replace("=", " = (object)"); // Cast to object
        }
        return null;
    }

    private async Task<string?> RepairSyntaxErrorAsync(string code, ErrorAnalysis error, string language)
    {
        return language switch
        {
            "typescript" or "ts" => await RepairTypeScriptSyntaxAsync(code),
            "csharp" or "cs" => RepairCSharpSyntax(code),
            "fsharp" or "fs" => RepairFSharpSyntax(code),
            _ => null
        };
    }

    private string? RepairReferenceError(string code, ErrorAnalysis error, string language)
    {
        var match = Regex.Match(error.FixDescription ?? "", @"'(\w+)'");
        if (match.Success)
        {
            var varName = match.Groups[1].Value;
            if (language is "typescript" or "ts")
            {
                return $"const {varName} = undefined;\n" + code;
            }
            if (language is "csharp" or "cs")
            {
                return $"var {varName} = null;\n" + code;
            }
        }
        return null;
    }

    private string RepairCSharpSyntax(string code)
    {
        // Add missing braces
        if (!code.Contains("{") && !code.Contains("}"))
        {
            code = "{\n" + code + "\n}";
        }

        // Add missing semicolons
        code = Regex.Replace(code, @"([^;{}\n])\n", "$1;\n");

        return code;
    }

    private async Task<string> RepairTypeScriptSyntaxAsync(string code)
    {
        // Add missing semicolons
        code = Regex.Replace(code, @"([^;{}\n])\n", "$1;\n");

        // Fix missing closing braces
        var openBraces = code.Count(c => c == '{');
        var closeBraces = code.Count(c => c == '}');
        if (openBraces > closeBraces)
        {
            code += "\n" + new string('}', openBraces - closeBraces);
        }

        return await Task.FromResult(code);
    }

    private string RepairFSharpSyntax(string code)
    {
        // F# specific fixes
        code = Regex.Replace(code, @";;?\s*$", ";;\n", RegexOptions.Multiline);
        return code;
    }

    private static string GuessNamespace(string typeName)
    {
        return typeName switch
        {
            "List" or "Dictionary" or "Queue" => "System.Collections.Generic",
            "Task" => "System.Threading.Tasks",
            "Regex" => "System.Text.RegularExpressions",
            "Linq" => "System.Linq",
            _ => "System"
        };
    }
}