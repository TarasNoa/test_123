import { Show, type Component } from 'solid-js';
import { store, setStore } from '../../IDEStore';
import type { ChatMessage } from '../../IDEStore';

export const AgentFileEditCard: Component<{ msg: ChatMessage }> = (props) => {
  return (
    <div class={[
      'rounded-xl p-3 border',
      props.msg.status === 'accepted' ? 'border-success/30 bg-success/5' :
      props.msg.status === 'rejected' ? 'border-surface-3 bg-surface-2/30' :
      'border-surface-3 bg-surface-2/30',
    ].join(' ')}>
      <div class="text-[11px] font-medium mb-1">📝 {props.msg.agentType} → {props.msg.path}</div>
      <div class="text-[10px] text-muted-foreground">
        +{props.msg.linesAdded} lines  -{props.msg.linesRemoved} lines
      </div>
      <Show when={props.msg.preview}>
        <div class="text-[10px] text-muted-foreground font-mono mt-1 opacity-60 truncate">{props.msg.preview}</div>
      </Show>
      <Show when={!props.msg.status || props.msg.status === 'pending'}>
        <div class="flex gap-2 mt-2">
          <button
            onClick={() => {
              const tab = store.openTabs.find((t: any) => t.path === props.msg.path);
              if (tab) {
                setStore('diffTabId', tab.id);
              }
            }}
            class="text-[10px] px-2 py-0.5 rounded bg-surface-3 text-foreground hover:bg-surface-2 transition-colors"
          >
            View Diff
          </button>
          <button
            onClick={() => {
              setStore('messages', (m) => m.map((x) => x.id === props.msg.id ? { ...x, status: 'accepted' as const } : x));
            }}
            class="text-[10px] px-2 py-0.5 rounded bg-success/10 text-success hover:bg-success/20 transition-colors"
          >
            Accept
          </button>
          <button
            onClick={() => {
              setStore('messages', (m) => m.map((x) => x.id === props.msg.id ? { ...x, status: 'rejected' as const } : x));
            }}
            class="text-[10px] px-2 py-0.5 rounded bg-error/10 text-error hover:bg-error/20 transition-colors"
          >
            Reject
          </button>
        </div>
      </Show>
      <Show when={props.msg.status === 'accepted'}>
        <div class="text-[10px] text-success mt-1">Accepted ✅</div>
      </Show>
      <Show when={props.msg.status === 'rejected'}>
        <div class="text-[10px] text-muted-foreground mt-1">Rejected</div>
      </Show>
    </div>
  );
};
