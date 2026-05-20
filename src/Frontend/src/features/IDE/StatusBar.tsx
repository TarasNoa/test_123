import { Show, type Component } from 'solid-js';
import { store } from './IDEStore';

export const StatusBar: Component = () => {
  const activeAgents = () => Object.values(store.activeAgents).filter((a) => a.status === 'running');
  const errors = () => store.problems.filter((p) => p.severity === 'error').length;
  const warnings = () => store.problems.filter((p) => p.severity === 'warning').length;
  const currentTab = () => store.openTabs.find((t) => t.id === store.activeTabId);

  return (
    <div class="shrink-0 h-6 flex items-center px-3 border-t border-surface-3 bg-surface text-[11px] font-mono select-none overflow-x-auto whitespace-nowrap scrollbar-hide">
      <div class="flex items-center gap-3 shrink-0">
        <span class="flex items-center gap-1 text-muted-foreground">
          <svg class="w-3 h-3" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="1.5">
            <path stroke-linecap="round" stroke-linejoin="round" d="M7.5 21L3 16.5m0 0L7.5 12M3 16.5h13.5m0-13.5L21 7.5m0 0L16.5 12M21 7.5H7.5" />
          </svg>
          main
        </span>
        <Show when={errors() > 0 || warnings() > 0}>
          <span class="flex items-center gap-1">
            <Show when={errors() > 0}>
              <span class="text-error">✗ {errors()}</span>
            </Show>
            <Show when={warnings() > 0}>
              <span class="text-warning">⚠ {warnings()}</span>
            </Show>
          </span>
        </Show>
        <Show when={activeAgents().length > 0}>
          <span class="flex items-center gap-1 text-secondary">
            <span class="w-1.5 h-1.5 rounded-full bg-secondary animate-pulse" />
            🤖 {activeAgents().length} running
          </span>
        </Show>
      </div>

      <div class="ml-auto flex items-center gap-3 shrink-0 pl-3">
        <span class="text-muted-foreground hidden sm:inline">{currentTab()?.language || 'plaintext'}</span>
        <span class="text-muted-foreground hidden md:inline">UTF-8</span>
        <span class="text-muted-foreground hidden md:inline">Spaces: 2</span>
        <span class="text-muted-foreground">
          Ln {store.cursorPosition.line}, Col {store.cursorPosition.column}
        </span>
        <span class="text-muted-foreground">
          {store.lastBuildStatus === 'running' ? '● Building...' :
           store.lastBuildStatus === 'success' ? '● Build ok' :
           store.lastBuildStatus === 'failed' ? '● Build failed' : '● Ready'}
        </span>
      </div>
    </div>
  );
};
