import { For, type Component } from 'solid-js';
import type { ChatMessage } from '../../IDEStore';

export const ParallelGroupCard: Component<{ msg: ChatMessage }> = (props) => {
  return (
    <div class="rounded-xl p-3 border-l-2 border-secondary bg-secondary/5">
      <div class="text-[11px] font-medium mb-2">⚡ Parallel group started</div>
      <div class="space-y-1.5">
        <For each={props.msg.agents}>{(a) => (
          <div class="flex items-center gap-2 text-xs">
            <span class="text-primary">🤖 {a.agentType}</span>
            <span class="text-muted-foreground text-[10px]">{a.task}</span>
            <div class="flex-1 h-1 bg-surface-3 rounded-full overflow-hidden max-w-[80px]">
              <div class="h-full bg-secondary rounded-full" style={{ width: `${a.progress}%` }} />
            </div>
          </div>
        )}</For>
      </div>
    </div>
  );
};
