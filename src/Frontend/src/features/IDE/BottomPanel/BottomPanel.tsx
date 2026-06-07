import { Show, For, type Component } from 'solid-js';
import { store, setStore } from '../IDEStore';
import { Terminal } from './Terminal';
import { OutputPanel } from './OutputPanel';
import { ProblemsPanel } from './ProblemsPanel';
import { Timeline } from './Timeline';
import { AILog } from './AILog';
import { SubagentsPanel } from './SubagentsPanel';
import { FlowPanel } from './FlowPanel';

const tabs = [
  { key: 'terminal' as const, label: 'Terminal' },
  { key: 'output' as const, label: 'Output' },
  { key: 'problems' as const, label: 'Problems' },
  { key: 'timeline' as const, label: 'Timeline' },
  { key: 'subagents' as const, label: 'Subagents' },
  { key: 'flow' as const, label: 'Flow' },
  { key: 'ai-log' as const, label: 'AI Log' },
];

export const BottomPanel: Component = () => {
  return (
    <Show when={store.bottomPanelOpen}>
      <div class="shrink-0 flex flex-col border-t border-surface-3 bg-surface overflow-hidden" style={{ height: `${store.bottomPanelHeight}px` }}>
        <div class="shrink-0 flex items-center gap-1 px-2 border-b border-surface-3 overflow-x-auto">
          <For each={tabs}>{(tab) => (
            <button
              onClick={() => setStore('bottomPanelTab', tab.key)}
              class={[
                'px-3 py-1.5 text-[11px] font-medium transition-colors rounded-t',
                store.bottomPanelTab === tab.key
                  ? 'text-foreground bg-surface-2 border-t border-x border-surface-3 -mb-[1px]'
                  : 'text-muted-foreground hover:text-foreground',
              ].join(' ')}
            >
              {tab.label}
            </button>
          )}</For>
          <div class="ml-auto">
            <button
              onClick={() => setStore('bottomPanelOpen', false)}
              class="text-muted-foreground hover:text-foreground text-xs p-1"
            >
              <svg class="w-3.5 h-3.5" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2">
                <path stroke-linecap="round" stroke-linejoin="round" d="M19.5 13.5L12 21m0 0l-7.5-7.5M12 21V3" />
              </svg>
            </button>
          </div>
        </div>
        <div class="flex-1 overflow-hidden">
          <Show when={store.bottomPanelTab === 'terminal'}><Terminal /></Show>
          <Show when={store.bottomPanelTab === 'output'}><OutputPanel /></Show>
          <Show when={store.bottomPanelTab === 'problems'}><ProblemsPanel /></Show>
          <Show when={store.bottomPanelTab === 'timeline'}><Timeline /></Show>
          <Show when={store.bottomPanelTab === 'subagents'}><SubagentsPanel /></Show>
          <Show when={store.bottomPanelTab === 'flow'}><FlowPanel /></Show>
          <Show when={store.bottomPanelTab === 'ai-log'}><AILog /></Show>
        </div>
      </div>
    </Show>
  );
};
