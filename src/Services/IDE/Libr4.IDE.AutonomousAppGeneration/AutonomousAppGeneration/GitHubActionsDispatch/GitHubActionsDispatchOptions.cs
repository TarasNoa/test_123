namespace Libr4.IDE.Application.AutonomousAppGeneration.GitHubActionsDispatch;

public sealed class GitHubActionsDispatchOptions
{
    public const string SectionName = "AutonomousAppGeneration:GitHubShip";

    public bool Enabled { get; set; }

    public string Owner { get; set; } = string.Empty;

    public string Repository { get; set; } = string.Empty;

    public string PersonalAccessToken { get; set; } = string.Empty;

    public string BaseBranch { get; set; } = "main";

    public string BranchPrefix { get; set; } = "libr4/autogen-";

    public bool RequireVerifyPass { get; set; } = true;

    public bool DispatchWorkflow { get; set; } = true;

    public string WorkflowFile { get; set; } = "libr4-autogen-ship.yml";

    public bool CreatePullRequest { get; set; } = true;

    public bool RequireShipSuccess { get; set; }

    public int MaxFilesPerPullRequest { get; set; } = 400;

    public int MaxFileBytes { get; set; } = 512 * 1024;

    /// <summary>Post Obscura evidence manifest as a PR issue comment after create.</summary>
    public bool AttachObscuraManifestComment { get; set; } = true;

    /// <summary>Optional public IDE API base URL for artifact links in PR comments (e.g. https://ide.example.com).</summary>
    public string? PublicApiBaseUrl { get; set; }

    public bool HasCredentials =>
        !string.IsNullOrWhiteSpace(Owner)
        && !string.IsNullOrWhiteSpace(Repository)
        && !string.IsNullOrWhiteSpace(PersonalAccessToken);
}
