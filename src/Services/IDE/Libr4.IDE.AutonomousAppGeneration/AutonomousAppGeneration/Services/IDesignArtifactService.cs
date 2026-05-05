namespace Libr4.IDE.Application.AutonomousAppGeneration.Services;

public interface IDesignArtifactService
{
    Task<DesignArtifact> CreateArtifactAsync(
        string runId,
        DesignTokens tokens,
        DesignPalette palette,
        TypographyScale typography,
        ComponentSpecifications components,
        ScreenMap screens,
        AccessibilityProfile accessibility,
        CancellationToken ct = default);

    Task<DesignArtifact?> GetArtifactAsync(string artifactId, CancellationToken ct = default);

    Task<DesignArtifact?> GetArtifactByRunAsync(string runId, CancellationToken ct = default);

    bool ValidateArtifact(DesignArtifact artifact, out IReadOnlyList<string> errors);

    string SerializeArtifact(DesignArtifact artifact);

    DesignArtifact? DeserializeArtifact(string json);
}

public class DesignArtifact
{
    public string Id { get; set; } = null!;
    public string RunId { get; set; } = null!;
    public DateTime CreatedAtUtc { get; set; }
    public string ContentHash { get; set; } = null!;
    public int Version { get; set; } = 1;

    public DesignTokens Tokens { get; set; } = null!;
    public DesignPalette Palette { get; set; } = null!;
    public TypographyScale Typography { get; set; } = null!;
    public ComponentSpecifications Components { get; set; } = null!;
    public ScreenMap Screens { get; set; } = null!;
    public AccessibilityProfile Accessibility { get; set; } = null!;
}

public class DesignTokens
{
    public ColorTokens Colors { get; set; } = new();
    public SpacingTokens Spacing { get; set; } = new();
    public RadiusTokens Radius { get; set; } = new();
    public ShadowTokens Shadows { get; set; } = new();
    public MotionTokens Motion { get; set; } = new();
}

public class ColorTokens
{
    public string Primary { get; set; } = "#007AFF";
    public string Secondary { get; set; } = "#5AC8FA";
    public string Success { get; set; } = "#34C759";
    public string Warning { get; set; } = "#FF9500";
    public string Error { get; set; } = "#FF3B30";
    public string Neutral50 { get; set; } = "#F9FAFB";
    public string Neutral900 { get; set; } = "#111827";
}

public class SpacingTokens
{
    public int Xs { get; set; } = 4;
    public int Sm { get; set; } = 8;
    public int Md { get; set; } = 16;
    public int Lg { get; set; } = 24;
    public int Xl { get; set; } = 32;
    public int Xxl { get; set; } = 48;
}

public class RadiusTokens
{
    public int Sm { get; set; } = 4;
    public int Md { get; set; } = 8;
    public int Lg { get; set; } = 12;
    public int Full { get; set; } = 9999;
}

public class ShadowTokens
{
    public string Sm { get; set; } = "0 1px 2px rgba(0,0,0,0.05)";
    public string Md { get; set; } = "0 4px 6px rgba(0,0,0,0.1)";
    public string Lg { get; set; } = "0 10px 15px rgba(0,0,0,0.1)";
    public string Xl { get; set; } = "0 20px 25px rgba(0,0,0,0.1)";
}

public class MotionTokens
{
    public int FastMs { get; set; } = 100;
    public int NormalMs { get; set; } = 200;
    public int SlowMs { get; set; } = 300;
    public string EasingFunction { get; set; } = "cubic-bezier(0.4, 0, 0.2, 1)";
}

public class DesignPalette
{
    public string Name { get; set; } = "Default";
    public Dictionary<string, string> SemanticColors { get; set; } = new()
    {
        { "background", "#FFFFFF" },
        { "surface", "#F5F5F5" },
        { "text-primary", "#000000" },
        { "text-secondary", "#666666" },
        { "border", "#E0E0E0" }
    };
    public Dictionary<string, string> BrandColors { get; set; } = new();
}

