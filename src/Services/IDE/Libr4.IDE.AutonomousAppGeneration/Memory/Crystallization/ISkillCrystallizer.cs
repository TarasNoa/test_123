using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Playbook;

namespace Libr4.IDE.Application.AutonomousAppGeneration.Memory.Crystallization;

public sealed record CrystallizedSkillResult(
    string SkillId,
    string FilePath,
    bool RequiresApproval,
    bool Created);

public interface ISkillCrystallizer
{
    Task<CrystallizedSkillResult?> TryCrystallizeAsync(RepairPlaybookEntry entry, CancellationToken ct = default);

    Task<bool> ApprovePendingAsync(string errorSignature, CancellationToken ct = default);
}
