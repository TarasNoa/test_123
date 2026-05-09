import { Component, createSignal } from "solid-js";
import { WorkspaceLayout } from "../shared/layouts/WorkspaceLayout";
import { Sidebar } from "../shared/layouts/Sidebar";
import { Topbar } from "../shared/layouts/Topbar";
import { AIPanel } from "../shared/layouts/AIPanel";
import { colors } from "../shared/ui/tokens";
import { IDETabs } from "../widgets/IDETabs";
import { ExecutionGraph } from "../widgets/ExecutionGraph";
import { WorkspaceSidebar } from "../widgets/WorkspaceSidebar";

/**
 * IDE Page
 * 
 * Full development environment with:
 * - File browser
 * - Code editor with tabs
 * - AI orchestration panel
 * - Execution graph
 */
export const IDE: Component = () => {
  const [activeTab, setActiveTab] = createSignal("app.tsx");

  const sidebarItems = [
    { id: "dashboard", label: "Dashboard", icon: "📊" },
    { id: "projects", label: "Projects", icon: "📁" },
    { id: "ai-agents", label: "AI Agents", icon: "🤖" },
    { id: "marketplace", label: "Marketplace", icon: "🛒" },
    { id: "ide", label: "IDE", icon: "💻", active: true },
    { id: "messages", label: "Messages", icon: "💬" },
    { id: "teams", label: "Teams", icon: "👥" },
    { id: "settings", label: "Settings", icon: "⚙️" },
  ];

  const tabs = [
    { id: "app.tsx", label: "app.tsx", icon: "⚛️", modified: true, closable: true },
    { id: "config.ts", label: "config.ts", icon: "⚙️", closable: true },
    { id: "api.ts", label: "api.ts", icon: "🔌", closable: true },
  ];

  const executionNodes = [
    { id: "1", label: "Analyze code", status: "completed" as const, duration: "0.5s" },
    { id: "2", label: "Generate tests", status: "completed" as const, duration: "1.2s" },
    { id: "3", label: "Run tests", status: "running" as const },
    { id: "4", label: "Generate docs", status: "pending" as const },
  ];

  const executionEdges = [
    { from: "1", to: "2" },
    { from: "2", to: "3" },
    { from: "3", to: "4" },
  ];

  const workspaceSections = [
    {
      title: "Files",
      items: [
        { id: "src", label: "src", icon: "📁", count: 12 },
        { id: "components", label: "components", icon: "📁", count: 8 },
        { id: "pages", label: "pages", icon: "📁", count: 5 },
        { id: "utils", label: "utils", icon: "📁", count: 3 },
      ],
    },
    {
      title: "Tasks",
      items: [
        { id: "task1", label: "Implement auth", icon: "📋" },
        { id: "task2", label: "Add tests", icon: "📋" },
        { id: "task3", label: "Refactor API", icon: "📋" },
      ],
    },
  ];

  return (
    <WorkspaceLayout
      topbar={
        <Topbar
          title="IDE - Libr4 IDE"
          user={{ name: "User" }}
        />
      }
      sidebar={
        <Sidebar
          items={sidebarItems}
          onItemSelect={(id) => console.log("Selected:", id)}
        />
      }
      aiPanel={
        <AIPanel
          messages={[
            {
              id: "1",
              type: "thought",
              content: "Analyzing component structure...",
              timestamp: new Date(),
            },
          ]}
          onMessageSend={(msg) => console.log("Message:", msg)}
        />
      }
    >
      <div class="flex h-full">
        {/* Workspace Sidebar */}
        <div class="w-64 border-r" style={{ "border-color": colors.border }}>
          <WorkspaceSidebar
            sections={workspaceSections}
            onItemSelect={(sectionId, itemId) => console.log("Selected:", sectionId, itemId)}
          />
        </div>

        {/* Main Editor Area */}
        <div class="flex-1 flex flex-col">
          {/* IDE Tabs */}
          <IDETabs
            tabs={tabs}
            activeTab={activeTab()}
            onTabChange={setActiveTab}
            onTabClose={(id) => console.log("Close tab:", id)}
          />

          {/* Editor Content */}
          <div class="flex-1 p-4 overflow-auto">
            <div
              class="h-full rounded-lg p-4 font-mono text-sm"
              style={{
                "background-color": colors.bg,
                border: `1px solid ${colors.border}`,
                color: colors.text,
              }}
            >
              <pre>{`import { Component } from "solid-js";

export const App: Component = () => {
  return (
    <div class="p-4">
      <h1>Hello Libr4 IDE</h1>
    </div>
  );
};`}</pre>
            </div>
          </div>

          {/* Execution Graph */}
          <div class="p-4 border-t" style={{ "border-color": colors.border }}>
            <h3 class="text-sm font-semibold mb-3" style={{ color: colors.text }}>
              Execution Graph
            </h3>
            <ExecutionGraph
              nodes={executionNodes}
              edges={executionEdges}
            />
          </div>
        </div>
      </div>
    </WorkspaceLayout>
  );
};
