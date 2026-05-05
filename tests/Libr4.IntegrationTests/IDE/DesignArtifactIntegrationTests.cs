using Libr4.IDE.Application.AutonomousAppGeneration.Handlers;
using Libr4.IDE.Application.AutonomousAppGeneration.Queries;
using Libr4.IDE.Application.AutonomousAppGeneration.Services;
using Xunit;

namespace Libr4.IntegrationTests.IDE;

public class DesignArtifactIntegrationTests
{
    private readonly IDesignArtifactService _designArtifactService = new DesignArtifactService();
    private readonly IDesignArtifactGenerationBindingService _bindingService = new DesignArtifactGenerationBindingService();

    [Fact]
    public async Task Part1_CreateAndValidateArtifact_ShouldSucceed()
    {
        var runId = Guid.NewGuid().ToString();
        var artifact = await _designArtifactService.CreateArtifactAsync(
            runId,
            CreateDefaultTokens(),
            CreateDefaultPalette(),
            CreateDefaultTypography(),
            CreateDefaultComponents(),
            CreateDefaultScreens(),
            CreateDefaultAccessibility());

        var isValid = _designArtifactService.ValidateArtifact(artifact, out var errors);

        Assert.True(isValid);
        Assert.Empty(errors);
        Assert.NotEmpty(artifact.Id);
        Assert.NotEmpty(artifact.ContentHash);
    }

