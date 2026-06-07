import { config } from '../../../lib/config';

export interface AgentSpaceSummary {
  spaceId: string;
  name: string;
  repositoryUrl?: string | null;
  baseBranch: string;
  ownerId: string;
  sharedMemoryScope: string;
  mcpProfile?: string | null;
  createdAtUtc: string;
  rootPath: string;
  integrationBranch: string;
}

export interface SpaceMemberSummary {
  memberId: string;
  spaceId: string;
  role: string;
  runId?: string | null;
  worktreePath: string;
  branchName: string;
  status: string;
  createdAtUtc: string;
  updatedAtUtc: string;
  lastError?: string | null;
}

export interface SpaceContextEventSummary {
  eventId: string;
  spaceId: string;
  kind: string;
  title: string;
  payload?: string | null;
  authorMemberId?: string | null;
  timestampUtc: string;
}

export interface MergeSpaceMemberResult {
  success: boolean;
  output?: string | null;
  conflicts: string[];
  integrationBranch: string;
}

export interface MergePreviewFile {
  path: string;
  insertions: number;
  deletions: number;
  changeKind: string;
}

export interface MergePreviewResult {
  sourceBranch: string;
  integrationBranch: string;
  files: MergePreviewFile[];
  diffStat: string;
  unifiedDiff: string;
}

export interface SpaceOrchestrationResult {
  spaceId: string;
  stage: string;
  contextReady: boolean;
  explorer: SpaceMemberSummary;
  implementer: SpaceMemberSummary;
  verifier?: SpaceMemberSummary | null;
}

export interface WorktreeFileEntry {
  name: string;
  relativePath: string;
  isDirectory: boolean;
  sizeBytes?: number | null;
}

export interface WorktreeDirectoryListing {
  worktreePath: string;
  relativePath: string;
  entries: WorktreeFileEntry[];
}

function authHeaders(): Record<string, string> {
  const token = localStorage.getItem('accessToken');
  return token ? { Authorization: `Bearer ${token}` } : {};
}

export async function fetchAgentSpaces(ownerId?: string): Promise<AgentSpaceSummary[]> {
  const url = new URL(`${config.apiBaseUrl}/api/v1/ide/spaces`);
  if (ownerId) url.searchParams.set('ownerId', ownerId);
  const res = await fetch(url.toString(), { headers: authHeaders() });
  if (!res.ok) throw new Error(`spaces list failed: ${res.status}`);
  const data = await res.json();
  return (Array.isArray(data) ? data : []).map(normalizeSpace);
}

function normalizeSpace(raw: Record<string, unknown>): AgentSpaceSummary {
  return {
    spaceId: String(raw.spaceId ?? raw.SpaceId ?? ''),
    name: String(raw.name ?? raw.Name ?? ''),
    repositoryUrl: (raw.repositoryUrl ?? raw.RepositoryUrl) as string | null | undefined,
    baseBranch: String(raw.baseBranch ?? raw.BaseBranch ?? 'main'),
    ownerId: String(raw.ownerId ?? raw.OwnerId ?? ''),
    sharedMemoryScope: String(raw.sharedMemoryScope ?? raw.SharedMemoryScope ?? ''),
    mcpProfile: (raw.mcpProfile ?? raw.McpProfile) as string | null | undefined,
    createdAtUtc: String(raw.createdAtUtc ?? raw.CreatedAtUtc ?? new Date().toISOString()),
    rootPath: String(raw.rootPath ?? raw.RootPath ?? ''),
    integrationBranch: String(raw.integrationBranch ?? raw.IntegrationBranch ?? ''),
  };
}

export async function fetchAgentSpaceDetail(spaceId: string): Promise<{
  space: AgentSpaceSummary;
  members: SpaceMemberSummary[];
  recentContext: SpaceContextEventSummary[];
} | null> {
  const res = await fetch(`${config.apiBaseUrl}/api/v1/ide/spaces/${spaceId}`, { headers: authHeaders() });
  if (res.status === 404) return null;
  if (!res.ok) throw new Error(`space detail failed: ${res.status}`);
  const raw = await res.json();
  const spaceRaw = (raw.space ?? raw.Space ?? {}) as Record<string, unknown>;
  const membersRaw = Array.isArray(raw.members ?? raw.Members) ? (raw.members ?? raw.Members) : [];
  const contextRaw = Array.isArray(raw.recentContext ?? raw.RecentContext)
    ? (raw.recentContext ?? raw.RecentContext)
    : [];
  return {
    space: normalizeSpace(spaceRaw),
    members: membersRaw.map(normalizeMember),
    recentContext: contextRaw.map(normalizeContextEvent),
  };
}

export async function mergeSpaceMember(spaceId: string, memberId: string): Promise<MergeSpaceMemberResult> {
  const res = await fetch(`${config.apiBaseUrl}/api/v1/ide/spaces/${spaceId}/merge/${memberId}`, {
    method: 'POST',
    headers: authHeaders(),
  });
  const raw = await res.json();
  return {
    success: Boolean(raw.success ?? raw.Success),
    output: (raw.output ?? raw.Output) as string | null | undefined,
    conflicts: Array.isArray(raw.conflicts ?? raw.Conflicts) ? (raw.conflicts ?? raw.Conflicts) : [],
    integrationBranch: String(raw.integrationBranch ?? raw.IntegrationBranch ?? ''),
  };
}

