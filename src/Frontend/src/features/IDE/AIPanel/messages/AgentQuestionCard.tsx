import { Show, For, type Component } from 'solid-js';
import { setStore } from '../../IDEStore';
import type { ChatMessage } from '../../IDEStore';

export const AgentQuestionCard: Component<{ msg: ChatMessage }> = (props) => {
  return (
    <div class="rounded-xl p-3 border-l-2 border-warning bg-warning/5">
      <div class="text-[11px] font-medium mb-1">🤔 {props.msg.agentType} stopped</div>
      <div class="text-xs text-foreground mb-2">{props.msg.question}</div>
      <Show when={props.msg.options && props.msg.options.length > 0}>
        <div class="flex flex-wrap gap-1.5">
          <For each={props.msg.options}>{(opt) => (
            <button
              onClick={() => {
                // Send answer to backend
              }}
              class="text-[10px] px-2 py-1 rounded bg-surface-3 text-foreground hover:bg-surface-2 transition-colors"
            >
              {opt}
            </button>
          )}</For>
        </div>
      </Show>
    </div>
  );
};
