import { config } from '../../../lib/config';
import type { AgentFleetSummary } from './agentFleet';

export type FleetStreamEvent =
  | { type: 'snapshot'; items: AgentFleetSummary[] }
  | { type: 'status'; runId: string; status: string; stage: string; timestampUtc: string };

const activeStreams = new Map<string, EventSource>();

export function subscribeAgentFleetEvents(
  onEvent: (event: FleetStreamEvent) => void,
): () => void {
  const key = 'global';
  if (activeStreams.has(key)) {
    return () => stopAgentFleetEvents();
  }

  const url = new URL(`${config.apiBaseUrl}/api/v1/ide/agent-fleet/events/stream`);
  const source = new EventSource(url.toString(), { withCredentials: true });

  source.onmessage = (msg) => {
    try {
      const payload = JSON.parse(msg.data) as FleetStreamEvent;
      onEvent(payload);
    } catch {
      // ignore malformed chunks
    }
  };

  source.onerror = () => {
    stopAgentFleetEvents();
  };

  activeStreams.set(key, source);
  return () => stopAgentFleetEvents();
}

export function stopAgentFleetEvents() {
  const source = activeStreams.get('global');
  if (!source) return;
  source.close();
  activeStreams.delete('global');
}
