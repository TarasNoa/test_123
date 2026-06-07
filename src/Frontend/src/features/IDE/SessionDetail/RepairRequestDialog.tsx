import { createSignal, For, Show, type Component } from 'solid-js';
import { submitRunReview, type RunReviewStatus } from '../services/runReview';

export const RepairRequestDialog: Component<{
  runId: string;
  paths: string[];
  onClose: () => void;
  onSubmitted: (status: RunReviewStatus) => void;
}> = (props) => {
  const [notes, setNotes] = createSignal('');
  const [busy, setBusy] = createSignal(false);
  const [error, setError] = createSignal<string | null>(null);

  const submit = async () => {
    if (!props.paths.length || busy()) return;
    setBusy(true);
    setError(null);
    try {
      const status = await submitRunReview(props.runId, 'RequestRepair', props.paths, notes() || undefined);
      props.onSubmitted(status);
      props.onClose();
    } catch (e) {
      setError(e instanceof Error ? e.message : 'submit failed');
    } finally {
      setBusy(false);
    }
  };

  return (
    <div class="fixed inset-0 z-50 flex items-center justify-center bg-black/60 p-4">
      <div class="w-full max-w-md rounded-lg border border-surface-3 bg-surface p-4 space-y-3 shadow-xl">
        <h2 class="text-sm font-semibold">Request repair</h2>
        <p class="text-xs text-muted-foreground">
          Scoped repair subagent will receive only these paths:
        </p>
        <ul class="text-[10px] font-mono max-h-32 overflow-y-auto bg-surface-2 rounded p-2">
          <For each={props.paths}>{(p) => <li>{p}</li>}</For>
        </ul>
        <textarea
          class="w-full text-xs bg-surface-2 border border-surface-3 rounded p-2 min-h-[72px]"
          placeholder="Notes for reviewer / repair agent (optional)"
          value={notes()}
          onInput={(e) => setNotes(e.currentTarget.value)}
        />
        <Show when={error()}>
          <p class="text-xs text-error">{error()}</p>
        </Show>
        <div class="flex gap-2 justify-end">
          <button
            type="button"
            class="px-3 py-1.5 text-xs rounded border border-surface-3"
            disabled={busy()}
            onClick={() => props.onClose()}
          >
            Cancel
          </button>
          <button
            type="button"
            class="px-3 py-1.5 text-xs rounded bg-amber-500/20 text-amber-300 border border-amber-500/40 disabled:opacity-50"
            disabled={busy()}
            onClick={() => void submit()}
          >
            Request repair
          </button>
        </div>
      </div>
    </div>
  );
};

export default RepairRequestDialog;
