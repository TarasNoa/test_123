import { config } from '../../../lib/config';

export interface RunReviewStatus {
  runId: string;
  status: string;
  requireHumanReview: boolean;
  totalFiles: number;
  decidedFiles: number;
  approvedFiles: number;
  rejectedFiles: number;
  repairRequestedFiles: number;
  files: {
    path: string;
    decision: string;
    notes?: string | null;
    reviewerId?: string | null;
    decidedAtUtc: string;
  }[];
  pendingPaths: string[];
}

function authHeaders(): Record<string, string> {
  const token = localStorage.getItem('accessToken');
  return token ? { Authorization: `Bearer ${token}` } : {};
}

export async function fetchRunReviewStatus(runId: string): Promise<RunReviewStatus | null> {
  const res = await fetch(`${config.apiBaseUrl}/api/v1/ide/app-generation/${runId}/review`, {
    headers: authHeaders(),
  });
  if (res.status === 404) return null;
  if (!res.ok) return null;
  return normalizeReviewStatus(await res.json());
}

export async function submitRunReview(
  runId: string,
  decision: string,
  paths: string[],
  notes?: string,
): Promise<RunReviewStatus> {
  const res = await fetch(`${config.apiBaseUrl}/api/v1/ide/app-generation/${runId}/review`, {
    method: 'POST',
    headers: { ...authHeaders(), 'Content-Type': 'application/json' },
    body: JSON.stringify({ decision, paths, notes }),
  });
  if (!res.ok) throw new Error(`review submit failed: ${res.status}`);
  return normalizeReviewStatus(await res.json());
}

function normalizeReviewStatus(raw: Record<string, unknown>): RunReviewStatus {
  const filesRaw = Array.isArray(raw.files ?? raw.Files) ? (raw.files ?? raw.Files) : [];
  const pendingRaw = Array.isArray(raw.pendingPaths ?? raw.PendingPaths)
    ? (raw.pendingPaths ?? raw.PendingPaths)
    : [];
  return {
    runId: String(raw.runId ?? raw.RunId ?? ''),
    status: String(raw.status ?? raw.Status ?? 'Pending'),
    requireHumanReview: Boolean(raw.requireHumanReview ?? raw.RequireHumanReview ?? true),
    totalFiles: Number(raw.totalFiles ?? raw.TotalFiles ?? 0),
    decidedFiles: Number(raw.decidedFiles ?? raw.DecidedFiles ?? 0),
    approvedFiles: Number(raw.approvedFiles ?? raw.ApprovedFiles ?? 0),
    rejectedFiles: Number(raw.rejectedFiles ?? raw.RejectedFiles ?? 0),
    repairRequestedFiles: Number(raw.repairRequestedFiles ?? raw.RepairRequestedFiles ?? 0),
    files: filesRaw.map((f: Record<string, unknown>) => ({
      path: String(f.path ?? f.Path ?? ''),
      decision: String(f.decision ?? f.Decision ?? ''),
      notes: (f.notes ?? f.Notes) as string | null | undefined,
      reviewerId: (f.reviewerId ?? f.ReviewerId) as string | null | undefined,
      decidedAtUtc: String(f.decidedAtUtc ?? f.DecidedAtUtc ?? ''),
    })),
    pendingPaths: pendingRaw.map(String),
  };
}
