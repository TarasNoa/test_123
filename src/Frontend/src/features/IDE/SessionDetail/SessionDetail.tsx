import {
  createMemo,
  createSignal,
  For,
  onCleanup,
  onMount,
  Show,
  type Component,
} from 'solid-js';
import { useNavigate, useParams, useLocation } from '@solidjs/router';
import { setStore } from '../IDEStore';
import { subscribeGenerationRunEvents, stopGenerationRunEvents } from '../services/runEvents';
import { startRunOrchestrationPolling, stopRunOrchestrationPolling } from '../services/runOrchestration';
import {
  fetchBuildDashboard,
  fetchFleetRunDetail,
  fetchGeneratedFiles,
  fetchRunDiffs,
  fetchDiffOverlays,
  fetchConsoleErrors,
  type DiffPathOverlay,
  type RunDiffSummary,
  fetchPermissionPrompts,
  fetchRollout,
  fetchRunUsage,
  type AgentFleetRunDetail,
  type GeneratedFileSummary,
  type PermissionPrompt,
  type RunUsageSummary,
} from '../services/runSession';
import { fetchRunReviewStatus, type RunReviewStatus } from '../services/runReview';
import { PermissionPromptModal } from './PermissionPromptModal';
import { ObscuraExecPolicyPromptModal } from './ObscuraExecPolicyPromptModal';
import {
  subscribeExecPolicyPromptStream,
  type ExecPolicyPromptPayload,
} from '../services/execPolicyPromptStream';
import { SubagentsPanel } from '../BottomPanel/SubagentsPanel';
import { FlowPanel } from '../BottomPanel/FlowPanel';
import { Timeline } from '../BottomPanel/Timeline';
import { RunUsageMeter } from './RunUsageMeter';
import { BackgroundAgentsWidget } from './BackgroundAgentsWidget';
import { DiffPanel } from './DiffPanel';
import { MemoryPanel } from './MemoryPanel';
import { UnifiedTimeline } from './UnifiedTimeline';
import { parseConsoleErrors, type ConsoleErrorEntry } from './reviewUtils';
import {
  fetchHostProfile,
  isLocalHostProfile,
  promoteRunToCloud,
} from '../services/runHandoff';
import { RunSyncIndicator } from './RunSyncIndicator';
import { CiLogDrawer } from '../components/CiLogDrawer';
import { BuildDiagnosticsPanel } from './BuildDiagnosticsPanel';
import { SimilarRunsPanel } from './SimilarRunsPanel';
import type { AgentFleetSummary } from '../services/agentFleet';

type DetailTab = 'overview' | 'agents' | 'diff' | 'evidence' | 'memory' | 'rollout' | 'settings';

type ObscuraEvidenceArtifact = {
  kind: string;
  fileName: string;
  downloadUrl: string;
  contentType?: string;
  logicalName?: string;
  stepNumber?: number;
  toolName?: string;
  sizeBytes?: number;
};

type ObscuraEvidenceDashboard = {
  artifacts?: ObscuraEvidenceArtifact[];
  manifestUrl?: string;
  thumbnailUrl?: string;
};

const obscuraEvidence = (): ObscuraEvidenceDashboard | null => {
  const raw = dashboard()?.obscuraEvidence as ObscuraEvidenceDashboard | undefined;
  return raw ?? null;
};

const artifactMediaUrl = (url: string) =>
  url.startsWith('http') ? url : `${window.location.origin}${url}`;

const isVideoArtifact = (a: ObscuraEvidenceArtifact) =>
  a.kind === 'Video'
  || a.fileName.endsWith('.webm')
  || a.contentType?.includes('video') === true;

const isImageArtifact = (a: ObscuraEvidenceArtifact) =>
  a.kind === 'Screenshot'
  || a.fileName.endsWith('.png')
  || a.fileName.endsWith('.jpg')
  || a.contentType?.startsWith('image/') === true;

