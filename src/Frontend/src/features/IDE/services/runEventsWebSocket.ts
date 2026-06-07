import { config } from '../../../lib/config';
import { addTimelineEvent, updateTimelineEvent } from '../IDEStore';
import {
  parseExecPolicyPromptEvent,
  publishExecPolicyPrompt,
} from './execPolicyPromptStream';

type RuntimeEvent = {
  type?: string;
  stepNumber?: number;
  toolName?: string;
  finishReason?: string;
  message?: string;
  promptId?: string;
  target?: string;
  reason?: string;
  matchedRule?: string;
  timestamp?: number;
  payload?: RuntimeEvent;
  raw?: string;
  [key: string]: unknown;
};

const activeSockets = new Map<string, WebSocket>();

function mapEventType(type: string): string {
  switch (type) {
    case 'step_start':
      return 'FixAgent';
    case 'tool_use':
      return 'CodeGenAgent';
    case 'step_finish':
      return 'ShadowBuildAgent';
    case 'error':
      return 'FixAgent';
    case 'reasoning':
      return 'PlannerAgent';
    default:
      return 'ObserverAgent';
  }
}

function unwrapPayload(data: RuntimeEvent): RuntimeEvent {
  if (data.payload && typeof data.payload === 'object') {
    return { ...data.payload, type: data.type ?? data.payload.type };
  }
  return data;
}

export function handleRuntimeEvent(runId: string, payload: RuntimeEvent) {
  const event = unwrapPayload(payload);
  const type = event.type ?? 'unknown';
  const agentId = `${runId}:${event.stepNumber ?? 0}:${type}`;

  if (type === 'step_start') {
    addTimelineEvent({
      agentId,
      agentType: mapEventType(type) as any,
      task: `step ${event.stepNumber ?? '?'}`,
      start: new Date(),
      status: 'running',
    });
    return;
  }

  if (type === 'reasoning') {
    addTimelineEvent({
      agentId: `${agentId}:reasoning`,
      agentType: mapEventType(type) as any,
      task: event.message ?? 'reasoning',
      start: new Date(),
      end: new Date(),
      status: 'completed',
    });
    return;
  }

  if (type === 'tool_use') {
    addTimelineEvent({
      agentId: `${agentId}:${event.toolName ?? 'tool'}`,
      agentType: mapEventType(type) as any,
      task: event.toolName ?? 'tool',
      start: new Date(),
      end: new Date(),
      status: 'completed',
    });
    return;
  }

  if (type.startsWith('browser-')) {
    addTimelineEvent({
      agentId: `${agentId}:${type}`,
      agentType: 'ObserverAgent' as any,
      task: type.replace('browser-', ''),
      start: new Date(),
      end: new Date(),
      status: 'completed',
    });
    return;
  }

  if (type === 'obscura_execpolicy_prompt') {
    const prompt = parseExecPolicyPromptEvent(runId, event as Record<string, unknown>);
    if (prompt) publishExecPolicyPrompt(runId, prompt);

    addTimelineEvent({
      agentId: `${agentId}:execpolicy`,
      agentType: 'ObserverAgent' as any,
      task: `Obscura consent: ${event.toolName ?? 'browser'}`,
      start: new Date(),
      status: 'running',
    });
    return;
  }

  if (type === 'step_finish' || type === 'error') {
    updateTimelineEvent(agentId, {
      end: new Date(),
      status: type === 'error' ? 'failed' : 'completed',
      task: event.finishReason ?? event.message ?? type,
    });
  }
}

function buildWebSocketUrl(runId: string): string {
  const token = localStorage.getItem('accessToken');
  const base = config.apiBaseUrl.replace(/^http/, 'ws');
  const url = new URL(`${base}/ws/events/${runId}`);
  if (token) url.searchParams.set('access_token', token);
  return url.toString();
}

export function subscribeGenerationRunWebSocket(runId: string): () => void {
  if (activeSockets.has(runId)) {
    return () => stopGenerationRunWebSocket(runId);
  }

  const socket = new WebSocket(buildWebSocketUrl(runId));

  socket.onmessage = (msg) => {
    try {
      const payload = JSON.parse(String(msg.data)) as RuntimeEvent;
      handleRuntimeEvent(runId, payload);
    } catch {
      // ignore malformed chunks
    }
  };

  socket.onclose = () => {
    activeSockets.delete(runId);
  };

  activeSockets.set(runId, socket);
  return () => stopGenerationRunWebSocket(runId);
}

export function stopGenerationRunWebSocket(runId: string) {
  const socket = activeSockets.get(runId);
  if (!socket) return;
  if (socket.readyState === WebSocket.OPEN || socket.readyState === WebSocket.CONNECTING) {
    socket.close();
  }
  activeSockets.delete(runId);
}

export function isGenerationRunWebSocketActive(runId: string): boolean {
  return activeSockets.has(runId);
}
