using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Libr4.IDE.Application.AutonomousAppGeneration.Services;

public class DesignArtifactService : IDesignArtifactService
{
    private readonly Dictionary<string, DesignArtifact> _artifacts = new();
    private readonly Dictionary<string, string> _runToArtifactId = new();
    private readonly JsonSerializerOptions _jsonOptions;

    public DesignArtifactService()
    {
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            WriteIndented = true
        };
    }

    public Task<DesignArtifact> CreateArtifactAsync(
        string runId,
        DesignTokens tokens,
        DesignPalette palette,
        TypographyScale typography,
        ComponentSpecifications components,
        ScreenMap screens,
        AccessibilityProfile accessibility,
        CancellationToken ct = default)
    {
        var artifact = new DesignArtifact
        {
            Id = GenerateDeterministicId(runId),
            RunId = runId,
            CreatedAtUtc = DateTime.UtcNow,
            Tokens = tokens,
            Palette = palette,
            Typography = typography,
            Components = components,
            Screens = screens,
            Accessibility = accessibility,
            Version = 1
        };

        artifact.ContentHash = ComputeContentHash(artifact);
        _artifacts[artifact.Id] = artifact;
        _runToArtifactId[runId] = artifact.Id;

        return Task.FromResult(artifact);
    }

    public Task<DesignArtifact?> GetArtifactAsync(string artifactId, CancellationToken ct = default)
    {
        _artifacts.TryGetValue(artifactId, out var artifact);
        return Task.FromResult(artifact);
    }

    public Task<DesignArtifact?> GetArtifactByRunAsync(string runId, CancellationToken ct = default)
    {
        if (_runToArtifactId.TryGetValue(runId, out var artifactId))
        {
            _artifacts.TryGetValue(artifactId, out var artifact);
            return Task.FromResult(artifact);
        }

        return Task.FromResult<DesignArtifact?>(null);
    }

    public bool ValidateArtifact(DesignArtifact artifact, out IReadOnlyList<string> errors)
    {
        var errorList = new List<string>();

        if (artifact == null)
        {
            errorList.Add("Artifact cannot be null");
            errors = errorList;
            return false;
        }

        if (string.IsNullOrWhiteSpace(artifact.Id))
            errorList.Add("Artifact ID is required");

        if (string.IsNullOrWhiteSpace(artifact.RunId))
            errorList.Add("Run ID is required");

        if (artifact.Tokens == null)
            errorList.Add("Design tokens are required");
        else
            ValidateTokens(artifact.Tokens, errorList);

        if (artifact.Palette == null)
            errorList.Add("Design palette is required");
        else
            ValidatePalette(artifact.Palette, errorList);

        if (artifact.Typography == null)
            errorList.Add("Typography scale is required");
        else
            ValidateTypography(artifact.Typography, errorList);

        if (artifact.Components == null)
            errorList.Add("Component specifications are required");
        else
            ValidateComponents(artifact.Components, errorList);

        if (artifact.Screens == null)
            errorList.Add("Screen map is required");
        else
            ValidateScreens(artifact.Screens, errorList);

        if (artifact.Accessibility == null)
            errorList.Add("Accessibility profile is required");
        else
            ValidateAccessibility(artifact.Accessibility, errorList);

        if (string.IsNullOrWhiteSpace(artifact.ContentHash))
            errorList.Add("Content hash is required");

        if (artifact.Version < 1)
            errorList.Add("Version must be >= 1");

        errors = errorList;
        return errorList.Count == 0;
    }

    public string SerializeArtifact(DesignArtifact artifact)
    {
        return JsonSerializer.Serialize(artifact, _jsonOptions);
    }

    public DesignArtifact? DeserializeArtifact(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<DesignArtifact>(json, _jsonOptions);
        }
        catch
        {
            return null;
        }
    }

    private void ValidateTokens(DesignTokens tokens, List<string> errors)
    {
        if (tokens.Colors == null)
            errors.Add("Color tokens are required");
        else if (string.IsNullOrWhiteSpace(tokens.Colors.Primary))
            errors.Add("Primary color token is required");

        if (tokens.Spacing == null)
            errors.Add("Spacing tokens are required");
        else if (tokens.Spacing.Md <= 0)
            errors.Add("Spacing tokens must have positive values");

        if (tokens.Radius == null)
            errors.Add("Radius tokens are required");

        if (tokens.Shadows == null)
            errors.Add("Shadow tokens are required");

        if (tokens.Motion == null)
            errors.Add("Motion tokens are required");
    }

    private void ValidatePalette(DesignPalette palette, List<string> errors)
    {
        if (string.IsNullOrWhiteSpace(palette.Name))
            errors.Add("Palette name is required");

        if (palette.SemanticColors == null || palette.SemanticColors.Count == 0)
            errors.Add("Semantic colors are required");

        if (palette.BrandColors == null)
            errors.Add("Brand colors dictionary is required");
    }

    private void ValidateTypography(TypographyScale typography, List<string> errors)
    {
        if (typography.Display == null || typography.Display.Size <= 0)
            errors.Add("Display typography must have positive size");

        if (typography.Body == null || typography.Body.Size <= 0)
            errors.Add("Body typography must have positive size");

        if (string.IsNullOrWhiteSpace(typography.FontFamily))
            errors.Add("Font family is required");
    }

    private void ValidateComponents(ComponentSpecifications components, List<string> errors)
    {
        if (components.Button == null || components.Button.Variants.Length == 0)
            errors.Add("Button component must have variants");

        if (components.Input == null || components.Input.Types.Length == 0)
            errors.Add("Input component must have types");

        if (components.Card == null)
            errors.Add("Card component specification is required");

        if (components.Modal == null)
            errors.Add("Modal component specification is required");

        if (components.Navigation == null)
            errors.Add("Navigation component specification is required");

        if (components.Table == null)
            errors.Add("Table component specification is required");

        if (components.Form == null)
            errors.Add("Form component specification is required");
    }

    private void ValidateScreens(ScreenMap screens, List<string> errors)
    {
        if (screens.Screens == null)
            errors.Add("Screens dictionary is required");

        if (screens.KeyPages == null || screens.KeyPages.Length == 0)
            errors.Add("At least one key page must be defined");
    }

    private void ValidateAccessibility(AccessibilityProfile accessibility, List<string> errors)
    {
        if (string.IsNullOrWhiteSpace(accessibility.ContrastLevel))
            errors.Add("Contrast level is required (AA or AAA)");

        if (accessibility.FocusIndicatorStyles == null || accessibility.FocusIndicatorStyles.Length == 0)
            errors.Add("Focus indicator styles are required");

        if (accessibility.AriaLandmarks == null || accessibility.AriaLandmarks.Length == 0)
            errors.Add("ARIA landmarks are required");
    }

    private string GenerateDeterministicId(string runId)
    {
        using (var sha = SHA256.Create())
        {
            var hash = sha.ComputeHash(Encoding.UTF8.GetBytes($"design-artifact:{runId}:{DateTime.UtcNow:yyyy-MM-dd}"));
            return $"design-{BitConverter.ToString(hash, 0, 8).Replace("-", "").ToLowerInvariant()}";
        }
    }

    private string ComputeContentHash(DesignArtifact artifact)
    {
        var json = JsonSerializer.Serialize(new
        {
            artifact.Tokens,
            artifact.Palette,
            artifact.Typography,
            artifact.Components,
            artifact.Screens,
            artifact.Accessibility
        }, _jsonOptions);

        using (var sha = SHA256.Create())
        {
            var hash = sha.ComputeHash(Encoding.UTF8.GetBytes(json));
            return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
        }
    }
}
