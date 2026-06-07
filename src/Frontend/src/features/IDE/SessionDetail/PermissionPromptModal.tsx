import { createSignal, Show, type Component } from 'solid-js';
import { resolvePermissionPrompt, type PermissionPrompt } from '../services/runSession';

export const PermissionPromptModal: Component<{
  runId: string;
  prompt: PermissionPrompt;
  onResolved: () => void;
}> = (props) => {
  const [busy, setBusy] = createSignal(false);
  const [error, setError] = createSignal<string | null>(null);

  const resolve = async (accepted: boolean) => {
    setBusy(true);
    setError(null);
    try {
      await resolvePermissionPrompt(props.runId, props.prompt.id, accepted);
      props.onResolved();
    } catch (e) {
      setError(e instanceof Error ? e.message : 'resolve failed');
    } finally {
      setBusy(false);
    }
  };

  return (
    <div class="fixed inset-0 z-50 flex items-center justify-center bg-black/60 p-4">
      <div class="w-full max-w-md rounded-lg border border-surface-3 bg-surface p-4 space-y-3 shadow-xl">
        <h2 class="text-sm font-semibold text-foreground">Требуется разрешение</h2>
        <p class="text-xs text-muted-foreground">
          Инструмент <span class="font-mono text-secondary">{props.prompt.toolName}</span> запрашивает доступ.
        </p>
        <Show when={props.prompt.path}>
          <p class="text-[10px] font-mono text-muted-foreground break-all">{props.prompt.path}</p>
        </Show>
        <p class="text-xs text-foreground">{props.prompt.reason}</p>
        <Show when={error()}>
          <p class="text-xs text-error">{error()}</p>
        </Show>
        <div class="flex gap-2 justify-end pt-2">
          <button
            type="button"
            disabled={busy()}
            class="px-3 py-1.5 text-xs rounded border border-surface-3 text-muted-foreground hover:text-foreground"
            onClick={() => void resolve(false)}
          >
            Deny
          </button>
          <button
            type="button"
            disabled={busy()}
            class="px-3 py-1.5 text-xs rounded bg-secondary/20 text-secondary border border-secondary/40"
            onClick={() => void resolve(true)}
          >
            Allow
          </button>
        </div>
      </div>
    </div>
  );
};

export default PermissionPromptModal;
