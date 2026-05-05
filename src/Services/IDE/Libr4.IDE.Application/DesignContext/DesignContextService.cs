using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Libr4.IDE.Application.DesignContext;

/// <summary>
/// Implementation of design context service (awesome-design-md style)
/// </summary>
public class DesignContextService : IDesignContextService
{
    private readonly ILogger<DesignContextService> _logger;
    
    public DesignContextService(ILogger<DesignContextService> logger)
    {
        _logger = logger;
    }
    
    public async Task<DesignContext?> GetDesignContextAsync(string workspacePath, CancellationToken ct = default)
    {
        try
        {
            var designMdPath = Path.Combine(workspacePath, "DESIGN.md");
            
            if (!File.Exists(designMdPath))
            {
                _logger.LogInformation("DESIGN.md not found in workspace: {WorkspacePath}", workspacePath);
                return null;
            }
            
            var content = await File.ReadAllTextAsync(designMdPath, ct);
            var context = ParseDesignMarkdown(content);
            
            _logger.LogInformation("Loaded design context from DESIGN.md");
            return context;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load design context from DESIGN.md");
            throw;
        }
    }
    
    public async Task SaveDesignContextAsync(string workspacePath, DesignContext context, CancellationToken ct = default)
    {
        try
        {
            var designMdPath = Path.Combine(workspacePath, "DESIGN.md");
            var markdown = GenerateDesignMarkdown(context);
            
            await File.WriteAllTextAsync(designMdPath, markdown, ct);
            
            _logger.LogInformation("Saved design context to DESIGN.md");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save design context to DESIGN.md");
            throw;
        }
    }
    
    public Task<DesignContext> GenerateDesignContextAsync(string workspacePath, CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Generating design context for workspace: {WorkspacePath}", workspacePath);
            
            // Analyze existing codebase to infer design patterns
            var projectName = new DirectoryInfo(workspacePath).Name;
            
            // Default design system for Libr4
            var context = new DesignContext
            {
                ProjectName = projectName,
                DesignSystem = "Libr4 Design System",
                ColorPalette = "Primary: #007ACC, Secondary: #6C757D, Success: #28A745, Warning: #FFC107, Danger: #DC3545, Dark: #343A40, Light: #F8F9FA",
                Typography = "Font Family: Inter, sans-serif; H1: 32px, H2: 28px, H3: 24px, H4: 20px, Body: 16px, Small: 14px",
                ComponentLibrary = "TailwindCSS + shadcn/ui",
                SpacingScale = "4px base scale: 4, 8, 12, 16, 20, 24, 32, 40, 48, 64, 80, 96",
                Breakpoints = "Mobile: 640px, Tablet: 768px, Desktop: 1024px, Wide: 1280px",
                DesignPrinciples = new[]
                {
                    "Clarity: Information should be clear and easy to understand",
                    "Consistency: Use consistent patterns across the UI",
                    "Efficiency: Enable users to complete tasks quickly",
                    "Accessibility: Ensure WCAG 2.1 AA compliance",
                    "Responsiveness: Design for all screen sizes"
                },
                ComponentPatterns = new[]
                {
                    "Cards: Rounded corners, subtle shadows, clear hierarchy",
                    "Buttons: Primary, secondary, ghost variants with proper hover states",
                    "Forms: Clear labels, helpful error messages, inline validation",
                    "Navigation: Clear visual hierarchy, active state indication",
                    "Modals: Centered, backdrop blur, clear close action"
                }
            };
            
            _logger.LogInformation("Generated design context for workspace: {WorkspacePath}", workspacePath);
            
            return Task.FromResult(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate design context");
            throw;
        }
    }
    
    public async Task<string> GetUIPromptAsync(string workspacePath, string task, CancellationToken ct = default)
    {
        try
        {
            var context = await GetDesignContextAsync(workspacePath, ct) ?? await GenerateDesignContextAsync(workspacePath, ct);
            
            var prompt = $@"You are a UI/UX designer and frontend developer. Generate UI components for the following task:

{task}

Design System Guidelines:
- Design System: {context.DesignSystem}
- Color Palette: {context.ColorPalette}
- Typography: {context.Typography}
- Component Library: {context.ComponentLibrary}
- Spacing Scale: {context.SpacingScale}
- Breakpoints: {context.Breakpoints}

Design Principles:
{string.Join("\n", context.DesignPrinciples.Select((p, i) => $"{i + 1}. {p}"))}

Component Patterns:
{string.Join("\n", context.ComponentPatterns.Select((p, i) => $"{i + 1}. {p}"))}

Requirements:
- Use TailwindCSS for styling
- Follow shadcn/ui component patterns
- Ensure responsive design
- Include proper accessibility attributes
- Use the specified color palette and typography
- Apply the design principles consistently
- Follow the component patterns for consistency";

            return prompt;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate UI prompt");
            throw;
        }
    }
    
    private DesignContext ParseDesignMarkdown(string markdown)
    {
        // Simple parsing implementation
        var context = new DesignContext();
        
        foreach (var line in markdown.Split('\n'))
        {
            if (line.StartsWith("#"))
            {
                context.ProjectName = line.Trim('#').Trim();
            }
            else if (line.StartsWith("## Color Palette"))
            {
                context.ColorPalette = line.Replace("## Color Palette", "").Trim();
            }
            else if (line.StartsWith("## Typography"))
            {
                context.Typography = line.Replace("## Typography", "").Trim();
            }
            else if (line.StartsWith("## Component Library"))
            {
                context.ComponentLibrary = line.Replace("## Component Library", "").Trim();
            }
            // Add more parsing logic as needed
        }
        
        return context;
    }
    
    private string GenerateDesignMarkdown(DesignContext context)
    {
        return $@"# {context.ProjectName} Design System

## Overview
This document defines the design system for {context.ProjectName}.

## Color Palette
{context.ColorPalette}

## Typography
{context.Typography}

## Component Library
{context.ComponentLibrary}

## Spacing Scale
{context.SpacingScale}

## Breakpoints
{context.Breakpoints}

## Design Principles
{string.Join("\n", context.DesignPrinciples.Select(p => $"- {p}"))}

## Component Patterns
{string.Join("\n", context.ComponentPatterns.Select(p => $"- {p}"))}
";
    }
}
