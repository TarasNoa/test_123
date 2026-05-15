import { Show, type Component } from 'solid-js';
import type { ChatMessage } from '../../IDEStore';

export const AgentSpawnedCard: Component<{ msg: ChatMessage }> = (props) => {
  const isSubagent = !!props.msg.parentAgentId;
  return (
    <div class={[
      'rounded-xl p-3',
      isSubagent ? 'ml-6 border-l-2 border-secondary/50 bg-secondary/5' : 'border-l-2 border-primary bg-primary/5',
    ].join(' ')}>
      <div class="flex items-center gap-1.5 text-xs font-medium mb-1">
        {isSubagent && <span class="text-muted-foreground">↳</span>}
        <span>🤖 {props.msg.agentType} {isSubagent ? 'spawned' : 'started'}</span>
        <span class="text-muted-foreground text-[10px]">#{props.msg.agentId?.slice(0, 6)}</span>
      </div>
      <div class="text-[11px] text-muted-foreground">{props.msg.task}</div>
      <Show when={props.msg.targetFiles && props.msg.targetFiles.length > 0}>
        <div class="text-[10px] text-muted-foreground mt-1">Files: {props.msg.targetFiles!.join(', ')}</div>
      </Show>
    </div>
  );
};
