using System.Text.RegularExpressions;

namespace Libr4.IDE.Infrastructure.SemanticIndex;

/// <summary>
/// Regex-based symbol extractor for C#, F#, Rust, TypeScript/JS.
/// AST-aware via patterns — no external parser required.
/// Analogous to SocratiCode graph-symbols.ts
/// </summary>
public static class SymbolExtractor
{
    private static readonly Regex CSharpClassRx = new(
        @"(?:^|\n)\s*(?:public|internal|private|protected|file)?\s*(?:abstract|sealed|static|partial|readonly)?\s*(?:class|interface|record|struct|enum)\s+(?<name>\w+)",
        RegexOptions.Compiled | RegexOptions.Multiline);

    private static readonly Regex CSharpMethodRx = new(
        @"(?:^|\n)\s*(?:public|private|protected|internal|static|virtual|override|abstract|async|new|sealed|extern)\s+(?:(?:async|static|virtual|override|abstract|sealed|extern)\s+)*(?<ret>[\w<>\[\]?.,\s]+?)\s+(?<name>\w+)\s*[<(]",
        RegexOptions.Compiled | RegexOptions.Multiline);

    private static readonly Regex CSharpPropertyRx = new(
        @"(?:^|\n)\s*(?:public|private|protected|internal|static|virtual|override|abstract|new)\s+(?:(?:static|virtual|override|abstract|new)\s+)*(?<type>[\w<>\[\]?,\s]+?)\s+(?<name>\w+)\s*\{",
        RegexOptions.Compiled | RegexOptions.Multiline);

    private static readonly Regex FSharpTypeRx = new(
        @"(?:^|\n)\s*(?:type|module|let\s+rec|let)\s+(?<name>\w+)",
        RegexOptions.Compiled | RegexOptions.Multiline);

    private static readonly Regex RustFnRx = new(
        @"(?:^|\n)\s*(?:pub\s+)?(?:async\s+)?fn\s+(?<name>\w+)",
        RegexOptions.Compiled | RegexOptions.Multiline);

    private static readonly Regex RustStructRx = new(
        @"(?:^|\n)\s*(?:pub\s+)?(?:struct|enum|trait|impl)\s+(?<name>\w+)",
        RegexOptions.Compiled | RegexOptions.Multiline);

    private static readonly Regex TsClassRx = new(
        @"(?:^|\n)\s*(?:export\s+)?(?:abstract\s+)?(?:class|interface|type|enum)\s+(?<name>\w+)",
        RegexOptions.Compiled | RegexOptions.Multiline);

    private static readonly Regex TsFunctionRx = new(
        @"(?:^|\n)\s*(?:export\s+)?(?:async\s+)?function\s+(?<name>\w+)|(?:^|\n)\s*(?:export\s+)?const\s+(?<name2>\w+)\s*=\s*(?:async\s*)?\(",
        RegexOptions.Compiled | RegexOptions.Multiline);

    private static readonly Regex ImportCsRx = new(
        @"^\s*using\s+(?:static\s+)?(?<ns>[\w.]+)\s*;",
        RegexOptions.Compiled | RegexOptions.Multiline);

    private static readonly Regex ImportFsRx = new(
        @"^\s*open\s+(?<ns>[\w.]+)",
        RegexOptions.Compiled | RegexOptions.Multiline);

    private static readonly Regex ImportRsRx = new(
        @"^\s*use\s+(?<ns>[\w::{},\s]+)\s*;",
        RegexOptions.Compiled | RegexOptions.Multiline);

    private static readonly Regex ImportTsRx = new(
        @"^\s*(?:import|export)\s+.*?from\s+['""](?<path>[^'""]+)['""]",
        RegexOptions.Compiled | RegexOptions.Multiline);

    // ── Public API ────────────────────────────────────────────────────────

    public static ExtractedSymbols ExtractSymbols(string filePath, string content, string language)
    {
        var symbols = language switch
        {
            "cs"         => ExtractCSharp(filePath, content),
            "fs" or "fsi" or "fsx" => ExtractFSharp(filePath, content),
            "rs"         => ExtractRust(filePath, content),
            "ts" or "tsx" or "js" or "jsx" => ExtractTypeScript(filePath, content),
            _            => new List<RawSymbol>()
        };

        var imports = ExtractImports(filePath, content, language);

        return new ExtractedSymbols(filePath, language, symbols, imports);
    }

