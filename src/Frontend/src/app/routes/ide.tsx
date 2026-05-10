import { Component, createSignal, onMount, onCleanup, Show } from "solid-js";
import { WorkspaceLayout } from "../../shared/layouts/WorkspaceLayout";
import { AgentEventList } from "../../components/AgentEventList";
import { RealtimeService } from "../../lib/RealtimeService";
import { config } from "../../lib/config";

// ── Иконки (inline SVG, без внешних зависимостей) ──────────────────────────

const IconPlay = () => (
  <svg width="14" height="14" viewBox="0 0 24 24" fill="currentColor">
    <path d="M8 5v14l11-7z" />
  </svg>
);

const IconStop = () => (
  <svg width="14" height="14" viewBox="0 0 24 24" fill="currentColor">
    <rect x="6" y="6" width="12" height="12" rx="1" />
  </svg>
);

const IconClear = () => (
  <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round">
    <path d="M3 6h18M8 6V4h8v2M19 6l-1 14H6L5 6" />
  </svg>
);

const IconFile = () => (
  <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5">
    <path d="M14 2H6a2 2 0 00-2 2v16a2 2 0 002 2h12a2 2 0 002-2V8z" />
    <polyline points="14 2 14 8 20 8" />
  </svg>
);

const IconFolder = () => (
  <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5">
    <path d="M22 19a2 2 0 01-2 2H4a2 2 0 01-2-2V5a2 2 0 012-2h5l2 3h9a2 2 0 012 2z" />
  </svg>
);

const IconDot = (props: { color?: string }) => (
  <span
    style={{
      display: "inline-block",
      width: "6px",
      height: "6px",
      "border-radius": "50%",
      background: props.color ?? "hsl(var(--muted-foreground))",
      "flex-shrink": "0",
    }}
  />
);

// ── Вспомогательные компоненты ───────────────────────────────────────────────

const StatusDot = (props: { state: "idle" | "busy" | "error" | "offline" }) => {
  const colors = {
    idle:    "hsl(var(--success))",
    busy:    "hsl(var(--warning))",
    error:   "hsl(var(--error))",
    offline: "hsl(var(--muted-foreground))",
  };
  return <IconDot color={colors[props.state]} />;
};

// ── Фиктивное дерево файлов (placeholder до реального API) ──────────────────

const FileTree = () => (
  <div class="p-3 text-xs" style={{ color: "hsl(var(--muted-foreground))" }}>
    <div class="mb-2 uppercase tracking-wider" style={{ "font-size": "10px", "letter-spacing": "0.08em" }}>
      Explorer
    </div>

    <div class="space-y-0.5">
      {[
        { icon: <IconFolder />, name: "src", depth: 0 },
        { icon: <IconFolder />, name: "lib", depth: 1 },
        { icon: <IconFile />,   name: "config.ts", depth: 2, active: false },
        { icon: <IconFile />,   name: "api-client.ts", depth: 2, active: false },
        { icon: <IconFolder />, name: "routes", depth: 1 },
        { icon: <IconFile />,   name: "ide.tsx", depth: 2, active: true },
        { icon: <IconFile />,   name: "home.tsx", depth: 2, active: false },
        { icon: <IconFolder />, name: "components", depth: 1 },
        { icon: <IconFile />,   name: "AgentEventList.tsx", depth: 2, active: false },
      ].map((item) => (
        <div
          class="flex items-center gap-1.5 px-2 py-1 rounded cursor-pointer select-none"
          style={{
            "padding-left": `${8 + item.depth * 12}px`,
            background: (item as { active?: boolean }).active
              ? "hsl(var(--primary) / 0.12)"
              : "transparent",
            color: (item as { active?: boolean }).active
              ? "hsl(var(--primary))"
              : "hsl(var(--muted-foreground))",
            transition: "background 0.15s",
          }}
        >
          <span style={{ opacity: "0.7" }}>{item.icon}</span>
          <span>{item.name}</span>
        </div>
      ))}
    </div>
  </div>
);

// ── Главный компонент IDE ────────────────────────────────────────────────────

