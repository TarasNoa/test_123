export interface AgentEvent {
  status: string;
  timestamp: string;
  data?: string;
}

type Listener = (events: AgentEvent[]) => void;

// In-memory список событий. Обновляется из RealtimeService.
const eventLog: AgentEvent[] = [];
const listeners: Set<Listener> = new Set();

export const agentApi = {
  subscribeToEvents(callback: Listener): () => void {
    listeners.add(callback);
    // Сразу отдаём накопленные события если есть
    if (eventLog.length > 0) {
      callback([...eventLog]);
    }
    return () => listeners.delete(callback);
  },

  pushEvent(event: AgentEvent): void {
    eventLog.unshift(event);      // новые события сверху
    if (eventLog.length > 200) eventLog.pop(); // ограничение буфера
    listeners.forEach(fn => fn([...eventLog]));
  },
};
