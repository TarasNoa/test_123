using Libr4.IDE.Application.AutonomousAppGeneration.Services;
using Xunit;

namespace Libr4.IntegrationTests.IDE;

public class DesignArtifactServiceTests
{
    private readonly IDesignArtifactService _service = new DesignArtifactService();

    [Fact]
    public async Task CreateArtifactAsync_ShouldCreateValidArtifact()
    {
        var runId = Guid.NewGuid().ToString();
        var tokens = CreateDefaultTokens();
        var palette = CreateDefaultPalette();
        var typography = CreateDefaultTypography();
        var components = CreateDefaultComponents();
        var screens = CreateDefaultScreens();
        var accessibility = CreateDefaultAccessibility();

        var artifact = await _service.CreateArtifactAsync(
            runId, tokens, palette, typography, components, screens, accessibility);

        Assert.NotNull(artifact);
        Assert.NotEmpty(artifact.Id);
        Assert.Equal(runId, artifact.RunId);
        Assert.NotEmpty(artifact.ContentHash);
        Assert.Equal(1, artifact.Version);
    }

    [Fact]
    public async Task CreateArtifactAsync_ShouldGenerateDeterministicId()
    {
        var runId = Guid.NewGuid().ToString();
        var tokens = CreateDefaultTokens();
        var palette = CreateDefaultPalette();
        var typography = CreateDefaultTypography();
        var components = CreateDefaultComponents();
        var screens = CreateDefaultScreens();
        var accessibility = CreateDefaultAccessibility();

        var artifact1 = await _service.CreateArtifactAsync(
            runId, tokens, palette, typography, components, screens, accessibility);

        var artifact2 = await _service.CreateArtifactAsync(
            runId, tokens, palette, typography, components, screens, accessibility);

        Assert.Equal(artifact1.Id, artifact2.Id);
    }

    [Fact]
    public async Task ValidateArtifact_ShouldPassValidArtifact()
    {
        var runId = Guid.NewGuid().ToString();
        var artifact = await _service.CreateArtifactAsync(
            runId,
            CreateDefaultTokens(),
            CreateDefaultPalette(),
            CreateDefaultTypography(),
            CreateDefaultComponents(),
            CreateDefaultScreens(),
            CreateDefaultAccessibility());

        var isValid = _service.ValidateArtifact(artifact, out var errors);

        Assert.True(isValid);
        Assert.Empty(errors);
    }

    [Fact]
    public void ValidateArtifact_ShouldFailMissingTokens()
    {
        var artifact = new DesignArtifact
        {
            Id = "test-id",
            RunId = "test-run",
            CreatedAtUtc = DateTime.UtcNow,
            ContentHash = "test-hash",
            Tokens = null!,
            Palette = CreateDefaultPalette(),
            Typography = CreateDefaultTypography(),
            Components = CreateDefaultComponents(),
            Screens = CreateDefaultScreens(),
            Accessibility = CreateDefaultAccessibility()
        };

        var isValid = _service.ValidateArtifact(artifact, out var errors);

        Assert.False(isValid);
        Assert.Contains(errors, e => e.Contains("tokens"));
    }

    [Fact]
    public void ValidateArtifact_ShouldFailMissingPalette()
    {
        var artifact = new DesignArtifact
        {
            Id = "test-id",
            RunId = "test-run",
            CreatedAtUtc = DateTime.UtcNow,
            ContentHash = "test-hash",
            Tokens = CreateDefaultTokens(),
            Palette = null!,
            Typography = CreateDefaultTypography(),
            Components = CreateDefaultComponents(),
            Screens = CreateDefaultScreens(),
            Accessibility = CreateDefaultAccessibility()
        };

        var isValid = _service.ValidateArtifact(artifact, out var errors);

        Assert.False(isValid);
        Assert.Contains(errors, e => e.Contains("palette"));
    }

    [Fact]
    public void ValidateArtifact_ShouldFailMissingAccessibility()
    {
        var artifact = new DesignArtifact
        {
            Id = "test-id",
            RunId = "test-run",
            CreatedAtUtc = DateTime.UtcNow,
            ContentHash = "test-hash",
            Tokens = CreateDefaultTokens(),
            Palette = CreateDefaultPalette(),
            Typography = CreateDefaultTypography(),
            Components = CreateDefaultComponents(),
            Screens = CreateDefaultScreens(),
            Accessibility = null!
        };

        var isValid = _service.ValidateArtifact(artifact, out var errors);

        Assert.False(isValid);
        Assert.Contains(errors, e => e.Contains("Accessibility"));
    }

    [Fact]
    public async Task GetArtifactAsync_ShouldRetrieveByArtifactId()
    {
        var runId = Guid.NewGuid().ToString();
        var created = await _service.CreateArtifactAsync(
            runId,
            CreateDefaultTokens(),
            CreateDefaultPalette(),
            CreateDefaultTypography(),
            CreateDefaultComponents(),
            CreateDefaultScreens(),
            CreateDefaultAccessibility());

        var retrieved = await _service.GetArtifactAsync(created.Id);

        Assert.NotNull(retrieved);
        Assert.Equal(created.Id, retrieved.Id);
        Assert.Equal(runId, retrieved.RunId);
    }

