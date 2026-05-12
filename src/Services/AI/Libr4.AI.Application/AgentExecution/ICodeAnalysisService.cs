using System.Threading.Tasks;

namespace Libr4.AI.Application.AgentExecution;

// Stub interface to avoid circular dependency with Libr4.IDE.Domain
public interface ICodeAnalysisService
{
    Task<CodeAnalysisResult> AnalyzeCodeAsync(string code);
}

public class CodeAnalysisResult
{
    public int IssueCount { get; set; }
    public int QualityScore { get; set; }
    public int PerformanceIssues { get; set; }
    public List<string> Recommendations { get; set; } = new();
    public List<string> SecurityConcerns { get; set; } = new();
}