    [Fact]
    public async Task Part2_ExportArtifact_ShouldPersistToFile()
    {
        var runId = Guid.NewGuid().ToString();
        var artifact = await _designArtifactService.CreateArtifactAsync(
            runId,
            CreateDefaultTokens(),
            CreateDefaultPalette(),
            CreateDefaultTypography(),
            CreateDefaultComponents(),
            CreateDefaultScreens(),
            CreateDefaultAccessibility());

        var tempPath = Path.Combine(Path.GetTempPath(), $"design-artifact-{Guid.NewGuid()}.json");
        var handler = new ExportDesignArtifactQueryHandler(
            _designArtifactService,
            new Microsoft.Extensions.Logging.Abstractions.NullLogger<ExportDesignArtifactQueryHandler>());

        var result = await handler.Handle(
            new ExportDesignArtifactQuery(artifact.Id, tempPath),
            CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(artifact.Id, result.ArtifactId);
        Assert.True(File.Exists(result.ExportPath));
        Assert.True(result.PayloadBytes > 0);

        File.Delete(tempPath);
    }

    [Fact]
    public async Task Part3_BindArtifactToPrompt_ShouldIncludeAllSections()
    {
        var runId = Guid.NewGuid().ToString();
        var artifact = await _designArtifactService.CreateArtifactAsync(
            runId,
            CreateDefaultTokens(),
            CreateDefaultPalette(),
            CreateDefaultTypography(),
            CreateDefaultComponents(),
            CreateDefaultScreens(),
            CreateDefaultAccessibility());

        var basePrompt = "Generate a frontend application with the following design artifact.";
        var boundPrompt = await _bindingService.BindArtifactToGenerationPromptAsync(
            basePrompt,
            artifact,
            CancellationToken.None);

        Assert.Contains(artifact.Id, boundPrompt);
        Assert.Contains("Design Tokens", boundPrompt);
        Assert.Contains("Color Palette", boundPrompt);
        Assert.Contains("Typography", boundPrompt);
        Assert.Contains("Component Specifications", boundPrompt);
        Assert.Contains("Key Screens", boundPrompt);
        Assert.Contains("Accessibility Requirements", boundPrompt);
        Assert.Contains(artifact.Tokens.Colors.Primary, boundPrompt);
        Assert.Contains(artifact.Typography.FontFamily, boundPrompt);
    }

    [Fact]
    public async Task Part3_ValidateGenerationPromptReferencesArtifact_ShouldDetectMissingReferences()
    {
        var runId = Guid.NewGuid().ToString();
        var artifact = await _designArtifactService.CreateArtifactAsync(
            runId,
            CreateDefaultTokens(),
            CreateDefaultPalette(),
            CreateDefaultTypography(),
            CreateDefaultComponents(),
            CreateDefaultScreens(),
            CreateDefaultAccessibility());

        var basePrompt = "Generate a frontend application with the following design artifact.";
        var boundPrompt = await _bindingService.BindArtifactToGenerationPromptAsync(
            basePrompt,
            artifact,
            CancellationToken.None);

        var isValid = _bindingService.ValidateGenerationPromptReferencesArtifact(
            boundPrompt,
            artifact.Id,
            out var missingReferences);

        Assert.True(isValid);
        Assert.Empty(missingReferences);
    }

    [Fact]
    public async Task Part3_ValidateGenerationPromptReferencesArtifact_ShouldFailWhenMissingArtifactId()
    {
        var runId = Guid.NewGuid().ToString();
        var artifact = await _designArtifactService.CreateArtifactAsync(
            runId,
            CreateDefaultTokens(),
            CreateDefaultPalette(),
            CreateDefaultTypography(),
            CreateDefaultComponents(),
            CreateDefaultScreens(),
            CreateDefaultAccessibility());

        var invalidPrompt = "Generate a frontend application without referencing the artifact.";

        var isValid = _bindingService.ValidateGenerationPromptReferencesArtifact(
            invalidPrompt,
            artifact.Id,
            out var missingReferences);

        Assert.False(isValid);
        Assert.NotEmpty(missingReferences);
        Assert.Contains(missingReferences, m => m.Contains("Artifact ID"));
    }

    [Fact]
    public async Task Part3_ValidateGenerationPromptReferencesArtifact_ShouldFailWhenMissingMultipleSections()
    {
        var runId = Guid.NewGuid().ToString();
        var artifact = await _designArtifactService.CreateArtifactAsync(
            runId,
            CreateDefaultTokens(),
            CreateDefaultPalette(),
            CreateDefaultTypography(),
            CreateDefaultComponents(),
            CreateDefaultScreens(),
            CreateDefaultAccessibility());

        var invalidPrompt = $"Generate a frontend application with artifact {artifact.Id}.";

        var isValid = _bindingService.ValidateGenerationPromptReferencesArtifact(
            invalidPrompt,
            artifact.Id,
            out var missingReferences);

        Assert.False(isValid);
        Assert.NotEmpty(missingReferences);
        Assert.True(missingReferences.Count >= 5);
    }

    [Fact]
    public void Part3_ExtractArtifactIdFromResponse_ShouldFindId()
    {
        var artifactId = "design-abc123def456";
        var response = $"Generated code with artifact_id: '{artifactId}' and components...";

        var extracted = _bindingService.ExtractArtifactIdFromGenerationResponse(response);

        Assert.Equal(artifactId, extracted);
    }

    [Fact]
    public async Task EndToEnd_CreateValidateExportAndBind_ShouldSucceed()
    {
        var runId = Guid.NewGuid().ToString();

        var artifact = await _designArtifactService.CreateArtifactAsync(
            runId,
            CreateDefaultTokens(),
            CreateDefaultPalette(),
            CreateDefaultTypography(),
            CreateDefaultComponents(),
            CreateDefaultScreens(),
            CreateDefaultAccessibility());

        var isValid = _designArtifactService.ValidateArtifact(artifact, out var errors);
        Assert.True(isValid);

        var tempPath = Path.Combine(Path.GetTempPath(), $"design-artifact-{Guid.NewGuid()}.json");
        var handler = new ExportDesignArtifactQueryHandler(
            _designArtifactService,
            new Microsoft.Extensions.Logging.Abstractions.NullLogger<ExportDesignArtifactQueryHandler>());

        var exportResult = await handler.Handle(
            new ExportDesignArtifactQuery(artifact.Id, tempPath),
            CancellationToken.None);

        Assert.True(File.Exists(exportResult.ExportPath));

        var basePrompt = "Generate a modern dashboard application.";
        var boundPrompt = await _bindingService.BindArtifactToGenerationPromptAsync(
            basePrompt,
            artifact,
            CancellationToken.None);

        var promptValid = _bindingService.ValidateGenerationPromptReferencesArtifact(
            boundPrompt,
            artifact.Id,
            out var promptErrors);

        Assert.True(promptValid);
        Assert.Empty(promptErrors);

        File.Delete(tempPath);
    }

    private DesignTokens CreateDefaultTokens()
    {
        return new DesignTokens
        {
            Colors = new ColorTokens(),
            Spacing = new SpacingTokens(),
            Radius = new RadiusTokens(),
            Shadows = new ShadowTokens(),
            Motion = new MotionTokens()
        };
    }

    private DesignPalette CreateDefaultPalette()
    {
        return new DesignPalette
        {
            Name = "Default",
            SemanticColors = new Dictionary<string, string>
            {
                { "background", "#FFFFFF" },
                { "surface", "#F5F5F5" },
                { "text-primary", "#000000" }
            },
            BrandColors = new Dictionary<string, string>()
        };
    }

    private TypographyScale CreateDefaultTypography()
    {
        return new TypographyScale
        {
            Display = new FontDefinition { Size = 48, Weight = 700, LineHeight = 1.2 },
            H1 = new FontDefinition { Size = 36, Weight = 700, LineHeight = 1.3 },
            Body = new FontDefinition { Size = 16, Weight = 400, LineHeight = 1.5 },
            FontFamily = "system-ui, sans-serif"
        };
    }

    private ComponentSpecifications CreateDefaultComponents()
    {
        return new ComponentSpecifications
        {
            Button = new ButtonSpec { Variants = new[] { "primary", "secondary" } },
            Input = new InputSpec { Types = new[] { "text", "email" } },
            Card = new CardSpec(),
            Modal = new ModalSpec(),
            Navigation = new NavSpec(),
            Table = new TableSpec(),
            Form = new FormSpec()
        };
    }

    private ScreenMap CreateDefaultScreens()
    {
        return new ScreenMap
        {
            KeyPages = new[] { "dashboard", "tasks", "settings" },
            Screens = new Dictionary<string, ScreenDefinition>
            {
                {
                    "dashboard",
                    new ScreenDefinition
                    {
                        Name = "Dashboard",
                        Purpose = "Main overview",
                        LayoutRegions = new[] { "header", "sidebar", "main" }
                    }
                }
            }
        };
    }

    private AccessibilityProfile CreateDefaultAccessibility()
    {
        return new AccessibilityProfile
        {
            ContrastLevel = "AA",
            KeyboardNavigationSupported = true,
            ScreenReaderOptimized = true,
            FocusIndicatorStyles = new[] { "outline" },
            AriaLandmarks = new[] { "navigation", "main", "contentinfo" },
            SkipLinks = new[] { "skip-to-main" },
            ColorBlindSafePatterns = new Dictionary<string, string>()
        };
    }
}