export const SessionDetail: Component<{ initialTab?: DetailTab }> = (props) => {
  const params = useParams();
  const navigate = useNavigate();
  const location = useLocation();
  const runId = () => params.runId ?? '';

  const [detail, setDetail] = createSignal<AgentFleetRunDetail | null>(null);
  const [tab, setTab] = createSignal<DetailTab>(
    props.initialTab ?? (location.pathname.endsWith('/review') ? 'diff' : 'overview'),
  );
  const [rollout, setRollout] = createSignal<unknown[]>([]);
  const [dashboard, setDashboard] = createSignal<Record<string, unknown> | null>(null);
  const [usage, setUsage] = createSignal<RunUsageSummary | null>(null);
  const [files, setFiles] = createSignal<GeneratedFileSummary[]>([]);
  const [runDiffs, setRunDiffs] = createSignal<RunDiffSummary[]>([]);
  const [diffOverlays, setDiffOverlays] = createSignal<DiffPathOverlay[]>([]);
  const [reviewStatus, setReviewStatus] = createSignal<RunReviewStatus | null>(null);
  const [consoleErrors, setConsoleErrors] = createSignal<ConsoleErrorEntry[]>([]);
  const [filesLoading, setFilesLoading] = createSignal(false);
  const [prompt, setPrompt] = createSignal<PermissionPrompt | null>(null);
  const [obscuraPrompt, setObscuraPrompt] = createSignal<ExecPolicyPromptPayload | null>(null);
  const [loading, setLoading] = createSignal(true);
  const [stuckRepairing, setStuckRepairing] = createSignal(false);
  const [localHost, setLocalHost] = createSignal(false);
  const [promoting, setPromoting] = createSignal(false);
  const [promoteError, setPromoteError] = createSignal<string | null>(null);
  const [ciDrawerOpen, setCiDrawerOpen] = createSignal(false);

  const fleetEntry = (): AgentFleetSummary | null => {
    const d = detail();
    if (!d) return null;
    return d.entry;
  };

  const loadUsage = async (id: string) => {
    const u = await fetchRunUsage(id);
    setUsage(u);
    return u;
  };

  const loadFiles = async (id: string) => {
    setFilesLoading(true);
    try {
      const [fileList, diffs, overlays, review, consoleRaw] = await Promise.all([
        fetchGeneratedFiles(id),
        fetchRunDiffs(id).catch(() => ({ total: 0, items: [] as RunDiffSummary[] })),
        fetchDiffOverlays(id).catch(() => [] as DiffPathOverlay[]),
        fetchRunReviewStatus(id).catch(() => null),
        fetchConsoleErrors(id).catch(() => []),
      ]);
      setFiles(fileList);
      setRunDiffs(diffs.items);
      setDiffOverlays(overlays);
      setReviewStatus(review);
      setConsoleErrors(parseConsoleErrors(consoleRaw));
    } finally {
      setFilesLoading(false);
    }
  };

  const load = async () => {
    const id = runId();
    if (!id) return;
    setLoading(true);
    try {
      const [d, prompts, r, dash, u] = await Promise.all([
        fetchFleetRunDetail(id),
        fetchPermissionPrompts(id),
        fetchRollout(id),
        fetchBuildDashboard(id),
        fetchRunUsage(id),
      ]);
      setDetail(d);
      setRollout(r);
      setDashboard(dash);
      setUsage(u);
      const pending = prompts.find(
        (p) => p.accepted == null && (p.kind ?? 'tool') !== 'obscura_execpolicy',
      );
      setPrompt(pending ?? null);
      const pendingObscura = prompts.find(
        (p) => p.accepted == null && p.kind === 'obscura_execpolicy',
      );
      if (pendingObscura) {
        setObscuraPrompt({
          id: pendingObscura.id,
          toolName: pendingObscura.toolName,
          target: pendingObscura.path,
          reason: pendingObscura.reason,
          createdAtUtc: pendingObscura.createdAtUtc,
          kind: 'obscura_execpolicy',
        });
      }
      if (d?.entry.stage === 'stuck_repairing') setStuckRepairing(true);
    } finally {
      setLoading(false);
    }
  };

  const isStuck = createMemo(() => {
    if (stuckRepairing()) return true;
    const info = detail();
    if (!info || info.entry.status !== 'Repairing') return false;
    const lastTool = usage()?.lastToolActivityAtUtc ?? info.entry.lastActivityAtUtc;
    if (!lastTool) return false;
    const idleMs = Date.now() - new Date(lastTool).getTime();
    return idleMs >= 30 * 60 * 1000;
  });

  const canPromoteToCloud = createMemo(() => {
    const info = detail();
    if (!localHost() || !info) return false;
    if (info.entry.status === 'HandoffPending' || info.entry.status === 'HandoffComplete') return false;
    return !['Failed', 'Cancelled'].includes(info.entry.status);
  });

  const runSyncActive = createMemo(() => {
    const info = detail();
    if (!info) return false;
    if (info.entry.status === 'HandoffPending') return true;
    if (localHost() && !['Failed', 'Cancelled', 'HandoffComplete'].includes(info.entry.status)) return true;
    return false;
  });

  const handlePromoteToCloud = async () => {
    const id = runId();
    if (!id) return;
    setPromoting(true);
    setPromoteError(null);
    try {
      await promoteRunToCloud(id);
      await load();
    } catch (e) {
      setPromoteError(e instanceof Error ? e.message : 'promote failed');
    } finally {
      setPromoting(false);
    }
  };

  onMount(() => {
    const id = runId();
    if (!id) return;

    setStore('activeGenerationRunId', id);
    setStore('bottomPanelOpen', true);
    const stopEvents = subscribeGenerationRunEvents(id);
    const stopPoll = startRunOrchestrationPolling(id);
    void load();
    void loadFiles(id);
    void fetchHostProfile().then((profile) => {
      setLocalHost(isLocalHostProfile(profile?.profile));
    });

    const stopExecPolicy = subscribeExecPolicyPromptStream(id, (payload) => {
      setObscuraPrompt(payload);
    });

    const promptPoll = setInterval(() => {
      void fetchPermissionPrompts(id).then((prompts) => {
        const pending = prompts.find(
          (p) => p.accepted == null && (p.kind ?? 'tool') !== 'obscura_execpolicy',
        );
        setPrompt(pending ?? null);
        if (!obscuraPrompt()) {
          const pendingObscura = prompts.find(
            (p) => p.accepted == null && p.kind === 'obscura_execpolicy',
          );
          if (pendingObscura) {
            setObscuraPrompt({
              id: pendingObscura.id,
              toolName: pendingObscura.toolName,
              target: pendingObscura.path,
              reason: pendingObscura.reason,
              createdAtUtc: pendingObscura.createdAtUtc,
              kind: 'obscura_execpolicy',
            });
          }
        }
      });
    }, 3000);

    const usagePoll = setInterval(() => {
      void loadUsage(id);
    }, 5000);

    onCleanup(() => {
      stopEvents();
      stopPoll();
      stopGenerationRunEvents(id);
      stopRunOrchestrationPolling(id);
      stopExecPolicy();
      clearInterval(promptPoll);
      clearInterval(usagePoll);
    });
  });

  const d = () => detail();

  return (
    <div class="h-screen w-screen flex flex-col bg-background text-foreground">
      <header class="flex items-center gap-3 px-4 py-3 border-b border-surface-3 shrink-0 flex-wrap">
        <button type="button" class="text-xs text-secondary hover:underline" onClick={() => navigate('/ide/agent-board')}>
          ← Board
        </button>
        <div class="min-w-0">
          <h1 class="text-sm font-semibold truncate">{d()?.entry.title ?? 'Session'}</h1>
          <p class="text-[10px] text-muted-foreground font-mono truncate">{runId()}</p>
        </div>
        <RunUsageMeter usage={usage()} />
        <Show when={canPromoteToCloud()}>
          <button
            type="button"
            data-testid="promote-to-cloud"
            class="text-xs px-3 py-1 rounded border border-secondary/40 bg-secondary/10 text-secondary hover:bg-secondary/20 disabled:opacity-50"
            disabled={promoting()}
            onClick={() => void handlePromoteToCloud()}
          >
            {promoting() ? 'Promoting…' : 'Continue in cloud'}
          </button>
        </Show>
        <Show when={d()?.entry.status === 'HandoffPending'}>
          <span class="text-[10px] uppercase px-2 py-0.5 rounded border border-amber-500/40 text-amber-400">
            Handoff pending
          </span>
        </Show>
        <Show when={d()?.entry.status === 'HandoffComplete'}>
          <span class="text-[10px] uppercase px-2 py-0.5 rounded border border-success/40 text-success">
            Handoff complete
          </span>
        </Show>
        <RunSyncIndicator runId={runId()} active={runSyncActive()} />
        <Show when={d()?.entry.ciStatus && d()!.entry.ciStatus !== 'none'}>
          <button
            type="button"
            data-testid="session-ci-drawer-toggle"
            class="text-[10px] uppercase px-2 py-0.5 rounded border border-amber-500/40 text-amber-400 hover:bg-amber-500/10"
            onClick={() => setCiDrawerOpen(true)}
          >
            CI {d()!.entry.ciStatus}
          </button>
        </Show>
        <Show when={promoteError()}>
          <span class="text-[10px] text-error">{promoteError()}</span>
        </Show>
        <Show when={d()}>
          {(info) => (
            <span class="ml-auto text-[10px] uppercase px-2 py-0.5 rounded border border-surface-3 text-muted-foreground">
              {info().entry.status} · {info().entry.stage}
              <Show when={info().entry.verifyStatus}>
                {' '}· verify {info().entry.verifyStatus}
              </Show>
            </span>
          )}
        </Show>
      </header>

      <Show when={isStuck()}>
        <div class="px-4 py-2 bg-amber-500/10 border-b border-amber-500/30 text-amber-200 text-xs">
          Run stuck in Repairing — no tool activity for 30+ minutes. Consider cancel or inspect rollout.
        </div>
      </Show>

      <div class="flex gap-1 px-4 py-2 border-b border-surface-3 shrink-0 overflow-x-auto">
        <For each={[
          ['overview', 'Overview'],
          ['agents', 'Agents'],
          ['diff', 'Diff'],
          ['evidence', 'Evidence'],
          ['memory', 'Memory'],
          ['rollout', 'Rollout'],
          ['settings', 'Settings'],
        ] as const}>{([key, label]) => (
          <button
            type="button"
            class={[
              'px-2 py-1 text-xs rounded border whitespace-nowrap',
              tab() === key ? 'border-secondary text-secondary' : 'border-surface-3 text-muted-foreground',
            ].join(' ')}
            onClick={() => {
              setTab(key);
              if (key === 'diff' && files().length === 0) void loadFiles(runId());
            }}
          >
            {label}
          </button>
        )}</For>
      </div>

      <Show when={loading()} fallback={
        <div class="flex-1 min-h-0 grid grid-cols-1 lg:grid-cols-2 gap-0">
          <div class="border-r border-surface-3 overflow-hidden flex flex-col min-h-0">
            <Show when={tab() === 'overview'}>
              <div class="p-4 space-y-3 overflow-y-auto text-xs">
                <Show when={d()} fallback={<p class="text-muted-foreground">Run not found</p>}>
                  {(info) => (
                    <>
                      <div class="grid grid-cols-2 gap-2">
                        <Stat label="Subagents" value={String(info().subagentCount)} />
                        <Stat label="Delegations" value={String(info().delegationCount)} />
                        <Stat label="Evidence" value={String(info().evidenceCount)} />
                        <Stat label="Agents" value={String(info().entry.agentCount)} />
                      </div>
                      <BackgroundAgentsWidget />
                      <BuildDiagnosticsPanel runId={runId()} />
                      <SimilarRunsPanel runId={runId()} />
                      <Show when={info().entry.stack}>
                        <p class="text-muted-foreground">Stack: {info().entry.stack}</p>
                      </Show>
                      <Show when={info().flowName}>
                        <p class="text-muted-foreground">
                          Flow: {info().flowName} · node {info().currentFlowNodeId ?? '—'}
                        </p>
                      </Show>
                      <Show when={info().lastError}>
                        <p class="text-error text-[10px]">{info().lastError}</p>
                      </Show>
                    </>
                  )}
                </Show>
                <div class="h-48 border border-surface-3 rounded overflow-hidden">
                  <FlowPanel />
                </div>
                <UnifiedTimeline runId={runId()} />
              </div>
            </Show>
            <Show when={tab() === 'agents'}>
              <SubagentsPanel />
            </Show>
            <Show when={tab() === 'diff'}>
              <DiffPanel
                runId={runId()}
                files={files()}
                loading={filesLoading()}
                runDiffs={runDiffs()}
                diffOverlays={diffOverlays()}
                reviewStatus={reviewStatus()}
                fleetEntry={fleetEntry()}
                onFleetRefresh={() => void load()}
                consoleErrors={consoleErrors()}
                onReviewUpdated={setReviewStatus}
              />
            </Show>
            <Show when={tab() === 'evidence'}>
              <div class="p-4 text-xs space-y-3 overflow-y-auto">
                <p class="text-muted-foreground">Evidence artifacts: {d()?.evidenceCount ?? 0}</p>
                <Show when={obscuraEvidence()?.artifacts?.length}>
                  <div class="space-y-3">
                    <For each={obscuraEvidence()?.artifacts ?? []}>
                      {(artifact) => (
                        <div class="rounded border border-surface-3 p-2 space-y-2">
                          <div class="flex flex-wrap gap-2 text-[10px] text-muted-foreground">
                            <span>{artifact.kind}</span>
                            <span>{artifact.fileName}</span>
                            <Show when={artifact.stepNumber != null}>
                              <span>step {artifact.stepNumber}</span>
                            </Show>
                          </div>
                          <Show when={isVideoArtifact(artifact)}>
                            <video
                              controls
                              class="max-w-full max-h-64 rounded bg-black"
                              src={artifactMediaUrl(artifact.downloadUrl)}
                            />
                          </Show>
                          <Show when={isImageArtifact(artifact) && !isVideoArtifact(artifact)}>
                            <img
                              alt={artifact.fileName}
                              class="max-w-full max-h-64 rounded border border-surface-3"
                              src={artifactMediaUrl(artifact.downloadUrl)}
                            />
                          </Show>
                          <a
                            class="text-secondary hover:underline block"
                            href={artifactMediaUrl(artifact.downloadUrl)}
                            target="_blank"
                            rel="noreferrer"
                          >
                            Download
                          </a>
                        </div>
                      )}
                    </For>
                  </div>
                </Show>
                <Show when={!obscuraEvidence()?.artifacts?.length && dashboard()?.obscuraEvidence}>
                  <pre class="text-[10px] bg-surface-2 p-2 rounded overflow-x-auto">
                    {JSON.stringify(dashboard()?.obscuraEvidence, null, 2)}
                  </pre>
                </Show>
              </div>
            </Show>
            <Show when={tab() === 'memory'}>
              <MemoryPanel runId={runId()} />
            </Show>
            <Show when={tab() === 'rollout'}>
              <div class="p-2 overflow-y-auto text-[10px] font-mono">
                <For each={rollout().slice(-50)}>{(line) => (
                  <pre class="whitespace-pre-wrap border-b border-surface-3 py-1">{JSON.stringify(line)}</pre>
                )}</For>
              </div>
            </Show>
            <Show when={tab() === 'settings'}>
              <div class="p-4 text-xs space-y-2">
                <p>Permission prompts poll every 3s. Pending modal blocks external browser actions.</p>
                <button type="button" class="text-secondary hover:underline" onClick={() => void load()}>
                  Refresh
                </button>
              </div>
            </Show>
          </div>
          <div class="overflow-hidden flex flex-col min-h-0">
            <Timeline />
          </div>
        </div>
      }>
        <div class="flex-1 flex items-center justify-center text-muted-foreground text-sm">Loading session…</div>
      </Show>

      <Show when={obscuraPrompt()}>
        {(p) => (
          <ObscuraExecPolicyPromptModal
            runId={runId()}
            prompt={p()}
            onResolved={() => {
              setObscuraPrompt(null);
              void load();
            }}
          />
        )}
      </Show>

      <Show when={prompt() && !obscuraPrompt()}>
        {(p) => (
          <PermissionPromptModal
            runId={runId()}
            prompt={p()}
            onResolved={() => {
              setPrompt(null);
              void load();
            }}
          />
        )}
      </Show>

      <CiLogDrawer
        run={fleetEntry()}
        open={ciDrawerOpen()}
        onClose={() => setCiDrawerOpen(false)}
      />
    </div>
  );
};

const Stat: Component<{ label: string; value: string }> = (props) => (
  <div class="rounded border border-surface-3 p-2">
    <div class="text-[10px] text-muted-foreground uppercase">{props.label}</div>
    <div class="text-sm font-medium">{props.value}</div>
  </div>
);

export default SessionDetail;
