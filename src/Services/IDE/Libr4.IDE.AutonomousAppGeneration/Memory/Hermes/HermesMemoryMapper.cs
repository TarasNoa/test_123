using Libr4.IDE.Application.AutonomousAppGeneration.AgentIntegration;
using Libr4.IDE.Domain.AutonomousAppGeneration;

namespace Libr4.IDE.Application.AutonomousAppGeneration.Memory.Hermes;

internal static class HermesMemoryMapper
{
    public static HermesMemoryEntry ToHermesEntry(MemoryRecord record) =>
        new(
            Id: Guid.NewGuid(),
            RunId: record.RunId,
            UserId: null,
            RequestFingerprint: record.RequestFingerprint,
            Kind: record.Kind,
            Stage: record.Stage,
            Key: record.Key,
            Summary: record.Summary,
            PayloadJson: record.PayloadJson,
            Tokens: record.TokenEstimate,
            Score: 0,
            CreatedAtUtc: record.CreatedAtUtc);

    public static MemoryRecord ToMemoryRecord(HermesMemoryEntry entry) =>
        new(
            entry.RunId,
            entry.RequestFingerprint,
            entry.Stage,
            entry.Kind,
            entry.Key,
            entry.Summary,
            entry.PayloadJson,
            entry.Tokens,
            entry.CreatedAtUtc);
}
