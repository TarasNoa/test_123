import { For, type Component } from 'solid-js';
import { store, setStore, addTab } from '../IDEStore';

export const ProblemsPanel: Component = () => {
  const errors = () => store.problems.filter((p) => p.severity === 'error').length;
  const warnings = () => store.problems.filter((p) => p.severity === 'warning').length;

  return (
    <div class="flex flex-col h-full">
      <div class="px-3 py-1 border-b border-surface-3 text-[10px] text-muted-foreground uppercase tracking-wider">
        Problems ({errors()} errors, {warnings()} warnings)
      </div>
      <div class="flex-1 overflow-y-auto">
        <For each={store.problems}>{(p) => (
          <div
            class="flex items-start gap-2 px-3 py-1.5 text-xs hover:bg-surface-2/50 cursor-pointer border-b border-surface-3/50"
            onClick={() => addTab({ id: p.file, path: p.file, name: p.file.split('/').pop() || p.file, language: 'typescript', content: '', isDirty: false, isAgentEditing: false })}
          >
            <span class={p.severity === 'error' ? 'text-error' : 'text-warning'}>
              {p.severity === 'error' ? '✗' : '⚠'}
            </span>
            <div class="flex-1 min-w-0">
              <div class="text-foreground truncate">{p.message}</div>
              <div class="text-[10px] text-muted-foreground">{p.file}:{p.line}:{p.column}</div>
            </div>
          </div>
        )}</For>
      </div>
    </div>
  );
};
