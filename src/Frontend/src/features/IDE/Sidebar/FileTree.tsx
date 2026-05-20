import { For, Show, type Component } from 'solid-js';
import { store, setStore, addTab } from '../IDEStore';
import { config } from '../../../lib/config';
import type { FileNode } from '../IDEStore';

const FileIcon: Component<{ node: FileNode }> = (props) => {
  if (props.node.type === 'folder') {
    return (
      <svg class="w-3.5 h-3.5 text-muted-foreground shrink-0" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="1.5">
        <path stroke-linecap="round" stroke-linejoin="round" d={props.node.isOpen ? 'M19.5 8.25l-7.5 7.5-7.5-7.5' : 'M8.25 4.5l7.5 7.5-7.5 7.5'} />
      </svg>
    );
  }
  const ext = props.node.name.split('.').pop()?.toLowerCase();
  if (ext === 'ts' || ext === 'tsx') return <svg class="w-3.5 h-3.5 text-primary shrink-0" viewBox="0 0 24 24" fill="currentColor"><path d="M3 3h18v18H3V3zm14.5 11.5c0 .8-.6 1.5-1.5 1.5h-2v-2h2v-1h-3v-2h3c.8 0 1.5.6 1.5 1.5v2zm-6 0c0 .8-.6 1.5-1.5 1.5h-2v-2h2v-1h-3v-2h3c.8 0 1.5.6 1.5 1.5v2z" opacity="0.6"/></svg>;
  if (ext === 'css') return <svg class="w-3.5 h-3.5 text-info shrink-0" viewBox="0 0 24 24" fill="currentColor"><path d="M3 3h18v18H3V3zm14.5 11.5c0 .8-.6 1.5-1.5 1.5h-2v-2h2v-1h-3v-2h3c.8 0 1.5.6 1.5 1.5v2z" opacity="0.6"/></svg>;
  if (ext === 'json') return <svg class="w-3.5 h-3.5 text-warning shrink-0" viewBox="0 0 24 24" fill="currentColor"><path d="M3 3h18v18H3V3zm14.5 11.5c0 .8-.6 1.5-1.5 1.5h-2v-2h2v-1h-3v-2h3c.8 0 1.5.6 1.5 1.5v2z" opacity="0.6"/></svg>;
  return (
    <svg class="w-3.5 h-3.5 text-muted-foreground shrink-0" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="1.5">
      <path stroke-linecap="round" stroke-linejoin="round" d="M19.5 14.25v-2.625a3.375 3.375 0 00-3.375-3.375h-1.5A1.125 1.125 0 0113.5 7.125v-1.5a3.375 3.375 0 00-3.375-3.375H8.25m2.25 0H5.625c-.621 0-1.125.504-1.125 1.125v17.25c0 .621.504 1.125 1.125 1.125h12.75c.621 0 1.125-.504 1.125-1.125V11.25a9 9 0 00-9-9z" />
    </svg>
  );
};

const TreeNode: Component<{ node: FileNode; depth?: number }> = (props) => {
  const depth = props.depth ?? 0;
  const isFolder = props.node.type === 'folder';

  const toggle = async () => {
    if (isFolder) {
      setStore('fileTree', (tree) => toggleNode(tree, props.node.path));
    } else {
      const existing = store.openTabs.find((t) => t.path === props.node.path);
      if (!existing) {
        addTab({
          id: props.node.path,
          path: props.node.path,
          name: props.node.name,
          language: props.node.language || 'text',
          content: '// Loading...',
          isDirty: false,
          isAgentEditing: false,
        });
      } else {
        setStore('activeTabId', existing.id);
      }

      const token = localStorage.getItem('accessToken') || '';
      try {
        const res = await fetch(
          `${config.apiBaseUrl}/api/v1/ide/files/content?path=${encodeURIComponent(props.node.path)}&sessionId=${store.sessionId}`,
          { headers: { Authorization: `Bearer ${token}` } }
        );
        if (res.ok) {
          const data = await res.json();
          setStore('openTabs', (tabs) =>
            tabs.map((t) =>
              t.path === props.node.path
                ? { ...t, content: data.content || '', isDirty: false }
                : t
            )
          );
        }
      } catch {
        // Leave loading text or empty on failure
      }
    }
  };

  return (
    <div>
      <div
        class={[
          'flex items-center gap-1.5 py-0.5 pr-2 cursor-pointer select-none text-xs',
          store.activeTabId === props.node.path ? 'text-secondary bg-secondary/5' : 'text-foreground hover:bg-surface-2/50',
        ].join(' ')}
        style={{ 'padding-left': `${8 + depth * 12}px` }}
        onClick={toggle}
      >
        <FileIcon node={props.node} />
        <Show when={props.node.isAgentEditing}>
          <span class="text-secondary text-[8px] animate-pulse">🤖</span>
        </Show>
        <span class="truncate">{props.node.name}</span>
        <Show when={props.node.isDirty}>
          <span class="text-secondary text-[6px]">●</span>
        </Show>
      </div>
      <Show when={isFolder && props.node.isOpen && props.node.children && props.node.children.length > 0}>
        <For each={props.node.children}>{(child) => <TreeNode node={child} depth={depth + 1} />}</For>
      </Show>
    </div>
  );
};

function toggleNode(nodes: FileNode[], path: string): FileNode[] {
  return nodes.map((n) => {
    if (n.path === path) return { ...n, isOpen: !n.isOpen };
    if (n.children) return { ...n, children: toggleNode(n.children, path) };
    return n;
  });
}

export const FileTree: Component = () => {
  return (
    <div class="flex-1 overflow-y-auto py-2">
      <div class="px-3 pb-2 text-[10px] font-semibold text-muted-foreground uppercase tracking-wider">Explorer</div>
      <For each={store.fileTree}>{(node) => <TreeNode node={node} />}</For>
    </div>
  );
};
