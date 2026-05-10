import { createSignal, onMount, onCleanup, For, Show } from 'solid-js';
import { agentApi, AgentEvent } from '../lib/api-client';

const statusColor = (status: string): string => {
  if (status.includes('Success') || status.includes('VAL') || status.includes('Completed') || status.includes('Idle')) {
    return 'hsl(var(--success))';
  }
  if (status.includes('BUSY') || status.includes('Processing') || status.includes('Running')) {
    return 'hsl(var(--warning))';
  }
  if (status.includes('Error') || status.includes('ERR') || status.includes('Failed')) {
    return 'hsl(var(--error))';
  }
  return 'hsl(var(--muted-foreground))';
};

export const AgentEventList = () => {
  const [events, setEvents] = createSignal<AgentEvent[]>([]);
  const [loading, setLoading] = createSignal(true);

  onMount(() => {
    const unsubscribe = agentApi.subscribeToEvents((data) => {
      setEvents(data);
      setLoading(false);
    });

    const timeout = setTimeout(() => setLoading(false), 3000);

    onCleanup(() => {
      unsubscribe();
      clearTimeout(timeout);
    });
  });

  return (
    <div class="flex flex-col h-full p-3">
      {/* Заголовок */}
      <div
        class="flex items-center justify-between mb-3"
        style={{ "flex-shrink": "0" }}
      >
        <span
          class="uppercase tracking-wider"
          style={{ "font-size": "10px", color: "hsl(var(--muted-foreground))" }}
        >
          Agent Events
        </span>
        <Show when={loading()}>
          <span
            class="animate-pulse text-xs"
            style={{ color: "hsl(var(--primary))" }}
          >
            ●
          </span>
        </Show>
      </div>

      {/* Список событий */}
      <div class="flex-1 overflow-y-auto space-y-1.5">
        <Show
          when={events().length > 0}
          fallback={
            <p
              class="text-xs text-center py-6"
              style={{ color: "hsl(var(--muted-foreground))" }}
            >
              {loading() ? "Waiting for events..." : "No events yet"}
            </p>
          }
        >
          <For each={events()}>
            {(event) => (
              <div
                class="rounded p-2.5"
                style={{
                  background: "hsl(var(--surface-2))",
                  "border-left": `3px solid ${statusColor(event.status)}`,
                }}
              >
                <div
                  class="flex justify-between items-center mb-1"
                  style={{ "font-size": "10px", color: "hsl(var(--muted-foreground))" }}
                >
                  <span
                    class="font-medium"
                    style={{ color: statusColor(event.status) }}
                  >
                    {event.status}
                  </span>
                  <span>{new Date(event.timestamp).toLocaleTimeString()}</span>
                </div>
                <Show when={event.data}>
                  <p
                    class="text-xs leading-relaxed"
                    style={{
                      color: "hsl(var(--foreground))",
                      "font-family": "monospace",
                      "word-break": "break-all",
                    }}
                  >
                    {event.data}
                  </p>
                </Show>
              </div>
            )}
          </For>
        </Show>
      </div>
    </div>
  );
};
