import { createSignal, onMount, onCleanup, For } from 'solid-js';
import { agentApi, AgentEvent } from '../lib/api-client';

export const AgentEventList = () => {
  const [events, setEvents] = createSignal<AgentEvent[]>([]);
  const [loading, setLoading] = createSignal(true);

  onMount(() => {
    const unsubscribe = agentApi.subscribeToEvents((data) => {
      setEvents(data);
      setLoading(false);
    });

    // Если за 3 секунды событий нет — скрываем индикатор загрузки
    const timeout = setTimeout(() => setLoading(false), 3000);

    onCleanup(() => {
      unsubscribe();
      clearTimeout(timeout);
    });
  });

  return (
    <div class="p-4 bg-card text-card-foreground rounded-lg shadow-xl border border-border">
      <h2 class="text-xl font-bold mb-4 border-b border-border pb-2">
        Agent Activity Feed
        {loading() && <span class="ml-2 text-sm animate-pulse text-primary">●</span>}
      </h2>

      <div class="space-y-2 max-h-96 overflow-y-auto">
        <For each={events()}>{(event) => (
          <div class="p-3 bg-muted rounded border-l-4"
               classList={{
                 'border-green-500': event.status.includes('Success') || event.status.includes('VAL'),
                 'border-yellow-500': event.status.includes('BUSY'),
                 'border-red-500': event.status.includes('Error') || event.status.includes('ERR'),
                 'border-primary': !event.status.includes('Success') && !event.status.includes('VAL') && !event.status.includes('BUSY') && !event.status.includes('Error') && !event.status.includes('ERR')
               }}>
            <div class="flex justify-between text-xs text-muted-foreground">
              <span>{event.status}</span>
              <span>{new Date(event.timestamp).toLocaleTimeString()}</span>
            </div>
            <p class="mt-1 text-sm font-mono text-foreground">{event.data || 'No data payload'}</p>
          </div>
        )}</For>
      </div>
    </div>
  );
};
