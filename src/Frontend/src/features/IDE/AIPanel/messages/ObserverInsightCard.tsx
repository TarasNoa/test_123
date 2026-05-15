import { Show, type Component } from 'solid-js';
import type { ChatMessage } from '../../IDEStore';

export const ObserverInsightCard: Component<{ msg: ChatMessage }> = (props) => {
  return (
    <div class="rounded-xl p-3 border-l-2 border-muted bg-muted/5">
      <div class="text-[11px] font-medium mb-1">🔍 Observer Insight</div>
      <div class="text-[10px] text-muted-foreground mb-1">Pattern: {props.msg.pattern}</div>
      <div class="text-[10px] text-foreground">{props.msg.suggestion}</div>
      <Show when={props.msg.frequency}>
        <div class="text-[9px] text-muted-foreground mt-1">Seen {props.msg.frequency} times</div>
      </Show>
    </div>
  );
};
