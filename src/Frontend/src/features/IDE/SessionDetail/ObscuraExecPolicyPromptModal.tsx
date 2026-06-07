import { createSignal, Show, type Component } from 'solid-js';
import { resolvePermissionPrompt } from '../services/runSession';
import type { ExecPolicyPromptPayload } from '../services/execPolicyPromptStream';

export const ObscuraExecPolicyPromptModal: Component<{
  runId: string;
  prompt: ExecPolicyPromptPayload;
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

  const isScript = () =>
    props.prompt.toolName.toLowerCase().includes('script')
    || props.prompt.toolName.toLowerCase().includes('js');

  return (
    <div
      class="fixed inset-0 z-[60] flex items-center justify-center bg-black/70 p-4"
      data-testid="obscura-execpolicy-modal"
    >
      <div class="w-full max-w-lg rounded-lg border border-amber-500/40 bg-surface p-4 space-y-3 shadow-xl">
        <div class="flex items-start gap-2">
          <span class="text-lg" aria-hidden="true">
            🛡️
          </span>
          <div class="min-w-0 space-y-1">
            <h2 class="text-sm font-semibold text-foreground">Obscura Exec Policy</h2>
            <p class="text-xs text-muted-foreground">
              Агент запрашивает доступ к внешнему ресурсу. Это не ошибка — требуется ваше явное согласие.
            </p>
          </div>
        </div>

        <dl class="grid grid-cols-[auto_1fr] gap-x-3 gap-y-2 text-xs">
          <dt class="text-muted-foreground">Инструмент</dt>
          <dd class="font-mono text-secondary break-all">{props.prompt.toolName}</dd>

          <Show when={props.prompt.target}>
            <dt class="text-muted-foreground">{isScript() ? 'Script' : 'Target'}</dt>
            <dd class="font-mono text-foreground break-all max-h-24 overflow-auto">{props.prompt.target}</dd>
          </Show>

          <Show when={props.prompt.matchedRule}>
            <dt class="text-muted-foreground">Правило</dt>
            <dd class="font-mono text-muted-foreground break-all">{props.prompt.matchedRule}</dd>
          </Show>

          <dt class="text-muted-foreground">Причина</dt>
          <dd class="text-foreground">{props.prompt.reason}</dd>
        </dl>

        <p class="text-[10px] text-muted-foreground border-t border-surface-3 pt-2">
          Allow — разрешить только этот запрос. Deny — заблокировать и записать отказ в audit log.
        </p>

        <Show when={error()}>
          <p class="text-xs text-error">{error()}</p>
        </Show>

        <div class="flex gap-2 justify-end pt-1">
          <button
            type="button"
            data-testid="obscura-execpolicy-deny"
            disabled={busy()}
            class="px-3 py-1.5 text-xs rounded border border-error/40 text-error hover:bg-error/10"
            onClick={() => void resolve(false)}
          >
            Deny
          </button>
          <button
            type="button"
            data-testid="obscura-execpolicy-allow"
            disabled={busy()}
            class="px-3 py-1.5 text-xs rounded bg-amber-500/20 text-amber-300 border border-amber-500/50"
            onClick={() => void resolve(true)}
          >
            Allow once
          </button>
        </div>
      </div>
    </div>
  );
};

export default ObscuraExecPolicyPromptModal;
