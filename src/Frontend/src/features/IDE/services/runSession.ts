import { config } from '../../../lib/config';
import { normalizeFleetSummary, type AgentFleetSummary } from './agentFleet';

export interface AgentFleetRunDetail {
  entry: AgentFleetSummary & {
    spaceId?: string | null;
    startedAtUtc: string;
    costUsd: number;
    verifyStatus?: string | null;
    stack?: string | null;
    failureReason?: string | null;
  };
  subagentCount: number;
  delegationCount: number;
  evidenceCount: number;
  flowName?: string | null;
  currentFlowNodeId?: string | null;
  lastError?: string | null;
}

export interface PermissionPrompt {
  id: string;
  toolName: string;
  path?: string | null;
  reason: string;
  createdAtUtc: string;
  accepted?: boolean | null;
  kind?: string | null;
}

export interface RunUsageSummary {
  stepCount: number;
  toolCallCount: number;
  inputTokens: number;
  outputTokens: number;
  totalTokens: number;
  costUsd: number;
  llmRequestCount: number;
  lastActivityAtUtc?: string | null;
  lastToolActivityAtUtc?: string | null;
}

export interface SessionTimelineEvent {
  kind: string;
  timestampUtc: string;
  title: string;
  detail?: string | null;
  success?: boolean | null;
  stepNumber?: number | null;
  actorId?: string | null;
}

export interface GeneratedFileSummary {
  relativePath: string;
  language: string;
  contentLength: number;
  content?: string | null;
}

export interface RunDiffSummary {
  path: string;
  language: string;
  changeKind: string;
  stepNumber: number;
  toolName: string;
  hunkCount: number;
  lastChangedUtc: string;
  provenanceId: string;
}

export interface DiffPathOverlay {
  path: string;
  overlayKinds: string[];
  reasons: string[];
}

export interface DiffEvidenceItem {
  source: string;
  kind: string;
  fileName: string;
  downloadUrl: string;
  thumbnailUrl?: string | null;
  stepNumber?: number | null;
  toolName?: string | null;
  stepMatched: boolean;
  sizeBytes: number;
  lastModifiedUtc: string;
}

export interface FileDiffEvidence {
  path: string;
  correlatedStepNumber?: number | null;
  items: DiffEvidenceItem[];
  overlays: { kind: string; reason: string; category?: string | null }[];
}

export async function fetchRunDiffs(runId: string): Promise<{ total: number; items: RunDiffSummary[] }> {
  const res = await fetch(`${config.apiBaseUrl}/api/v1/ide/app-generation/${runId}/diffs`, {
    headers: authHeaders(),
  });
  if (!res.ok) throw new Error(`run diffs failed: ${res.status}`);
  const raw = await res.json();
  const itemsRaw = Array.isArray(raw.items ?? raw.Items) ? (raw.items ?? raw.Items) : [];
  return {
    total: Number(raw.total ?? raw.Total ?? itemsRaw.length),
    items: itemsRaw.map((i: Record<string, unknown>) => ({
      path: String(i.path ?? i.Path ?? ''),
      language: String(i.language ?? i.Language ?? ''),
      changeKind: String(i.changeKind ?? i.ChangeKind ?? ''),
      stepNumber: Number(i.stepNumber ?? i.StepNumber ?? 0),
      toolName: String(i.toolName ?? i.ToolName ?? ''),
      hunkCount: Number(i.hunkCount ?? i.HunkCount ?? 0),
      lastChangedUtc: String(i.lastChangedUtc ?? i.LastChangedUtc ?? ''),
      provenanceId: String(i.provenanceId ?? i.ProvenanceId ?? ''),
    })),
  };
}

export async function fetchDiffOverlays(runId: string): Promise<DiffPathOverlay[]> {
  const res = await fetch(`${config.apiBaseUrl}/api/v1/ide/app-generation/${runId}/diffs/evidence`, {
    headers: authHeaders(),
  });
  if (res.status === 404) return [];
  if (!res.ok) return [];
  const raw = await res.json();
  const pathsRaw = Array.isArray(raw.paths ?? raw.Paths) ? (raw.paths ?? raw.Paths) : [];
  return pathsRaw.map((p: Record<string, unknown>) => ({
    path: String(p.path ?? p.Path ?? ''),
    overlayKinds: Array.isArray(p.overlayKinds ?? p.OverlayKinds)
      ? (p.overlayKinds ?? p.OverlayKinds as string[])
      : [],
    reasons: Array.isArray(p.reasons ?? p.Reasons)
      ? (p.reasons ?? p.Reasons as string[])
      : [],
  }));
}

