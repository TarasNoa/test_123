import { config } from '../../../lib/config';

export interface FleetSessionSearchHit {
  runId: string;
  title: string;
  status: string;
  stack?: string | null;
  spaceId?: string | null;
  snippet: string;
  score: number;
  lastActivityAtUtc: string;
  pinned: boolean;
}

export interface FleetSimilarRunHit {
  runId: string;
  title: string;
  status: string;
  stack?: string | null;
  spaceId?: string | null;
  snippet: string;
  score: number;
  lastActivityAtUtc: string;
  pinned: boolean;
}

export interface FleetSessionSearchResult {
  query: string;
  count: number;
  hits: FleetSessionSearchHit[];
  facets?: {
    stacks: string[];
    outcomes: string[];
    dateBuckets: string[];
  };
}

function authHeaders(): Record<string, string> {
  const token = localStorage.getItem('accessToken');
  return token ? { Authorization: `Bearer ${token}` } : {};
}

export async function searchFleetSessions(params: {
  q: string;
  stack?: string;
  outcome?: string;
  spaceId?: string;
  dateBucket?: string;
  limit?: number;
}): Promise<FleetSessionSearchResult> {
  const url = new URL(`${config.apiBaseUrl}/api/v1/ide/agent-fleet/search`);
  url.searchParams.set('q', params.q);
  if (params.stack) url.searchParams.set('stack', params.stack);
  if (params.outcome) url.searchParams.set('outcome', params.outcome);
  if (params.spaceId) url.searchParams.set('spaceId', params.spaceId);
  if (params.dateBucket) url.searchParams.set('dateBucket', params.dateBucket);
  if (params.limit) url.searchParams.set('limit', String(params.limit));

  const res = await fetch(url.toString(), { headers: authHeaders() });
  if (!res.ok) throw new Error(`fleet search failed: ${res.status}`);
  const raw = await res.json();
  const hitsRaw = Array.isArray(raw.hits) ? raw.hits : Array.isArray(raw.Hits) ? raw.Hits : [];
  return {
    query: String(raw.query ?? raw.Query ?? params.q),
    count: Number(raw.count ?? raw.Count ?? hitsRaw.length),
    hits: hitsRaw.map(normalizeHit),
    facets: raw.facets ?? raw.Facets,
  };
}

export async function fetchSimilarRuns(
  runId: string,
  limit?: number,
): Promise<{ sourceRunId: string; hits: FleetSimilarRunHit[]; method: string }> {
  const url = new URL(`${config.apiBaseUrl}/api/v1/ide/agent-fleet/${runId}/similar`);
  if (limit) url.searchParams.set('limit', String(limit));
  const res = await fetch(url.toString(), { headers: authHeaders() });
  if (!res.ok) throw new Error(`similar runs failed: ${res.status}`);
  const raw = await res.json();
  const hitsRaw = Array.isArray(raw.hits) ? raw.hits : Array.isArray(raw.Hits) ? raw.Hits : [];
  return {
    sourceRunId: String(raw.sourceRunId ?? raw.SourceRunId ?? runId),
    method: String(raw.method ?? raw.Method ?? 'embedding'),
    hits: hitsRaw.map(normalizeSimilarHit),
  };
}

export async function forkFleetRun(runId: string): Promise<{ sourceRunId: string; newRunId: string; title: string }> {
  const res = await fetch(`${config.apiBaseUrl}/api/v1/ide/agent-fleet/${runId}/fork`, {
    method: 'POST',
    headers: authHeaders(),
  });
  if (!res.ok) throw new Error(`fork failed: ${res.status}`);
  const raw = await res.json();
  return {
    sourceRunId: String(raw.sourceRunId ?? raw.SourceRunId ?? runId),
    newRunId: String(raw.newRunId ?? raw.NewRunId ?? ''),
    title: String(raw.title ?? raw.Title ?? 'Fork'),
  };
}

function normalizeHit(raw: Record<string, unknown>): FleetSessionSearchHit {
  return {
    runId: String(raw.runId ?? raw.RunId ?? ''),
    title: String(raw.title ?? raw.Title ?? 'Untitled'),
    status: String(raw.status ?? raw.Status ?? 'Queued'),
    stack: raw.stack != null ? String(raw.stack ?? raw.Stack) : null,
    spaceId: raw.spaceId != null ? String(raw.spaceId ?? raw.SpaceId) : null,
    snippet: String(raw.snippet ?? raw.Snippet ?? ''),
    score: Number(raw.score ?? raw.Score ?? 0),
    lastActivityAtUtc: String(raw.lastActivityAtUtc ?? raw.LastActivityAtUtc ?? new Date().toISOString()),
    pinned: Boolean(raw.pinned ?? raw.Pinned),
  };
}

function normalizeSimilarHit(raw: Record<string, unknown>): FleetSimilarRunHit {
  return normalizeHit(raw);
}
