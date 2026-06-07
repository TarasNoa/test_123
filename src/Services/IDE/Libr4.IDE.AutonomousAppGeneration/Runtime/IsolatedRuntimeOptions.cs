namespace Libr4.IDE.Application.AutonomousAppGeneration.Runtime;

public sealed class IsolatedRuntimeOptions
{
    public const string SectionName = "AutonomousAppGeneration:IsolatedRuntime";

    /// <summary>
    /// When true, <see cref="RustBackedIsolatedRuntime"/> wraps process fallback and routes
    /// python/node/shell commands through <c>libr4_sandbox_executor</c> when the cdylib is present.
    /// </summary>
    public bool UseRustSandboxExecutor { get; set; }
}
