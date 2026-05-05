namespace Libr4.IDE.Infrastructure.SemanticIndex;

/// <summary>
/// AST-aware chunker: splits files at function/class boundaries.
/// Falls back to line-based sliding window for unknown languages.
/// Analogous to SocratiCode indexer.ts chunking.
/// </summary>
public static class CodeChunker
{
    private const int MaxChunkLines = 60;
    private const int OverlapLines = 5;
    private const int MinChunkLines = 3;

    public static IReadOnlyList<CodeChunk> Chunk(string filePath, string content, string language)
    {
        var lines = content.Split('\n');
        var symbols = SymbolExtractor.ExtractSymbols(filePath, content, language).Symbols;

        if (symbols.Count > 0)
            return ChunkAtBoundaries(filePath, lines, symbols, language);

        return ChunkLineBased(filePath, lines, language);
    }

    private static IReadOnlyList<CodeChunk> ChunkAtBoundaries(
        string filePath,
        string[] lines,
        List<RawSymbol> symbols,
        string language)
    {
        var chunks = new List<CodeChunk>();
        var sortedSymbols = symbols.OrderBy(s => s.StartLine).ToList();

        for (int i = 0; i < sortedSymbols.Count; i++)
        {
            var sym = sortedSymbols[i];
            var endLine = i + 1 < sortedSymbols.Count
                ? sortedSymbols[i + 1].StartLine - 1
                : lines.Length;

            endLine = Math.Min(endLine, sym.StartLine + MaxChunkLines - 1);
            endLine = Math.Min(endLine, lines.Length);

            var startLine = Math.Max(0, sym.StartLine - 1);
            var chunkLines = endLine - startLine;
            if (chunkLines < MinChunkLines) continue;

            var chunkContent = string.Join('\n', lines.Skip(startLine).Take(chunkLines));
            if (string.IsNullOrWhiteSpace(chunkContent)) continue;

            chunks.Add(new CodeChunk(
                Id: $"{filePath}::{sym.Name}::{sym.StartLine}",
                FilePath: filePath,
                Language: language,
                StartLine: sym.StartLine,
                EndLine: endLine,
                Content: chunkContent,
                SymbolName: sym.Name,
                SymbolKind: sym.Kind));
        }

        // Capture any file header / preamble before first symbol
        if (sortedSymbols.Count > 0 && sortedSymbols[0].StartLine > 3)
        {
            var preamble = string.Join('\n', lines.Take(Math.Min(sortedSymbols[0].StartLine - 1, 30)));
            if (!string.IsNullOrWhiteSpace(preamble))
            {
                chunks.Insert(0, new CodeChunk(
                    Id: $"{filePath}::__header__::1",
                    FilePath: filePath,
                    Language: language,
                    StartLine: 1,
                    EndLine: Math.Min(sortedSymbols[0].StartLine - 1, 30),
                    Content: preamble,
                    SymbolName: null,
                    SymbolKind: "Preamble"));
            }
        }

        return chunks;
    }

    private static IReadOnlyList<CodeChunk> ChunkLineBased(
        string filePath,
        string[] lines,
        string language)
    {
        var chunks = new List<CodeChunk>();
        int i = 0;
        while (i < lines.Length)
        {
            var end = Math.Min(i + MaxChunkLines, lines.Length);
            var chunkLines = lines.Skip(i).Take(end - i);
            var content = string.Join('\n', chunkLines);

            if (!string.IsNullOrWhiteSpace(content))
            {
                chunks.Add(new CodeChunk(
                    Id: $"{filePath}::line::{i + 1}",
                    FilePath: filePath,
                    Language: language,
                    StartLine: i + 1,
                    EndLine: end,
                    Content: content,
                    SymbolName: null,
                    SymbolKind: "Block"));
            }

            i = Math.Max(end - OverlapLines, i + 1);
        }
        return chunks;
    }
}

public sealed record CodeChunk(
    string Id,
    string FilePath,
    string Language,
    int StartLine,
    int EndLine,
    string Content,
    string? SymbolName,
    string SymbolKind);
