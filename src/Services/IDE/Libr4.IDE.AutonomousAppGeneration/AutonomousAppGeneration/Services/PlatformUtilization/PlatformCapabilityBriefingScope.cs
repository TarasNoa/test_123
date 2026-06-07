using Libr4.IDE.Domain.AutonomousAppGeneration;

namespace Libr4.IDE.Application.AutonomousAppGeneration.Services.PlatformUtilization;

/// <summary>AsyncLocal scope carrying per-run context for stage-aware scoped briefings.</summary>
public static class PlatformCapabilityBriefingScope
{
    private static readonly AsyncLocal<RunContext?> Context = new();

    public static string? CurrentBriefing => Context.Value?.CachedBriefing;

    public static IReadOnlyList<string> ActivatedFeatures =>
        Context.Value?.ActivatedFeatures ?? Array.Empty<string>();

    public static GenerationPlan? CurrentPlan => Context.Value?.Plan;

    public static string? UserRequest => Context.Value?.UserRequest;

    public static void SetJitOverlay(string overlay) =>
        Context.Value = Context.Value is null
            ? new RunContext { JitOverlay = overlay }
            : Context.Value with { JitOverlay = overlay };

    public static void ClearJitOverlay()
    {
        if (Context.Value is not null)
            Context.Value = Context.Value with { JitOverlay = null };
    }

    public static IDisposable Begin(
        string initialBriefing,
        IReadOnlyList<string>? activatedFeatures = null,
        string? userRequest = null,
        GenerationPlan? plan = null)
    {
        var prior = Context.Value;
        Context.Value = new RunContext
        {
            CachedBriefing = initialBriefing,
            ActivatedFeatures = activatedFeatures ?? Array.Empty<string>(),
            UserRequest = userRequest,
            Plan = plan
        };
        return new ScopeDisposable(prior);
    }

    public static void UpdateBriefing(string briefing) =>
        Context.Value = Context.Value is null
            ? new RunContext { CachedBriefing = briefing }
            : Context.Value with { CachedBriefing = briefing };

    public static void UpdatePlan(GenerationPlan plan) =>
        Context.Value = Context.Value is null
            ? new RunContext { Plan = plan }
            : Context.Value with { Plan = plan };

    public static string AppendToPrompt(
        string prompt,
        PlatformCapabilityBriefingStage stage = PlatformCapabilityBriefingStage.Generation)
    {
        var ctx = Context.Value;
        if (ctx?.BriefingService is not null)
        {
            var briefing = ctx.BriefingService.BuildBriefing(new PlatformCapabilityBriefingRequest(
                stage,
                ctx.Plan,
                ctx.UserRequest));
            if (!string.IsNullOrWhiteSpace(ctx.JitOverlay))
            {
                briefing = briefing + "\n\n[ORCHESTRATOR_JIT]\n" + ctx.JitOverlay + "\n[/ORCHESTRATOR_JIT]";
            }

            return WrapPrompt(prompt, briefing);
        }

        if (!string.IsNullOrWhiteSpace(ctx?.CachedBriefing))
            return WrapPrompt(prompt, ctx.CachedBriefing);

        if (!string.IsNullOrWhiteSpace(ctx?.JitOverlay))
            return WrapPrompt(prompt, ctx.JitOverlay);

        return prompt;
    }

    internal static IDisposable BeginWithService(
        IPlatformCapabilityBriefingService briefingService,
        IReadOnlyList<string>? activatedFeatures,
        string? userRequest)
    {
        var prior = Context.Value;
        Context.Value = new RunContext
        {
            BriefingService = briefingService,
            ActivatedFeatures = activatedFeatures ?? Array.Empty<string>(),
            UserRequest = userRequest
        };
        return new ScopeDisposable(prior);
    }

    private static string WrapPrompt(string prompt, string briefing) =>
        prompt + """

            
            [LIBR4_PLATFORM_CAPABILITIES]
            """ + briefing + """
            
            [/LIBR4_PLATFORM_CAPABILITIES]
            """;

    private sealed record RunContext
    {
        public string? CachedBriefing { get; init; }
        public string? JitOverlay { get; init; }
        public IPlatformCapabilityBriefingService? BriefingService { get; init; }
        public IReadOnlyList<string> ActivatedFeatures { get; init; } = Array.Empty<string>();
        public string? UserRequest { get; init; }
        public GenerationPlan? Plan { get; init; }
    }

    private sealed class ScopeDisposable : IDisposable
    {
        private readonly RunContext? _prior;

        public ScopeDisposable(RunContext? prior) => _prior = prior;

        public void Dispose() => Context.Value = _prior;
    }
}
