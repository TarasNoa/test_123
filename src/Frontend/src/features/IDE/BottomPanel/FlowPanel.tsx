import { For, Show, type Component } from 'solid-js';
import { store } from '../IDEStore';

const nodeStatus = (status: string, isCurrent: boolean) => {
  if (isCurrent) return 'border-secondary bg-secondary/10 text-secondary';
  switch (status.toLowerCase()) {
    case 'completed':
      return 'border-success/40 text-success';
    case 'running':
      return 'border-secondary/60 text-secondary';
    case 'failed':
      return 'border-error/40 text-error';
    default:
      return 'border-surface-3 text-muted-foreground';
  }
};

export const FlowPanel: Component = () => {
  const flow = () => store.flowProgress;

  return (
    <div class="flex flex-col h-full">
      <div class="px-3 py-1 border-b border-surface-3 text-[10px] text-muted-foreground uppercase tracking-wider">
        Flow
        <Show when={flow()}>
          <span class="ml-2 normal-case">{flow()?.flowName} — {flow()?.status}</span>
        </Show>
      </div>
      <div class="flex-1 overflow-y-auto p-2">
        <Show
          when={flow()}
          fallback={<div class="text-xs text-muted-foreground p-2">Flow не запущен для этого run</div>}
        >
          {(f) => (
            <div class="space-y-1">
              <For each={f().nodes}>{(node) => {
                const isCurrent = () => f().currentNodeId === node.nodeId;
                return (
                  <div
                    class={[
                      'flex items-center gap-2 px-2 py-1.5 rounded border text-xs',
                      nodeStatus(node.status, isCurrent()),
                    ].join(' ')}
                  >
                    <span class="font-mono">{node.nodeId}</span>
                    <span class="text-[10px] uppercase">{node.status}</span>
                    <Show when={isCurrent()}>
                      <span class="text-[10px] ml-auto">current</span>
                    </Show>
                    <Show when={node.lastError}>
                      <span class="text-error text-[10px] ml-auto truncate max-w-[40%]">{node.lastError}</span>
                    </Show>
                  </div>
                );
              }}</For>
            </div>
          )}
        </Show>
      </div>
    </div>
  );
};
