import { createSignal, For, Show } from 'solid-js';
import { AgentHierarchyService, AgentExecutionTrace } from '../../lib/services/AgentHierarchyService';

interface Props {
  task: string;
  onExecute?: (trace: AgentExecutionTrace) => void;
}

export function AgentHierarchyPanel(props: Props) {
  const [executionTrace, setExecutionTrace] = createSignal<AgentExecutionTrace | null>(null);
  const [loading, setLoading] = createSignal(false);
  const [error, setError] = createSignal<string | null>(null);

  const executeTask = async () => {
    setLoading(true);
    setError(null);

    try {
      const trace = await AgentHierarchyService.executeWithHierarchy(
        props.task,
        'IDE context',
        { language: 'csharp' }
      );

      setExecutionTrace(trace);
      props.onExecute?.(trace);
    } catch (err: any) {
      setError(err.message || 'Execution failed');
    } finally {
      setLoading(false);
    }
  };

  return (
    <div class="agent-hierarchy-panel">
      <div class="panel-header">
        <h3>🤖 Agent Hierarchy Executor</h3>
        <button onClick={executeTask} disabled={loading()} class="execute-btn">
          {loading() ? '⏳ Processing...' : '▶️ Execute Task'}
        </button>
      </div>

      <Show when={executionTrace()}>
        {(trace) => <AgentExecutionTree trace={trace()} />}
      </Show>

      <Show when={error()}>
        <div class="error-message">{error()}</div>
      </Show>
    </div>
  );
}

function AgentExecutionTree(props: { trace: AgentExecutionTrace }) {
  return (
    <div class="execution-tree">
      <AgentExecutionNode node={props.trace} level={0} />
    </div>
  );
}

function AgentExecutionNode(props: { node: AgentExecutionTrace; level: number }) {
  const [expanded, setExpanded] = createSignal(true);

  return (
    <div class={`agent-node level-${props.level}`}>
      <div
        class={`node-header ${props.node.status}`}
        onClick={() => setExpanded(!expanded())}
      >
        <span class="expand-icon">{expanded() ? '▼' : '▶'}</span>
        <span class="agent-name">{props.node.agentName}</span>
        <span class={`status-badge ${props.node.status}`}>
          {props.node.status === 'success' && '✅'}
          {props.node.status === 'failed' && '❌'}
          {props.node.status === 'running' && '⏳'}
          {props.node.status === 'pending' && '⏸️'}
        </span>
        <span class="duration">⏱️ {props.node.duration}ms</span>
      </div>

      <Show when={expanded()}>
        <div class="node-content">
          <div class="task-info">
            <strong>Task:</strong> {props.node.taskName}
          </div>

          <Show when={props.node.result}>
            <div class="result">
              <strong>Result:</strong>
              <pre>{props.node.result}</pre>
            </div>
          </Show>

          <Show when={props.node.error}>
            <div class="error">
              <strong>Error:</strong>
              <pre>{props.node.error}</pre>
            </div>
          </Show>

          <Show when={props.node.subExecutions.length > 0}>
            <div class="sub-executions">
              <h5>Sub-Agent Executions:</h5>
              <For each={props.node.subExecutions}>
                {(subNode) => <AgentExecutionNode node={subNode} level={props.level + 1} />}
              </For>
            </div>
          </Show>
        </div>
      </Show>
    </div>
  );
}