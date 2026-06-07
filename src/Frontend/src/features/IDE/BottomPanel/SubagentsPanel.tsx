import { For, Show, type Component } from 'solid-js';
import { store } from '../IDEStore';

const statusColor = (status: string) => {
  switch (status.toLowerCase()) {
    case 'running':
      return 'text-secondary';
    case 'completed':
      return 'text-success';
    case 'failed':
      return 'text-error';
    default:
      return 'text-muted-foreground';
  }
};

export const SubagentsPanel: Component = () => {
  return (
    <div class="flex flex-col h-full">
      <div class="px-3 py-1 border-b border-surface-3 text-[10px] text-muted-foreground uppercase tracking-wider">
        Subagents
        <Show when={store.activeGenerationRunId}>
          <span class="ml-2 normal-case">run {store.activeGenerationRunId?.slice(0, 8)}</span>
        </Show>
      </div>
      <div class="flex-1 overflow-y-auto p-2 space-y-2">
        <Show when={store.delegations.length > 0}>
          <div class="text-[10px] text-muted-foreground uppercase tracking-wider px-1">Background delegations</div>
          <For each={store.delegations}>{(d) => (
            <div class="border border-surface-3 rounded p-2 text-xs space-y-1">
              <div class="flex items-center gap-2">
                <span class={statusColor(d.status)}>{d.status}</span>
                <span class="text-muted-foreground text-[10px] ml-auto font-mono">{d.id}</span>
              </div>
              <div class="text-muted-foreground line-clamp-2">{d.task}</div>
              <Show when={d.outputPreview}>
                <pre class="text-[10px] bg-surface-2 rounded p-1 overflow-x-auto whitespace-pre-wrap">{d.outputPreview}</pre>
              </Show>
              <Show when={d.error}>
                <div class="text-error text-[10px]">{d.error}</div>
              </Show>
            </div>
          )}</For>
        </Show>
        <Show
          when={store.subagents.length > 0}
          fallback={
            store.delegations.length > 0
              ? null
              : <div class="text-xs text-muted-foreground p-2">Нет активных subagents</div>
          }
        >
          <For each={store.subagents}>{(s) => (
            <div class="border border-surface-3 rounded p-2 text-xs space-y-1">
              <div class="flex items-center gap-2">
                <span class={statusColor(s.status)}>{s.status}</span>
                <span class="font-medium text-foreground">{s.name}</span>
                <span class="text-muted-foreground text-[10px] ml-auto">{s.id}</span>
              </div>
              <div class="text-muted-foreground line-clamp-2">{s.task}</div>
              <Show when={s.outputPreview}>
                <pre class="text-[10px] bg-surface-2 rounded p-1 overflow-x-auto whitespace-pre-wrap">{s.outputPreview}</pre>
              </Show>
              <Show when={s.error}>
                <div class="text-error text-[10px]">{s.error}</div>
              </Show>
            </div>
          )}</For>
        </Show>
      </div>
    </div>
  );
};
