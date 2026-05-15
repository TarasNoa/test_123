import { type Component } from 'solid-js';
import { EditorTabs } from './EditorTabs';
import { AgentBanner } from './AgentBanner';
import { MonacoEditor } from './MonacoEditor';

export const EditorArea: Component = () => {
  return (
    <div class="flex-1 flex flex-col min-w-0 bg-surface overflow-hidden">
      <EditorTabs />
      <AgentBanner />
      <MonacoEditor />
    </div>
  );
};
