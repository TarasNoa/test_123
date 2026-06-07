using System.Text;
using Libr4.IDE.Application.AutonomousAppGeneration.Infrastructure.Algorithms;
using Microsoft.Extensions.Options;

namespace Libr4.IDE.Application.AutonomousAppGeneration.Context.Fragments;

public sealed class ContextFragmentManager : IContextFragmentManager
{
    private readonly ContextFragmentOptions _options;
    private readonly List<ContextFragment> _fragments = new();

    public ContextFragmentManager(IOptions<ContextFragmentOptions> options) =>
        _options = options.Value;

    public IReadOnlyList<ContextFragment> Fragments => _fragments;

    public int TotalChars => _fragments.Sum(f => MarkerLength(f) + f.Content.Length + 2);

    public void Clear() => _fragments.Clear();

    public void Add(ContextFragment fragment)
    {
        if (string.IsNullOrWhiteSpace(fragment.Content))
            return;

        _fragments.Add(fragment with
        {
            Content = Truncate(fragment.Content, _options.GetCap(fragment.Type)),
            Priority = fragment.Priority > 0 ? fragment.Priority : DefaultPriority(fragment.Type)
        });
    }

    public string Assemble()
    {
        if (_fragments.Count == 0)
            return string.Empty;

        return FSharpAlgorithmsBridge.AssembleContextFragments(
            _fragments,
            _options.MaxTotalChars,
            _options.PerTypeCaps);
    }

    internal static string FormatMarker(ContextFragment fragment) =>
        FSharpAlgorithmsBridge.FormatContextFragmentMarker(fragment);

    internal static int DefaultPriority(ContextFragmentType type) =>
        FSharpAlgorithmsBridge.DefaultContextFragmentPriority(type);

    private static int MarkerLength(ContextFragment fragment) =>
        FormatMarker(fragment).Length;

    private static string Truncate(string text, int max) =>
        text.Length <= max ? text : text[..max] + "…";
}
