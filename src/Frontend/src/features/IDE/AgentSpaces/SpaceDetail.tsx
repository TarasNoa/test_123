import {
  createSignal,
  For,
  onMount,
  Show,
  type Component,
} from 'solid-js';
import { useNavigate, useParams } from '@solidjs/router';
import {
  fetchAgentSpaceDetail,
  fetchMergePreview,
  mergeSpaceMember,
  orchestrateSpace,
  type MergePreviewResult,
  type SpaceContextEventSummary,
  type SpaceMemberSummary,
} from '../services/agentSpaces';

const roleColor = (role: string) => {
  switch (role.toLowerCase()) {
    case 'explorer':
      return 'border-info/40 bg-info/5';
    case 'implementer':
      return 'border-secondary/50 bg-secondary/5';
    case 'verifier':
      return 'border-warning/40 bg-warning/5';
    default:
      return 'border-surface-3 bg-surface-1';
  }
};

export const SpaceDetail: Component = () => {
  const params = useParams();
  const navigate = useNavigate();
  const spaceId = () => params.spaceId ?? '';

  const [loading, setLoading] = createSignal(true);
  const [error, setError] = createSignal<string | null>(null);
  const [name, setName] = createSignal('');
  const [integrationBranch, setIntegrationBranch] = createSignal('');
  const [members, setMembers] = createSignal<SpaceMemberSummary[]>([]);
  const [context, setContext] = createSignal<SpaceContextEventSummary[]>([]);
  const [busyMember, setBusyMember] = createSignal<string | null>(null);
  const [previewMember, setPreviewMember] = createSignal<string | null>(null);
  const [mergePreview, setMergePreview] = createSignal<MergePreviewResult | null>(null);
  const [previewLoading, setPreviewLoading] = createSignal(false);
  const [orchestrating, setOrchestrating] = createSignal(false);
  const [mergeResult, setMergeResult] = createSignal<string | null>(null);

  const load = async () => {
    const id = spaceId();
    if (!id) return;
    try {
      setError(null);
      setLoading(true);
      const detail = await fetchAgentSpaceDetail(id);
      if (!detail) {
        setError('Space not found');
        return;
      }
      setName(detail.space.name);
      setIntegrationBranch(detail.space.integrationBranch);
      setMembers(detail.members);
      setContext(detail.recentContext);
    } catch (e) {
      setError(e instanceof Error ? e.message : 'load failed');
    } finally {
      setLoading(false);
    }
  };

  onMount(() => void load());

  const handleMerge = async (memberId: string) => {
    setBusyMember(memberId);
    setMergeResult(null);
    try {
      const result = await mergeSpaceMember(spaceId(), memberId);
      setMergeResult(
        result.success
          ? `Merged into ${result.integrationBranch}`
          : `Conflict: ${result.conflicts.join(', ') || result.output || 'merge failed'}`
      );
      setPreviewMember(null);
      setMergePreview(null);
      await load();
    } catch (e) {
      setMergeResult(e instanceof Error ? e.message : 'merge failed');
    } finally {
      setBusyMember(null);
    }
  };

  const handlePreviewMerge = async (memberId: string) => {
    if (previewMember() === memberId) {
      setPreviewMember(null);
      setMergePreview(null);
      return;
    }

    setPreviewMember(memberId);
    setPreviewLoading(true);
    setMergeResult(null);
    try {
      const preview = await fetchMergePreview(spaceId(), memberId);
      setMergePreview(preview);
    } catch (e) {
      setMergeResult(e instanceof Error ? e.message : 'preview failed');
      setPreviewMember(null);
      setMergePreview(null);
    } finally {
      setPreviewLoading(false);
    }
  };

  const handleOrchestrate = async () => {
    setOrchestrating(true);
    setMergeResult(null);
    try {
      const result = await orchestrateSpace(spaceId(), {
        explorerTask: 'Research requirements and publish plan',
        implementerTask: 'Implement based on shared context',
        verifierTask: 'Verify on integration branch',
      });
      setMergeResult(`Pipeline stage: ${result.stage}`);
      await load();
    } catch (e) {
      setMergeResult(e instanceof Error ? e.message : 'orchestrate failed');
    } finally {
      setOrchestrating(false);
    }
  };

  return (
    <div data-testid="space-detail" class="h-screen w-screen flex flex-col bg-background text-foreground">
      <header class="flex items-center gap-3 px-4 py-3 border-b border-surface-3 shrink-0 flex-wrap">
        <button type="button" class="text-xs text-secondary hover:underline" onClick={() => navigate('/ide/agent-board')}>
          ← Board
        </button>
        <h1 data-testid="space-detail-title" class="text-sm font-semibold">
          Space: {name() || spaceId()}
        </h1>
        <span class="text-xs text-muted-foreground">integration → {integrationBranch()}</span>
        <button
          type="button"
          data-testid="space-orchestrate-btn"
          class="ml-auto text-xs px-2 py-1 rounded border border-secondary text-secondary disabled:opacity-50"
          disabled={orchestrating()}
          onClick={() => void handleOrchestrate()}
        >
          {orchestrating() ? 'Running…' : 'Run pipeline'}
        </button>
      </header>

      <Show when={loading()}>
        <div class="p-4 text-xs text-muted-foreground">Loading space…</div>
      </Show>

      <Show when={error()}>
        <div class="p-4 text-xs text-error">{error()}</div>
      </Show>

      <Show when={mergeResult()}>
        <div class="px-4 py-2 text-xs border-b border-surface-3 bg-surface-1">{mergeResult()}</div>
      </Show>

      <div class="flex-1 overflow-auto p-4 grid gap-4 lg:grid-cols-[1fr_18rem]">
        <section>
          <h2 class="text-xs font-semibold text-muted-foreground mb-2">Members</h2>
          <div class="grid gap-2 sm:grid-cols-2">
            <For each={members()}>
              {(member) => (
                <article
                  data-testid={`space-member-${member.memberId}`}
                  class={['rounded border p-3 text-xs', roleColor(member.role)].join(' ')}
                >
                  <div class="flex items-center justify-between gap-2 mb-1">
                    <span class="font-semibold capitalize">{member.role}</span>
                    <span class="text-muted-foreground">{member.status}</span>
                  </div>
                  <div class="text-muted-foreground truncate" title={member.branchName}>
                    branch: {member.branchName}
                  </div>
                  <div class="text-muted-foreground truncate" title={member.worktreePath}>
                    {member.worktreePath}
                  </div>
                  <Show when={member.runId}>
                    <button
                      type="button"
                      class="mt-2 text-secondary hover:underline"
                      onClick={() => navigate(`/ide/runs/${member.runId}`)}
                    >
                      Open run →
                    </button>
                  </Show>
                  <button
                    type="button"
                    data-testid={`open-worktree-${member.memberId}`}
                    class="mt-2 block text-secondary hover:underline"
                    onClick={() => navigate(`/ide/spaces/${spaceId()}/worktree/${member.memberId}`)}
                  >
                    Open in worktree →
                  </button>
                  <Show when={member.status !== 'Completed' && member.status !== 'Removed'}>
                    <div class="mt-2 flex flex-wrap gap-2">
                      <button
                        type="button"
                        data-testid={`preview-merge-${member.memberId}`}
                        class="text-xs px-2 py-1 rounded border border-secondary/40 text-secondary disabled:opacity-50"
                        disabled={previewLoading() && previewMember() === member.memberId}
                        onClick={() => void handlePreviewMerge(member.memberId)}
                      >
                        {previewMember() === member.memberId ? 'Hide preview' : 'Preview merge'}
                      </button>
                      <button
                        type="button"
                        data-testid={`merge-member-${member.memberId}`}
                        class="text-xs px-2 py-1 rounded border border-surface-3 disabled:opacity-50"
                        disabled={busyMember() === member.memberId}
                        onClick={() => void handleMerge(member.memberId)}
                      >
                        {busyMember() === member.memberId ? 'Merging…' : 'Merge to integration'}
                      </button>
                    </div>
                  </Show>
                  <Show when={previewMember() === member.memberId && mergePreview()}>
                    {(preview) => (
                      <div
                        data-testid={`merge-preview-${member.memberId}`}
                        class="mt-2 border border-surface-3 rounded p-2 bg-surface-1 space-y-2"
                      >
                        <div class="text-muted-foreground">
                          {preview().sourceBranch} → {preview().integrationBranch}
                        </div>
                        <pre class="text-[10px] whitespace-pre-wrap text-muted-foreground">{preview().diffStat}</pre>
                        <ul class="space-y-1 max-h-24 overflow-auto">
                          <For each={preview().files}>{(file) => (
                            <li class="font-mono text-[10px]">
                              {file.changeKind} {file.path}
                              {' '}(+{file.insertions}/-{file.deletions})
                            </li>
                          )}</For>
                        </ul>
                        <pre class="text-[10px] whitespace-pre-wrap overflow-auto max-h-48 bg-surface-2 rounded p-2">
                          {preview().unifiedDiff || 'No diff'}
                        </pre>
                      </div>
                    )}
                  </Show>
                </article>
              )}
            </For>
          </div>
          <Show when={!loading() && members().length === 0}>
            <p class="text-xs text-muted-foreground">No agents spawned yet. Run pipeline or spawn from API.</p>
          </Show>
        </section>

        <aside>
          <h2 class="text-xs font-semibold text-muted-foreground mb-2">Context timeline</h2>
          <ul class="space-y-2">
            <For each={context()}>
              {(evt) => (
                <li class="text-xs border border-surface-3 rounded p-2 bg-surface-1">
                  <div class="font-medium">{evt.title}</div>
                  <div class="text-muted-foreground">{evt.kind}</div>
                  <Show when={evt.payload}>
                    <pre class="mt-1 whitespace-pre-wrap text-[10px] text-muted-foreground max-h-24 overflow-auto">
                      {evt.payload}
                    </pre>
                  </Show>
                </li>
              )}
            </For>
          </ul>
        </aside>
      </div>
    </div>
  );
};
