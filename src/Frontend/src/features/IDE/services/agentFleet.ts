import { config } from '../../../lib/config';

export type AgentFleetStatus =
  | 'Queued'
  | 'Planning'
  | 'Generating'
  | 'Verifying'
  | 'Repairing'
  | 'WaitingForApproval'
  | 'WaitingForCi'
  | 'PrReady'
  | 'HandoffPending'
  | 'HandoffComplete'
  | 'Completed'
  | 'Failed'
  | 'Cancelled';

export interface AgentFleetSummary {
  runId: string;
  title: string;
  status: AgentFleetStatus;
  stage: string;
  agentCount: number;
  lastActivityAtUtc: string;
  pinned: boolean;
  archived: boolean;
  backendKind?: string | null;
  backendFallbackFrom?: string | null;
  prUrl?: string | null;
  prNumber?: number | null;
  ciStatus?: string | null;
  ciLogsUrl?: string | null;
  playbookHits?: number;
  playbookAttempts?: number;
  qualityScore?: number;
}

function authHeaders(): Record<string, string> {
  const token = localStorage.getItem('accessToken');
  return token ? { Authorization: `Bearer ${token}` } : {};
}

export async function fetchAgentFleet(params?: {
  status?: string;
  search?: string;
  spaceId?: string;
  includeArchived?: boolean;
  sortBy?: 'quality' | 'activity';
}): Promise<AgentFleetSummary[]> {
  const url = new URL(`${config.apiBaseUrl}/api/v1/ide/agent-fleet`);
  if (params?.status) url.searchParams.set('status', params.status);
  if (params?.search) url.searchParams.set('search', params.search);
  if (params?.spaceId) url.searchParams.set('spaceId', params.spaceId);
  if (params?.includeArchived) url.searchParams.set('includeArchived', 'true');
  if (params?.sortBy) url.searchParams.set('sortBy', params.sortBy);

  const res = await fetch(url.toString(), { headers: authHeaders() });
  if (!res.ok) throw new Error(`fleet list failed: ${res.status}`);
  const data = await res.json();
  return (Array.isArray(data) ? data : []).map(normalizeFleetSummary);
}

export async function cancelFleetRun(runId: string): Promise<void> {
  const res = await fetch(`${config.apiBaseUrl}/api/v1/ide/agent-fleet/${runId}/cancel`, {
    method: 'POST',
    headers: authHeaders(),
  });
  if (!res.ok) throw new Error(`cancel failed: ${res.status}`);
}

export async function patchFleetRun(
  runId: string,
  patch: { title?: string; pinned?: boolean; archived?: boolean },
): Promise<void> {
  const res = await fetch(`${config.apiBaseUrl}/api/v1/ide/agent-fleet/${runId}`, {
    method: 'PATCH',
    headers: { ...authHeaders(), 'Content-Type': 'application/json' },
    body: JSON.stringify(patch),
  });
  if (!res.ok) throw new Error(`patch failed: ${res.status}`);
}

export interface FleetPullRequestResult {
  success: boolean;
  skipped: boolean;
  summary: string;
  pullRequestUrl?: string | null;
  pullRequestNumber?: number | null;
}

export async function createFleetPullRequest(runId: string): Promise<FleetPullRequestResult> {
  const res = await fetch(`${config.apiBaseUrl}/api/v1/ide/agent-fleet/${runId}/pull-request`, {
    method: 'POST',
    headers: authHeaders(),
  });
  const raw = await res.json().catch(() => ({}));
  const result: FleetPullRequestResult = {
    success: Boolean(raw.success ?? raw.Success),
    skipped: Boolean(raw.skipped ?? raw.Skipped),
    summary: String(raw.summary ?? raw.Summary ?? ''),
    pullRequestUrl: (raw.pullRequestUrl ?? raw.PullRequestUrl) as string | null | undefined,
    pullRequestNumber: raw.pullRequestNumber != null ? Number(raw.pullRequestNumber ?? raw.PullRequestNumber) : null,
  };
  if (!res.ok && !result.skipped) throw new Error(result.summary || `create PR failed: ${res.status}`);
  return result;
}