export async function fetchDiffDetail(runId: string, path: string): Promise<{
  path: string;
  language: string;
  changeKind: string;
  unifiedDiff: string | null;
} | null> {
  const url = new URL(`${config.apiBaseUrl}/api/v1/ide/app-generation/${runId}/diffs/detail`);
  url.searchParams.set('path', path);
  const res = await fetch(url.toString(), { headers: authHeaders() });
  if (res.status === 404) return null;
  if (!res.ok) throw new Error(`diff detail failed: ${res.status}`);
  const raw = await res.json();
  return {
    path: String(raw.path ?? raw.Path ?? path),
    language: String(raw.language ?? raw.Language ?? 'text'),
    changeKind: String(raw.changeKind ?? raw.ChangeKind ?? ''),
    unifiedDiff: (raw.unifiedDiff ?? raw.UnifiedDiff ?? null) as string | null,
  };
}

export async function fetchConsoleErrors(runId: string): Promise<unknown> {
  const res = await fetch(
    `${config.apiBaseUrl}/api/v1/ide/app-generation/${runId}/verify/artifacts/console-errors.json`,
    { headers: authHeaders() },
  );
  if (res.status === 404) return [];
  if (!res.ok) return [];
  try {
    return await res.json();
  } catch {
    return [];
  }
}

export async function fetchDiffEvidence(runId: string, path: string): Promise<FileDiffEvidence | null> {
  const url = new URL(`${config.apiBaseUrl}/api/v1/ide/app-generation/${runId}/diffs/evidence`);
  url.searchParams.set('path', path);
  const res = await fetch(url.toString(), { headers: authHeaders() });
  if (res.status === 404) return null;
  if (!res.ok) throw new Error(`diff evidence failed: ${res.status}`);
  const raw = await res.json();
  const itemsRaw = Array.isArray(raw.items ?? raw.Items) ? (raw.items ?? raw.Items) : [];
  const overlaysRaw = Array.isArray(raw.overlays ?? raw.Overlays) ? (raw.overlays ?? raw.Overlays) : [];
  return {
    path: String(raw.path ?? raw.Path ?? path),
    correlatedStepNumber: (raw.correlatedStepNumber ?? raw.CorrelatedStepNumber) as number | null | undefined,
    items: itemsRaw.map((i: Record<string, unknown>) => ({
      source: String(i.source ?? i.Source ?? ''),
      kind: String(i.kind ?? i.Kind ?? ''),
      fileName: String(i.fileName ?? i.FileName ?? ''),
      downloadUrl: String(i.downloadUrl ?? i.DownloadUrl ?? ''),
      thumbnailUrl: (i.thumbnailUrl ?? i.ThumbnailUrl) as string | null | undefined,
      stepNumber: (i.stepNumber ?? i.StepNumber) as number | null | undefined,
      toolName: (i.toolName ?? i.ToolName) as string | null | undefined,
      stepMatched: Boolean(i.stepMatched ?? i.StepMatched ?? false),
      sizeBytes: Number(i.sizeBytes ?? i.SizeBytes ?? 0),
      lastModifiedUtc: String(i.lastModifiedUtc ?? i.LastModifiedUtc ?? ''),
    })),
    overlays: overlaysRaw.map((o: Record<string, unknown>) => ({
      kind: String(o.kind ?? o.Kind ?? ''),
      reason: String(o.reason ?? o.Reason ?? ''),
      category: (o.category ?? o.Category) as string | null | undefined,
    })),
  };
}

export interface MemorySearchHit {
  source: string;
  runId?: string | null;
  stepNumber?: number | null;
  toolName?: string | null;
  snippet: string;
  score: number;
}

function authHeaders(): Record<string, string> {
  const token = localStorage.getItem('accessToken');
  return token ? { Authorization: `Bearer ${token}` } : {};
}

