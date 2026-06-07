import {
  createSignal,
  For,
  onCleanup,
  onMount,
  Show,
  type Component,
} from 'solid-js';
import { useNavigate } from '@solidjs/router';
import {
  bulkArchiveFleetRuns,
  cancelFleetRun,
  fetchAgentFleet,
  type AgentFleetStatus,
  type AgentFleetSummary,
} from '../services/agentFleet';
import { subscribeAgentFleetEvents, stopAgentFleetEvents } from '../services/agentFleetEvents';
import { CiLogDrawer } from '../components/CiLogDrawer';

const STATUS_COLUMNS: AgentFleetStatus[] = [
  'Planning',
  'Generating',
  'Verifying',
  'Repairing',
  'WaitingForApproval',
  'PrReady',
  'WaitingForCi',
  'HandoffPending',
  'HandoffComplete',
  'Completed',
  'Failed',
];

const statusLabel: Record<string, string> = {
  Planning: 'Planning',
  Generating: 'Generating',
  Verifying: 'Verify',
  Repairing: 'Repair',
  WaitingForApproval: 'Approval',
  PrReady: 'PR ready',
  WaitingForCi: 'CI',
  HandoffPending: 'Handoff…',
  HandoffComplete: 'Handoff ✓',
  Completed: 'Done',
  Failed: 'Failed',
  Cancelled: 'Cancelled',
  Queued: 'Queued',
};

const statusColor = (status: string) => {
  switch (status) {
    case 'Completed':
    case 'HandoffComplete':
      return 'border-success/40 bg-success/5';
    case 'Failed':
    case 'Cancelled':
      return 'border-error/40 bg-error/5';
    case 'HandoffPending':
      return 'border-amber-500/50 bg-amber-500/5';
    case 'Verifying':
    case 'Repairing':
      return 'border-secondary/50 bg-secondary/5';
    default:
      return 'border-surface-3 bg-surface-1';
  }
};

const isTerminal = (status: string) =>
  ['Completed', 'Failed', 'Cancelled', 'HandoffComplete'].includes(status);

