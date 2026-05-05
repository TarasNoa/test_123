using Microsoft.Extensions.Logging;

namespace Libr4.IDE.Application.DesignSkills;

/// <summary>
/// Implementation of design skills service (TypeUI Design Skills style)
/// </summary>
public class DesignSkillsService : IDesignSkillsService
{
    private readonly ILogger<DesignSkillsService> _logger;
    
    private static readonly Dictionary<string, string> DesignSkills = new()
    {
        ["responsive"] = "Ensure the component works seamlessly across all screen sizes using Tailwind's responsive prefixes (sm:, md:, lg:, xl:)",
        ["accessible"] = "Include proper ARIA labels, semantic HTML, keyboard navigation, and ensure WCAG 2.1 AA compliance",
        ["dark-mode"] = "Support both light and dark modes using Tailwind's dark: prefix and CSS variables",
        ["animated"] = "Add subtle, purposeful animations using Tailwind's transition and transform utilities",
        ["minimal"] = "Use clean, simple design with ample whitespace and focus on essential elements",
        ["material"] = "Apply Material Design principles including elevation, ripples, and FABs where appropriate",
        ["glassmorphism"] = "Use glass-like effects with backdrop-blur, semi-transparent backgrounds, and subtle borders",
        ["neumorphism"] = "Apply soft UI with subtle shadows that create depth and tactile feel",
        ["brutalist"] = "Use bold, raw aesthetics with strong borders, high contrast, and unconventional layouts",
        ["gradient"] = "Incorporate smooth gradients for backgrounds, text, or borders to add visual interest"
    };
    
    public DesignSkillsService(ILogger<DesignSkillsService> logger)
    {
        _logger = logger;
    }
    
    public Task<string> ApplyDesignSkillAsync(string componentDescription, string skillType, CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Applying design skill: {SkillType}", skillType);
            
            if (!DesignSkills.TryGetValue(skillType, out var skillGuidance))
            {
                _logger.LogWarning("Unknown design skill: {SkillType}", skillType);
                return Task.FromResult(componentDescription);
            }
            
            var enhancedDescription = $@"{componentDescription}

Design Skill Applied ({skillType}):
{skillGuidance}

Implementation Requirements:
- Follow the guidance above when implementing this component
- Ensure the skill is properly integrated with the overall design system
- Test the component in different contexts to verify the skill is effective";
            
            _logger.LogInformation("Applied design skill: {SkillType}", skillType);
            
            return Task.FromResult(enhancedDescription);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to apply design skill: {SkillType}", skillType);
            throw;
        }
    }
    
    public Task<string[]> GetAvailableSkillsAsync(CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Getting available design skills");
            
            var skills = DesignSkills.Keys.ToArray();
            
            _logger.LogInformation("Found {Count} available design skills", skills.Length);
            
            return Task.FromResult(skills);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get available design skills");
            throw;
        }
    }
    
    public Task<string> GenerateComponentWithSkillsAsync(string componentName, string[] skills, CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Generating component {ComponentName} with skills: {Skills}", componentName, string.Join(", ", skills));
            
            var skillGuidances = skills
                .Where(s => DesignSkills.ContainsKey(s))
                .Select(s => DesignSkills[s])
                .ToArray();
            
            var prompt = $@"Generate a React/TailwindCSS component named {componentName} with the following design skills applied:

{string.Join("\n\n", skillGuidances.Select((sg, i) => $"Skill {i + 1}:\n{sg}"))}

Component Requirements:
- Use TailwindCSS for all styling
- Follow the design skills guidance above
- Ensure the component is reusable and maintainable
- Include proper TypeScript types
- Add JSDoc comments for clarity
- Ensure accessibility (ARIA labels, semantic HTML)
- Test responsiveness across breakpoints
- Support both light and dark modes where applicable

Output the complete component code with all necessary imports and exports.";
            
            _logger.LogInformation("Generated component prompt for {ComponentName}", componentName);
            
            return Task.FromResult(prompt);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate component with skills");
            throw;
        }
    }
    
    public Task<DesignEvaluation> EvaluateDesignAsync(string componentCode, CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Evaluating design quality");
            
            var strengths = new List<string>();
            var weaknesses = new List<string>();
            var suggestions = new List<string>();
            
            // Simple heuristic evaluation
            if (componentCode.Contains("aria-"))
            {
                strengths.Add("Includes ARIA attributes for accessibility");
            }
            else
            {
                weaknesses.Add("Missing ARIA attributes");
                suggestions.Add("Add proper ARIA labels for better accessibility");
            }
            
            if (componentCode.Contains("dark:"))
            {
                strengths.Add("Supports dark mode");
            }
            else
            {
                weaknesses.Add("No dark mode support");
                suggestions.Add("Add dark mode variants using Tailwind's dark: prefix");
            }
            
            if (componentCode.Contains("sm:") || componentCode.Contains("md:") || componentCode.Contains("lg:"))
            {
                strengths.Add("Responsive design implemented");
            }
            else
            {
                weaknesses.Add("Not responsive");
                suggestions.Add("Add responsive breakpoints using Tailwind's responsive prefixes");
            }
            
            if (componentCode.Contains("transition"))
            {
                strengths.Add("Includes smooth transitions");
            }
            else
            {
                weaknesses.Add("Missing transitions");
                suggestions.Add("Add transition utilities for smoother interactions");
            }
            
            var score = strengths.Count * 20.0 / (strengths.Count + weaknesses.Count);
            
            _logger.LogInformation("Design evaluation completed with score: {Score}", score);
            
            return Task.FromResult(new DesignEvaluation
            {
                OverallScore = score,
                Strengths = strengths.ToArray(),
                Weaknesses = weaknesses.ToArray(),
                Suggestions = suggestions.ToArray()
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to evaluate design");
            throw;
        }
    }
}
