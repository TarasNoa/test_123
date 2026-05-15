import { type Component } from 'solid-js';
import type { ChatMessage } from '../../IDEStore';

export const AgentThinkingLine: Component<{ msg: ChatMessage }> = (props) => {
  return (
    <div class="text-[11px] text-muted-foreground italic pl-3">
      {props.msg.isMemoryRetrieval ? '💾' : '💭'} {props.msg.message}
    </div>
  );
};
