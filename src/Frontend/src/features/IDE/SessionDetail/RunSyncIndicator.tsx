import { createSignal, For, onCleanup, onMount, Show, type Component } from 'solid-js';
import { fetchRunSyncConflicts, type RunSyncConflict } from '../services/runHandoff';

export const RunSyncIndicator: Component<{ runId: string; active: boolean }> = (props) => {
  const [conflicts, setConflicts] = createSignal<RunSyncConflict[]>([]);
  const [expanded, setExpanded] = createSignal(false);

  const poll = async () => {
    const result = await fetchRunSyncConflicts(props.runId);
    setConflicts(result.conflicts);
  };

  onMount(() => {
    void poll();
    const handle = setInterval(() => void poll(), 5000);
    onCleanup(() => clearInterval(handle));
  });

  const count = () => conflicts().length;
  const visible = () => props.active || count() > 0;

  return (
    <Show when={visible()}>
      <div class="relative">
        <button
          type="button"
          data-testid="run-sync-indicator"
          class={`text-[10px] uppercase px-2 py-0.5 rounded border ${
            count() > 0
              ? 'border-amber-500/50 text-amber-400 bg-amber-500/10'
              : 'border-secondary/40 text-secondary bg-secondary/10'
          }`}
          onClick={() => setExpanded((v) => !v)}
        >
          {count() > 0 ? `${count()} sync conflict${count() === 1 ? '' : 's'}` : 'Sync active'}
        </button>
        <Show when={expanded() && count() > 0}>
          <div class="absolute top-full left-0 mt-1 z-20 min-w-[16rem] max-w-md border border-surface-3 rounded bg-background shadow-lg p-2 space-y-1">
            <For each={conflicts()}>{(c) => (
              <div class="text-[10px] border border-surface-3 rounded px-2 py-1">
                <div class="font-mono text-foreground truncate">{c.relativePath}</div>
                <div class="text-muted-foreground">
                  {c.winnerSource} won · {c.loserSource} lost
                </div>
              </div>
            )}</For>
          </div>
        </Show>
      </div>
    </Show>
  );
};