public class TypographyScale
{
    public FontDefinition Display { get; set; } = new() { Size = 48, Weight = 700, LineHeight = 1.2 };
    public FontDefinition H1 { get; set; } = new() { Size = 36, Weight = 700, LineHeight = 1.3 };
    public FontDefinition H2 { get; set; } = new() { Size = 28, Weight = 600, LineHeight = 1.3 };
    public FontDefinition H3 { get; set; } = new() { Size = 24, Weight = 600, LineHeight = 1.4 };
    public FontDefinition Body { get; set; } = new() { Size = 16, Weight = 400, LineHeight = 1.5 };
    public FontDefinition BodySmall { get; set; } = new() { Size = 14, Weight = 400, LineHeight = 1.5 };
    public FontDefinition Caption { get; set; } = new() { Size = 12, Weight = 400, LineHeight = 1.4 };
    public string FontFamily { get; set; } = "-apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif";
}

public class FontDefinition
{
    public int Size { get; set; }
    public int Weight { get; set; }
    public double LineHeight { get; set; }
}

public class ComponentSpecifications
{
    public ButtonSpec Button { get; set; } = new();
    public InputSpec Input { get; set; } = new();
    public CardSpec Card { get; set; } = new();
    public ModalSpec Modal { get; set; } = new();
    public NavSpec Navigation { get; set; } = new();
    public TableSpec Table { get; set; } = new();
    public FormSpec Form { get; set; } = new();
}

public class ButtonSpec
{
    public string[] Variants { get; set; } = new[] { "primary", "secondary", "outline", "ghost" };
    public string[] Sizes { get; set; } = new[] { "sm", "md", "lg" };
    public string[] States { get; set; } = new[] { "default", "hover", "active", "disabled" };
    public string PaddingX { get; set; } = "16px";
    public string PaddingY { get; set; } = "8px";
    public string BorderRadius { get; set; } = "8px";
}

public class InputSpec
{
    public string[] Types { get; set; } = new[] { "text", "email", "password", "number", "date", "textarea" };
    public string[] Sizes { get; set; } = new[] { "sm", "md", "lg" };
    public string[] States { get; set; } = new[] { "default", "focus", "error", "disabled" };
    public string BorderRadius { get; set; } = "8px";
    public string BorderColor { get; set; } = "#E0E0E0";
}

public class CardSpec
{
    public string BorderRadius { get; set; } = "12px";
    public string Padding { get; set; } = "16px";
    public string Shadow { get; set; } = "0 1px 3px rgba(0,0,0,0.1)";
    public string[] Layouts { get; set; } = new[] { "vertical", "horizontal", "grid" };
}

public class ModalSpec
{
    public string Backdrop { get; set; } = "rgba(0,0,0,0.5)";
    public string BorderRadius { get; set; } = "12px";
    public string MaxWidth { get; set; } = "600px";
    public string[] AnimationTypes { get; set; } = new[] { "fade", "slide", "zoom" };
}

public class NavSpec
{
    public string[] Patterns { get; set; } = new[] { "horizontal-top", "vertical-sidebar", "bottom-mobile" };
    public string Height { get; set; } = "64px";
    public string[] ItemStates { get; set; } = new[] { "default", "hover", "active" };
}

public class TableSpec
{
    public string HeaderBackground { get; set; } = "#F5F5F5";
    public string RowHeight { get; set; } = "48px";
    public string[] Features { get; set; } = new[] { "sorting", "filtering", "pagination", "selection" };
}

public class FormSpec
{
    public string LabelPosition { get; set; } = "top";
    public string FieldSpacing { get; set; } = "16px";
    public string[] ValidationStates { get; set; } = new[] { "default", "error", "success", "warning" };
    public bool RequiredIndicator { get; set; } = true;
}

public class ScreenMap
{
    public string[] KeyPages { get; set; } = Array.Empty<string>();
    public Dictionary<string, ScreenDefinition> Screens { get; set; } = new();
}

public class ScreenDefinition
{
    public string Name { get; set; } = null!;
    public string Purpose { get; set; } = null!;
    public string[] LayoutRegions { get; set; } = new[] { "header", "sidebar", "main", "footer" };
    public Dictionary<string, string> InteractionStates { get; set; } = new();
}

public class AccessibilityProfile
{
    public string ContrastLevel { get; set; } = "AA";
    public bool KeyboardNavigationSupported { get; set; } = true;
    public bool ScreenReaderOptimized { get; set; } = true;
    public string[] FocusIndicatorStyles { get; set; } = new[] { "outline", "underline" };
    public string[] AriaLandmarks { get; set; } = new[] { "navigation", "main", "contentinfo" };
    public string[] SkipLinks { get; set; } = new[] { "skip-to-main", "skip-to-nav" };
    public Dictionary<string, string> ColorBlindSafePatterns { get; set; } = new();
}
