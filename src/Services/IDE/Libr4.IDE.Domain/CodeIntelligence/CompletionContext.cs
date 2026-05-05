namespace Libr4.IDE.Domain.CodeIntelligence;

/// <summary>
/// Value object for completion context
/// </summary>
public class CompletionContext
{
    public string FilePath { get; private set; }
    public int Line { get; private set; }
    public int Column { get; private set; }
    public string Prefix { get; private set; }
    public string SurroundingCode { get; private set; }
    public string Language { get; private set; }
    
    private CompletionContext() { }
    
    public CompletionContext(
        string filePath,
        int line,
        int column,
        string prefix,
        string surroundingCode = "",
        string language = "")
    {
        FilePath = filePath;
        Line = line;
        Column = column;
        Prefix = prefix;
        SurroundingCode = surroundingCode;
        Language = language;
    }
    
    public static CompletionContext Create(
        string filePath,
        int line,
        int column,
        string prefix,
        string surroundingCode = "",
        string language = "")
    {
        return new CompletionContext(filePath, line, column, prefix, surroundingCode, language);
    }
}
