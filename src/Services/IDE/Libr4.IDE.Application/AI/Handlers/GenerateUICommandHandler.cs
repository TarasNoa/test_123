/*
using System.Text.RegularExpressions;
using Libr4.IDE.Application.AI.Commands;
using Libr4.Shared.Contracts.AI;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Libr4.IDE.Application.AI.Handlers;

/// <summary>
/// Handler for GenerateUICommand - AI-powered React component generation
/// </summary>
public class GenerateUICommandHandler : IRequestHandler<GenerateUICommand, GenerateUIResult>
{
    private readonly ILogger<GenerateUICommandHandler> _logger;
    private readonly IAIProvider _aiProvider;
    
    public GenerateUICommandHandler(
        ILogger<GenerateUICommandHandler> logger,
        IAIProvider aiProvider)
    {
        _logger = logger;
        _aiProvider = aiProvider;
    }
    
    public async Task<GenerateUIResult> Handle(GenerateUICommand request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Generating UI component for workspace: {WorkspaceId}", request.WorkspaceId);
            
            var componentName = GenerateComponentName(request.Prompt);
            var code = await GenerateAIComponentAsync(componentName, request.Prompt, cancellationToken);
            
            _logger.LogInformation("Successfully generated UI component: {ComponentName}", componentName);
            
            return new GenerateUIResult(true, code, componentName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate UI component");
            return new GenerateUIResult(false, Error: ex.Message);
        }
    }
    
    private string GenerateComponentName(string prompt)
    {
        // Extract key noun from prompt for component name
        var words = prompt.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var keyWord = words.FirstOrDefault(w => w.Length > 3)?.Replace("[^a-zA-Z]", "") ?? "Component";
        return $"{char.ToUpper(keyWord[0])}{keyWord.Substring(1)}Component";
    }
    
    private async Task<string> GenerateAIComponentAsync(string componentName, string prompt, CancellationToken ct)
    {
        var systemPrompt = @"You are an expert React/TypeScript developer. Generate clean, modern React components following these rules:
1. Use TypeScript with proper interfaces
2. Use Tailwind CSS classes for styling
3. Make components functional with hooks if needed
4. Include proper prop types and JSDoc comments
5. Export as named export
6. Use modern React patterns (no class components)
7. Ensure accessibility (aria labels where appropriate)
8. Keep code under 100 lines, focused on the single responsibility

Respond ONLY with the code, no markdown formatting, no explanations.";

        var userPrompt = $"Generate a React component named '{componentName}' based on this description:\n\n{prompt}\n\nThe component should be production-ready and follow best practices.";

        var generatedCode = await _aiProvider.GenerateCompletionAsync(userPrompt, systemPrompt, "gpt-4");
        
        // Clean up the response
        var code = generatedCode.Trim();
        
        // Remove markdown code blocks if present
        if (code.StartsWith("```"))
        {
            code = Regex.Replace(code, "^```\\w*\\n?", "");
            code = Regex.Replace(code, "\\n?```$", "");
        }
        
        return code.Trim();
    }
}
*/
