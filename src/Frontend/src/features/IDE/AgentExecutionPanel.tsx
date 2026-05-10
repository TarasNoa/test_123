import { createSignal, createEffect, Show, For } from 'solid-js';
import { AgentExecutionService, ExecutionStatus } from '../../lib/services/AgentExecutionService';

interface Props {
  code: string;
  language: string;
  task: string;
  onExecute?: () => void;
}

export function AgentExecutionPanel(props: Props) {
  const [executionId, setExecutionId] = createSignal<string | null>(null);
  const [context, setContext] = createSignal<any>(null);
  const [loading, setLoading] = createSignal(false);
  const [error, setError] = createSignal<string | null>(null);

  const executeCode = async () => {
    setLoading(true);
    setError(null);

    try {
      const response = await AgentExecutionService.executeCode(
        props.code,
        props.language,
        props.task
      );

      setExecutionId(response.id);

      // Poll for execution completion
      const result = await AgentExecutionService.pollExecutionStatus(response.id);
      setContext(result);

      if (result.currentStatus === ExecutionStatus.Failed && result.lastError) {
        setError(result.lastError);
      }

      props.onExecute?.();
    } catch (err: any) {
      setError(err.message || 'Execution failed');
    } finally {
      setLoading(false);
    }
  };

  return (
    <div class="agent-execution-panel">
      <div class="execution-header">
        <h3>🤖 Agent Code Execution</h3>
        <button
          onClick={executeCode}
          disabled={loading()}
          class={`execute-btn ${loading() ? 'disabled' : ''}`}
        >
          {loading() ? '⏳ Executing...' : '▶️ Execute & Auto-Fix'}
        </button>
      </div>

      <Show when={context()}>
        {(ctx) => (
          <div class="execution-results">
            {/* Status Badge */}
            <div class={`status-badge ${ctx().currentStatus.toLowerCase()}`}>
              {ctx().currentStatus === ExecutionStatus.Success && '✅ Success'}
              {ctx().currentStatus === ExecutionStatus.Failed && '❌ Failed'}
              {ctx().currentStatus === ExecutionStatus.FixRequired && '🔧 Fixed'}
              {ctx().currentStatus === ExecutionStatus.Running && '⏳ Running'}
            </div>

            {/* Attempts */}
            <div class="attempts-info">
              <span>Attempt {ctx().currentAttempt} of {ctx().maxRetryAttempts}</span>
            </div>

            {/* Code Generations */}
            <div class="code-generations">
              <h4>📝 Code Generations ({ctx().codeGenerations.length})</h4>
              <For each={ctx().codeGenerations}>
                {(generation, index) => (
                  <details class="code-gen-item">
                    <summary>
                      {generation.description || `Version ${index() + 1}`}
                      <span class="lang-badge">{generation.language}</span>
                    </summary>
                    <pre>
                      <code>{generation.code}</code>
                    </pre>
                  </details>
                )}
              </For>
            </div>

            {/* Execution Results */}
            <div class="execution-results-list">
              <h4>📊 Execution Results ({ctx().executionResults.length})</h4>
              <For each={ctx().executionResults}>
                {(result) => (
                  <div class={`result-item ${result.status.toLowerCase()}`}>
                    <div class="result-header">
                      <span class="status">{result.status}</span>
                      <span class="time">⏱️ {result.executionTime}</span>
                      <span class="attempt">Attempt #{result.attemptNumber}</span>
                    </div>

                    <Show when={result.output}>
                      <div class="output">
                        <strong>Output:</strong>
                        <pre>{result.output}</pre>
                      </div>
                    </Show>

                    <Show when={result.errorMessage}>
                      <div class="error">
                        <strong>Error:</strong>
                        <pre>{result.errorMessage}</pre>
                      </div>
                    </Show>
                  </div>
                )}
              </For>
            </div>

            {/* Timing */}
            <div class="execution-time">
              <span>Started: {new Date(ctx().startedAt).toLocaleTimeString()}</span>
              <Show when={ctx().completedAt}>
                <span>Completed: {new Date(ctx().completedAt).toLocaleTimeString()}</span>
              </Show>
            </div>
          </div>
        )}
      </Show>

      <Show when={error()}>
        <div class="error-banner">
          <strong>❌ Error:</strong> {error()}
        </div>
      </Show>
    </div>
  );
}