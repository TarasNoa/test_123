import { createSignal, For, type Component } from 'solid-js';
import { store, setStore, addTab } from '../IDEStore';

export const SearchPanel: Component = () => {
  const [query, setQuery] = createSignal('');
  const [results, setResults] = createSignal<Array<{ file: string; line: number; text: string }>>([]);

  const search = () => {
    const q = query().toLowerCase();
    if (!q) { setResults([]); return; }
    const found: Array<{ file: string; line: number; text: string }> = [];
    const walk = (nodes: typeof store.fileTree) => {
      for (const n of nodes) {
        if (n.type === 'file' && n.name.toLowerCase().includes(q)) {
          found.push({ file: n.path, line: 1, text: n.name });
        }
        if (n.children) walk(n.children);
      }
    };
    walk(store.fileTree);
    setResults(found);
  };

  return (
    <div class="flex-1 flex flex-col overflow-hidden">
      <div class="px-3 py-2 text-[10px] font-semibold text-muted-foreground uppercase tracking-wider">Search</div>
      <div class="px-2 pb-2">
        <input
          value={query()}
          onInput={(e) => { setQuery(e.currentTarget.value); search(); }}
          placeholder="Search files..."
          class="w-full bg-surface-2 border border-surface-3 rounded px-2 py-1 text-xs text-foreground outline-none focus:border-secondary/30 placeholder:text-muted-foreground/40"
        />
      </div>
      <div class="flex-1 overflow-y-auto px-2">
        <For each={results()}>{(r) => (
          <div
            class="py-1 px-2 text-xs text-foreground cursor-pointer hover:bg-surface-2/50 rounded"
            onClick={() => addTab({ id: r.file, path: r.file, name: r.file.split('/').pop() || r.file, language: 'typescript', content: '', isDirty: false, isAgentEditing: false })}
          >
            <div class="truncate">{r.file}</div>
          </div>
        )}</For>
      </div>
    </div>
  );
};
