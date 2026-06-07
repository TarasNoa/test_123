import { createSignal, For, onMount, Show, type Component } from 'solid-js';
import { useNavigate } from '@solidjs/router';
import { fetchSimilarRuns, type FleetSimilarRunHit } from '../services/fleetHistory';

export const SimilarRunsPanel: Component<{ runId: string }> = (props) => {
  const navigate = useNavigate();
  const [hits, setHits] = createSignal<FleetSimilarRunHit[]>([]);
  const [loading, setLoading] = createSignal(true);

  onMount(async () => {
    try {
      const result = await fetchSimilarRuns(props.runId);
      setHits(result.hits);
    } catch {
      setHits([]);
    } finally {
      setLoading(false);
    }
  });

  return (
    <section data-testid="similar-runs-panel" class="rounded border border-surface-3 p-3 space-y-2">
      <h3 class="text-[10px] uppercase tracking-wider text-muted-foreground">Similar runs</h3>
      <Show when={loading()} fallback={
        <Show
          when={hits().length > 0}
          fallback={<p class="text-[10px] text-muted-foreground">No similar runs found.</p>}
        >
          <div class="space-y-1">
            <For each={hits()}>
              {(hit) => (
                <button
                  type="button"
                  class="w-full text-left rounded border border-surface-3 px-2 py-1.5 hover:bg-surface-1"
                  onClick={() => navigate(`/ide/runs/${hit.runId}`)}
                >
                  <div class="flex items-center gap-2 text-[10px]">
                    <span class="font-medium truncate flex-1">{hit.title}</span>
                    <span class="text-muted-foreground">{Math.round(hit.score * 100)}%</span>
                  </div>
                  <p class="text-muted-foreground line-clamp-1">{hit.snippet}</p>
                </button>
              )}
            </For>
          </div>
        </Show>
      }>
        <p class="text-[10px] text-muted-foreground">Loading similar runs…</p>
      </Show>
    </section>
  );
};
