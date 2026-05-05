namespace Libr4.AI.Infrastructure.CodeGraph;

public class CodeNode
{
    public string Id { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string NodeType { get; set; } = string.Empty;  // "class", "method", "property", "interface", etc.
    public string FilePath { get; set; } = string.Empty;
    public int LineNumber { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = new();
}

public class CodeEdge
{
    public string Source { get; set; } = string.Empty;
    public string Target { get; set; } = string.Empty;
    public string Relation { get; set; } = string.Empty;  // "contains", "calls", "inherits", "implements", "imports", etc.
    public string FilePath { get; set; } = string.Empty;
    public int LineNumber { get; set; }
}
