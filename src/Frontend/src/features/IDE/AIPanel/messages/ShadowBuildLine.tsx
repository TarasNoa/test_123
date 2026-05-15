import { Show, type Component } from 'solid-js';
import type { ChatMessage } from '../../IDEStore';

export const ShadowBuildLine: Component<{ msg: ChatMessage }> = (props) => {
  return (
    <div class="text-[11px] pl-2">
      <Show when={props.msg.buildStatus === 'running'}>
        <span class="text-muted-foreground">🏗️ Shadow build running...</span>
      </Show>
      <Show when={props.msg.buildStatus === 'success'}>
        <span class="text-success">✅ Build passed · {props.msg.buildDuration || 0}ms · {props.msg.testsPassed || 0} tests ✓</span>
      </Show>
      <Show when={props.msg.buildStatus === 'failed'}>
        <div class="text-error">
          ❌ Build failed · {props.msg.errors?.length || 0} errors
          <Show when={props.msg.errors && props.msg.errors.length > 0}>
            <div class="mt-1 space-y-0.5">
              {props.msg.errors!.map((e) => (
                <div class="text-[10px] text-muted-foreground">{e.file}:{e.line} — {e.message}</div>
              ))}
            </div>
          </Show>
        </div>
      </Show>
    </div>
  );
};
