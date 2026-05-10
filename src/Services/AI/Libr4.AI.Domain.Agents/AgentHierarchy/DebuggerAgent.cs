using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Libr4.AI.Domain.Agents.AgentHierarchy;

public class DebuggerAgent : BaseAgent
{
    private readonly ICodeExecutor _executor;
    private readonly ICodeErrorAnalyzer _errorAnalyzer;
    private readonly ICodeRepairService _repairService;

    public DebuggerAgent(
        ILogger<BaseAgent> logger,
        ICodeExecutor executor,
        ICodeErrorAnalyzer errorAnalyzer,
        ICodeRepairService repairService)
        : base(logger, "DebuggerAgent", AgentType.Debugger)
    {
        _executor = executor;
        _errorAnalyzer = errorAnalyzer;
        _repairService = repairService;
    }

    protected override async Task<string> ExecuteInternalAsync(AgentRequest request)
    {
        _logger.LogInformation($"Debugger processing: {request.Task}");

        var code = request.Parameters.ContainsKey("code")
            ? request.Parameters["code"].ToString() ?? ""
            : "";

        var language = request.Parameters.ContainsKey("language")
            ? request.Parameters["language"].ToString() ?? "csharp"
            : "csharp";

        // Execute code
        var result = await _executor.ExecuteAsync(code, language);

        if (result.Status == ExecutionStatus.Success)
        {
            return $"✅ Code executed successfully!\n\nOutput:\n{result.Output}";
        }

        // Analyze error
        var errorAnalysis = _errorAnalyzer.AnalyzeError(result.ErrorMessage ?? "", code, language);
        var repairedCode = await _repairService.RepairCodeAsync(code, errorAnalysis, language);

        if (repairedCode != null)
        {
            return $@"
🐛 Bug Found and Fixed!

Original Error: {errorAnalysis.ErrorMessage}
Error Type: {errorAnalysis.ErrorType}
Confidence: {errorAnalysis.Confidence * 100}%

Fix Applied: {errorAnalysis.FixDescription}

Repaired Code:
{repairedCode}
";
        }

        return $"❌ Unable to debug:\n{result.ErrorMessage}";
    }

    public override Task<bool> CanHandleAsync(string taskType)
    {
        var canHandle = taskType.Contains("debug", StringComparison.OrdinalIgnoreCase) ||
                       taskType.Contains("fix", StringComparison.OrdinalIgnoreCase) ||
                       taskType.Contains("error", StringComparison.OrdinalIgnoreCase);

        return Task.FromResult(canHandle);
    }

    public override AgentCapabilities GetCapabilities()
    {
        return new AgentCapabilities
        {
            SupportedTasks = new List<string>
            {
                "debug code",
                "find error",
                "fix bug",
                "trace execution",
                "diagnose issue"
            },
            SupportedLanguages = new List<string> { "csharp", "fsharp", "typescript" },
            MaxConcurrentTasks = 3,
            AverageExecutionTime = TimeSpan.FromSeconds(4),
            SuccessRate = 0.87
        };
    }
}