const IDE: Component = () => {
  const [code, setCode] = createSignal(
    "// Welcome to Libr4 IDE\n// Write your code here\n\nconsole.log('Hello, Golden Stack!');"
  );
  const [output, setOutput] = createSignal("");
  const [isExecuting, setIsExecuting] = createSignal(false);
  const [isAgentBusy, setIsAgentBusy] = createSignal(false);
  const [agentId, setAgentId] = createSignal<string>("");
  const [connectionError, setConnectionError] = createSignal<string | null>(null);
  const [agentState, setAgentState] = createSignal<"idle" | "busy" | "error" | "offline">("offline");
  const [currentFile, setCurrentFile] = createSignal("main.py");

  // ── Инициализация agentId ─────────────────────────────────────────────────

  const initAgent = async () => {
    const params = new URLSearchParams(window.location.search);
    const urlAgentId = params.get("agentId");
    if (urlAgentId) {
      setAgentId(urlAgentId);
      setAgentState("idle");
      return;
    }

    const token = localStorage.getItem("access_token");
    if (!token) {
      setConnectionError("Не авторизован. Войдите в систему.");
      setAgentState("offline");
      return;
    }

    try {
      const response = await fetch(`${config.apiBaseUrl}/api/ide/agents/my`, {
        headers: { Authorization: `Bearer ${token}` },
      });
      if (response.ok) {
        const data = await response.json();
        setAgentId(data.agentId ?? data.id ?? "");
        setAgentState("idle");
      } else {
        setConnectionError("Не удалось получить агента.");
        setAgentState("error");
      }
    } catch {
      setConnectionError("Сервер недоступен.");
      setAgentState("offline");
    }
  };

  // ── Выполнение кода ───────────────────────────────────────────────────────

  const executeCode = async () => {
    if (!agentId()) {
      setOutput("Ошибка: agentId не инициализирован");
      return;
    }

    setIsExecuting(true);
    setIsAgentBusy(true);
    setAgentState("busy");
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
      // Результат придёт через SignalR → OnAgentStateUpdated
    } catch (error) {
      setOutput(`Error: ${error instanceof Error ? error.message : String(error)}`);
      setIsExecuting(false);
      setIsAgentBusy(false);
      setAgentState("error");
    }
  };

  // ── SignalR подписка ──────────────────────────────────────────────────────

  onMount(async () => {
    await initAgent();
    if (!agentId()) return;

    const rt = RealtimeService.getInstance();
    try {
      await rt.start(agentId(), (data) => {
        const state = (data as { State?: string }).State ?? "";

        if (state.includes("IDLE") || state.includes("Idle") || state.includes("Completed")) {
          setIsExecuting(false);
          setIsAgentBusy(false);
          setAgentState("idle");
          setOutput("Execution completed successfully!");
        } else if (state.includes("ERROR") || state.includes("Failed")) {
          setIsExecuting(false);
          setIsAgentBusy(false);
          setAgentState("error");
          setOutput(`Execution failed: ${state}`);
        } else if (state.includes("Processing") || state.includes("BUSY")) {
          setAgentState("busy");
        }
      });
    } catch (err) {
      setConnectionError(`SignalR: ${err instanceof Error ? err.message : String(err)}`);
      setAgentState("offline");
    }
  });

  onCleanup(() => {
    RealtimeService.getInstance().stop(agentId());
  });

  // ── Кнопка Run ───────────────────────────────────────────────────────────

  const canRun = () => !isExecuting() && !isAgentBusy() && !!agentId();

  const runLabel = () => {
    if (isExecuting()) return "Running...";
    if (isAgentBusy()) return "Agent Busy";
    return "Run";
  };

  // ── Render ────────────────────────────────────────────────────────────────

  return (
    <WorkspaceLayout

      // ── Topbar ──────────────────────────────────────────────────────────
      topbar={
        <div class="flex items-center justify-between w-full px-4 h-full">
          {/* Лого */}
          <div class="flex items-center gap-3">
            <span
              class="font-bold text-sm tracking-tight"
              style={{ color: "hsl(var(--primary))" }}
            >
              LIBR4
            </span>
            <span
              class="text-xs px-2 py-0.5 rounded"
              style={{
                background: "hsl(var(--surface-3))",
                color: "hsl(var(--muted-foreground))",
              }}
            >
              IDE
            </span>
          </div>

          {/* Текущий файл */}
          <div
            class="flex items-center gap-2 text-xs px-3 py-1.5 rounded"
            style={{
              background: "hsl(var(--surface-3))",
              color: "hsl(var(--muted-foreground))",
            }}
          >
            <IconFile />
            <span>{currentFile()}</span>
          </div>

          {/* Правые контролы */}
          <div class="flex items-center gap-2">
            {/* Статус агента */}
            <div
              class="flex items-center gap-2 text-xs px-3 py-1.5 rounded"
              style={{
                background: "hsl(var(--surface-3))",
                color: "hsl(var(--muted-foreground))",
              }}
            >
              <StatusDot state={agentState()} />
              <span>
                {agentState() === "idle"    && "Agent ready"}
                {agentState() === "busy"    && "Executing..."}
                {agentState() === "error"   && "Error"}
                {agentState() === "offline" && "Offline"}
              </span>
            </div>

            {/* Ошибка подключения */}
            <Show when={connectionError()}>
              <span class="text-xs text-error">{connectionError()}</span>
            </Show>

            {/* Clear */}
            <button
              class="flex items-center gap-1.5 px-3 py-1.5 rounded text-xs transition-all"
              style={{
                background: "hsl(var(--surface-3))",
                color: "hsl(var(--muted-foreground))",
              }}
              onClick={() => { setCode(""); setOutput(""); }}
            >
              <IconClear />
              Clear
            </button>

            {/* Run */}
            <button
              class="flex items-center gap-1.5 px-4 py-1.5 rounded text-xs font-medium transition-all"
              style={{
                background: canRun()
                  ? "hsl(var(--primary))"
                  : "hsl(var(--surface-3))",
                color: canRun()
                  ? "hsl(var(--primary-foreground))"
                  : "hsl(var(--muted-foreground))",
                opacity: canRun() ? "1" : "0.5",
                cursor: canRun() ? "pointer" : "not-allowed",
                "box-shadow": canRun() ? "0 0 12px hsl(var(--primary) / 0.35)" : "none",
                transition: "all 0.15s ease",
              }}
              onClick={executeCode}
              disabled={!canRun()}
            >
              <Show when={!isExecuting()} fallback={<IconStop />}>
                <IconPlay />
              </Show>
              {runLabel()}
            </button>
          </div>
        </div>
      }

      // ── Sidebar ─────────────────────────────────────────────────────────
      sidebar={<FileTree />}

      // ── AI Panel ────────────────────────────────────────────────────────
      aiPanel={
        <div class="flex flex-col h-full">
          {/* Output секция */}
          <div class="p-3 border-b border-surface-3">
            <div
              class="text-xs mb-2 uppercase tracking-wider"
              style={{ color: "hsl(var(--muted-foreground))", "font-size": "10px" }}
            >
              Output
            </div>
            <pre
              class="text-xs leading-relaxed overflow-auto"
              style={{
                color: output().startsWith("Error")
                  ? "hsl(var(--error))"
                  : "hsl(var(--foreground))",
                "min-height": "48px",
                "max-height": "140px",
                "white-space": "pre-wrap",
                "word-break": "break-word",
              }}
            >
              {output() || <span style={{ color: "hsl(var(--muted-foreground))" }}>No output yet</span>}
            </pre>
          </div>

          {/* AgentEventList занимает остаток высоты */}
          <div class="flex-1 overflow-hidden">
            <AgentEventList />
          </div>
        </div>
      }
    >

      {/* ── Editor (main area) ─────────────────────────────────────────── */}
      <div class="flex flex-col h-full">
        {/* Строка с названием файла */}
        <div
          class="flex items-center gap-2 px-4 border-b border-surface-3 text-xs"
          style={{
            height: "32px",
            color: "hsl(var(--muted-foreground))",
            background: "hsl(var(--surface-2))",
            "flex-shrink": "0",
          }}
        >
          <IconFile />
          <span style={{ color: "hsl(var(--foreground))" }}>{currentFile()}</span>
          <span class="ml-auto">Python</span>
        </div>

        {/* Textarea — занимает всё оставшееся место */}
        <textarea
          class="flex-1 w-full resize-none outline-none p-4 text-sm leading-relaxed"
          style={{
            background: "hsl(var(--background))",
            color: "hsl(var(--foreground))",
            border: "none",
            "font-family": "'JetBrains Mono', 'Fira Code', monospace",
            "tab-size": "4",
            "caret-color": "hsl(var(--primary))",
          }}
          value={code()}
          onInput={(e) => setCode(e.currentTarget.value)}
          onKeyDown={(e) => {
            // Tab → 4 пробела вместо потери фокуса
            if (e.key === "Tab") {
              e.preventDefault();
              const start = e.currentTarget.selectionStart;
              const end = e.currentTarget.selectionEnd;
              const val = code();
              setCode(val.substring(0, start) + "    " + val.substring(end));
              // Возвращаем курсор
              requestAnimationFrame(() => {
                e.currentTarget.selectionStart = start + 4;
                e.currentTarget.selectionEnd = start + 4;
              });
            }
            // Cmd/Ctrl+Enter → Run
            if ((e.metaKey || e.ctrlKey) && e.key === "Enter" && canRun()) {
              e.preventDefault();
              executeCode();
            }
          }}
          spellcheck={false}
          autocomplete="off"
          placeholder="// Write your code here..."
        />
      </div>
    </WorkspaceLayout>
  );
};

export default IDE;
