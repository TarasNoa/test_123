import { createSignal, For, Show, type Component } from 'solid-js';
import { searchSessionMemory, type MemorySearchHit } from '../services/runSession';

export const MemoryPanel: Component<{ runId: string }> = (props) => {
  const [query, setQuery] = createSignal('');
  const [hits, setHits] = createSignal<MemorySearchHit[]>([]);
  const [loading, setLoading] = createSignal(false);
  const [error, setError] = createSignal<string | null>(null);

  const search = async () => {
    const q = query().trim();
    if (!q) return;
    setLoading(true);
    setError(null);
    try {
      const results = await searchSessionMemory(q, 25);
      setHits(results);
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Search failed');
      setHits([]);
    } finally {
      setLoading(false);
    }
  };

  return (
    <div class="flex flex-col h-full min-h-0 p-4 text-xs gap-3">
      <form
        class="flex gap-2"
        onSubmit={(e) => {
          e.preventDefault();
          void search();
        }}
      >
        <input
          type="search"
          class="flex-1 px-2 py-1 rounded border border-surface-3 bg-background text-foreground text-xs"
          placeholder="Search session memory (errors, lessons, rollout)…"
          value={query()}
          onInput={(e) => setQuery(e.currentTarget.value)}
        />
        <button type="submit" class="px-3 py-1 rounded border border-secondary text-secondary text-xs" disabled={loading()}>
          Search
        </button>
      </form>
      <Show when={error()}>
        <p class="text-error text-[10px]">{error()}</p>
      </Show>
      <Show when={loading()}>
        <p class="text-muted-foreground">Searching…</p>
      </Show>
      <ul class="flex-1 overflow-y-auto space-y-2">
        <For each={hits()}>{(hit) => (
          <li class="border border-surface-3 rounded p-2">
            <div class="flex justify-between text-[10px] text-muted-foreground mb-1">
              <span>{hit.source}</span>
              <span>{hit.score.toFixed(2)}</span>
            </div>
            <Show when={hit.toolName}>
              <div class="text-[10px] text-secondary">{hit.toolName} · step {hit.stepNumber ?? '—'}</div>
            </Show>
            <p class="text-[10px] mt-1 whitespace-pre-wrap">{hit.snippet}</p>
          </li>
        )}</For>
      </ul>
      <p class="text-[10px] text-muted-foreground">
        Run {props.runId.slice(0, 8)}… — Hermes + rollout FTS (project-wide index).
      </p>
    </div>
  );
};

export default MemoryPanel;
