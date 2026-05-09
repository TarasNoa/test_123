import { Component, createSignal } from "solid-js";
import { WorkspaceLayout } from "../shared/layouts/WorkspaceLayout";
import { Sidebar } from "../shared/layouts/Sidebar";
import { Topbar } from "../shared/layouts/Topbar";
import { AIPanel } from "../shared/layouts/AIPanel";
import { colors, spacing } from "../shared/ui/tokens";
import { WorkspaceSidebar } from "../widgets/WorkspaceSidebar";
import { AgentPanel } from "../widgets/AgentPanel";
import { ExecutionGraph } from "../widgets/ExecutionGraph";
import { AIActivityFeed } from "../widgets/AIActivityFeed";

/**
 * Project Workspace Page
 * 
 * Main project workspace with:
 * - Left: Project structure (roadmap, files, tasks, collaborators)
 * - Center: Active work area (kanban, editor, preview, documents)
 * - Right: AI orchestration (thoughts, tasks, analysis, suggestions, execution graph)
 */
export const ProjectWorkspace: Component = () => {
  const [activeView, setActiveView] = createSignal("kanban");

  const sidebarItems = [
    { id: "dashboard", label: "Dashboard", icon: "📊" },
    { id: "projects", label: "Projects", icon: "📁", active: true },
    { id: "ai-agents", label: "AI Agents", icon: "🤖" },
    { id: "marketplace", label: "Marketplace", icon: "🛒" },
    { id: "ide", label: "IDE", icon: "💻" },
    { id: "messages", label: "Messages", icon: "💬" },
    { id: "teams", label: "Teams", icon: "👥" },
    { id: "settings", label: "Settings", icon: "⚙️" },
  ];

  const workspaceSections = [
    {
      title: "Roadmap",
      items: [
        { id: "phase1", label: "Phase 1: Foundation", icon: "🎯" },
        { id: "phase2", label: "Phase 2: Features", icon: "🎯" },
        { id: "phase3", label: "Phase 3: Launch", icon: "🎯" },
      ],
    },
    {
      title: "Files",
      items: [
        { id: "src", label: "src", icon: "📁", count: 24 },
        { id: "tests", label: "tests", icon: "📁", count: 12 },
        { id: "docs", label: "docs", icon: "📁", count: 5 },
      ],
    },
    {
      title: "Tasks",
      items: [
        { id: "todo", label: "To Do", icon: "📋", count: 8 },
        { id: "inprogress", label: "In Progress", icon: "🔄", count: 3 },
        { id: "done", label: "Done", icon: "✅", count: 15 },
      ],
    },
    {
      title: "Collaborators",
      items: [
        { id: "user1", label: "John Doe", icon: "👤" },
        { id: "user2", label: "Jane Smith", icon: "👤" },
        { id: "user3", label: "AI Agent", icon: "🤖" },
      ],
    },
  ];

  const executionNodes = [
    { id: "1", label: "Analyze requirements", status: "completed" as const, duration: "2s" },
    { id: "2", label: "Generate architecture", status: "completed" as const, duration: "5s" },
    { id: "3", label: "Create components", status: "running" as const },
    { id: "4", label: "Write tests", status: "pending" as const },
    { id: "5", label: "Deploy", status: "pending" as const },
  ];

  const executionEdges = [
    { from: "1", to: "2" },
    { from: "2", to: "3" },
    { from: "3", to: "4" },
    { from: "4", to: "5" },
  ];

  const activities = [
    {
      id: "1",
      type: "thought" as const,
      content: "Analyzing project requirements...",
      timestamp: new Date(),
    },
    {
      id: "2",
      type: "task" as const,
      content: "Creating component structure",
      timestamp: new Date(Date.now() - 30000),
    },
    {
      id: "3",
      type: "analysis" as const,
      content: "Architecture review complete",
      timestamp: new Date(Date.now() - 60000),
    },
    {
      id: "4",
      type: "suggestion" as const,
      content: "Consider adding error handling",
      timestamp: new Date(Date.now() - 90000),
    },
  ];

  return (
    <WorkspaceLayout
      topbar={
        <Topbar
          title="Project Workspace - Libr4 IDE"
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
        <div class="flex flex-col h-full gap-4">
          <AgentPanel
            agentName="Project Orchestrator"
            status="active"
            currentTask="Creating component structure"
            progress={60}
          />
          <div class="flex-1 overflow-auto">
            <h3 class="text-sm font-semibold mb-3" style={{ color: colors.text }}>
              Activity Feed
            </h3>
            <AIActivityFeed activities={activities} />
          </div>
          <div>
            <h3 class="text-sm font-semibold mb-3" style={{ color: colors.text }}>
              Execution Graph
            </h3>
            <ExecutionGraph
              nodes={executionNodes}
              edges={executionEdges}
            />
          </div>
        </div>
      }
    >
      <div class="flex h-full">
        {/* Left: Project Structure */}
        <div class="w-64 border-r" style={{ "border-color": colors.border }}>
          <WorkspaceSidebar
            sections={workspaceSections}
            onItemSelect={(sectionId, itemId) => console.log("Selected:", sectionId, itemId)}
          />
        </div>

        {/* Center: Active Work Area */}
        <div class="flex-1 flex flex-col">
          {/* View Toggle */}
          <div class="flex gap-2 p-4 border-b" style={{ "border-color": colors.border }}>
            <button
              onClick={() => setActiveView("kanban")}
              class="px-4 py-2 rounded-lg text-sm font-medium"
              style={{
                "background-color": activeView() === "kanban" ? colors.turquoise : colors.surface2,
                color: activeView() === "kanban" ? colors.bg : colors.text,
                border: activeView() === "kanban" ? "none" : `1px solid ${colors.border}`,
              }}
            >
              Kanban
            </button>
            <button
              onClick={() => setActiveView("editor")}
              class="px-4 py-2 rounded-lg text-sm font-medium"
              style={{
                "background-color": activeView() === "editor" ? colors.turquoise : colors.surface2,
                color: activeView() === "editor" ? colors.bg : colors.text,
                border: activeView() === "editor" ? "none" : `1px solid ${colors.border}`,
              }}
            >
              Editor
            </button>
            <button
              onClick={() => setActiveView("preview")}
              class="px-4 py-2 rounded-lg text-sm font-medium"
              style={{
                "background-color": activeView() === "preview" ? colors.turquoise : colors.surface2,
                color: activeView() === "preview" ? colors.bg : colors.text,
                border: activeView() === "preview" ? "none" : `1px solid ${colors.border}`,
              }}
            >
              Preview
            </button>
          </div>

          {/* Content Area */}
          <div class="flex-1 p-6 overflow-auto">
            {activeView() === "kanban" && (
              <div class="grid grid-cols-3 gap-4">
                <div class="rounded-lg p-4" style={{ "background-color": colors.surface2, border: `1px solid ${colors.border}` }}>
                  <h3 class="text-sm font-semibold mb-3" style={{ color: colors.text }}>To Do</h3>
                  <div class="space-y-2">
                    <div class="p-3 rounded" style={{ "background-color": colors.surface }}>
                      <p class="text-sm" style={{ color: colors.text }}>Design homepage</p>
                    </div>
                    <div class="p-3 rounded" style={{ "background-color": colors.surface }}>
                      <p class="text-sm" style={{ color: colors.text }}>Setup auth</p>
                    </div>
                  </div>
                </div>
                <div class="rounded-lg p-4" style={{ "background-color": colors.surface2, border: `1px solid ${colors.border}` }}>
                  <h3 class="text-sm font-semibold mb-3" style={{ color: colors.text }}>In Progress</h3>
                  <div class="space-y-2">
                    <div class="p-3 rounded" style={{ "background-color": colors.surface }}>
                      <p class="text-sm" style={{ color: colors.text }}>Build API</p>
                    </div>
                  </div>
                </div>
                <div class="rounded-lg p-4" style={{ "background-color": colors.surface2, border: `1px solid ${colors.border}` }}>
                  <h3 class="text-sm font-semibold mb-3" style={{ color: colors.text }}>Done</h3>
                  <div class="space-y-2">
                    <div class="p-3 rounded" style={{ "background-color": colors.surface }}>
                      <p class="text-sm" style={{ color: colors.text }}>Setup project</p>
                    </div>
                  </div>
                </div>
              </div>
            )}

            {activeView() === "editor" && (
              <div class="h-full rounded-lg p-4 font-mono text-sm" style={{ "background-color": colors.bg, border: `1px solid ${colors.border}`, color: colors.text }}>
                <pre>{`// Project Workspace Editor
// AI-assisted development environment

import { Component } from "solid-js";

export const Workspace: Component = () => {
  return (
    <div class="workspace">
      <h1>Project Workspace</h1>
    </div>
  );
};`}</pre>
              </div>
            )}

            {activeView() === "preview" && (
              <div class="h-full rounded-lg p-4 flex items-center justify-center" style={{ "background-color": colors.surface, border: `1px solid ${colors.border}` }}>
                <p class="text-lg" style={{ color: colors.textMuted }}>Preview Mode</p>
              </div>
            )}
          </div>
        </div>
      </div>
    </WorkspaceLayout>
  );
};
