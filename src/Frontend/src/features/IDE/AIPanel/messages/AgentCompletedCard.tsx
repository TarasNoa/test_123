import { Show, type Component } from 'solid-js';
import type { ChatMessage } from '../../IDEStore';

export const AgentCompletedCard: Component<{ msg: ChatMessage }> = (props) => {
  const fmt = (s?: number) => s ? `${Math.floor(s / 60)}m ${s % 60}s` : '';

  return (
    <div class="rounded-xl p-3 border-l-2 border-success bg-success/5">
      <div class="text-[11px] font-medium text-success mb-1">✅ {props.msg.agentType} completed {fmt(props.msg.duration)}</div>
      <Show when={props.msg.whatWasDone}>
        <div class="text-[11px] text-foreground mb-1">
          <span class="text-muted-foreground">Done:</span> {props.msg.whatWasDone}
        </div>
      </Show>
      <Show when={props.msg.whatWasNOTDone}>
        <div class="text-[11px] text-warning mb-1">
          <span class="text-muted-foreground">Not done:</span> {props.msg.whatWasNOTDone}
        </div>
      </Show>
      <Show when={props.msg.nextStep}>
        <div class="text-[11px] mb-1">
          <span class="text-muted-foreground">Next:</span> <span class="text-secondary">{props.msg.nextStep}</span>
        </div>
      </Show>
      <Show when={props.msg.filesModified && props.msg.filesModified.length > 0}>
        <div class="text-[10px] text-muted-foreground mt-1">{props.msg.filesModified!.join(' · ')}</div>
      </Show>
      <Show when={props.msg.linesAdded !== undefined || props.msg.linesRemoved !== undefined}>
        <div class="text-[10px] text-muted-foreground mt-0.5">
          +{props.msg.linesAdded || 0} lines, -{props.msg.linesRemoved || 0} lines
        </div>
      </Show>
    </div>
  );
};