export const AgentBoard: Component = () => {
  const navigate = useNavigate();
  const [runs, setRuns] = createSignal<AgentFleetSummary[]>([]);
  const [loading, setLoading] = createSignal(true);
  const [filter, setFilter] = createSignal<'all' | 'running' | 'done'>('all');
  const [view, setView] = createSignal<'board' | 'list'>('board');
  const [selectedIndex, setSelectedIndex] = createSignal(0);
  const [error, setError] = createSignal<string | null>(null);
  const [spaces, setSpaces] = createSignal<AgentSpaceSummary[]>([]);
  const [spaceFilter, setSpaceFilter] = createSignal<string>('');
  const [sortBy, setSortBy] = createSignal<'activity' | 'quality'>('activity');
  const [ciDrawerRun, setCiDrawerRun] = createSignal<AgentFleetSummary | null>(null);

  const load = async () => {
    try {
      setError(null);
      const spaceId = spaceFilter();
      const items = await fetchAgentFleet({
        ...(spaceId ? { spaceId } : {}),
        sortBy: view() === 'list' && sortBy() === 'quality' ? 'quality' : undefined,
      });
      setRuns(items);
    } catch (e) {
      setError(e instanceof Error ? e.message : 'load failed');
    } finally {
      setLoading(false);
    }
  };

  const loadSpaces = async () => {
    try {
      setSpaces(await fetchAgentSpaces());
    } catch {
      setSpaces([]);
    }
  };

  const applySnapshot = (items: AgentFleetSummary[]) => {
    setRuns(items);
    setLoading(false);
  };

  const applyStatus = (runId: string, status: string, stage: string) => {
    setRuns((prev) =>
      prev.map((r) =>
        r.runId === runId
          ? { ...r, status: status as AgentFleetStatus, stage, lastActivityAtUtc: new Date().toISOString() }
          : r,
      ),
    );
  };

  onMount(() => {
    void loadSpaces();
    void load();
    const stopSse = subscribeAgentFleetEvents((evt) => {
      if (evt.type === 'snapshot') applySnapshot(evt.items);
      if (evt.type === 'status') applyStatus(evt.runId, evt.status, evt.stage);
    });

    const onKey = (e: KeyboardEvent) => {
      const items = filtered();
      if (items.length === 0) return;
      if (e.key === 'j') {
        e.preventDefault();
        setSelectedIndex((i) => Math.min(i + 1, items.length - 1));
      }
      if (e.key === 'k') {
        e.preventDefault();
        setSelectedIndex((i) => Math.max(i - 1, 0));
      }
      if (e.key === 'Enter') {
        const run = items[selectedIndex()];
        if (run) navigate(`/ide/runs/${run.runId}`);
      }
      if (e.key === 'c') {
        const run = items[selectedIndex()];
        if (run && !isTerminal(run.status)) void cancelFleetRun(run.runId).then(load);
      }
    };
    window.addEventListener('keydown', onKey);

    onCleanup(() => {
      stopSse();
      stopAgentFleetEvents();
      window.removeEventListener('keydown', onKey);
    });
  });

  const filtered = () => {
    const items = runs();
    if (filter() === 'running') return items.filter((r) => !isTerminal(r.status));
    if (filter() === 'done') return items.filter((r) => isTerminal(r.status));
    return items;
  };

  const byColumn = (status: AgentFleetStatus) => filtered().filter((r) => r.status === status);
  const uncategorized = () => filtered().filter((r) => !STATUS_COLUMNS.includes(r.status));

  const openRun = (runId: string) => navigate(`/ide/runs/${runId}`);

  const RunCard: Component<{ run: AgentFleetSummary; highlight?: boolean }> = (props) => (
    <article
      data-testid={`fleet-card-${props.run.runId}`}
      class={[
        'rounded border p-2 text-xs space-y-1 cursor-pointer transition-colors',
        statusColor(props.run.status),
        props.highlight ? 'ring-1 ring-secondary' : '',
      ].join(' ')}
      onClick={() => openRun(props.run.runId)}
    >
      <div class="font-medium truncate">{props.run.title}</div>
      <div class="text-[10px] text-muted-foreground truncate">{props.run.stage}</div>
      <Show when={props.run.backendKind && props.run.backendKind !== 'Libr4Native'}>
        <span class="inline-block text-[9px] uppercase tracking-wide px-1.5 py-0.5 rounded border border-secondary/40 text-secondary">
          {props.run.backendKind}
        </span>
      </Show>
      <Show when={props.run.backendFallbackFrom}>
        <span class="inline-block text-[9px] uppercase tracking-wide px-1.5 py-0.5 rounded border border-amber-500/40 text-amber-400">
          fallback from {props.run.backendFallbackFrom}
        </span>
      </Show>
      <Show when={props.run.prUrl}>
        <a
          href={props.run.prUrl!}
          target="_blank"
          rel="noreferrer"
          class="inline-block text-[9px] uppercase tracking-wide px-1.5 py-0.5 rounded border border-blue-500/40 text-blue-400 hover:underline"
          onClick={(e) => e.stopPropagation()}
        >
          PR #{props.run.prNumber ?? '…'}
        </a>
      </Show>
      <Show when={props.run.ciStatus && props.run.ciStatus !== 'none'}>
        <button
          type="button"
          data-testid={`fleet-ci-badge-${props.run.runId}`}
          class={[
            'inline-block text-[9px] uppercase tracking-wide px-1.5 py-0.5 rounded border',
            props.run.ciStatus === 'success'
              ? 'border-success/40 text-success hover:bg-success/10'
              : props.run.ciStatus === 'failure'
                ? 'border-error/40 text-error hover:bg-error/10'
                : 'border-amber-500/40 text-amber-400 hover:bg-amber-500/10',
          ].join(' ')}
          onClick={(e) => {
            e.stopPropagation();
            setCiDrawerRun(props.run);
          }}
        >
          CI {props.run.ciStatus}
        </button>
      </Show>
      <Show when={(props.run.playbookAttempts ?? 0) > 0}>
        <span
          class="inline-block text-[9px] uppercase tracking-wide px-1.5 py-0.5 rounded border border-violet-500/40 text-violet-300"
          title="Repair playbook hit rate"
        >
          PB {props.run.playbookHits ?? 0}/{props.run.playbookAttempts ?? 0}
        </span>
      </Show>
      <Show when={(props.run.qualityScore ?? 0) > 0}>
        <span
          class="inline-block text-[9px] uppercase tracking-wide px-1.5 py-0.5 rounded border border-emerald-500/40 text-emerald-300"
          title="Composite run quality score"
          data-testid={`fleet-quality-${props.run.runId}`}
        >
          Q {props.run.qualityScore}
        </span>
      </Show>
      <Show when={props.run.status === 'HandoffPending' || props.run.status === 'HandoffComplete'}>
        <span
          class={[
            'inline-block text-[9px] uppercase tracking-wide px-1.5 py-0.5 rounded border',
            props.run.status === 'HandoffPending'
              ? 'border-amber-500/40 text-amber-400'
              : 'border-success/40 text-success',
          ].join(' ')}
        >
          {props.run.status === 'HandoffPending' ? 'Handoff pending' : 'Handoff complete'}
        </span>
      </Show>
      <div class="flex items-center gap-2 text-[10px] text-muted-foreground">
        <span>{props.run.agentCount} agents</span>
        <span class="ml-auto font-mono">{props.run.runId.slice(0, 8)}</span>
      </div>
      <Show when={!isTerminal(props.run.status)}>
        <div class="flex justify-end pt-1" onClick={(e) => e.stopPropagation()}>
          <button
            type="button"
            class="text-[10px] text-error hover:underline"
            onClick={() => void cancelFleetRun(props.run.runId).then(load)}
          >
            Cancel
          </button>
        </div>
      </Show>
    </article>
  );

  return (
    <div data-testid="agent-board" class="h-screen w-screen flex flex-col bg-background text-foreground">
      <header class="flex items-center gap-3 px-4 py-3 border-b border-surface-3 shrink-0 flex-wrap">
        <button type="button" class="text-xs text-secondary hover:underline" onClick={() => navigate('/ide')}>
          ← IDE
        </button>
        <button type="button" class="text-xs text-secondary hover:underline" onClick={() => navigate('/ide/history')}>
          History
        </button>
        <h1 data-testid="agent-board-title" class="text-sm font-semibold">Agent Board</h1>
        <select
          data-testid="fleet-space-filter"
          class="text-xs border border-surface-3 rounded px-2 py-1 bg-surface-1 max-w-[10rem] truncate"
          value={spaceFilter()}
          onChange={(e) => {
            setSpaceFilter(e.currentTarget.value);
            setLoading(true);
            void load();
          }}
        >
          <option value="">All spaces</option>
          <For each={spaces()}>{(space) => (
            <option value={space.spaceId}>{space.name}</option>
          )}</For>
        </select>
        <Show when={spaceFilter()}>
          <button
            type="button"
            data-testid="open-space-detail"
            class="text-xs text-secondary hover:underline"
            onClick={() => navigate(`/ide/spaces/${spaceFilter()}`)}
          >
            Space detail →
          </button>
        </Show>
        <div class="flex gap-1">
          {(['all', 'running', 'done'] as const).map((f) => (
            <button
              type="button"
              class={[
                'px-2 py-1 text-xs rounded border',
                filter() === f ? 'border-secondary text-secondary' : 'border-surface-3 text-muted-foreground',
              ].join(' ')}
              onClick={() => setFilter(f)}
            >
              {f === 'all' ? 'All' : f === 'running' ? 'Running' : 'Done'}
            </button>
          ))}
        </div>
        <div class="flex gap-1">
          <button
            type="button"
            class={['px-2 py-1 text-xs rounded border', view() === 'board' ? 'border-secondary text-secondary' : 'border-surface-3 text-muted-foreground'].join(' ')}
            onClick={() => setView('board')}
          >
            Board
          </button>
          <button
            type="button"
            class={['px-2 py-1 text-xs rounded border', view() === 'list' ? 'border-secondary text-secondary' : 'border-surface-3 text-muted-foreground'].join(' ')}
            onClick={() => setView('list')}
          >
            List
          </button>
        </div>
        <Show when={view() === 'list'}>
          <select
            data-testid="fleet-sort-by"
            class="text-xs border border-surface-3 rounded px-2 py-1 bg-surface-1"
            value={sortBy()}
            onChange={(e) => {
              setSortBy(e.currentTarget.value as 'activity' | 'quality');
              setLoading(true);
              void load();
            }}
          >
            <option value="activity">Sort: activity</option>
            <option value="quality">Sort: quality</option>
          </select>
        </Show>
        <button
          type="button"
          class="text-[10px] text-muted-foreground hover:text-foreground border border-surface-3 px-2 py-1 rounded"
          onClick={() => void bulkArchiveFleetRuns(7).then(load)}
        >
          Archive done &gt;7d
        </button>
        <button
          type="button"
          class="text-[10px] bg-secondary/10 text-secondary border border-secondary/30 px-2 py-1 rounded"
          onClick={() => navigate('/ide')}
        >
          Start generation
        </button>
        <span class="text-xs text-muted-foreground ml-auto hidden sm:inline">j/k · Enter · c</span>
        <span class="text-xs text-muted-foreground">{filtered().length} runs</span>
      </header>

      <Show when={error()}>
        <div class="px-4 py-2 text-xs text-error">{error()}</div>
      </Show>

      <Show when={loading()} fallback={
        <Show
          when={filtered().length > 0}
          fallback={
            <div class="flex-1 flex flex-col items-center justify-center gap-3 p-8 text-center">
              <p class="text-sm text-muted-foreground">Нет agent runs. Запустите генерацию из IDE.</p>
              <button
                type="button"
                class="px-4 py-2 text-xs rounded bg-secondary/10 text-secondary border border-secondary/30"
                onClick={() => navigate('/ide')}
              >
                Start new generation
              </button>
            </div>
          }
        >
          <Show when={view() === 'board'} fallback={
            <div class="flex-1 overflow-auto p-4">
              <table class="w-full text-xs">
                <thead>
                  <tr class="text-left text-muted-foreground border-b border-surface-3">
                    <th class="py-2 pr-4">Title</th>
                    <th class="py-2 pr-4">Status</th>
                    <th class="py-2 pr-4">Quality</th>
                    <th class="py-2 pr-4">Stage</th>
                    <th class="py-2 pr-4">Agents</th>
                    <th class="py-2">Updated</th>
                  </tr>
                </thead>
                <tbody>
                  <For each={filtered()}>{(run, i) => (
                    <tr
                      class={[
                        'border-b border-surface-3 cursor-pointer hover:bg-surface-2',
                        selectedIndex() === i() ? 'bg-secondary/5' : '',
                      ].join(' ')}
                      onClick={() => openRun(run.runId)}
                    >
                      <td class="py-2 pr-4 font-medium">{run.title}</td>
                      <td class="py-2 pr-4">{run.status}</td>
                      <td class="py-2 pr-4 text-emerald-300">{(run.qualityScore ?? 0) > 0 ? run.qualityScore : '—'}</td>
                      <td class="py-2 pr-4 text-muted-foreground">{run.stage}</td>
                      <td class="py-2 pr-4">{run.agentCount}</td>
                      <td class="py-2 text-muted-foreground">{new Date(run.lastActivityAtUtc).toLocaleString()}</td>
                    </tr>
                  )}</For>
                </tbody>
              </table>
            </div>
          }>
            <div class="flex-1 overflow-x-auto p-4 md:p-4">
              <div class="flex flex-col md:flex-row gap-3 md:min-w-max h-full">
                <For each={STATUS_COLUMNS}>{(col) => (
                  <section data-testid={`fleet-column-${col}`} class="w-full md:w-56 flex flex-col shrink-0">
                    <h2 class="text-[10px] uppercase tracking-wider text-muted-foreground mb-2 px-1">
                      {statusLabel[col] ?? col}
                      <span class="ml-1">({byColumn(col).length})</span>
                    </h2>
                    <div class="flex-1 space-y-2 overflow-y-auto max-h-64 md:max-h-none">
                      <For each={byColumn(col)}>{(run) => <RunCard run={run} />}</For>
                    </div>
                  </section>
                )}</For>
                <Show when={uncategorized().length > 0}>
                  <section class="w-full md:w-56 flex flex-col shrink-0">
                    <h2 class="text-[10px] uppercase tracking-wider text-muted-foreground mb-2 px-1">
                      Other ({uncategorized().length})
                    </h2>
                    <div class="space-y-2">
                      <For each={uncategorized()}>{(run) => <RunCard run={run} />}</For>
                    </div>
                  </section>
                </Show>
              </div>
            </div>
          </Show>
        </Show>
      }>
        <div class="flex-1 flex items-center justify-center text-muted-foreground text-sm">Loading fleet…</div>
      </Show>

      <CiLogDrawer
        run={ciDrawerRun()}
        open={ciDrawerRun() != null}
        onClose={() => setCiDrawerRun(null)}
      />
    </div>
  );
};

export default AgentBoard;
