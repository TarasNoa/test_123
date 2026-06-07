import {
  createEffect,
  createMemo,
  createSignal,
  For,
  onCleanup,
  onMount,
  Show,
  type Component,
} from 'solid-js';
import {
  fetchDiffDetail,
  fetchDiffEvidence,
  type DiffPathOverlay,
  type GeneratedFileSummary,
  type RunDiffSummary,
} from '../services/runSession';
import type { RunReviewStatus } from '../services/runReview';
import { submitRunReview } from '../services/runReview';
import { createFleetPullRequest, type AgentFleetSummary } from '../services/agentFleet';
import { ConsoleErrorPanel } from './ConsoleErrorPanel';
import { EvidenceFilmstrip } from './EvidenceFilmstrip';
import { RepairRequestDialog } from './RepairRequestDialog';
import { ReviewDiffViewer } from './ReviewDiffViewer';
import {
  overlayFor as findOverlay,
  pathMatches,
  parseConsoleErrors,
  toFilmstripItems,
  type ConsoleErrorEntry,
  type FilmstripItem,
} from './reviewUtils';

const overlayBadge = (kinds: string[]) => {
  if (kinds.includes('security_flag')) return { label: 'security', class: 'text-red-400' };
  if (kinds.includes('verify_console')) return { label: 'verify fail', class: 'text-amber-400' };
  return null;
};

const fileDecision = (status: RunReviewStatus | null | undefined, path: string) =>
  status?.files.find((f) => pathMatches(f.path, path))?.decision ?? null;

