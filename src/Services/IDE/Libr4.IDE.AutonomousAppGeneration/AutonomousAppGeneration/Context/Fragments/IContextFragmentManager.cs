namespace Libr4.IDE.Application.AutonomousAppGeneration.Context.Fragments;

public interface IContextFragmentManager
{
    void Clear();

    void Add(ContextFragment fragment);

    IReadOnlyList<ContextFragment> Fragments { get; }

    int TotalChars { get; }

    string Assemble();
}
