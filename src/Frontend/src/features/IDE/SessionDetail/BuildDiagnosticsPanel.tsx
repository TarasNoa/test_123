import { createSignal, For, onMount, Show, type Component } from 'solid-js';
import { fetchBuildDashboard } from '../services/runSession';

type StackFilterOption = {
  recipeId: string;
  displayName: string;
  gateCount: number;
};

type GateTimelineEntry = {
  sequence: number;
  stage: string;
  category: string;
  tier: string;
  score: number;
  passed: boolean;
  reasons: string[];
};

type BuildDashboard = {
  summary?: {
    totalGates?: number;
    passedGates?: number;
    failedGates?: number;
    passRate?: number;
    detectedStack?: string;
    overallQualityScore?: number;
  };
  verifyRecipe?: {
    recipeId: string;
    displayName: string;
    detectionMethod: string;
    smokeKind: string;
  };
  stackFilters?: StackFilterOption[];
  activeStackFilter?: string;
  timeline?: GateTimelineEntry[];
  recommendations?: string[];
};

function normalizeDashboard(raw: Record<string, unknown>): BuildDashboard {
  const summaryRaw = (raw.summary ?? raw.Summary) as Record<string, unknown> | undefined;
  const verifyRaw = (raw.verifyRecipe ?? raw.VerifyRecipe) as Record<string, unknown> | undefined;
  const filtersRaw = (raw.stackFilters ?? raw.StackFilters) as Record<string, unknown>[] | undefined;
  const timelineRaw = (raw.timeline ?? raw.Timeline) as Record<string, unknown>[] | undefined;

  return {
    summary: summaryRaw
      ? {
          totalGates: Number(summaryRaw.totalGates ?? summaryRaw.TotalGates ?? 0),
          passedGates: Number(summaryRaw.passedGates ?? summaryRaw.PassedGates ?? 0),
          failedGates: Number(summaryRaw.failedGates ?? summaryRaw.FailedGates ?? 0),
          passRate: Number(summaryRaw.passRate ?? summaryRaw.PassRate ?? 0),
          detectedStack: String(summaryRaw.detectedStack ?? summaryRaw.DetectedStack ?? ''),
          overallQualityScore: Number(summaryRaw.overallQualityScore ?? summaryRaw.OverallQualityScore ?? 0),
        }
      : undefined,
    verifyRecipe: verifyRaw
      ? {
          recipeId: String(verifyRaw.recipeId ?? verifyRaw.RecipeId ?? ''),
          displayName: String(verifyRaw.displayName ?? verifyRaw.DisplayName ?? ''),
          detectionMethod: String(verifyRaw.detectionMethod ?? verifyRaw.DetectionMethod ?? ''),
          smokeKind: String(verifyRaw.smokeKind ?? verifyRaw.SmokeKind ?? ''),
        }
      : undefined,
    stackFilters: (filtersRaw ?? []).map((f) => ({
      recipeId: String(f.recipeId ?? f.RecipeId ?? ''),
      displayName: String(f.displayName ?? f.DisplayName ?? ''),
      gateCount: Number(f.gateCount ?? f.GateCount ?? 0),
    })),
    activeStackFilter: String(raw.activeStackFilter ?? raw.ActiveStackFilter ?? 'all'),
    timeline: (timelineRaw ?? []).map((g) => ({
      sequence: Number(g.sequence ?? g.Sequence ?? 0),
      stage: String(g.stage ?? g.Stage ?? ''),
      category: String(g.category ?? g.Category ?? ''),
      tier: String(g.tier ?? g.Tier ?? ''),
      score: Number(g.score ?? g.Score ?? 0),
      passed: Boolean(g.passed ?? g.Passed ?? false),
      reasons: Array.isArray(g.reasons ?? g.Reasons)
        ? (g.reasons ?? g.Reasons).map((r) => String(r))
        : [],
    })),
    recommendations: Array.isArray(raw.recommendations ?? raw.Recommendations)
      ? (raw.recommendations ?? raw.Recommendations).map((r) => String(r))
      : [],
  };
}

export const BuildDiagnosticsPanel: Component<{ runId: string }> = (props) => {
  const [dashboard, setDashboard] = createSignal<BuildDashboard | null>(null);
  const [stackFilter, setStackFilter] = createSignal('all');
  const [loading, setLoading] = createSignal(false);

  const load = async (filter: string) => {
    setLoading(true);
    try {
      const raw = await fetchBuildDashboard(
        props.runId,
        filter === 'all' ? undefined : filter,
      );
      setDashboard(raw ? normalizeDashboard(raw) : null);
      setStackFilter(filter);
    } finally {
      setLoading(false);
    }
  };

  onMount(() => {
    void load('all');
  });

  return (
    <section
      data-testid="build-diagnostics-panel"
      class="rounded border border-surface-3 p-3 space-y-2"
    >
      <div class="flex flex-wrap items-center justify-between gap-2">
        <h3 class="text-[10px] uppercase tracking-wider text-muted-foreground">Build diagnostics</h3>
        <Show when={(dashboard()?.stackFilters?.length ?? 0) > 1}>
          <select
            data-testid="stack-gate-filter"
            class="text-[10px] border border-surface-3 rounded px-2 py-1 bg-surface-1"
            value={stackFilter()}
            disabled={loading()}
            onChange={(e) => {
              void load(e.currentTarget.value);
            }}
          >
            <For each={dashboard()?.stackFilters ?? []}>
              {(opt) => (
                <option value={opt.recipeId}>
                  {opt.displayName} ({opt.gateCount})
                </option>
              )}
            </For>
          </select>
        </Show>
      </div>

      <Show when={dashboard()?.verifyRecipe}>
        {(recipe) => (
          <p class="text-[10px] text-muted-foreground">
            Verify recipe: <span class="text-secondary">{recipe().displayName}</span>
            {' · '}
            {recipe().detectionMethod}
            {' · '}
            {recipe().smokeKind}
          </p>
        )}
      </Show>

      <Show when={dashboard()?.summary}>
        {(s) => (
          <div class="grid grid-cols-3 gap-2 text-[10px]">
            <div>Gates: {s().passedGates}/{s().totalGates}</div>
            <div>Pass rate: {Math.round((s().passRate ?? 0) * 100)}%</div>
            <div>Quality: {s().overallQualityScore}</div>
          </div>
        )}
      </Show>

      <Show when={(dashboard()?.timeline?.length ?? 0) > 0} fallback={
        <p class="text-[10px] text-muted-foreground">No quality gates for this filter.</p>
      }>
        <div class="max-h-40 overflow-y-auto space-y-1">
          <For each={dashboard()?.timeline ?? []}>
            {(gate) => (
              <div
                class={[
                  'text-[10px] px-2 py-1 rounded border',
                  gate.passed ? 'border-emerald-500/30 text-emerald-200' : 'border-red-500/30 text-red-200',
                ].join(' ')}
              >
                <span class="font-medium">{gate.stage}</span>
                <span class="text-muted-foreground ml-2">{gate.category} · {gate.tier} · {gate.score}</span>
              </div>
            )}
          </For>
        </div>
      </Show>
    </section>
  );
};
