import { Show, type Component } from 'solid-js';
import type { AgentFleetSummary } from '../services/agentFleet';

export const CiLogDrawer: Component<{
  run: AgentFleetSummary | null;
  open: boolean;
  onClose: () => void;
}> = (props) => (
  <Show when={props.open && props.run}>
    {(run) => (
      <div
        class="fixed inset-0 z-50 flex justify-end"
        data-testid="ci-log-drawer"
        onClick={(e) => {
          if (e.target === e.currentTarget) props.onClose();
        }}
      >
        <div class="w-full max-w-md h-full bg-surface-1 border-l border-surface-3 shadow-xl flex flex-col text-xs">
          <header class="flex items-center gap-2 px-4 py-3 border-b border-surface-3 shrink-0">
            <div class="min-w-0 flex-1">
              <div class="font-semibold truncate">{run().title}</div>
              <div class="text-[10px] text-muted-foreground font-mono">{run().runId.slice(0, 8)}</div>
            </div>
            <button
              type="button"
              class="text-muted-foreground hover:text-foreground px-2"
              onClick={() => props.onClose()}
            >
              ✕
            </button>
          </header>

          <div class="flex-1 overflow-y-auto p-4 space-y-4">
            <div>
              <div class="text-[10px] uppercase tracking-wide text-muted-foreground mb-1">CI status</div>
              <span
                class={[
                  'inline-block text-[10px] uppercase px-2 py-0.5 rounded border',
                  run().ciStatus === 'success'
                    ? 'border-success/40 text-success'
                    : run().ciStatus === 'failure'
                      ? 'border-error/40 text-error'
                      : 'border-amber-500/40 text-amber-400',
                ].join(' ')}
                data-testid="ci-drawer-status"
              >
                {run().ciStatus ?? 'unknown'}
              </span>
            </div>

            <Show when={run().prUrl}>
              <div>
                <div class="text-[10px] uppercase tracking-wide text-muted-foreground mb-1">Pull request</div>
                <a
                  href={run().prUrl!}
                  target="_blank"
                  rel="noreferrer"
                  class="text-secondary hover:underline break-all"
                  data-testid="ci-drawer-pr-link"
                >
                  PR #{run().prNumber ?? '…'}
                </a>
              </div>
            </Show>

            <Show
              when={run().ciLogsUrl}
              fallback={
                <p class="text-muted-foreground text-[11px]">
                  CI logs URL not available yet. Check back when the workflow completes.
                </p>
              }
            >
              <div class="space-y-2">
                <div class="text-[10px] uppercase tracking-wide text-muted-foreground">Logs</div>
                <a
                  href={run().ciLogsUrl!}
                  target="_blank"
                  rel="noreferrer"
                  class="inline-block text-[11px] px-3 py-1.5 rounded bg-secondary/20 text-secondary hover:bg-secondary/30"
                  data-testid="ci-drawer-open-logs"
                >
                  Open CI logs on GitHub →
                </a>
                <p class="text-[10px] text-muted-foreground break-all font-mono">{run().ciLogsUrl}</p>
              </div>
            </Show>
          </div>
        </div>
      </div>
    )}
  </Show>
);
