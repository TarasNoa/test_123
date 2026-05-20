import { For, type Component } from 'solid-js';
import { store } from '../IDEStore';

export const Timeline: Component = () => {
  const fmt = (d: Date) => d.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' });
  const duration = (start: Date, end?: Date) => {
    if (!end) return 'running';
    const s = Math.floor((end.getTime() - start.getTime()) / 1000);
    return `${Math.floor(s / 60)}m ${s % 60}s`;
  };

  return (
    <div class="flex flex-col h-full">
      <div class="px-3 py-1 border-b border-surface-3 text-[10px] text-muted-foreground uppercase tracking-wider">Timeline</div>
      <div class="flex-1 overflow-y-auto p-2 space-y-2">
        <For each={store.timelineEvents}>{(e) => (
          <div class="flex items-center gap-2 text-xs">
            <span class="text-muted-foreground w-10 text-right shrink-0">{fmt(e.start)}</span>
            <span class={e.status === 'failed' ? 'text-error' : e.status === 'completed' ? 'text-success' : 'text-secondary'}>
              {e.status === 'failed' ? '❌' : e.status === 'completed' ? '✅' : '●'}
            </span>
            <span class="text-foreground">{e.agentType}</span>
            <span class="text-muted-foreground text-[10px]">{duration(e.start, e.end)}</span>
          </div>
        )}</For>
      </div>
    </div>
  );
};
