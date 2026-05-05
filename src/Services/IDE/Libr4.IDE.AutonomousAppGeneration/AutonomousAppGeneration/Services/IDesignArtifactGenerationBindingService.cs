namespace Libr4.IDE.Application.AutonomousAppGeneration.Services;

public interface IDesignArtifactGenerationBindingService
{
    Task<string> BindArtifactToGenerationPromptAsync(
        string basePrompt,
        DesignArtifact artifact,
        CancellationToken ct = default);

    bool ValidateGenerationPromptReferencesArtifact(
        string generationPrompt,
        string artifactId,
        out IReadOnlyList<string> missingReferences);

    string ExtractArtifactIdFromGenerationResponse(string response);
}

public sealed class DesignArtifactGenerationBindingService : IDesignArtifactGenerationBindingService
{
    public Task<string> BindArtifactToGenerationPromptAsync(
        string basePrompt,
        DesignArtifact artifact,
        CancellationToken ct = default)
    {
        var boundPrompt = BuildBoundPrompt(basePrompt, artifact);
        return Task.FromResult(boundPrompt);
    }

    public bool ValidateGenerationPromptReferencesArtifact(
        string generationPrompt,
        string artifactId,
        out IReadOnlyList<string> missingReferences)
    {
        var missing = new List<string>();

        if (!generationPrompt.Contains(artifactId, StringComparison.OrdinalIgnoreCase))
            missing.Add($"Artifact ID '{artifactId}' not referenced in generation prompt");

        if (!generationPrompt.Contains("design-tokens", StringComparison.OrdinalIgnoreCase) &&
            !generationPrompt.Contains("tokens", StringComparison.OrdinalIgnoreCase))
            missing.Add("Design tokens not referenced in generation prompt");

        if (!generationPrompt.Contains("palette", StringComparison.OrdinalIgnoreCase) &&
            !generationPrompt.Contains("colors", StringComparison.OrdinalIgnoreCase))
            missing.Add("Color palette not referenced in generation prompt");

        if (!generationPrompt.Contains("typography", StringComparison.OrdinalIgnoreCase) &&
            !generationPrompt.Contains("font", StringComparison.OrdinalIgnoreCase))
            missing.Add("Typography scale not referenced in generation prompt");

        if (!generationPrompt.Contains("component", StringComparison.OrdinalIgnoreCase))
            missing.Add("Component specifications not referenced in generation prompt");

        if (!generationPrompt.Contains("screen", StringComparison.OrdinalIgnoreCase) &&
            !generationPrompt.Contains("page", StringComparison.OrdinalIgnoreCase))
            missing.Add("Screen map not referenced in generation prompt");

        if (!generationPrompt.Contains("accessibility", StringComparison.OrdinalIgnoreCase) &&
            !generationPrompt.Contains("wcag", StringComparison.OrdinalIgnoreCase))
            missing.Add("Accessibility profile not referenced in generation prompt");

        missingReferences = missing;
        return missing.Count == 0;
    }

    public string ExtractArtifactIdFromGenerationResponse(string response)
    {
        var patterns = new[]
        {
            "artifact[_-]?id[\"']?\\s*[:=]\\s*[\"']?([a-z0-9-]+)",
            "design[_-]?artifact[_-]?id[\"']?\\s*[:=]\\s*[\"']?([a-z0-9-]+)",
            "[\"']?([a-z0-9-]*design[a-z0-9-]*)[\"']?"
        };

        foreach (var pattern in patterns)
        {
            var match = System.Text.RegularExpressions.Regex.Match(
                response,
                pattern,
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);

            if (match.Success && match.Groups.Count > 1)
                return match.Groups[1].Value;
        }

        return string.Empty;
    }

    private static string BuildBoundPrompt(string basePrompt, DesignArtifact artifact)
    {
        var sb = new System.Text.StringBuilder();

        sb.AppendLine(basePrompt);
        sb.AppendLine();
        sb.AppendLine("## Design Artifact Reference");
        sb.AppendLine($"Artifact ID: {artifact.Id}");
        sb.AppendLine($"Version: {artifact.Version}");
        sb.AppendLine();

        sb.AppendLine("### Design Tokens");
        sb.AppendLine($"- Primary Color: {artifact.Tokens.Colors.Primary}");
        sb.AppendLine($"- Secondary Color: {artifact.Tokens.Colors.Secondary}");
        sb.AppendLine($"- Success Color: {artifact.Tokens.Colors.Success}");
        sb.AppendLine($"- Spacing Unit: {artifact.Tokens.Spacing.Md}px");
        sb.AppendLine($"- Border Radius: {artifact.Tokens.Radius.Md}px");
        sb.AppendLine();

        sb.AppendLine("### Color Palette");
        foreach (var (key, value) in artifact.Palette.SemanticColors)
            sb.AppendLine($"- {key}: {value}");
        sb.AppendLine();

        sb.AppendLine("### Typography");
        sb.AppendLine($"- Font Family: {artifact.Typography.FontFamily}");
        sb.AppendLine($"- Display: {artifact.Typography.Display.Size}px, weight {artifact.Typography.Display.Weight}");
        sb.AppendLine($"- Body: {artifact.Typography.Body.Size}px, weight {artifact.Typography.Body.Weight}");
        sb.AppendLine($"- Caption: {artifact.Typography.Caption.Size}px, weight {artifact.Typography.Caption.Weight}");
        sb.AppendLine();

        sb.AppendLine("### Component Specifications");
        sb.AppendLine($"- Button Variants: {string.Join(", ", artifact.Components.Button.Variants)}");
        sb.AppendLine($"- Button Sizes: {string.Join(", ", artifact.Components.Button.Sizes)}");
        sb.AppendLine($"- Input Types: {string.Join(", ", artifact.Components.Input.Types)}");
        sb.AppendLine($"- Navigation Patterns: {string.Join(", ", artifact.Components.Navigation.Patterns)}");
        sb.AppendLine();

        sb.AppendLine("### Key Screens");
        foreach (var page in artifact.Screens.KeyPages)
            sb.AppendLine($"- {page}");
        sb.AppendLine();

        sb.AppendLine("### Accessibility Requirements");
        sb.AppendLine($"- Contrast Level: {artifact.Accessibility.ContrastLevel}");
        sb.AppendLine($"- Keyboard Navigation: {artifact.Accessibility.KeyboardNavigationSupported}");
        sb.AppendLine($"- Screen Reader Optimized: {artifact.Accessibility.ScreenReaderOptimized}");
        sb.AppendLine($"- Focus Indicators: {string.Join(", ", artifact.Accessibility.FocusIndicatorStyles)}");
        sb.AppendLine($"- ARIA Landmarks: {string.Join(", ", artifact.Accessibility.AriaLandmarks)}");
        sb.AppendLine();

        sb.AppendLine("## Instructions");
        sb.AppendLine("Use the design artifact specifications above to guide code generation.");
        sb.AppendLine("Ensure all generated components match the defined design tokens, palette, and typography.");
        sb.AppendLine("Implement all required accessibility features from the accessibility profile.");
        sb.AppendLine("Reference this artifact ID in your response for traceability.");

        return sb.ToString();
    }
}
