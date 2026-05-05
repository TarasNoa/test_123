/// API client for communicating with C# backend
const API_BASE = "http://localhost:5000/api/ide/agent-states";

export interface AgentEvent {
  id: string;
  agentId: string;
  runId: string;
  status: string;
  data: string;
  timestamp: string;
}

export const agentApi = {
  // Fetching with typing
  async fetchAgentEvents(): Promise<AgentEvent[]> {
    const response = await fetch(`${API_BASE}/events`);
    if (!response.ok) throw new Error("Failed to fetch events");
    return response.json();
  },

  async fetchEventsByRunId(runId: string): Promise<AgentEvent[]> {
    const response = await fetch(`${API_BASE}/events/${runId}`);
    if (!response.ok) throw new Error("Failed to fetch events for run");
    return response.json();
  },

  // Polling (auto-update every 3 sec while Rust is running)
  subscribeToEvents(callback: (events: AgentEvent[]) => void) {
    const interval = setInterval(async () => {
      try {
        const data = await this.fetchAgentEvents();
        callback(data);
      } catch (e) {
        console.error("Polling error:", e);
      }
    }, 3000);

    return () => clearInterval(interval); // Unsubscribe function
  }
};
