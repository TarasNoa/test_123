using Libr4.IDE.Domain.AgentMemorySystem;
using Libr4.IDE.Domain.AutonomousAppGeneration;

namespace Libr4.IDE.Application.AutonomousAppGeneration.Memory.Cognitive;

public static class CognitiveMemoryLayerMapper
{
    public static MemoryLayer ToLayer(MemoryKind kind) => kind switch
    {
        MemoryKind.Episodic => MemoryLayer.SessionArchive,
        MemoryKind.Procedural => MemoryLayer.TaskSkills,
        MemoryKind.Semantic => MemoryLayer.GlobalFacts,
        MemoryKind.Strategic => MemoryLayer.InsightIndex,
        MemoryKind.Meta => MemoryLayer.MetaRules,
        _ => MemoryLayer.SessionArchive
    };

    public static MemoryKind? ToKind(MemoryLayer layer) => layer switch
    {
        MemoryLayer.SessionArchive => MemoryKind.Episodic,
        MemoryLayer.TaskSkills => MemoryKind.Procedural,
        MemoryLayer.GlobalFacts => MemoryKind.Semantic,
        MemoryLayer.InsightIndex => MemoryKind.Strategic,
        MemoryLayer.MetaRules => MemoryKind.Meta,
        _ => null
    };

    public static MemoryKind[]? ToKinds(MemoryLayer? layer) =>
        layer is null ? null : ToKind(layer.Value) is { } kind ? [kind] : null;
}
