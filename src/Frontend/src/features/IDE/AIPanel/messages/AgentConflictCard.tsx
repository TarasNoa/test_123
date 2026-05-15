import { type Component } from 'solid-js';
import type { ChatMessage } from '../../IDEStore';

export const AgentConflictCard: Component<{ msg: ChatMessage }> = (props) => {
  return (
    <div class="rounded-xl p-3 border-l-2 border-error bg-error/5">
      <div class="text-[11px] font-medium text-error mb-1">⚠️ Agent Conflict</div>
      <div class="text-[10px] text-muted-foreground">
        File: {props.msg.conflictFile}
      </div>
      <div class="text-[10px] text-muted-foreground">
        Agents: {props.msg.conflictAgents?.join(', ')}
      </div>
      <div class="flex gap-2 mt-2">
        <button class="text-[10px] px-2 py-0.5 rounded bg-surface-3 text-foreground hover:bg-surface-2 transition-colors">Merge</button>
        <button class="text-[10px] px-2 py-0.5 rounded bg-error/10 text-error hover:bg-error/20 transition-colors">Discard All</button>
      </div>
    </div>
  );
};
