import { Show, type Component } from 'solid-js';
import type { ChatMessage } from '../../IDEStore';

export const AgentFailedCard: Component<{ msg: ChatMessage }> = (props) => {
  return (
    <div class="rounded-xl p-3 border-l-2 border-error bg-error/5">
      <div class="text-[11px] font-medium text-error mb-1">❌ {props.msg.agentType} failed</div>
      <div class="text-[11px] text-foreground">{props.msg.error}</div>
      <Show when={props.msg.canRetry || props.msg.canAutoFix}>
        <div class="flex gap-2 mt-2">
          <Show when={props.msg.canRetry}>
            <button class="text-[10px] px-2 py-0.5 rounded bg-surface-3 text-foreground hover:bg-surface-2 transition-colors">Retry</button>
          </Show>
          <Show when={props.msg.canAutoFix}>
            <button class="text-[10px] px-2 py-0.5 rounded bg-secondary/10 text-secondary hover:bg-secondary/20 transition-colors">Auto-fix</button>
          </Show>
        </div>
      </Show>
    </div>
  );
};
