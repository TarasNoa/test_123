import { Show, For, type Component } from 'solid-js';
import { store, setStore } from '../IDEStore';

export const ContextChips: Component = () => {
  const allChips = () => {
    const chips: { label: string; type: 'file' | 'selection' }[] = [];
    if (store.activeTabId) {
      const name = store.activeTabId.split('/').pop() || store.activeTabId;
      chips.push({ label: `📄 ${name}`, type: 'file' });
    }
    if (store.contextSelectedCode) {
      chips.push({ label: `✂️ selected: ${store.contextSelectedCode.split('\n').length} lines`, type: 'selection' });
    }
    store.contextFiles.forEach((f) => chips.push({ label: `📎 ${f.split('/').pop() || f}`, type: 'file' }));
    return chips;
  };

  return (
    <Show when={allChips().length > 0}>
      <div class="shrink-0 flex flex-wrap gap-1 px-3 py-1.5 border-b border-surface-3">
        <For each={allChips()}>{(chip) => (
          <button
            onClick={() => {
              if (chip.type === 'selection') setStore('contextSelectedCode', null);
            }}
            class="text-[10px] px-1.5 py-0.5 rounded-full bg-surface-2 border border-surface-3 text-muted-foreground hover:text-foreground hover:border-secondary/30 transition-colors"
          >
            {chip.label} ✕
          </button>
        )}</For>
      </div>
    </Show>
  );
};
