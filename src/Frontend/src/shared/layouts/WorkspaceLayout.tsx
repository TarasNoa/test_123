import { Component, JSX } from "solid-js";

interface WorkspaceLayoutProps {
  topbar?: JSX.Element;
  sidebar?: JSX.Element;
  aiPanel?: JSX.Element;
  children?: JSX.Element;
}

/**
 * WorkspaceLayout
 *
 * Трёхпанельный layout IDE:
 *   [topbar — 64px]
 *   [sidebar 240px] [children flex-1] [aiPanel 320px]
 *
 * Все цвета через CSS-переменные из app.css / tokens.ts
 */
export const WorkspaceLayout: Component<WorkspaceLayoutProps> = (props) => {
  return (
    <div class="flex flex-col h-screen bg-background text-foreground overflow-hidden">

      {/* ── Topbar ── */}
      <header
        class="flex-shrink-0 flex items-center border-b border-surface-3"
        style={{ height: "64px", "background-color": "hsl(var(--surface))" }}
      >
        {props.topbar}
      </header>

      {/* ── Main row ── */}
      <div class="flex flex-1 overflow-hidden">

        {/* ── Sidebar ── */}
        <aside
          class="hidden md:flex flex-shrink-0 border-r border-surface-3 overflow-y-auto"
          style={{
            width: "240px",
            "background-color": "hsl(var(--surface))",
            transition: "width 0.2s ease",
          }}
        >
          {props.sidebar}
        </aside>

        {/* ── Editor / main area ── */}
        <main class="flex-1 overflow-hidden flex flex-col">
          {props.children}
        </main>

        {/* ── AI Panel ── */}
        <aside
          class="hidden md:flex flex-shrink-0 border-l border-surface-3 overflow-y-auto flex flex-col"
          style={{
            width: "320px",
            "background-color": "hsl(var(--surface))",
            transition: "width 0.2s ease",
          }}
        >
          {props.aiPanel}
        </aside>
      </div>
    </div>
  );
};
