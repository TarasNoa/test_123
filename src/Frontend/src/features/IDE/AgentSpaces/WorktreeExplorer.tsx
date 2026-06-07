import {
  createSignal,
  For,
  onMount,
  Show,
  type Component,
} from 'solid-js';
import { useNavigate, useParams } from '@solidjs/router';
import { fetchWorktreeFiles, type WorktreeFileEntry } from '../services/agentSpaces';

export const WorktreeExplorer: Component = () => {
  const params = useParams();
  const navigate = useNavigate();
  const spaceId = () => params.spaceId ?? '';
  const memberId = () => params.memberId ?? '';

  const [path, setPath] = createSignal('');
  const [worktreePath, setWorktreePath] = createSignal('');
  const [entries, setEntries] = createSignal<WorktreeFileEntry[]>([]);
  const [loading, setLoading] = createSignal(true);
  const [error, setError] = createSignal<string | null>(null);

  const load = async (relativePath = '') => {
    const sid = spaceId();
    const mid = memberId();
    if (!sid || !mid) return;
    setLoading(true);
    setError(null);
    try {
      const listing = await fetchWorktreeFiles(sid, mid, relativePath || undefined);
      if (!listing) {
        setError('Worktree not found');
        setEntries([]);
        return;
      }
      setWorktreePath(listing.worktreePath);
      setPath(listing.relativePath);
      setEntries(listing.entries);
    } catch (e) {
      setError(e instanceof Error ? e.message : 'load failed');
    } finally {
      setLoading(false);
    }
  };

  onMount(() => void load());

  const breadcrumbs = () => {
    const parts = path().split('/').filter(Boolean);
    const crumbs: { label: string; target: string }[] = [{ label: 'root', target: '' }];
    let acc = '';
    for (const part of parts) {
      acc = acc ? `${acc}/${part}` : part;
      crumbs.push({ label: part, target: acc });
    }
    return crumbs;
  };

  const openEntry = (entry: WorktreeFileEntry) => {
    if (entry.isDirectory) {
      void load(entry.relativePath);
      return;
    }
    // File preview is out of scope — show path for now.
    setError(`File: ${entry.relativePath} (${entry.sizeBytes ?? 0} bytes)`);
  };

  return (
    <div data-testid="worktree-explorer" class="h-screen w-screen flex flex-col bg-background text-foreground">
      <header class="flex items-center gap-3 px-4 py-3 border-b border-surface-3 shrink-0 flex-wrap">
        <button
          type="button"
          class="text-xs text-secondary hover:underline"
          onClick={() => navigate(`/ide/spaces/${spaceId()}`)}
        >
          ← Space
        </button>
        <h1 class="text-sm font-semibold">Worktree · {memberId()}</h1>
        <span class="text-[10px] text-muted-foreground font-mono truncate max-w-md" title={worktreePath()}>
          {worktreePath()}
        </span>
      </header>

      <div class="px-4 py-2 border-b border-surface-3 flex flex-wrap gap-1 text-xs">
        <For each={breadcrumbs()}>{(crumb, i) => (
          <>
            <Show when={i() > 0}>
              <span class="text-muted-foreground">/</span>
            </Show>
            <button
              type="button"
              class="text-secondary hover:underline"
              onClick={() => void load(crumb.target)}
            >
              {crumb.label}
            </button>
          </>
        )}</For>
      </div>

      <Show when={loading()}>
        <div class="p-4 text-xs text-muted-foreground">Loading files…</div>
      </Show>

      <Show when={error()}>
        <div class="px-4 py-2 text-xs text-error border-b border-surface-3">{error()}</div>
      </Show>

      <div class="flex-1 overflow-auto p-4">
        <ul class="space-y-1 text-xs">
          <For each={entries()}>{(entry) => (
            <li>
              <button
                type="button"
                data-testid={`worktree-entry-${entry.name}`}
                class="w-full text-left px-2 py-1 rounded hover:bg-surface-2 flex items-center gap-2"
                onClick={() => openEntry(entry)}
              >
                <span>{entry.isDirectory ? '📁' : '📄'}</span>
                <span class="font-mono">{entry.name}</span>
                <Show when={!entry.isDirectory && entry.sizeBytes != null}>
                  <span class="ml-auto text-muted-foreground">{entry.sizeBytes} B</span>
                </Show>
              </button>
            </li>
          )}</For>
        </ul>
        <Show when={!loading() && entries().length === 0}>
          <p class="text-xs text-muted-foreground">Empty directory</p>
        </Show>
      </div>
    </div>
  );
};