export async function fetchFleetRunDetail(runId: string): Promise<AgentFleetRunDetail | null> {
  const res = await fetch(`${config.apiBaseUrl}/api/v1/ide/agent-fleet/${runId}/summary`, {
    headers: authHeaders(),
  });
  if (res.status === 404) return null;
  if (!res.ok) throw new Error(`summary failed: ${res.status}`);
  const raw = await res.json();
  return normalizeDetail(raw);
}

export async function fetchPermissionPrompts(runId: string): Promise<PermissionPrompt[]> {
  const res = await fetch(`${config.apiBaseUrl}/api/v1/ide/app-generation/${runId}/permission-mode`, {
    headers: authHeaders(),
  });
  if (!res.ok) return [];
  const data = await res.json();
  const prompts = Array.isArray(data.pendingPrompts) ? data.pendingPrompts : [];
  return prompts.map((p: Record<string, unknown>) => ({
    id: String(p.id ?? p.Id ?? ''),
    toolName: String(p.toolName ?? p.ToolName ?? ''),
    path: (p.path ?? p.Path) as string | null | undefined,
    reason: String(p.reason ?? p.Reason ?? ''),
    createdAtUtc: String(p.createdAtUtc ?? p.CreatedAtUtc ?? ''),
    accepted: (p.accepted ?? p.Accepted) as boolean | null | undefined,
    kind: (p.kind ?? p.Kind ?? 'tool') as string | null | undefined,
  }));
}

export async function resolvePermissionPrompt(
  runId: string,
  promptId: string,
  accepted: boolean,
): Promise<void> {
  const res = await fetch(
    `${config.apiBaseUrl}/api/v1/ide/app-generation/${runId}/permission-mode/resolve`,
    {
      method: 'POST',
      headers: { ...authHeaders(), 'Content-Type': 'application/json' },
      body: JSON.stringify({ promptId, accepted }),
    },
  );
  if (!res.ok) throw new Error(`resolve failed: ${res.status}`);
}

export async function fetchRollout(runId: string): Promise<unknown[]> {
  const res = await fetch(`${config.apiBaseUrl}/api/v1/ide/app-generation/${runId}/rollout`, {
    headers: authHeaders(),
  });
  if (res.status === 404) return [];
  if (!res.ok) throw new Error(`rollout failed: ${res.status}`);
  const data = await res.json();
  return Array.isArray(data) ? data : [];
}

export async function fetchBuildDashboard(
  runId: string,
  stackFilter?: string,
): Promise<Record<string, unknown> | null> {
  const url = new URL(`${config.apiBaseUrl}/api/v1/ide/app-generation/${runId}/dashboard/build`);
  if (stackFilter) url.searchParams.set('stackFilter', stackFilter);
  const res = await fetch(url.toString(), {
    headers: authHeaders(),
  });
  if (res.status === 404) return null;
  if (!res.ok) return null;
  return res.json();
}

export async function fetchRunUsage(runId: string): Promise<RunUsageSummary | null> {
  const res = await fetch(`${config.apiBaseUrl}/api/v1/ide/app-generation/${runId}/usage`, {
    headers: authHeaders(),
  });
  if (res.status === 404) return null;
  if (!res.ok) return null;
  const raw = await res.json();
  return {
    stepCount: Number(raw.stepCount ?? raw.StepCount ?? 0),
    toolCallCount: Number(raw.toolCallCount ?? raw.ToolCallCount ?? 0),
    inputTokens: Number(raw.inputTokens ?? raw.InputTokens ?? 0),
    outputTokens: Number(raw.outputTokens ?? raw.OutputTokens ?? 0),
    totalTokens: Number(raw.totalTokens ?? raw.TotalTokens ?? 0),
    costUsd: Number(raw.costUsd ?? raw.CostUsd ?? 0),
    llmRequestCount: Number(raw.llmRequestCount ?? raw.LlmRequestCount ?? 0),
    lastActivityAtUtc: (raw.lastActivityAtUtc ?? raw.LastActivityAtUtc) as string | null | undefined,
    lastToolActivityAtUtc: (raw.lastToolActivityAtUtc ?? raw.LastToolActivityAtUtc) as string | null | undefined,
  };
}