export async function fetchMergePreview(
  spaceId: string,
  memberId: string
): Promise<MergePreviewResult | null> {
  const res = await fetch(`${config.apiBaseUrl}/api/v1/ide/spaces/${spaceId}/merge/${memberId}/preview`, {
    headers: authHeaders(),
  });
  if (res.status === 404) return null;
  if (!res.ok) throw new Error(`merge preview failed: ${res.status}`);
  const raw = await res.json();
  const filesRaw = Array.isArray(raw.files ?? raw.Files) ? (raw.files ?? raw.Files) : [];
  return {
    sourceBranch: String(raw.sourceBranch ?? raw.SourceBranch ?? ''),
    integrationBranch: String(raw.integrationBranch ?? raw.IntegrationBranch ?? ''),
    diffStat: String(raw.diffStat ?? raw.DiffStat ?? ''),
    unifiedDiff: String(raw.unifiedDiff ?? raw.UnifiedDiff ?? ''),
    files: filesRaw.map((f: Record<string, unknown>) => ({
      path: String(f.path ?? f.Path ?? ''),
      insertions: Number(f.insertions ?? f.Insertions ?? 0),
      deletions: Number(f.deletions ?? f.Deletions ?? 0),
      changeKind: String(f.changeKind ?? f.ChangeKind ?? 'change'),
    })),
  };
}

export async function orchestrateSpace(
  spaceId: string,
  body: {
    explorerTask?: string;
    implementerTask?: string;
    verifierTask?: string;
    skipVerifier?: boolean;
  }
): Promise<SpaceOrchestrationResult> {
  const res = await fetch(`${config.apiBaseUrl}/api/v1/ide/spaces/${spaceId}/orchestrate`, {
    method: 'POST',
    headers: { ...authHeaders(), 'Content-Type': 'application/json' },
    body: JSON.stringify({
      explorerTask: body.explorerTask,
      implementerTask: body.implementerTask,
      verifierTask: body.verifierTask,
      skipVerifier: body.skipVerifier ?? false,
    }),
  });
  if (!res.ok) throw new Error(`orchestrate failed: ${res.status}`);
  const raw = await res.json();
  return {
    spaceId: String(raw.spaceId ?? raw.SpaceId ?? spaceId),
    stage: String(raw.stage ?? raw.Stage ?? ''),
    contextReady: Boolean(raw.contextReady ?? raw.ContextReady),
    explorer: normalizeMember((raw.explorer ?? raw.Explorer ?? {}) as Record<string, unknown>),
    implementer: normalizeMember((raw.implementer ?? raw.Implementer ?? {}) as Record<string, unknown>),
    verifier: raw.verifier ?? raw.Verifier
      ? normalizeMember((raw.verifier ?? raw.Verifier) as Record<string, unknown>)
      : null,
  };
}

export async function fetchWorktreeFiles(
  spaceId: string,
  memberId: string,
  path?: string
): Promise<WorktreeDirectoryListing | null> {
  const url = new URL(`${config.apiBaseUrl}/api/v1/ide/spaces/${spaceId}/members/${memberId}/files`);
  if (path) url.searchParams.set('path', path);
  const res = await fetch(url.toString(), { headers: authHeaders() });
  if (res.status === 404) return null;
  if (!res.ok) throw new Error(`worktree files failed: ${res.status}`);
  const raw = await res.json();
  const entriesRaw = Array.isArray(raw.entries ?? raw.Entries) ? (raw.entries ?? raw.Entries) : [];
  return {
    worktreePath: String(raw.worktreePath ?? raw.WorktreePath ?? ''),
    relativePath: String(raw.relativePath ?? raw.RelativePath ?? ''),
    entries: entriesRaw.map((e: Record<string, unknown>) => ({
      name: String(e.name ?? e.Name ?? ''),
      relativePath: String(e.relativePath ?? e.RelativePath ?? ''),
      isDirectory: Boolean(e.isDirectory ?? e.IsDirectory),
      sizeBytes: (e.sizeBytes ?? e.SizeBytes) as number | null | undefined,
    })),
  };
}

function normalizeMember(m: Record<string, unknown>): SpaceMemberSummary {
  return {
    memberId: String(m.memberId ?? m.MemberId ?? ''),
    spaceId: String(m.spaceId ?? m.SpaceId ?? ''),
    role: String(m.role ?? m.Role ?? ''),
    runId: (m.runId ?? m.RunId) as string | null | undefined,
    worktreePath: String(m.worktreePath ?? m.WorktreePath ?? ''),
    branchName: String(m.branchName ?? m.BranchName ?? ''),
    status: String(m.status ?? m.Status ?? ''),
    createdAtUtc: String(m.createdAtUtc ?? m.CreatedAtUtc ?? ''),
    updatedAtUtc: String(m.updatedAtUtc ?? m.UpdatedAtUtc ?? ''),
    lastError: (m.lastError ?? m.LastError) as string | null | undefined,
  };
}

function normalizeContextEvent(e: Record<string, unknown>): SpaceContextEventSummary {
  return {
    eventId: String(e.eventId ?? e.EventId ?? ''),
    spaceId: String(e.spaceId ?? e.SpaceId ?? ''),
    kind: String(e.kind ?? e.Kind ?? ''),
    title: String(e.title ?? e.Title ?? ''),
    payload: (e.payload ?? e.Payload) as string | null | undefined,
    authorMemberId: (e.authorMemberId ?? e.AuthorMemberId) as string | null | undefined,
    timestampUtc: String(e.timestampUtc ?? e.TimestampUtc ?? ''),
  };
}
