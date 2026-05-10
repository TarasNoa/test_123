import { Component, createSignal, onMount, onCleanup, JSX } from "solid-js";
import { AgentEventList } from "../../components/AgentEventList";
import { RealtimeService } from "../../lib/RealtimeService";
import { config } from "../../lib/config";
import { WorkspaceLayout } from "../../shared/layouts/WorkspaceLayout";

const IDE: Component = () => {
  const [code, setCode] = createSignal(
    "// Welcome to Libr4 IDE\n// Write your code here\n\nconsole.log('Hello, Golden Stack!');"
  );
  const [output, setOutput] = createSignal("");
  const [isExecuting, setIsExecuting] = createSignal(false);
  const [isAgentBusy, setIsAgentBusy] = createSignal(false);
  const [agentId, setAgentId] = createSignal<string>("");
  const [connectionError, setConnectionError] = createSignal<string | null>(null);

  // Получаем agentId из URL или из API текущего пользователя
  const initAgent = async () => {
    const params = new URLSearchParams(window.location.search);
    const urlAgentId = params.get("agentId");
    if (urlAgentId) {
      setAgentId(urlAgentId);
      return;
    }

    // Если нет в URL — пробуем получить из API
    const token = localStorage.getItem("access_token");
    if (!token) {
      setConnectionError("Не авторизован. Войдите в систему.");
      return;
    }

    try {
      const response = await fetch(`${config.apiBaseUrl}/api/ide/agents/my`, {
        headers: { Authorization: `Bearer ${token}` },
      });
      if (response.ok) {
        const data = await response.json();
        setAgentId(data.agentId ?? data.id ?? "");
      } else {
        setConnectionError("Не удалось получить агента. Проверьте авторизацию.");
      }
    } catch {
      setConnectionError("Сервер недоступен.");
    }
  };

  const executeCode = async () => {
    if (!agentId()) {
      setOutput("Ошибка: agentId не инициализирован");
      return;
    }

    setIsExecuting(true);
    setIsAgentBusy(true);
    setOutput("Executing in Rust sandbox...");

    const token = localStorage.getItem("access_token");

    try {
      const response = await fetch(`${config.apiBaseUrl}/api/ide/agent-states/run`, {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
          ...(token ? { Authorization: `Bearer ${token}` } : {}),
        },
        body: JSON.stringify({ code: code(), language: "python" }),
      });

      if (!response.ok) {
        const text = await response.text();
        throw new Error(`HTTP ${response.status}: ${text}`);
      }

      // Результат придёт через SignalR OnAgentStateUpdated
    } catch (error) {
      setOutput(`Error: ${error instanceof Error ? error.message : String(error)}`);
      setIsExecuting(false);
      setIsAgentBusy(false);
    }
  };

  onMount(async () => {
    await initAgent();

    if (!agentId()) return;

    const realtimeService = RealtimeService.getInstance();
    try {
      await realtimeService.start(agentId(), (data) => {
        const state = (data as { State?: string }).State ?? "";
        console.log("Agent state changed:", state);

        if (state.includes("IDLE") || state.includes("Idle") || state.includes("Completed")) {
          setIsExecuting(false);
          setIsAgentBusy(false);
          setOutput("Execution completed successfully!");
        } else if (state.includes("ERROR") || state.includes("Failed") || state.includes("Error")) {
          setIsExecuting(false);
          setIsAgentBusy(false);
          setOutput(`Execution failed: ${state}`);
        }
      });
    } catch (err) {
      setConnectionError(`SignalR: ${err instanceof Error ? err.message : String(err)}`);
    }
  });

  onCleanup(() => {
    const realtimeService = RealtimeService.getInstance();
    realtimeService.stop(agentId());
  });

  return (
    <WorkspaceLayout
      topbar={
        <div class="flex items-center justify-between h-full px-4">
          <h1 class="text-xl font-bold text-foreground">Libr4 IDE</h1>
          <div class="flex gap-2 items-center">
            {connectionError() && (
              <span class="text-sm text-destructive mr-2">{connectionError()}</span>
            )}
            <button
              class="px-4 py-2 bg-secondary text-secondary-foreground rounded hover:opacity-90"
              onClick={() => setCode("")}
            >
              Clear
            </button>
            <button
              class="px-4 py-2 bg-primary text-primary-foreground rounded hover:opacity-90 disabled:opacity-50"
              onClick={executeCode}
              disabled={isExecuting() || isAgentBusy() || !agentId()}
            >
              {isExecuting() ? "Running..." : isAgentBusy() ? "Agent Busy" : "Run"}
            </button>
          </div>
        </div>
      }
      sidebar={
        <div class="p-3">
          {/* File tree placeholder */}
          <div class="text-sm text-muted-foreground">Explorer</div>
        </div>
      }
      aiPanel={<AgentEventList />}
    >
      <div class="flex flex-col h-full">
        <div class="flex-1 p-4">
          <textarea
            class="w-full h-full p-4 font-mono text-sm border rounded resize-none bg-card text-card-foreground"
            value={code()}
            onInput={(e) => setCode(e.currentTarget.value)}
            placeholder="Write your code here..."
          />
        </div>

        <div class="flex-1 p-4 border-t flex flex-col">
          <h2 class="text-sm font-semibold mb-2 text-foreground">Output</h2>
          <pre class="flex-1 w-full p-4 font-mono text-sm border rounded bg-muted text-muted-foreground overflow-auto whitespace-pre-wrap">
            {output() || "No output yet"}
          </pre>
        </div>
      </div>
    </WorkspaceLayout>
  );
};

export default IDE;