export async function fetchGeneratedFiles(runId: string): Promise<GeneratedFileSummary[]> {
  const res = await fetch(`${config.apiBaseUrl}/api/v1/ide/app-generation/${runId}/generated-files`, {
    headers: authHeaders(),
  });
  if (res.status === 404) return [];
  if (!res.ok) throw new Error(`generated-files failed: ${res.status}`);
  const data = await res.json();
  const files = Array.isArray(data.files) ? data.files : [];
  return files.map((f: Record<string, unknown>) => ({
    relativePath: String(f.relativePath ?? f.RelativePath ?? ''),
    language: String(f.language ?? f.Language ?? 'text'),
    contentLength: Number(f.contentLength ?? f.ContentLength ?? 0),
    content: (f.content ?? f.Content) as string | null | undefined,
  }));
}

export async function fetchSessionTimeline(runId: string): Promise<SessionTimelineEvent[]> {
  const res = await fetch(`${config.apiBaseUrl}/api/v1/ide/agent-fleet/${runId}/timeline`, {
    headers: authHeaders(),
  });
  if (res.status === 404) return [];
  if (!res.ok) throw new Error(`timeline failed: ${res.status}`);
  const raw = await res.json();
  const events = Array.isArray(raw.events ?? raw.Events) ? (raw.events ?? raw.Events) : [];
  return events.map((e: Record<string, unknown>) => ({
    kind: String(e.kind ?? e.Kind ?? ''),
    timestampUtc: String(e.timestampUtc ?? e.TimestampUtc ?? new Date().toISOString()),
    title: String(e.title ?? e.Title ?? ''),
    detail: (e.detail ?? e.Detail) as string | null | undefined,
    success: (e.success ?? e.Success) as boolean | null | undefined,
    stepNumber: (e.stepNumber ?? e.StepNumber) as number | null | undefined,
    actorId: (e.actorId ?? e.ActorId) as string | null | undefined,
  }));
}

export async function searchSessionMemory(query: string, limit = 25): Promise<MemorySearchHit[]> {
  const url = new URL(`${config.apiBaseUrl}/api/v1/ide/memory/search`);
  url.searchParams.set('q', query);
  url.searchParams.set('limit', String(limit));
  const res = await fetch(url.toString(), { headers: authHeaders() });
  if (!res.ok) throw new Error(`memory search failed: ${res.status}`);
  const data = await res.json();
  const hits = Array.isArray(data.hits) ? data.hits : [];
  return hits.map((h: Record<string, unknown>) => ({
    source: String(h.source ?? h.Source ?? 'memory'),
    runId: (h.runId ?? h.RunId) as string | null | undefined,
    stepNumber: (h.stepNumber ?? h.StepNumber) as number | null | undefined,
    toolName: (h.toolName ?? h.ToolName) as string | null | undefined,
    snippet: String(h.snippet ?? h.Snippet ?? ''),
    score: Number(h.score ?? h.Score ?? 0),
  }));
}

function normalizeDetail(raw: Record<string, unknown>): AgentFleetRunDetail {
  const entry = (raw.entry ?? raw.Entry ?? {}) as Record<string, unknown>;
  return {
    entry: {
      ...normalizeFleetSummary(entry),
      spaceId: (entry.spaceId ?? entry.SpaceId) as string | null | undefined,
      startedAtUtc: String(entry.startedAtUtc ?? entry.StartedAtUtc ?? ''),
      costUsd: Number(entry.costUsd ?? entry.CostUsd ?? 0),
      verifyStatus: (entry.verifyStatus ?? entry.VerifyStatus) as string | null | undefined,
      stack: (entry.stack ?? entry.Stack) as string | null | undefined,
      failureReason: (entry.failureReason ?? entry.FailureReason) as string | null | undefined,
    },
    subagentCount: Number(raw.subagentCount ?? raw.SubagentCount ?? 0),
    delegationCount: Number(raw.delegationCount ?? raw.DelegationCount ?? 0),
    evidenceCount: Number(raw.evidenceCount ?? raw.EvidenceCount ?? 0),
    flowName: (raw.flowName ?? raw.FlowName) as string | null | undefined,
    currentFlowNodeId: (raw.currentFlowNodeId ?? raw.CurrentFlowNodeId) as string | null | undefined,
    lastError: (raw.lastError ?? raw.LastError) as string | null | undefined,
  };
}
