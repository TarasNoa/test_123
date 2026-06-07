import { config } from '../../../lib/config';
import { handleRuntimeEvent } from './runEventsWebSocket';
import {
  isGenerationRunWebSocketActive,
  subscribeGenerationRunWebSocket,
  stopGenerationRunWebSocket,
} from './runEventsWebSocket';

type RuntimeEvent = {
  type?: string;
  stepNumber?: number;
  toolName?: string;
  finishReason?: string;
  message?: string;
};

const activeStreams = new Map<string, EventSource>();

function subscribeGenerationRunEventsSse(runId: string): () => void {
  if (activeStreams.has(runId)) {
    return () => stopGenerationRunEvents(runId);
  }

  const url = new URL(`${config.apiBaseUrl}/api/v1/ide/app-generation/${runId}/events/stream`);
  const source = new EventSource(url.toString(), { withCredentials: true });

  source.onmessage = (msg) => {
    try {
      const payload = JSON.parse(msg.data) as RuntimeEvent;
      handleRuntimeEvent(runId, payload);
    } catch {
      // ignore malformed chunks
    }
  };

  source.onerror = () => {
    stopGenerationRunEvents(runId);
  };

  activeStreams.set(runId, source);
  return () => stopGenerationRunEvents(runId);
}

export function subscribeGenerationRunEvents(runId: string): () => void {
  const stopWs = subscribeGenerationRunWebSocket(runId);
  const stopSse = subscribeGenerationRunEventsSse(runId);
  return () => {
    stopWs();
    stopSse();
  };
}

export function stopGenerationRunEvents(runId: string) {
  stopGenerationRunWebSocket(runId);
  const source = activeStreams.get(runId);
  if (!source) return;
  source.close();
  activeStreams.delete(runId);
}

export { handleRuntimeEvent, isGenerationRunWebSocketActive };
