import { Show, For, type Component } from 'solid-js';
import { store, closeTab, setStore } from '../IDEStore';

const IconClose: Component = () => (
  <svg class="w-3 h-3" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2">
    <path stroke-linecap="round" stroke-linejoin="round" d="M6 18L18 6M6 6l12 12" />
  </svg>
);

const fileIcon = (name: string) => {
  const ext = name.split('.').pop()?.toLowerCase();
  if (ext === 'ts' || ext === 'tsx') return (
    <svg class="w-3.5 h-3.5 text-primary shrink-0" viewBox="0 0 24 24" fill="currentColor"><path d="M3 3h18v18H3V3zm14.5 11.5c0 .8-.6 1.5-1.5 1.5h-2v-2h2v-1h-3v-2h3c.8 0 1.5.6 1.5 1.5v2zm-6 0c0 .8-.6 1.5-1.5 1.5h-2v-2h2v-1h-3v-2h3c.8 0 1.5.6 1.5 1.5v2z" opacity="0.6"/></svg>
  );
  return (
    <svg class="w-3.5 h-3.5 text-muted-foreground shrink-0" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="1.5">
      <path stroke-linecap="round" stroke-linejoin="round" d="M19.5 14.25v-2.625a3.375 3.375 0 00-3.375-3.375h-1.5A1.125 1.125 0 0113.5 7.125v-1.5a3.375 3.375 0 00-3.375-3.375H8.25m2.25 0H5.625c-.621 0-1.125.504-1.125 1.125v17.25c0 .621.504 1.125 1.125 1.125h12.75c.621 0 1.125-.504 1.125-1.125V11.25a9 9 0 00-9-9z" />
    </svg>
  );
};

export const EditorTabs: Component = () => {
  return (
    <div class="shrink-0 flex items-center border-b border-surface-3 bg-surface overflow-x-auto">
      <Show when={store.diffTabId}>
        <button
          onClick={() => setStore('diffTabId', null)}
          class="shrink-0 px-3 py-2 text-xs text-secondary hover:text-foreground transition-colors border-r border-surface-3"
        >
          ← Exit Diff
        </button>
      </Show>
      <For each={store.openTabs}>{(tab) => (
        <div
          class={[
            'group flex items-center gap-1.5 px-3 py-2 text-xs border-r border-surface-3 cursor-pointer select-none whitespace-nowrap transition-all min-w-0',
            store.activeTabId === tab.id
              ? 'text-foreground bg-surface-2 border-b-2 border-b-secondary -mb-[1px]'
              : 'text-muted-foreground hover:text-foreground hover:bg-surface-2/50',
          ].join(' ')}
          onClick={() => setStore('activeTabId', tab.id)}
        >
          {fileIcon(tab.name)}
          <span class="truncate max-w-[140px]">{tab.name}</span>
          <Show when={tab.isDirty}>
            <span class="text-secondary text-[8px] leading-none">●</span>
          </Show>
          <Show when={tab.isAgentEditing}>
            <span class="text-secondary text-[8px] animate-pulse">🤖</span>
          </Show>
          <button
            onClick={(e) => { e.stopPropagation(); closeTab(tab.id); }}
            class="ml-1 opacity-0 group-hover:opacity-100 hover:text-error transition-opacity p-0.5 rounded"
          >
            <IconClose />
          </button>
        </div>
      )}</For>
      <Show when={store.openTabs.length === 0}>
        <div class="px-3 py-2 text-xs text-muted-foreground">No file open</div>
      </Show>
    </div>
  );
};
