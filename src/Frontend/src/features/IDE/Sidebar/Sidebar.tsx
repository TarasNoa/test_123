import { Show, type Component } from 'solid-js';
import { store } from '../IDEStore';
import { FileTree } from './FileTree';
import { SearchPanel } from './SearchPanel';
import { GitPanel } from './GitPanel';

export const Sidebar: Component = () => {
  return (
    <Show when={store.sidebarOpen}>
      <div class="shrink-0 flex flex-col border-r border-surface-3 bg-surface overflow-hidden" style={{ width: `${store.sidebarWidth}px` }}>
        <Show when={store.activeActivityTab === 'files'}>
          <FileTree />
        </Show>
        <Show when={store.activeActivityTab === 'search'}>
          <SearchPanel />
        </Show>
        <Show when={store.activeActivityTab === 'git'}>
          <GitPanel />
        </Show>
      </div>
    </Show>
  );
};
