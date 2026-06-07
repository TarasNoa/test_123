namespace Libr4.IDE.Application.AutonomousAppGeneration.Verify;

public interface IObscuraVerifySmokeRunner
{
    Task<ObscuraVerifySmokeResult> RunBrowserTargetsAsync(
        Guid runId,
        IReadOnlyList<VerifySmokeTarget> targets,
        CancellationToken ct = default);
}
