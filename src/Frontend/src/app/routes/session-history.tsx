import { createSignal, For, onMount, Show, type Component } from 'solid-js';
import { useNavigate } from '@solidjs/router';
import {
  fetchAgentFleet,
  patchFleetRun,
  type AgentFleetSummary,
} from '../../features/IDE/services/agentFleet';
import {
  forkFleetRun,
  searchFleetSessions,
  type FleetSessionSearchHit,
} from '../../features/IDE/services/fleetHistory';

const SessionHistoryPage: Component = () => {
  const navigate = useNavigate();
  const [query, setQuery] = createSignal('');
  const [hits, setHits] = createSignal<FleetSessionSearchHit[]>([]);
  const [recent, setRecent] = createSignal<AgentFleetSummary[]>([]);
  const [loading, setLoading] = createSignal(true);
  const [error, setError] = createSignal<string | null>(null);
  const [stackFilter, setStackFilter] = createSignal('');
  const [outcomeFilter, setOutcomeFilter] = createSignal('');

  const loadRecent = async () => {
    try {
      setError(null);
      const items = await fetchAgentFleet();
      setRecent(items.slice(0, 30));
    } catch (e) {
      setError(e instanceof Error ? e.message : 'load failed');
    } finally {
      setLoading(false);
    }
  };

  const runSearch = async () => {
    const q = query().trim();
    if (!q) {
      setHits([]);
      return;
    }
    try {
      setError(null);
      setLoading(true);
      const result = await searchFleetSessions({
        q,
        stack: stackFilter() || undefined,
        outcome: outcomeFilter() || undefined,
      });
      setHits(result.hits);
    } catch (e) {
      setError(e instanceof Error ? e.message : 'search failed');
    } finally {
      setLoading(false);
    }
  };

  onMount(() => {
    const token = localStorage.getItem('accessToken');
    if (!token) {
      navigate('/auth');
      return;
    }
    void loadRecent();
  });

  const displayItems = () => (query().trim() ? hits() : recent().map(toHitFromSummary));

  return (
    <div data-testid="session-history" class="h-screen w-screen flex flex-col bg-background text-foreground">
      <header class="border-b border-surface-3 px-4 py-3 flex items-center gap-3">
        <button type="button" class="text-xs text-secondary hover:underline" onClick={() => navigate('/ide/agent-board')}>
          ← Board
        </button>
        <h1 class="text-sm font-semibold">Session History</h1>
      </header>

      <div class="p-4 border-b border-surface-3 flex flex-wrap gap-2 items-center">
        <input
          data-testid="history-search"
          class="flex-1 min-w-[200px] rounded border border-surface-3 bg-surface-1 px-2 py-1 text-xs"
          placeholder="Search runs, errors, files…"
          value={query()}
          onInput={(e) => setQuery(e.currentTarget.value)}
          onKeyDown={(e) => e.key === 'Enter' && void runSearch()}
        />
        <select
          class="rounded border border-surface-3 bg-surface-1 px-2 py-1 text-xs"
          value={stackFilter()}
          onChange={(e) => setStackFilter(e.currentTarget.value)}
        >
          <option value="">All stacks</option>
          <option value="django">django</option>
          <option value="spring">spring</option>
          <option value="dotnet">dotnet</option>
        </select>
        <select
          class="rounded border border-surface-3 bg-surface-1 px-2 py-1 text-xs"
          value={outcomeFilter()}
          onChange={(e) => setOutcomeFilter(e.currentTarget.value)}
        >
          <option value="">All outcomes</option>
          <option value="pass">pass</option>
          <option value="fail">fail</option>
          <option value="running">running</option>
        </select>
        <button
          type="button"
          class="rounded border border-secondary/40 px-3 py-1 text-xs text-secondary hover:bg-secondary/10"
          onClick={() => void runSearch()}
        >
          Search
        </button>
      </div>

      <Show when={error()}>
        <div class="px-4 py-2 text-xs text-error">{error()}</div>
      </Show>

      <div class="flex-1 overflow-auto p-4 space-y-2">
        <Show when={loading()} fallback={null}>
          <div class="text-xs text-muted-foreground">Loading…</div>
        </Show>
        <For each={displayItems()}>
          {(item) => (
            <article
              class="rounded border border-surface-3 p-3 text-xs space-y-1 cursor-pointer hover:bg-surface-1"
              onClick={() => navigate(`/ide/runs/${item.runId}`)}
            >
              <div class="flex items-center gap-2">
                <span class="font-medium truncate flex-1">{item.title}</span>
                <span class="text-[10px] uppercase text-muted-foreground">{item.status}</span>
              </div>
              <Show when={item.snippet}>
                <p class="text-muted-foreground line-clamp-2">{item.snippet}</p>
              </Show>
              <div class="flex gap-2 pt-1">
                <button
                  type="button"
                  class="text-[10px] text-secondary hover:underline"
                  onClick={(e) => {
                    e.stopPropagation();
                    void patchFleetRun(item.runId, { pinned: true });
                  }}
                >
                  Pin
                </button>
                <button
                  type="button"
                  class="text-[10px] text-secondary hover:underline"
                  onClick={(e) => {
                    e.stopPropagation();
                    void forkFleetRun(item.runId).then((r) => navigate(`/ide/runs/${r.newRunId}`));
                  }}
                >
                  Fork
                </button>
              </div>
            </article>
          )}
        </For>
      </div>
    </div>
  );
};

function toHitFromSummary(run: AgentFleetSummary): FleetSessionSearchHit {
  return {
    runId: run.runId,
    title: run.title,
    status: run.status,
    stack: null,
    spaceId: null,
    snippet: run.stage,
    score: 0,
    lastActivityAtUtc: run.lastActivityAtUtc,
    pinned: run.pinned,
  };
}

export default SessionHistoryPage;