    public static List<string> ExtractImports(string filePath, string content, string language)
    {
        return language switch
        {
            "cs" => ImportCsRx.Matches(content)
                        .Select(m => m.Groups["ns"].Value)
                        .ToList(),
            "fs" or "fsi" or "fsx" => ImportFsRx.Matches(content)
                        .Select(m => m.Groups["ns"].Value)
                        .ToList(),
            "rs" => ImportRsRx.Matches(content)
                        .Select(m => m.Groups["ns"].Value.Trim())
                        .ToList(),
            "ts" or "tsx" or "js" or "jsx" => ImportTsRx.Matches(content)
                        .Select(m => m.Groups["path"].Value)
                        .ToList(),
            _ => new List<string>()
        };
    }

    // ── Private per-language extractors ──────────────────────────────────

    private static List<RawSymbol> ExtractCSharp(string filePath, string content)
    {
        var results = new List<RawSymbol>();
        var lines = content.Split('\n');

        foreach (Match m in CSharpClassRx.Matches(content))
        {
            var line = GetLineNumber(content, m.Index);
            results.Add(new RawSymbol(m.Groups["name"].Value, "Class", line, line));
        }

        foreach (Match m in CSharpMethodRx.Matches(content))
        {
            var name = m.Groups["name"].Value;
            if (IsKeyword(name)) continue;
            var line = GetLineNumber(content, m.Index);
            results.Add(new RawSymbol(name, "Method", line, line + 20));
        }

        foreach (Match m in CSharpPropertyRx.Matches(content))
        {
            var name = m.Groups["name"].Value;
            if (IsKeyword(name)) continue;
            var line = GetLineNumber(content, m.Index);
            results.Add(new RawSymbol(name, "Property", line, line));
        }

        return results;
    }

    private static List<RawSymbol> ExtractFSharp(string filePath, string content)
    {
        var results = new List<RawSymbol>();
        foreach (Match m in FSharpTypeRx.Matches(content))
        {
            var line = GetLineNumber(content, m.Index);
            results.Add(new RawSymbol(m.Groups["name"].Value, "Function", line, line + 10));
        }
        return results;
    }

    private static List<RawSymbol> ExtractRust(string filePath, string content)
    {
        var results = new List<RawSymbol>();
        foreach (Match m in RustFnRx.Matches(content))
        {
            var line = GetLineNumber(content, m.Index);
            results.Add(new RawSymbol(m.Groups["name"].Value, "Function", line, line + 20));
        }
        foreach (Match m in RustStructRx.Matches(content))
        {
            var line = GetLineNumber(content, m.Index);
            results.Add(new RawSymbol(m.Groups["name"].Value, "Class", line, line + 10));
        }
        return results;
    }

    private static List<RawSymbol> ExtractTypeScript(string filePath, string content)
    {
        var results = new List<RawSymbol>();
        foreach (Match m in TsClassRx.Matches(content))
        {
            var line = GetLineNumber(content, m.Index);
            results.Add(new RawSymbol(m.Groups["name"].Value, "Class", line, line));
        }
        foreach (Match m in TsFunctionRx.Matches(content))
        {
            var name = m.Groups["name"].Success ? m.Groups["name"].Value : m.Groups["name2"].Value;
            if (string.IsNullOrWhiteSpace(name)) continue;
            var line = GetLineNumber(content, m.Index);
            results.Add(new RawSymbol(name, "Function", line, line + 20));
        }
        return results;
    }

    private static int GetLineNumber(string content, int charIndex)
    {
        var sub = content[..Math.Min(charIndex, content.Length)];
        return sub.Count(c => c == '\n') + 1;
    }

    private static readonly HashSet<string> Keywords = new(StringComparer.OrdinalIgnoreCase)
    {
        "if", "else", "for", "while", "return", "new", "this", "base", "true", "false",
        "null", "void", "int", "string", "bool", "object", "var", "using", "namespace",
        "class", "interface", "enum", "struct", "record", "static", "public", "private",
        "protected", "internal", "override", "virtual", "abstract", "sealed", "partial",
        "async", "await", "get", "set", "add", "remove", "value", "yield", "where",
        "select", "from", "in", "out", "ref", "params", "readonly", "const", "event",
        "delegate", "operator", "explicit", "implicit", "checked", "unchecked", "typeof",
        "sizeof", "stackalloc", "lock", "fixed", "throw", "try", "catch", "finally",
        "switch", "case", "break", "continue", "goto", "default"
    };

    private static bool IsKeyword(string name) => Keywords.Contains(name);
}

public sealed record RawSymbol(string Name, string Kind, int StartLine, int EndLine);

public sealed record ExtractedSymbols(
    string FilePath,
    string Language,
    List<RawSymbol> Symbols,
    List<string> Imports);
