using Libr4.AI.Infrastructure.AI;
using Microsoft.Extensions.Logging;

namespace Libr4.IDE.AutonomousAppGeneration.Agents;

/// <summary>
/// CI/CD pipeline agent with stack detection and pipeline generation
/// Inspired by claude-skills ci-cd-pipeline-builder skill
/// </summary>
public class CICDPipelineAgent : AgentSkillBase
{
    private readonly IAIService _aiService;
    private readonly ILogger _logger;

    public CICDPipelineAgent(
        string skillPath,
        IAIService aiService,
        ILogger logger) : base(skillPath)
    {
        _aiService = aiService;
        _logger = logger;
    }

    public override async Task<AgentResult> ExecuteAsync(AgentContext context)
    {
        _logger.LogInformation("Executing CICDPipelineAgent for application: {ApplicationName}", context.ApplicationName);

        var stack = DetectStack(context);
        _logger.LogInformation("Detected stack: Language={Language}, Framework={Framework}", stack.Language, stack.Framework);

        var skillInstructions = GetSkillInstructions();
        var prompt = BuildPrompt(context, stack, skillInstructions);

        var response = await _aiService.GenerateCompletionAsync(prompt, skillInstructions);

        var pipeline = ParsePipelineConfig(response, stack);

        _logger.LogInformation("CI/CD pipeline generated: Platform={Platform}", pipeline.Platform);

        return new AgentResult
        {
            IsSuccess = true,
            CICDPipeline = pipeline,
            Content = response
        };
    }

    private TechStack DetectStack(AgentContext context)
    {
        var stack = new TechStack();

        // Detect language from file extensions
        if (context.GeneratedFiles != null)
        {
            var extensions = context.GeneratedFiles
                .Select(f => Path.GetExtension(f.RelativePath).ToLower())
                .Distinct()
                .ToList();

            if (extensions.Contains(".cs"))
            {
                stack.Language = "C#";
                stack.Framework = DetectCSharpFramework(context);
            }
            else if (extensions.Contains(".ts") || extensions.Contains(".tsx"))
            {
                stack.Language = "TypeScript";
                stack.Framework = DetectTypeScriptFramework(context);
            }
            else if (extensions.Contains(".py"))
            {
                stack.Language = "Python";
                stack.Framework = DetectPythonFramework(context);
            }
            else if (extensions.Contains(".go"))
            {
                stack.Language = "Go";
            }
            else if (extensions.Contains(".rs"))
            {
                stack.Language = "Rust";
            }
        }

        // Default to context tech stack if detection fails
        if (string.IsNullOrEmpty(stack.Language) && !string.IsNullOrEmpty(context.TechStack))
        {
            stack.Language = context.TechStack;
        }

        return stack;
    }

    private string DetectCSharpFramework(AgentContext context)
    {
        var csprojFiles = context.GeneratedFiles?.Where(f => f.RelativePath.EndsWith(".csproj"));
        if (csprojFiles != null && csprojFiles.Any())
        {
            var content = csprojFiles.First().Content;
            if (content.Contains("Microsoft.AspNetCore"))
                return "ASP.NET Core";
            if (content.Contains("Microsoft.NET.Sdk.Web"))
                return "ASP.NET Core";
            if (content.Contains("Microsoft.NET.Sdk"))
                return ".NET";
        }
        return ".NET";
    }

    private string DetectTypeScriptFramework(AgentContext context)
    {
        var packageFiles = context.GeneratedFiles?.Where(f => f.RelativePath.Contains("package.json"));
        if (packageFiles != null && packageFiles.Any())
        {
            var content = packageFiles.First().Content;
            if (content.Contains("next"))
                return "Next.js";
            if (content.Contains("react"))
                return "React";
            if (content.Contains("vue"))
                return "Vue";
            if (content.Contains("angular"))
                return "Angular";
        }
        return "TypeScript";
    }

    private string DetectPythonFramework(AgentContext context)
    {
        var reqFiles = context.GeneratedFiles?.Where(f => f.RelativePath.Contains("requirements.txt") || f.RelativePath.Contains("pyproject.toml"));
        if (reqFiles != null && reqFiles.Any())
        {
            var content = reqFiles.First().Content;
            if (content.Contains("django"))
                return "Django";
            if (content.Contains("fastapi"))
                return "FastAPI";
            if (content.Contains("flask"))
                return "Flask";
        }
        return "Python";
    }

    private string BuildPrompt(AgentContext context, TechStack stack, string skillInstructions)
    {
        var sb = new System.Text.StringBuilder();
        
        sb.AppendLine("Generate a CI/CD pipeline configuration for the following application:");
        sb.AppendLine();
        sb.AppendLine($"Application Name: {context.ApplicationName}");
        sb.AppendLine($"Language: {stack.Language}");
        sb.AppendLine($"Framework: {stack.Framework}");
        sb.AppendLine($"Description: {context.Description}");
        sb.AppendLine();
        
        sb.AppendLine("Requirements:");
        sb.AppendLine("- Automated build");
        sb.AppendLine("- Automated testing");
        sb.AppendLine("- Code quality checks (linting, formatting)");
        sb.AppendLine("- Security scanning");
        sb.AppendLine("- Docker image build");
        sb.AppendLine("- Deployment to environment");
        sb.AppendLine();
        
        sb.AppendLine("Please provide the complete pipeline configuration file.");
        sb.AppendLine("Use GitHub Actions or GitLab CI based on the stack.");
        sb.AppendLine("Include all necessary steps and configurations.");

        return sb.ToString();
    }

    private CICDPipeline ParsePipelineConfig(string content, TechStack stack)
    {
        var pipeline = new CICDPipeline
        {
            Platform = stack.Language switch
            {
                "C#" or ".NET" => "GitHub Actions",
                "TypeScript" or "JavaScript" => "GitHub Actions",
                "Python" => "GitHub Actions",
                "Go" => "GitHub Actions",
                "Rust" => "GitHub Actions",
                _ => "GitHub Actions"
            },
            Language = stack.Language,
            Framework = stack.Framework,
            ConfigContent = content,
            Stages = new List<CICDStage>
            {
                new() { Name = "Build", Description = "Build the application" },
                new() { Name = "Test", Description = "Run automated tests" },
                new() { Name = "Quality", Description = "Code quality checks" },
                new() { Name = "Security", Description = "Security scanning" },
                new() { Name = "Deploy", Description = "Deploy to environment" }
            }
        };

        return pipeline;
    }
}

/// <summary>
/// Technology stack information
/// </summary>
public class TechStack
{
    public string Language { get; set; } = string.Empty;
    public string Framework { get; set; } = string.Empty;
    public string? Platform { get; set; }
}

/// <summary>
/// CI/CD pipeline configuration
/// </summary>
public class CICDPipeline
{
    public string Platform { get; set; } = string.Empty;
    public string Language { get; set; } = string.Empty;
    public string Framework { get; set; } = string.Empty;
    public string ConfigContent { get; set; } = string.Empty;
    public List<CICDStage> Stages { get; set; } = new();
    public Dictionary<string, string> EnvironmentVariables { get; set; } = new();
}

/// <summary>
/// CI/CD pipeline stage
/// </summary>
public class CICDStage
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public List<string> Commands { get; set; } = new();
}
