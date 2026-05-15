import { Show, type Component } from 'solid-js';
import { store } from '../IDEStore';
import { AIHeader } from './AIHeader';
import { AutonomyToggle } from './AutonomyToggle';
import { MessageList } from './MessageList';
import { ContextChips } from './ContextChips';
import { AIInput } from './AIInput';

export const AIPanel: Component = () => {
  return (
    <Show when={store.aiPanelOpen}>
      <div class="shrink-0 flex flex-col border-l border-surface-3 bg-surface overflow-hidden" style={{ width: `${store.aiPanelWidth}px` }}>
        <AIHeader />
        <AutonomyToggle />
        <MessageList />
        <ContextChips />
        <AIInput />
      </div>
    </Show>
  );
};
