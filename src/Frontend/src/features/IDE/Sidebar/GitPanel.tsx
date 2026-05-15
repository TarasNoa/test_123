import { For, type Component } from 'solid-js';
import { store } from '../IDEStore';

export const GitPanel: Component = () => {
  return (
    <div class="flex-1 flex flex-col overflow-hidden">
      <div class="px-3 py-2 text-[10px] font-semibold text-muted-foreground uppercase tracking-wider">Source Control</div>
      <div class="px-3 py-4 text-xs text-muted-foreground text-center">
        <svg class="w-6 h-6 mx-auto mb-2 opacity-30" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="1.5">
          <path stroke-linecap="round" stroke-linejoin="round" d="M7.5 21L3 16.5m0 0L7.5 12M3 16.5h13.5m0-13.5L21 7.5m0 0L16.5 12M21 7.5H7.5" />
        </svg>
        Git integration coming soon
      </div>
    </div>
  );
};
