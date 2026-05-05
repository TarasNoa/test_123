/**
 * Typed client for the Autonomous App Generation HTTP surface.
 *
 * Endpoints are exposed by the `Libr4.IDE.AutonomousAppGeneration.Host` service
 * under `/api/ide/app-generation/...` and proxied from Next via `next.config.mjs`
 * rewrites (`/api/ide/:path*` → `${NEXT_PUBLIC_AUTOGEN_BASE_URL}/api/ide/:path*`).
 *
 * P2-4 of audit roadmap: this client backs the checkpoint / run-telemetry UI.
 */

// ---- Domain types -----------------------------------------------------------
// Mirror the C# DTOs returned by the host. Fields are kept loose (`unknown` /
// optional) so the UI degrades gracefully if the backend evolves.

export type RunStatus =
  | 'Created'
  | 'Planning'
  | 'Generating'
  | 'Testing'
  | 'Fixing'
  | 'Completed'
  | 'Failed'
  | 'Cancelled'

export interface RunSummary {
  id: string
  status: RunStatus | string
  applicationName?: string | null
  iterations?: number
  startedAt?: string
  completedAt?: string | null
  failureReason?: string | null
  tenantId?: string | null
}

export interface QualityGateSnapshot {
  stage: string
  score: number
  passed: boolean
  reasons: string[]
  evaluatedAtUtc: string
}

export interface GeneratedFile {
  relativePath: string
  language?: string
  content: string
}

export interface IterationCycle {
  id?: string
  number: number
  startedAt?: string
  completedAt?: string | null
  errors?: { code?: string; message: string; severity?: string }[]
  status?: string
}

export interface AppGenerationReport {
  id: string
  status: string
  failureReason?: string | null
  plan?: {
    applicationName?: string
    applicationDescription?: string
    techStack?: { languages?: string[]; frameworks?: string[]; databases?: string[]; tools?: string[] }
    runtimeImage?: string
    maxIterations?: number
    phases?: { order: number; name: string; description?: string }[]
  } | null
  qualityGates?: QualityGateSnapshot[]
  iterations?: IterationCycle[]
  files?: GeneratedFile[]
  outstandingErrors?: { code?: string; message: string; severity?: string }[]
  startedAt?: string
  completedAt?: string | null
  runRemediationHints?: string[]
  recoveryTrace?: unknown[]
  benchmarkSummary?: {
    totalQualityEvaluations?: number
    totalFailedEvaluations?: number
    topFailureReasons?: string[]
  }
}

// ---- HTTP plumbing ----------------------------------------------------------

import { getAccessToken } from './api'

class AutoGenApiError extends Error {
  status: number
  constructor(message: string, status: number) {
    super(message)
    this.status = status
  }
}

async function request<T>(path: string, init: RequestInit = {}): Promise<T> {
  const url = path.startsWith('http') ? path : path
  const headers = new Headers(init.headers)
  if (!headers.has('content-type') && init.body) {
    headers.set('content-type', 'application/json')
  }
  const token = getAccessToken()
  if (token) {
    headers.set('authorization', `Bearer ${token}`)
  }
  const res = await fetch(url, { ...init, headers })
  if (!res.ok) {
    let message = res.statusText
    try {
      const j = (await res.json()) as { error?: string; detail?: string; title?: string }
      message = j.error ?? j.detail ?? j.title ?? message
    } catch {
      // Body not JSON; keep statusText.
    }
    throw new AutoGenApiError(message, res.status)
  }
  if (res.status === 204) return undefined as unknown as T
  return (await res.json()) as T
}

const BASE = '/api/ide/app-generation'

// ---- Endpoints --------------------------------------------------------------

export interface StartRunRequest {
  userRequest: string
  maxIterations?: number
  tenantId?: string
  resumeFromRunId?: string
}

export async function startRun(req: StartRunRequest): Promise<{ message: string; hint?: string }> {
  return request(`${BASE}/start`, {
    method: 'POST',
    body: JSON.stringify(req),
  })
}

export async function listRuns(): Promise<RunSummary[]> {
  // Backend returns either an array or a single record (rare); normalise.
  const raw = await request<RunSummary | RunSummary[]>(`${BASE}/list`)
  return Array.isArray(raw) ? raw : [raw]
}

export async function getReport(id: string): Promise<AppGenerationReport | null> {
  try {
    return await request<AppGenerationReport>(`${BASE}/${id}`)
  } catch (err) {
    if (err instanceof AutoGenApiError && err.status === 404) return null
    throw err
  }
}

export async function pauseRun(id: string): Promise<{ runId: string; paused: boolean }> {
  return request(`${BASE}/${id}/pause`, { method: 'POST' })
}

export async function resumeRun(id: string): Promise<{ runId: string; resumed: boolean }> {
  return request(`${BASE}/${id}/resume`, { method: 'POST' })
}

export async function cancelRun(
  id: string,
  payload: { actor?: string; reason?: string } = {}
): Promise<{ runId: string; cancelled: boolean }> {
  return request(`${BASE}/${id}/cancel`, {
    method: 'POST',
    body: JSON.stringify(payload),
  })
}

export async function getRunState(id: string): Promise<unknown | null> {
  try {
    return await request<unknown>(`${BASE}/${id}/state`)
  } catch (err) {
    if (err instanceof AutoGenApiError && err.status === 404) return null
    throw err
  }
}

export async function exportDiagnostics(id: string): Promise<unknown> {
  return request(`${BASE}/${id}/diagnostics/export`)
}

export { AutoGenApiError }

// ---- Helpers ----------------------------------------------------------------

const ACTIVE_STATUSES = new Set<string>(['Created', 'Planning', 'Generating', 'Testing', 'Fixing'])

export function isRunActive(status: string | undefined | null): boolean {
  return !!status && ACTIVE_STATUSES.has(status)
}

export function isRunTerminal(status: string | undefined | null): boolean {
  return !!status && (status === 'Completed' || status === 'Failed' || status === 'Cancelled')
}
