import { config } from '../../../lib/config';
import { setStore } from '../IDEStore';
import { fetchBackgroundDelegations } from './agentFleet';

export type SubagentRecord = {
  id: string;
  runId: string;
  name: string;
  task: string;
  status: string;
  createdAtUtc: string;
  updatedAtUtc: string;
  outputPreview?: string | null;
  error?: string | null;
};

export type FlowNodeProgress = {
  nodeId: string;
  status: string;
  attempts?: number;
  lastError?: string | null;
};

export type FlowProgress = {
  runId: string;
  flowName: string;
  currentNodeId?: string | null;
  status: string;
  nodes: FlowNodeProgress[];
  updatedAtUtc: string;
};

export type DelegationRecord = {
  id: string;
  runId: string;
  task: string;
  status: string;
  createdAtUtc: string;
  updatedAtUtc: string;
  outputPreview?: string | null;
  error?: string | null;
};

const activePollers = new Map<string, ReturnType<typeof setInterval>>();

function authHeaders(): Record<string, string> {
  const token = localStorage.getItem('accessToken');
  return token ? { Authorization: `Bearer ${token}` } : {};
}

async function fetchSubagents(runId: string): Promise<SubagentRecord[]> {
  const res = await fetch(`${config.apiBaseUrl}/api/v1/ide/app-generation/${runId}/subagents`, {
    headers: authHeaders(),
  });
  if (!res.ok) return [];
  const data = await res.json();
  return Array.isArray(data.subagents) ? data.subagents : [];
}

async function fetchFlow(runId: string): Promise<FlowProgress | null> {
  const res = await fetch(`${config.apiBaseUrl}/api/v1/ide/app-generation/${runId}/flow`, {
    headers: authHeaders(),
  });
  if (res.status === 404) return null;
  if (!res.ok) return null;
  return (await res.json()) as FlowProgress;
}

async function fetchDelegations(runId: string): Promise<DelegationRecord[]> {
  const res = await fetch(`${config.apiBaseUrl}/api/v1/ide/app-generation/${runId}/delegations`, {
    headers: authHeaders(),
  });
  if (!res.ok) return [];
  const data = await res.json();
  const rows = Array.isArray(data.delegations) ? data.delegations : [];
  return rows.map((raw: Record<string, unknown>) => ({
    id: String(raw.id ?? raw.Id ?? ''),
    runId: String(raw.runId ?? raw.RunId ?? runId),
    task: String(raw.task ?? raw.Task ?? ''),
    status: String(raw.status ?? raw.Status ?? ''),
    createdAtUtc: String(raw.createdAtUtc ?? raw.CreatedAtUtc ?? ''),
    updatedAtUtc: String(raw.updatedAtUtc ?? raw.UpdatedAtUtc ?? ''),
    outputPreview: raw.outputPreview != null ? String(raw.outputPreview ?? raw.OutputPreview) : null,
    error: raw.error != null ? String(raw.error ?? raw.Error) : null,
  }));
}

async function pollOnce(runId: string) {
  try {
    const [subagents, flow, delegations, background] = await Promise.all([
      fetchSubagents(runId),
      fetchFlow(runId),
      fetchDelegations(runId),
      fetchBackgroundDelegations({ runId, activeOnly: true }).catch(() => null),
    ]);
    setStore('activeGenerationRunId', runId);
    setStore('subagents', subagents);
    setStore('delegations', delegations);
    if (background) {
      setStore('backgroundFleet', {
        runningCount: background.runningCount,
        queuedCount: background.queuedCount,
      });
    }
    if (flow) setStore('flowProgress', flow);
  } catch {
    // keep polling
  }
}

export function startRunOrchestrationPolling(runId: string, intervalMs = 4000): () => void {
  stopRunOrchestrationPolling(runId);
  void pollOnce(runId);
  const handle = setInterval(() => void pollOnce(runId), intervalMs);
  activePollers.set(runId, handle);
  return () => stopRunOrchestrationPolling(runId);
}

export function stopRunOrchestrationPolling(runId: string) {
  const handle = activePollers.get(runId);
  if (!handle) return;
  clearInterval(handle);
  activePollers.delete(runId);
}