export const DiffPanel: Component<{
  runId: string;
  files: GeneratedFileSummary[];
  loading: boolean;
  runDiffs?: RunDiffSummary[];
  diffOverlays?: DiffPathOverlay[];
  reviewStatus?: RunReviewStatus | null;
  fleetEntry?: AgentFleetSummary | null;
  onFleetRefresh?: () => void;
  consoleErrors?: ConsoleErrorEntry[];
  onReviewUpdated?: (status: RunReviewStatus) => void;
}> = (props) => {
  const [selected, setSelected] = createSignal<string | null>(null);
  const [checked, setChecked] = createSignal<Set<string>>(new Set());
  const [reviewBusy, setReviewBusy] = createSignal(false);
  const [diffDetail, setDiffDetail] = createSignal<string | null>(null);
  const [filmstrip, setFilmstrip] = createSignal<FilmstripItem[]>([]);
  const [repairOpen, setRepairOpen] = createSignal(false);
  const [prBusy, setPrBusy] = createSignal(false);
  const [prError, setPrError] = createSignal<string | null>(null);

  const reviewApproved = () => props.reviewStatus?.status === 'Approved';
  const prUrl = () => props.fleetEntry?.prUrl ?? null;

  const fileList = () => props.files;
  const activeIndex = () => {
    const list = fileList();
    const sel = selected();
    if (sel) {
      const idx = list.findIndex((f) => f.relativePath === sel);
      if (idx >= 0) return idx;
    }
    return 0;
  };
  const active = () => fileList()[activeIndex()] ?? null;

  const diffFor = (path: string) =>
    props.runDiffs?.find((d) => pathMatches(d.path, path));

  const activeOverlay = createMemo(() => {
    const file = active();
    if (!file) return null;
    return findOverlay(props.diffOverlays, file.relativePath);
  });

  const activeStep = createMemo(() => diffFor(active()?.relativePath ?? '')?.stepNumber ?? null);

  const toggleCheck = (path: string) => {
    setChecked((prev) => {
      const next = new Set(prev);
      if (next.has(path)) next.delete(path);
      else next.add(path);
      return next;
    });
  };

  const selectedPaths = () => {
    const c = checked();
    if (c.size > 0) return [...c];
    const file = active();
    return file ? [file.relativePath] : [];
  };

  const loadFileContext = async (path: string) => {
    const [detail, evidence] = await Promise.all([
      fetchDiffDetail(props.runId, path).catch(() => null),
      fetchDiffEvidence(props.runId, path).catch(() => null),
    ]);
    setDiffDetail(detail?.unifiedDiff ?? null);
    setFilmstrip(toFilmstripItems(evidence?.items ?? []));
  };

  createEffect(() => {
    const file = active();
    if (file) void loadFileContext(file.relativePath);
  });

  const submitDecision = async (decision: string, paths?: string[]) => {
    const targets = paths ?? selectedPaths();
    if (!targets.length || reviewBusy()) return;
    setReviewBusy(true);
    try {
      const status = await submitRunReview(props.runId, decision, targets);
      props.onReviewUpdated?.(status);
      setChecked(new Set<string>());
    } finally {
      setReviewBusy(false);
    }
  };

  const batchApprove = () => void submitDecision('Approve', props.files.map((f) => f.relativePath));

  const openOrCreatePr = async () => {
    const url = prUrl();
    if (url) {
      window.open(url, '_blank', 'noopener,noreferrer');
      return;
    }
    if (prBusy()) return;
    setPrError(null);
    setPrBusy(true);
    try {
      const result = await createFleetPullRequest(props.runId);
      if (result.pullRequestUrl) window.open(result.pullRequestUrl, '_blank', 'noopener,noreferrer');
      props.onFleetRefresh?.();
    } catch (e) {
      setPrError(e instanceof Error ? e.message : 'create PR failed');
    } finally {
      setPrBusy(false);
    }
  };

  const goRelative = (delta: number) => {
    const list = fileList();
    if (!list.length) return;
    const next = (activeIndex() + delta + list.length) % list.length;
    setSelected(list[next].relativePath);
  };

  const jumpToPath = (path: string) => {
    const match = fileList().find((f) => pathMatches(f.relativePath, path));
    if (match) setSelected(match.relativePath);
  };

  onMount(() => {
    const onKey = (e: KeyboardEvent) => {
      const tag = (e.target as HTMLElement)?.tagName;
      if (tag === 'INPUT' || tag === 'TEXTAREA' || (e.target as HTMLElement)?.isContentEditable) return;
      if (repairOpen()) return;
      switch (e.key) {
        case 'a':
          e.preventDefault();
          void submitDecision('Approve');
          break;
        case 'r':
          e.preventDefault();
          void submitDecision('Reject');
          break;
        case 'n':
          e.preventDefault();
          goRelative(1);
          break;
        case 'p':
          e.preventDefault();
          goRelative(-1);
          break;
        default:
          break;
      }
    };
    window.addEventListener('keydown', onKey);
    onCleanup(() => window.removeEventListener('keydown', onKey));
  });

  return (
    <div class="flex flex-col h-full min-h-0 text-xs" data-testid="diff-panel">
      <Show when={props.reviewStatus?.requireHumanReview}>
        <div class="flex flex-wrap items-center gap-2 px-2 py-1 border-b border-surface-3 bg-surface-2/50">
          <span class="text-[10px] text-muted-foreground" data-testid="review-status">
            Review: {props.reviewStatus?.status ?? 'Pending'}
            {' '}({props.reviewStatus?.approvedFiles ?? 0}/{props.reviewStatus?.totalFiles ?? 0})
          </span>
          <span class="text-[10px] text-muted-foreground hidden sm:inline">
            · a approve · r reject · n/p nav
          </span>
          <div class="ml-auto flex gap-1">
            <button
              type="button"
              class="text-[10px] px-2 py-0.5 rounded border border-amber-500/40 text-amber-300 disabled:opacity-50"
              disabled={reviewBusy() || selectedPaths().length === 0}
              onClick={() => setRepairOpen(true)}
            >
              Request repair
            </button>
            <button
            type="button"
            data-testid="review-approve-all"
            class="text-[10px] px-2 py-0.5 rounded bg-secondary/20 hover:bg-secondary/30 disabled:opacity-50"
              disabled={reviewBusy() || props.reviewStatus?.status === 'Approved'}
              onClick={batchApprove}
            >
              Approve all
            </button>
            <Show when={reviewApproved()}>
              <button
                type="button"
                data-testid="review-open-pr"
                class="text-[10px] px-2 py-0.5 rounded border border-blue-500/40 text-blue-300 hover:bg-blue-500/10 disabled:opacity-50"
                disabled={prBusy()}
                onClick={() => void openOrCreatePr()}
              >
                {prUrl() ? `Open PR #${props.fleetEntry?.prNumber ?? '…'}` : 'Create PR'}
              </button>
            </Show>
          </div>
          <Show when={prError()}>
            <span class="text-[10px] text-error w-full">{prError()}</span>
          </Show>
        </div>
      </Show>

      <Show when={!props.loading} fallback={<p class="p-4 text-muted-foreground">Loading files…</p>}>
        <Show when={props.files.length > 0} fallback={
          <p class="p-4 text-muted-foreground">No generated files yet for this run.</p>
        }>
          <div class="flex flex-1 min-h-0">
            {/* File tree */}
            <ul class="w-44 shrink-0 border-r border-surface-3 overflow-y-auto">
              <For each={props.files}>{(file) => {
                const overlay = () => findOverlay(props.diffOverlays, file.relativePath);
                const badge = () => overlayBadge(overlay()?.overlayKinds ?? []);
                const decision = () => fileDecision(props.reviewStatus, file.relativePath);
                const isActive = () => active()?.relativePath === file.relativePath;
                return (
                  <li>
                    <div class={[
                      'flex items-start gap-1 px-1 py-1 hover:bg-surface-2',
                      isActive() ? 'bg-surface-2' : '',
                      overlay() ? 'border-l-2 border-amber-500/70' : '',
                    ].join(' ')}>
                      <Show when={props.reviewStatus?.requireHumanReview}>
                        <input
                          type="checkbox"
                          class="mt-0.5 shrink-0"
                          checked={checked().has(file.relativePath)}
                          onChange={() => toggleCheck(file.relativePath)}
                        />
                      </Show>
                      <button
                        type="button"
                        class="flex-1 min-w-0 text-left truncate"
                        onClick={() => setSelected(file.relativePath)}
                        title={file.relativePath}
                      >
                        <span class={['truncate block', isActive() ? 'text-secondary' : ''].join(' ')}>
                          {file.relativePath}
                        </span>
                        <Show when={diffFor(file.relativePath)}>
                          {(d) => (
                            <span class="text-[10px] text-muted-foreground">
                              step {d().stepNumber} · {d().changeKind}
                            </span>
                          )}
                        </Show>
                        <Show when={decision()}>
                          {(d) => <span class="text-[10px] block text-secondary">{d()}</span>}
                        </Show>
                        <Show when={badge()}>
                          {(b) => <span class={['text-[10px] block', b().class].join(' ')}>{b().label}</span>}
                        </Show>
                      </button>
                    </div>
                  </li>
                );
              }}</For>
            </ul>

            {/* Diff center */}
            <div class="flex-1 min-w-0 flex flex-col min-h-0 border-r border-surface-3">
              <Show when={active()}>
                {(file) => (
                  <>
                    <div class="px-2 py-1 text-[10px] text-muted-foreground border-b border-surface-3 shrink-0">
                      {file().relativePath} · {file().language}
                      · {file().contentLength.toLocaleString()} chars
                      <Show when={activeStep() != null}>
                        <span> · step {activeStep()}</span>
                      </Show>
                    </div>
                    <div class="flex-1 min-h-0">
                      <ReviewDiffViewer
                        path={file().relativePath}
                        language={file().language}
                        content={file().content ?? ''}
                        unifiedDiff={diffDetail()}
                      />
                    </div>
                    <EvidenceFilmstrip items={filmstrip()} activeStep={activeStep()} />
                  </>
                )}
              </Show>
            </div>

            {/* Evidence / console right */}
            <aside class="w-52 shrink-0 overflow-y-auto p-2 space-y-3">
              <Show when={activeOverlay()}>
                {(o) => (
                  <div class="p-2 rounded bg-surface-2 border border-amber-500/30">
                    <div class="font-medium text-amber-400 mb-1">Overlay</div>
                    <For each={o().reasons}>{(reason) => (
                      <div class="text-muted-foreground truncate" title={reason}>{reason}</div>
                    )}</For>
                  </div>
                )}
              </Show>
              <ConsoleErrorPanel
                errors={props.consoleErrors ?? []}
                activePath={active()?.relativePath}
                onJumpToPath={jumpToPath}
              />
              <Show when={props.reviewStatus?.requireHumanReview}>
                <div class="flex flex-col gap-1 pt-1 border-t border-surface-3">
                  <button
                    type="button"
                    class="text-[10px] px-2 py-1 rounded bg-secondary/15 text-secondary disabled:opacity-50"
                    disabled={reviewBusy()}
                    onClick={() => void submitDecision('Approve')}
                  >
                    Approve selected (a)
                  </button>
                  <button
                    type="button"
                    class="text-[10px] px-2 py-1 rounded border border-red-500/30 text-red-400 disabled:opacity-50"
                    disabled={reviewBusy()}
                    onClick={() => void submitDecision('Reject')}
                  >
                    Reject selected (r)
                  </button>
                </div>
              </Show>
            </aside>
          </div>
        </Show>
      </Show>

      <Show when={repairOpen()}>
        <RepairRequestDialog
          runId={props.runId}
          paths={selectedPaths()}
          onClose={() => setRepairOpen(false)}
          onSubmitted={(s) => props.onReviewUpdated?.(s)}
        />
      </Show>
    </div>
  );
};

export default DiffPanel;
