import { For, type Component } from 'solid-js';
import { store, setStore } from '../IDEStore';

export const OutputPanel: Component = () => {
  const levelColor = (level: string) => {
    switch (level) {
      case 'error': return 'text-error';
      case 'warn': return 'text-warning';
      case 'build': return 'text-secondary';
      case 'agent': return 'text-primary';
      default: return 'text-muted-foreground';
    }
  };

  return (
    <div class="flex flex-col h-full">
      <div class="flex items-center justify-between px-3 py-1 border-b border-surface-3">
        <span class="text-[10px] text-muted-foreground uppercase tracking-wider">Output</span>
        <button onClick={() => setStore('outputLog', [])} class="text-[10px] text-muted-foreground hover:text-foreground">Clear</button>
      </div>
      <div class="flex-1 overflow-y-auto p-2 font-mono text-[11px] space-y-0.5">
        <For each={store.outputLog}>{(entry) => (
          <div class={levelColor(entry.level)}>
            <span class="text-muted-foreground/50">{entry.timestamp.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit', second: '2-digit' })}</span>{' '}
            {entry.text}
          </div>
        )}</For>
      </div>
    </div>
  );
};
