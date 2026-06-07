import { createSignal, For, Show, type Component } from 'solid-js';
import { store } from '../IDEStore';

export const BackgroundAgentsWidget: Component = () => {
  const [expanded, setExpanded] = createSignal(false);
  const running = () => store.backgroundFleet?.runningCount ?? 0;
  const queued = () => store.backgroundFleet?.queuedCount ?? 0;
  const activeDelegations = () =>
    store.delegations.filter((d) => d.status === 'running' || d.status === 'queued');

  return (
    <Show when={running() + queued() > 0 || activeDelegations().length > 0}>
      <div class="border border-surface-3 rounded p-2 space-y-2">
        <button
          type="button"
          class="w-full flex items-center justify-between text-left text-xs"
          onClick={() => setExpanded((v) => !v)}
        >
          <span class="font-medium text-foreground">
            {running()} background agent{running() === 1 ? '' : 's'} running
          </span>
          <span class="text-muted-foreground">
            {queued() > 0 ? `${queued()} queued · ` : ''}{expanded() ? '▾' : '▸'}
          </span>
        </button>
        <Show when={expanded()}>
          <div class="space-y-1">
            <For each={activeDelegations()}>{(d) => (
              <div class="text-[11px] border border-surface-3 rounded px-2 py-1">
                <div class="flex items-center gap-2">
                  <span class="text-secondary">{d.status}</span>
                  <span class="text-muted-foreground font-mono">{d.id}</span>
                </div>
                <div class="text-muted-foreground line-clamp-2">{d.task}</div>
              </div>
            )}</For>
          </div>
        </Show>
      </div>
    </Show>
  );
};
