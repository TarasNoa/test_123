import { For, Show, type Component } from 'solid-js';
import type { ConsoleErrorEntry } from './reviewUtils';
import { pathMatches } from './reviewUtils';

export const ConsoleErrorPanel: Component<{
  errors: ConsoleErrorEntry[];
  activePath?: string | null;
  onJumpToPath: (path: string) => void;
}> = (props) => {
  const relevant = () => {
    if (!props.activePath) return props.errors;
    return props.errors.filter((e) =>
      e.paths.some((p) => pathMatches(p, props.activePath!))
      || e.message.includes(props.activePath!));
  };

  return (
    <div class="space-y-1">
      <div class="text-[10px] font-medium text-muted-foreground">Console errors</div>
      <Show when={props.errors.length > 0} fallback={
        <p class="text-[10px] text-muted-foreground">No console errors recorded.</p>
      }>
        <Show when={relevant().length > 0} fallback={
          <p class="text-[10px] text-muted-foreground italic">No errors reference this file.</p>
        }>
          <For each={relevant()}>{(entry) => (
            <div class="rounded border border-surface-3 bg-surface-2/50 p-1.5 text-[10px]">
              <div class="text-amber-400 mb-0.5">{entry.level}</div>
              <div class="text-muted-foreground whitespace-pre-wrap break-words">{entry.message}</div>
              <Show when={entry.paths.length > 0}>
                <div class="flex flex-wrap gap-1 mt-1">
                  <For each={entry.paths}>{(p) => (
                    <button
                      type="button"
                      class="font-mono text-secondary hover:underline"
                      onClick={() => props.onJumpToPath(p)}
                    >
                      {p}
                    </button>
                  )}</For>
                </div>
              </Show>
            </div>
          )}</For>
        </Show>
      </Show>
    </div>
  );
};

export default ConsoleErrorPanel;
