import { For, type Component } from 'solid-js';
import { store } from '../IDEStore';

export const AILog: Component = () => {
  return (
    <div class="flex flex-col h-full">
      <div class="px-3 py-1 border-b border-surface-3 text-[10px] text-muted-foreground uppercase tracking-wider">AI Log</div>
      <div class="flex-1 overflow-y-auto p-2 font-mono text-[10px] space-y-1">
        <For each={store.aiLog}>{(msg) => (
          <div class="text-muted-foreground border-l-2 border-surface-3 pl-2">
            <span class="text-secondary">{msg.type}</span>
            <span class="text-muted-foreground/50 ml-1">{msg.timestamp.toLocaleTimeString()}</span>
            <pre class="mt-0.5 whitespace-pre-wrap text-[9px]">{JSON.stringify(msg, null, 2).slice(0, 200)}</pre>
          </div>
        )}</For>
      </div>
    </div>
  );
};