    [Fact]
    public async Task GetArtifactByRunAsync_ShouldRetrieveByRunId()
    {
        var runId = Guid.NewGuid().ToString();
        var created = await _service.CreateArtifactAsync(
            runId,
            CreateDefaultTokens(),
            CreateDefaultPalette(),
            CreateDefaultTypography(),
            CreateDefaultComponents(),
            CreateDefaultScreens(),
            CreateDefaultAccessibility());

        var retrieved = await _service.GetArtifactByRunAsync(runId);

        Assert.NotNull(retrieved);
        Assert.Equal(created.Id, retrieved.Id);
        Assert.Equal(runId, retrieved.RunId);
    }

    [Fact]
    public async Task SerializeArtifact_ShouldProduceValidJson()
    {
        var runId = Guid.NewGuid().ToString();
        var artifact = await _service.CreateArtifactAsync(
            runId,
            CreateDefaultTokens(),
            CreateDefaultPalette(),
            CreateDefaultTypography(),
            CreateDefaultComponents(),
            CreateDefaultScreens(),
            CreateDefaultAccessibility());

        var json = _service.SerializeArtifact(artifact);

        Assert.NotEmpty(json);
        Assert.Contains("\"id\"", json);
        Assert.Contains("\"runId\"", json);
        Assert.Contains("\"tokens\"", json);
        Assert.Contains("\"palette\"", json);
    }

    [Fact]
    public async Task DeserializeArtifact_ShouldRestoreFromJson()
    {
        var runId = Guid.NewGuid().ToString();
        var original = await _service.CreateArtifactAsync(
            runId,
            CreateDefaultTokens(),
            CreateDefaultPalette(),
            CreateDefaultTypography(),
            CreateDefaultComponents(),
            CreateDefaultScreens(),
            CreateDefaultAccessibility());

        var json = _service.SerializeArtifact(original);
        var restored = _service.DeserializeArtifact(json);

        Assert.NotNull(restored);
        Assert.Equal(original.Id, restored.Id);
        Assert.Equal(original.RunId, restored.RunId);
        Assert.Equal(original.ContentHash, restored.ContentHash);
    }

    [Fact]
    public void ValidateArtifact_ShouldDetectMissingComponentVariants()
    {
        var artifact = new DesignArtifact
        {
            Id = "test-id",
            RunId = "test-run",
            CreatedAtUtc = DateTime.UtcNow,
            ContentHash = "test-hash",
            Tokens = CreateDefaultTokens(),
            Palette = CreateDefaultPalette(),
            Typography = CreateDefaultTypography(),
            Components = new ComponentSpecifications
            {
                Button = new ButtonSpec { Variants = Array.Empty<string>() },
                Input = CreateDefaultComponents().Input,
                Card = CreateDefaultComponents().Card,
                Modal = CreateDefaultComponents().Modal,
                Navigation = CreateDefaultComponents().Navigation,
                Table = CreateDefaultComponents().Table,
                Form = CreateDefaultComponents().Form
            },
            Screens = CreateDefaultScreens(),
            Accessibility = CreateDefaultAccessibility()
        };

        var isValid = _service.ValidateArtifact(artifact, out var errors);

        Assert.False(isValid);
        Assert.Contains(errors, e => e.Contains("Button"));
    }

    [Fact]
    public void ValidateArtifact_ShouldDetectMissingKeyPages()
    {
        var artifact = new DesignArtifact
        {
            Id = "test-id",
            RunId = "test-run",
            CreatedAtUtc = DateTime.UtcNow,
            ContentHash = "test-hash",
            Tokens = CreateDefaultTokens(),
            Palette = CreateDefaultPalette(),
            Typography = CreateDefaultTypography(),
            Components = CreateDefaultComponents(),
            Screens = new ScreenMap
            {
                KeyPages = Array.Empty<string>(),
                Screens = new Dictionary<string, ScreenDefinition>()
            },
            Accessibility = CreateDefaultAccessibility()
        };

        var isValid = _service.ValidateArtifact(artifact, out var errors);

        Assert.False(isValid);
        Assert.Contains(errors, e => e.Contains("key page"));
    }

    [Fact]
    public void ValidateArtifact_ShouldDetectMissingAccessibilityLandmarks()
    {
        var artifact = new DesignArtifact
        {
            Id = "test-id",
            RunId = "test-run",
            CreatedAtUtc = DateTime.UtcNow,
            ContentHash = "test-hash",
            Tokens = CreateDefaultTokens(),
            Palette = CreateDefaultPalette(),
            Typography = CreateDefaultTypography(),
            Components = CreateDefaultComponents(),
            Screens = CreateDefaultScreens(),
            Accessibility = new AccessibilityProfile
            {
                ContrastLevel = "AA",
                KeyboardNavigationSupported = true,
                ScreenReaderOptimized = true,
                FocusIndicatorStyles = new[] { "outline" },
                AriaLandmarks = Array.Empty<string>(),
                SkipLinks = new[] { "skip-to-main" },
                ColorBlindSafePatterns = new Dictionary<string, string>()
            }
        };

        var isValid = _service.ValidateArtifact(artifact, out var errors);

        Assert.False(isValid);
        Assert.Contains(errors, e => e.Contains("ARIA landmarks"));
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
