import { For } from 'solid-js';

interface Agent {
  id: string;
  name?: string;
  status?: string;
}

interface AgentPanelProps {
  agents: Agent[];
}

export function AgentPanel(props: AgentPanelProps) {
  return (
    <div class="agent-panel">
      <h3>Agents</h3>
      <For each={props.agents} fallback={<p>No agents active</p>}>
        {(agent) => (
          <div class="agent-item">
            <span>{agent.name ?? agent.id}</span>
            <span class="agent-status">{agent.status ?? 'idle'}</span>
          </div>
        )}
      </For>
    </div>
  );
}
