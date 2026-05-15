import { For, Show, type Component } from 'solid-js';
import type { ChatMessage } from '../../IDEStore';

export const ArchitectPlanCard: Component<{ msg: ChatMessage }> = (props) => {
  return (
    <div class="rounded-xl p-3 border-l-2 border-info bg-info/5">
      <div class="text-[11px] font-medium mb-1">📐 {props.msg.title || 'Architect Plan'}</div>
      <Show when={props.msg.steps && props.msg.steps.length > 0}>
        <div class="space-y-1">
          <For each={props.msg.steps}>{(step, i) => (
            <div class="flex items-center gap-2 text-[10px]">
              <span class="text-muted-foreground w-5 text-right">{i() + 1}.</span>
              <span class="text-foreground">{step.description}</span>
              <span class="text-muted-foreground">({step.agentType})</span>
              <span class="text-muted-foreground/50 ml-auto">{step.estimatedFiles.join(', ')}</span>
            </div>
          )}</For>
        </div>
      </Show>
      <div class="flex gap-2 mt-2">
        <button class="text-[10px] px-2 py-0.5 rounded bg-surface-3 text-foreground hover:bg-surface-2 transition-colors">Execute Plan</button>
        <button class="text-[10px] px-2 py-0.5 rounded bg-surface-3 text-foreground hover:bg-surface-2 transition-colors">Modify</button>
      </div>
    </div>
  );
};
