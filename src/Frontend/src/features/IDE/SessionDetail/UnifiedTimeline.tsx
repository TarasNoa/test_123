import { createSignal, For, onMount, Show, type Component } from 'solid-js';
import { fetchSessionTimeline, type SessionTimelineEvent } from '../services/runSession';

const kindIcon = (kind: string, success?: boolean | null) => {
  switch (kind) {
    case 'ToolCall':
      return success === false ? '🔧✗' : '🔧';
    case 'SubagentSpawn':
      return '🤖';
    case 'SubagentComplete':
      return success === false ? '🤖✗' : '🤖✓';
    case 'DelegationStart':
      return '📤';
    case 'DelegationComplete':
      return success === false ? '📥✗' : '📥✓';
    case 'VerifyAttempt':
      return success === false ? '🔍✗' : success === true ? '🔍✓' : '🔍';
    case 'FlowNode':
    case 'Phase':
      return '⚙';
    case 'Error':
      return '❌';
    case 'Permission':
      return success === false ? '🔒✗' : '🔒';
    default:
      return '●';
  }
};

const kindLabel: Record<string, string> = {
  ToolCall: 'Tool',
  SubagentSpawn: 'Subagent',
  SubagentComplete: 'Subagent',
  DelegationStart: 'Delegation',
  DelegationComplete: 'Delegation',
  VerifyAttempt: 'Verify',
  FlowNode: 'Flow',
  Phase: 'Phase',
  StepStart: 'Step',
  StepFinish: 'Step',
  Error: 'Error',
  Permission: 'Permission',
};

export const UnifiedTimeline: Component<{ runId: string }> = (props) => {
  const [events, setEvents] = createSignal<SessionTimelineEvent[]>([]);
  const [loading, setLoading] = createSignal(true);
  const [error, setError] = createSignal<string | null>(null);

  const load = async () => {
    if (!props.runId) return;
    try {
      setError(null);
      const data = await fetchSessionTimeline(props.runId);
      setEvents(data);
    } catch (e) {
      setError(e instanceof Error ? e.message : 'timeline load failed');
    } finally {
      setLoading(false);
    }
  };

  onMount(() => {
    void load();
    const poll = setInterval(() => void load(), 8000);
    return () => clearInterval(poll);
  });

  const fmt = (iso: string) =>
    new Date(iso).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit', second: '2-digit' });

  return (
    <div class="flex flex-col border border-surface-3 rounded overflow-hidden min-h-48">
      <div class="px-3 py-1 border-b border-surface-3 text-[10px] text-muted-foreground uppercase tracking-wider flex items-center justify-between">
        <span>Unified timeline</span>
        <span>{events().length} events</span>
      </div>
      <Show when={loading()}>
        <p class="p-3 text-xs text-muted-foreground">Loading timeline…</p>
      </Show>
      <Show when={error()}>
        <p class="p-3 text-xs text-error">{error()}</p>
      </Show>
      <Show when={!loading() && !error() && events().length === 0}>
        <p class="p-3 text-xs text-muted-foreground">No timeline events yet.</p>
      </Show>
      <div class="flex-1 overflow-y-auto p-2 space-y-1 max-h-72">
        <For each={events()}>{(e) => (
          <div class="flex items-start gap-2 text-xs py-0.5">
            <span class="text-muted-foreground w-16 text-right shrink-0 font-mono text-[10px]">{fmt(e.timestampUtc)}</span>
            <span class="shrink-0 w-6 text-center">{kindIcon(e.kind, e.success)}</span>
            <div class="min-w-0 flex-1">
              <div class="flex flex-wrap items-center gap-1">
                <span class="text-[10px] uppercase text-muted-foreground">{kindLabel[e.kind] ?? e.kind}</span>
                <Show when={e.stepNumber != null}>
                  <span class="text-[10px] text-muted-foreground">#{e.stepNumber}</span>
                </Show>
                <span class={
                  e.success === false ? 'text-error' : e.success === true ? 'text-success' : 'text-foreground'
                }>
                  {e.title}
                </span>
              </div>
              <Show when={e.detail}>
                <p class="text-[10px] text-muted-foreground truncate">{e.detail}</p>
              </Show>
            </div>
          </div>
        )}</For>
      </div>
    </div>
  );
};
