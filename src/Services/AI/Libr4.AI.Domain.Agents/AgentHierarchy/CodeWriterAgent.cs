using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Libr4.AI.Application.AgentExecution;

namespace Libr4.AI.Domain.Agents.AgentHierarchy;

public class CodeWriterAgent : BaseAgent
{
    private readonly ICodeGenerationService _codeGenerator;

    public CodeWriterAgent(ILogger<BaseAgent> logger, ICodeGenerationService codeGenerator)
        : base(logger, "CodeWriterAgent", AgentType.CodeWriter)
    {
        _codeGenerator = codeGenerator;
    }

    protected override async Task<string> ExecuteInternalAsync(AgentRequest request)
    {
        _logger.LogInformation($"CodeWriter generating code for: {request.Task}");

        var language = request.Parameters.ContainsKey("language")
            ? request.Parameters["language"].ToString() ?? "csharp"
            : "csharp";

        var code = await _codeGenerator.GenerateCodeAsync(
            request.Task,
            request.Context,
            language
        );

        return code;
    }

    public override Task<bool> CanHandleAsync(string taskType)
    {
        var canHandle = taskType.Contains("code", StringComparison.OrdinalIgnoreCase) ||
                       taskType.Contains("write", StringComparison.OrdinalIgnoreCase) ||
                       taskType.Contains("implement", StringComparison.OrdinalIgnoreCase);

        return Task.FromResult(canHandle);
    }

    public override AgentCapabilities GetCapabilities()
    {
        return new AgentCapabilities
        {
            SupportedTasks = new List<string>
            {
                "write code",
                "generate function",
                "create class",
                "implement interface",
                "fix code"
            },
            SupportedLanguages = new List<string> { "csharp", "fsharp", "typescript", "python" },
            MaxConcurrentTasks = 5,
            AverageExecutionTime = TimeSpan.FromSeconds(3),
            SuccessRate = 0.88
        };
    }
}