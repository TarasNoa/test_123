namespace Libr4.IDE.Application.AutonomousAppGeneration.Context.Fragments;

public sealed record ContextFragment(
    ContextFragmentType Type,
    string Content,
    int Priority,
    IReadOnlyDictionary<string, string>? Provenance = null);