export async function bulkArchiveFleetRuns(olderThanDays = 7): Promise<void> {
  const res = await fetch(`${config.apiBaseUrl}/api/v1/ide/agent-fleet/bulk-archive`, {
    method: 'POST',
    headers: { ...authHeaders(), 'Content-Type': 'application/json' },
    body: JSON.stringify({ olderThanDays, actor: 'fleet-ui' }),
  });
  if (!res.ok) throw new Error(`bulk archive failed: ${res.status}`);
}

export type BackgroundDelegationItem = {
  runId: string;
  delegationId: string;
  task: string;
  queueStatus: string;
  priority: string;
  tenantUserId?: string | null;
  enqueuedAtUtc: string;
  startedAtUtc?: string | null;
};

export type BackgroundFleetSummary = {
  runningCount: number;
  queuedCount: number;
  items: BackgroundDelegationItem[];
};

export async function fetchBackgroundDelegations(params?: {
  runId?: string;
  tenantUserId?: string;
  activeOnly?: boolean;
}): Promise<BackgroundFleetSummary> {
  const url = new URL(`${config.apiBaseUrl}/api/v1/ide/agent-fleet/background-delegations`);
  if (params?.runId) url.searchParams.set('runId', params.runId);
  if (params?.tenantUserId) url.searchParams.set('tenantUserId', params.tenantUserId);
  if (params?.activeOnly === false) url.searchParams.set('activeOnly', 'false');

  const res = await fetch(url.toString(), { headers: authHeaders() });
  if (!res.ok) throw new Error(`background delegations failed: ${res.status}`);
  const raw = await res.json();
  return {
    runningCount: Number(raw.runningCount ?? raw.RunningCount ?? 0),
    queuedCount: Number(raw.queuedCount ?? raw.QueuedCount ?? 0),
    items: (Array.isArray(raw.items) ? raw.items : Array.isArray(raw.Items) ? raw.Items : []).map(
      (item: Record<string, unknown>) => ({
        runId: String(item.runId ?? item.RunId ?? ''),
        delegationId: String(item.delegationId ?? item.DelegationId ?? ''),
        task: String(item.task ?? item.Task ?? ''),
        queueStatus: String(item.queueStatus ?? item.QueueStatus ?? ''),
        priority: String(item.priority ?? item.Priority ?? ''),
        tenantUserId: item.tenantUserId != null ? String(item.tenantUserId ?? item.TenantUserId) : null,
        enqueuedAtUtc: String(item.enqueuedAtUtc ?? item.EnqueuedAtUtc ?? ''),
        startedAtUtc: item.startedAtUtc != null ? String(item.startedAtUtc ?? item.StartedAtUtc) : null,
      }),
    ),
  };
}

export function normalizeFleetSummary(raw: Record<string, unknown>): AgentFleetSummary {
  return {
    runId: String(raw.runId ?? raw.RunId ?? ''),
    title: String(raw.title ?? raw.Title ?? 'Untitled run'),
    status: String(raw.status ?? raw.Status ?? 'Queued') as AgentFleetStatus,
    stage: String(raw.stage ?? raw.Stage ?? ''),
    agentCount: Number(raw.agentCount ?? raw.AgentCount ?? 0),
    lastActivityAtUtc: String(raw.lastActivityAtUtc ?? raw.LastActivityAtUtc ?? new Date().toISOString()),
    pinned: Boolean(raw.pinned ?? raw.Pinned),
    archived: Boolean(raw.archived ?? raw.Archived),
    backendKind: raw.backendKind != null ? String(raw.backendKind ?? raw.BackendKind) : null,
    backendFallbackFrom:
      raw.backendFallbackFrom != null
        ? String(raw.backendFallbackFrom ?? raw.BackendFallbackFrom)
        : null,
    prUrl: raw.prUrl != null ? String(raw.prUrl ?? raw.PrUrl) : null,
    prNumber: raw.prNumber != null ? Number(raw.prNumber ?? raw.PrNumber) : null,
    ciStatus: raw.ciStatus != null ? String(raw.ciStatus ?? raw.CiStatus) : null,
    ciLogsUrl: raw.ciLogsUrl != null ? String(raw.ciLogsUrl ?? raw.CiLogsUrl) : null,
    playbookHits: Number(raw.playbookHits ?? raw.PlaybookHits ?? 0),
    playbookAttempts: Number(raw.playbookAttempts ?? raw.PlaybookAttempts ?? 0),
    qualityScore: Number(raw.qualityScore ?? raw.QualityScore ?? 0),
  };
}
