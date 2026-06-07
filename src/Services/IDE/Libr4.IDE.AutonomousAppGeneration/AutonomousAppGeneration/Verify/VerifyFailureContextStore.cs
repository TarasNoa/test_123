using System.Collections.Concurrent;

namespace Libr4.IDE.Application.AutonomousAppGeneration.Verify;

public sealed class VerifyFailureContextStore : IVerifyFailureContextStore
{
    private readonly ConcurrentDictionary<Guid, VerifyFailureEvidence> _byRun = new();

    public void Set(Guid runId, VerifyFailureEvidence evidence) =>
        _byRun[runId] = evidence;

    public bool TryGet(Guid runId, out VerifyFailureEvidence? evidence)
    {
        if (_byRun.TryGetValue(runId, out var stored))
        {
            evidence = stored;
            return true;
        }

        evidence = null;
        return false;
    }
}
