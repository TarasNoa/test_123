export interface ExecPolicyPromptPayload {
  id: string;
  toolName: string;
  target?: string | null;
  reason: string;
  matchedRule?: string | null;
  createdAtUtc: string;
  kind: 'obscura_execpolicy';
}

type Listener = (prompt: ExecPolicyPromptPayload) => void;

const listenersByRun = new Map<string, Set<Listener>>();

export function subscribeExecPolicyPromptStream(runId: string, listener: Listener): () => void {
  const set = listenersByRun.get(runId) ?? new Set<Listener>();
  set.add(listener);
  listenersByRun.set(runId, set);
  return () => {
    set.delete(listener);
    if (set.size === 0) listenersByRun.delete(runId);
  };
}

export function publishExecPolicyPrompt(runId: string, prompt: ExecPolicyPromptPayload) {
  listenersByRun.get(runId)?.forEach((listener) => listener(prompt));
}

export function parseExecPolicyPromptEvent(
  runId: string,
  event: Record<string, unknown>,
): ExecPolicyPromptPayload | null {
  if (event.type !== 'obscura_execpolicy_prompt') return null;

  const id = String(event.promptId ?? event.id ?? '');
  if (!id) return null;

  return {
    id,
    toolName: String(event.toolName ?? ''),
    target: (event.target as string | null | undefined) ?? null,
    reason: String(event.reason ?? 'obscura_external_consent_required'),
    matchedRule: (event.matchedRule as string | null | undefined) ?? null,
    createdAtUtc: new Date(
      typeof event.timestamp === 'number' ? event.timestamp : Date.now(),
    ).toISOString(),
    kind: 'obscura_execpolicy',
  };
}
