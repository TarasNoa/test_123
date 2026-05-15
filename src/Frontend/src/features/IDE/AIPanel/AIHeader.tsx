import { Show, type Component } from 'solid-js';
import { store, setStore } from '../IDEStore';

export const AIHeader: Component = () => {
  const activeCount = () => Object.values(store.activeAgents).filter((a) => a.status === 'running').length;

  return (
    <div class="shrink-0 h-12 flex items-center justify-between px-3 border-b border-surface-3">
      <div class="flex items-center gap-2">
        <span class="text-sm font-semibold">Libr4 AI</span>
        <Show when={activeCount() > 0}>
          <span class="text-[10px] px-1.5 py-0.5 rounded-full bg-primary/10 text-primary animate-pulse">
            ● {activeCount()} agent{activeCount() > 1 ? 's' : ''}
          </span>
        </Show>
        <Show when={activeCount() === 0}>
          <span class="text-[10px] text-muted-foreground">Ready</span>
        </Show>
      </div>
      <div class="flex items-center gap-2">
        <select
          value={store.selectedModel}
          onChange={(e) => setStore('selectedModel', e.currentTarget.value)}
          class="text-[10px] bg-surface-2 border border-surface-3 rounded px-1.5 py-1 text-muted-foreground outline-none focus:border-primary/30"
        >
          <option value="docker-model-runner">Docker Model Runner</option>
          <option value="openrouter">OpenRouter</option>
          <option value="ollama">Local Ollama</option>
        </select>
        <button
          onClick={() => {
            setStore('messages', []);
            setStore('outputLog', []);
          }}
          class="text-[10px] text-muted-foreground hover:text-foreground transition-colors"
        >
          Clear
        </button>
      </div>
    </div>
  );
};
