import { Show, type Component } from 'solid-js';
import type { ChatMessage } from '../../IDEStore';

export const UserMessage: Component<{ msg: ChatMessage }> = (props) => {
  return (
    <div class="flex justify-end">
      <div class="max-w-[90%] bg-surface-2 text-foreground text-xs px-3 py-2 rounded-2xl rounded-tr-sm space-y-2">
        <Show when={props.msg.selectedCode}>
          <div class="rounded-lg bg-surface p-2 font-mono text-[10px] border border-surface-3">
            <div class="text-muted-foreground mb-1">{props.msg.selectedCode!.language} · {props.msg.selectedCode!.lines}</div>
            <pre class="whitespace-pre-wrap">{props.msg.selectedCode!.code}</pre>
          </div>
        </Show>
        <div class="whitespace-pre-wrap">{props.msg.text}</div>
        <Show when={props.msg.attachedFiles && props.msg.attachedFiles.length > 0}>
          <div class="flex flex-wrap gap-1">
            {props.msg.attachedFiles!.map((f) => (
              <span class="text-[10px] px-1.5 py-0.5 rounded bg-surface border border-surface-3 text-muted-foreground">📄 {f}</span>
            ))}
          </div>
        </Show>
        <div class="text-[9px] text-muted-foreground text-right">
          {props.msg.timestamp.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })}
        </div>
      </div>
    </div>
  );
};
