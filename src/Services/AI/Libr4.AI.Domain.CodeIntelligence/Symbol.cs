using System;
using System.Collections.Generic;

namespace Libr4.AI.Domain.CodeIntelligence;

public enum SymbolType { Function, Class, Method, Variable, Constant, Interface, Enum, Type, Namespace, Module }
public enum ReferenceType { Definition, Usage, Call, Import, Inheritance }

public class CodeSymbol
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public SymbolType Type { get; set; }
    public string? Signature { get; set; }
    public string FilePath { get; set; } = string.Empty;
    public int LineStart { get; set; }
    public int LineEnd { get; set; }
    public int ColumnStart { get; set; }
    public int ColumnEnd { get; set; }
    public string? Documentation { get; set; }
    public string? ContainingSymbol { get; set; }
    public List<string> Modifiers { get; set; } = new List<string>();
    public DateTimeOffset IndexedAt { get; set; }
}

public class SymbolReference
{
    public Guid Id { get; set; }
    public Guid SymbolId { get; set; }
    public string FilePath { get; set; } = string.Empty;
    public int Line { get; set; }
    public int Column { get; set; }
    public ReferenceType ReferenceType { get; set; }
    public string? Context { get; set; }
}

public class RefactoringOperation
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string OperationType { get; set; } = string.Empty; // rename, extract, inline, move
    public string TargetSymbol { get; set; } = string.Empty;
    public Dictionary<string, object> Parameters { get; set; } = new Dictionary<string, object>();
    public List<string> AffectedFiles { get; set; } = new List<string>();
    public bool WasApplied { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

public class CodeIndex
{
    public Guid Id { get; set; }
    public Guid ProjectId { get; set; }
    public int TotalSymbols { get; set; }
    public int TotalFiles { get; set; }
    public Dictionary<string, int> SymbolsByType { get; set; } = new Dictionary<string, int>();
    public DateTimeOffset LastIndexedAt { get; set; }
    public string Status { get; set; } = "ready";
}

public class Symbol
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
}
