/// API client for communicating with C# backend
const API_BASE_URL = "http://localhost:5000";

export interface AgentEvent {
  id: string;
  runId: string;
  type: string;
  timestamp: string;
  command: string | null;
  output: string | null;
  exitCode: number | null;
  durationMs: number | null;
}

export async function fetchAgentEvents(): Promise<AgentEvent[]> {
  const response = await fetch(`${API_BASE_URL}/api/ide/agent-states/events`);
  if (!response.ok) {
    throw new Error(`Failed to fetch agent events: ${response.statusText}`);
  }
  return response.json();
}

export async function fetchEventsByRunId(runId: string): Promise<AgentEvent[]> {
  const response = await fetch(`${API_BASE_URL}/api/ide/agent-states/events/${runId}`);
  if (!response.ok) {
    throw new Error(`Failed to fetch events for run: ${response.statusText}`);
  }
  return response.json();
}
