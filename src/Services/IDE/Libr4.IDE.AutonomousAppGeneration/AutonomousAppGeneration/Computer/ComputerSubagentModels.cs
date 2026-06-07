namespace Libr4.IDE.Application.AutonomousAppGeneration.Computer;

public static class ComputerFlowNames
{
    public const string LoginFlow = "login-flow";
    public const string FormFill = "form-fill";
    public const string VisualDesignCheck = "visual-design-check";

    public static readonly HashSet<string> All = new(StringComparer.OrdinalIgnoreCase)
    {
        LoginFlow,
        FormFill,
        VisualDesignCheck
    };
}

public sealed record ComputerFlowRequest(
    string? Flow,
    string? Url,
    IReadOnlyDictionary<string, string> Parameters,
    string RawTask)
{
    public bool HasDeterministicFlow =>
        !string.IsNullOrWhiteSpace(Flow)
        && ComputerFlowNames.All.Contains(Flow)
        && !string.IsNullOrWhiteSpace(Url);
}

public sealed record ComputerSubagentResult(
    bool Succeeded,
    string Summary,
    string? EvidenceDir,
    bool UsedDeterministicFlow,
    IReadOnlyDictionary<string, object> ExtractedData);

public sealed record ComputerFlowStepResult(
    string Step,
    bool Success,
    string? Detail);

public sealed class ComputerSubagentOptions
{
    public const string SectionName = "AutonomousAppGeneration:Computer";

    public bool Enabled { get; set; } = true;

    public string EvidenceRoot { get; set; } = ".logs/runs";

    public string DefaultUsernameSelector { get; set; } = "input[name='username'], input[type='email'], #username";

    public string DefaultPasswordSelector { get; set; } = "input[type='password'], input[name='password'], #password";

    public string DefaultSubmitSelector { get; set; } = "button[type='submit'], input[type='submit'], #login-submit";

    public string DefaultSuccessSelector { get; set; } = "#dashboard, [data-testid='dashboard'], .dashboard, main h1";

    public string DefaultFormSelector { get; set; } = "form, [data-testid='form']";
}
