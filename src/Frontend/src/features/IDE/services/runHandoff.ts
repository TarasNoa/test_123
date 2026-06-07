import { config } from '../../../lib/config';

export interface AutonomousHostProfile {
  profile: string;
  aiDefaultProvider: string;
  gpuThrottleEnabled: boolean;
}

export interface RunPromoteResult {
  runId: string;
  sourceRunId: string;
  exportId: string;
  bundleSha256: string;
  status: string;
  promotedAtUtc: string;
}

export interface RunSyncConflict {
  relativePath: string;
  winnerSource: string;
  loserSource: string;
  timestampUtc: string;
  conflictFile?: string | null;
}

export interface RunSyncConflictsResponse {
  runId: string;
  conflicts: RunSyncConflict[];
}

function authHeaders(): Record<string, string> {
  const token = localStorage.getItem('accessToken');
  return token ? { Authorization: `Bearer ${token}` } : {};
}

export async function fetchHostProfile(): Promise<AutonomousHostProfile | null> {
  const res = await fetch(`${config.apiBaseUrl}/api/v1/ide/app-generation/host-profile`, {
    headers: authHeaders(),
  });
  if (!res.ok) return null;
  const raw = await res.json();
  return {
    profile: String(raw.profile ?? raw.Profile ?? ''),
    aiDefaultProvider: String(raw.aiDefaultProvider ?? raw.AiDefaultProvider ?? ''),
    gpuThrottleEnabled: Boolean(raw.gpuThrottleEnabled ?? raw.GpuThrottleEnabled),
  };
}

export function isLocalHostProfile(profile: string | undefined): boolean {
  const normalized = (profile ?? '').toLowerCase();
  return normalized === 'dockermodelrunner' || normalized === 'benchmark';
}

export async function promoteRunToCloud(runId: string): Promise<RunPromoteResult> {
  const res = await fetch(`${config.apiBaseUrl}/api/v1/ide/app-generation/${runId}/promote-to-cloud`, {
    method: 'POST',
    headers: authHeaders(),
  });
  if (!res.ok) {
    const body = await res.text();
    throw new Error(`promote failed: ${res.status} ${body}`);
  }
  const raw = await res.json();
  return {
    runId: String(raw.runId ?? raw.RunId ?? runId),
    sourceRunId: String(raw.sourceRunId ?? raw.SourceRunId ?? runId),
    exportId: String(raw.exportId ?? raw.ExportId ?? ''),
    bundleSha256: String(raw.bundleSha256 ?? raw.BundleSha256 ?? ''),
    status: String(raw.status ?? raw.Status ?? 'HandoffPending'),
    promotedAtUtc: String(raw.promotedAtUtc ?? raw.PromotedAtUtc ?? new Date().toISOString()),
  };
}

export async function fetchRunSyncConflicts(runId: string): Promise<RunSyncConflictsResponse> {
  const res = await fetch(`${config.apiBaseUrl}/api/v1/ide/app-generation/${runId}/sync/conflicts`, {
    headers: authHeaders(),
  });
  if (!res.ok) {
    return { runId, conflicts: [] };
  }
  const raw = await res.json();
  const rows = Array.isArray(raw.conflicts ?? raw.Conflicts) ? (raw.conflicts ?? raw.Conflicts) : [];
  return {
    runId: String(raw.runId ?? raw.RunId ?? runId),
    conflicts: rows.map((c: Record<string, unknown>) => ({
      relativePath: String(c.relativePath ?? c.RelativePath ?? ''),
      winnerSource: String(c.winnerSource ?? c.WinnerSource ?? ''),
      loserSource: String(c.loserSource ?? c.LoserSource ?? ''),
      timestampUtc: String(c.timestampUtc ?? c.TimestampUtc ?? ''),
      conflictFile: c.conflictFile != null ? String(c.conflictFile ?? c.ConflictFile) : null,
    })),
  };
}
