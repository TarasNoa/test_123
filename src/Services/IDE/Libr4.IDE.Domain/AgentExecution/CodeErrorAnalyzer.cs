using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Libr4.IDE.Domain.AgentExecution;

public interface ICodeErrorAnalyzer
{
    ErrorAnalysis AnalyzeError(string errorMessage, string code, string language);
    List<ErrorAnalysis> AnalyzeMultipleErrors(string errorMessage, string code, string language);
}

public record ErrorAnalysis
{
    public string ErrorType { get; init; } = string.Empty;
    public string ErrorMessage { get; init; } = string.Empty;
    public int? LineNumber { get; init; }
    public string? SuggestedFix { get; init; }
    public string? FixDescription { get; init; }
    public double Confidence { get; init; } // 0.0 - 1.0
}

public class CodeErrorAnalyzer : ICodeErrorAnalyzer
{
    private readonly ILogger<CodeErrorAnalyzer> _logger;

    public CodeErrorAnalyzer(ILogger<CodeErrorAnalyzer> logger)
    {
        _logger = logger;
    }

    public ErrorAnalysis AnalyzeError(string errorMessage, string code, string language)
    {
        return language.ToLower() switch
        {
            "csharp" or "cs" => AnalyzeCSharpError(errorMessage, code),
            "fsharp" or "fs" => AnalyzeFSharpError(errorMessage, code),
            "typescript" or "ts" or "javascript" or "js" => AnalyzeTypeScriptError(errorMessage, code),
            _ => new ErrorAnalysis
            {
                ErrorType = "Unknown",
                ErrorMessage = errorMessage,
                Confidence = 0.0
            }
        };
    }

    public List<ErrorAnalysis> AnalyzeMultipleErrors(string errorMessage, string code, string language)
    {
        var lines = errorMessage.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
        return lines.Where(l => !string.IsNullOrWhiteSpace(l))
            .Select(line => AnalyzeError(line, code, language))
            .ToList();
    }

    private ErrorAnalysis AnalyzeCSharpError(string errorMessage, string code)
    {
        // NullReferenceException
        if (errorMessage.Contains("NullReferenceException"))
        {
            return new ErrorAnalysis
            {
                ErrorType = "NullReferenceException",
                ErrorMessage = errorMessage,
                FixDescription = "Object reference is null. Add null checks.",
                Confidence = 0.9
            };
        }

        // Syntax errors
        if (errorMessage.Contains("CS") && Regex.Match(errorMessage, @"CS\d{4}") is Match csMatch)
        {
            var errorCode = csMatch.Value;
            return AnalyzeCSharpSyntaxError(errorCode, errorMessage);
        }

        // Missing using
        if (errorMessage.Contains("does not exist in the current context"))
        {
            var match = Regex.Match(errorMessage, @"'(\w+)'");
            if (match.Success)
            {
                return new ErrorAnalysis
                {
                    ErrorType = "MissingReference",
                    ErrorMessage = errorMessage,
                    FixDescription = $"Add missing using statement for '{match.Groups[1].Value}'",
                    Confidence = 0.7
                };
            }
        }

        return new ErrorAnalysis
        {
            ErrorType = "Unknown",
            ErrorMessage = errorMessage,
            Confidence = 0.3
        };
    }

    private ErrorAnalysis AnalyzeCSharpSyntaxError(string errorCode, string errorMessage)
    {
        return errorCode switch
        {
            "CS0001" => new ErrorAnalysis
            {
                ErrorType = "SyntaxError",
                ErrorMessage = errorMessage,
                FixDescription = "Unexpected symbol in program",
                Confidence = 0.8
            },
            "CS0019" => new ErrorAnalysis
            {
                ErrorType = "TypeError",
                ErrorMessage = errorMessage,
                FixDescription = "Operator cannot be applied to operands of these types",
                Confidence = 0.85
            },
            _ => new ErrorAnalysis
            {
                ErrorType = "SyntaxError",
                ErrorMessage = errorMessage,
                Confidence = 0.6
            }
        };
    }

    private ErrorAnalysis AnalyzeFSharpError(string errorMessage, string code)
    {
        // Parse F# error format
        if (Regex.Match(errorMessage, @"error FS(\d+)") is Match match)
        {
            var errorCode = match.Groups[1].Value;
            var lineMatch = Regex.Match(errorMessage, @"line (\d+)");
            var lineNumber = lineMatch.Success ? int.Parse(lineMatch.Groups[1].Value) : null;

            return new ErrorAnalysis
            {
                ErrorType = $"FS{errorCode}",
                ErrorMessage = errorMessage,
                LineNumber = lineNumber,
                Confidence = 0.8
            };
        }

        return new ErrorAnalysis
        {
            ErrorType = "Unknown",
            ErrorMessage = errorMessage,
            Confidence = 0.4
        };
    }

    private ErrorAnalysis AnalyzeTypeScriptError(string errorMessage, string code)
    {
        // Type errors
        if (errorMessage.Contains("Type") && errorMessage.Contains("is not assignable"))
        {
            return new ErrorAnalysis
            {
                ErrorType = "TypeError",
                ErrorMessage = errorMessage,
                FixDescription = "Type mismatch - check variable types",
                Confidence = 0.85
            };
        }

        // Cannot find name
        if (errorMessage.Contains("Cannot find name"))
        {
            var match = Regex.Match(errorMessage, @"'(\w+)'");
            if (match.Success)
            {
                return new ErrorAnalysis
                {
                    ErrorType = "ReferenceError",
                    ErrorMessage = errorMessage,
                    FixDescription = $"Variable or function '{match.Groups[1].Value}' is not defined",
                    Confidence = 0.9
                };
            }
        }

        // Syntax error
        if (errorMessage.Contains("Unexpected token"))
        {
            return new ErrorAnalysis
            {
                ErrorType = "SyntaxError",
                ErrorMessage = errorMessage,
                FixDescription = "Check syntax around the error location",
                Confidence = 0.7
            };
        }

        return new ErrorAnalysis
        {
            ErrorType = "Unknown",
            ErrorMessage = errorMessage,
            Confidence = 0.3
        };
    }
}