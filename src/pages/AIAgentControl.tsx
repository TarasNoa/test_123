import { Component } from "solid-js";
import { WorkspaceLayout } from "../shared/layouts/WorkspaceLayout";
import { Sidebar } from "../shared/layouts/Sidebar";
import { Topbar } from "../shared/layouts/Topbar";
import { colors } from "../shared/ui/tokens";
import { AgentPanel } from "../widgets/AgentPanel";
import { AIActivityFeed } from "../widgets/AIActivityFeed";
import { ExecutionGraph } from "../widgets/ExecutionGraph";

/**
 * AI Agent Control Page
 * 
 * AI agent management and control with:
 * - Agent status monitoring
 * - Task assignment
 * - Activity feed
 * - Execution graph
 */
export const AIAgentControl: Component = () => {
  const sidebarItems = [
    { id: "dashboard", label: "Dashboard", icon: "📊" },
    { id: "projects", label: "Projects", icon: "📁" },
    { id: "ai-agents", label: "AI Agents", icon: "🤖", active: true },
    { id: "marketplace", label: "Marketplace", icon: "🛒" },
    { id: "ide", label: "IDE", icon: "💻" },
    { id: "messages", label: "Messages", icon: "💬" },
    { id: "teams", label: "Teams", icon: "👥" },
    { id: "settings", label: "Settings", icon: "⚙️" },
  ];

  const executionNodes = [
    { id: "1", label: "Initialize agent", status: "completed" as const, duration: "1s" },
    { id: "2", label: "Load context", status: "completed" as const, duration: "2s" },
    { id: "3", label: "Process task", status: "running" as const },
    { id: "4", label: "Generate response", status: "pending" as const },
  ];

  const executionEdges = [
    { from: "1", to: "2" },
    { from: "2", to: "3" },
    { from: "3", to: "4" },
  ];

  const activities = [
    {
      id: "1",
      type: "thought" as const,
      content: "Initializing agent context...",
      timestamp: new Date(),
    },
    {
      id: "2",
      type: "task" as const,
      content: "Processing user request",
      timestamp: new Date(Date.now() - 10000),
    },
    {
      id: "3",
      type: "execution" as const,
      content: "Executing analysis pipeline",
      timestamp: new Date(Date.now() - 20000),
    },
  ];

  return (
    <WorkspaceLayout
      topbar={
        <Topbar
          title="AI Agent Control"
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
            agentName="Main Orchestrator"
            status="active"
            currentTask="Processing task queue"
            progress={45}
          />
          <div class="flex-1 overflow-auto">
            <AIActivityFeed activities={activities} />
          </div>
        </div>
      }
    >
      <div class="p-6">
        <h1 class="text-2xl font-semibold mb-6" style={{ color: colors.text }}>
          AI Agent Management
        </h1>

        {/* Agent Grid */}
        <div class="grid grid-cols-2 gap-4 mb-8">
          <AgentPanel
            agentName="Code Generator"
            status="active"
            currentTask="Generating component"
            progress={80}
          />
          <AgentPanel
            agentName="Test Runner"
            status="idle"
          />
          <AgentPanel
            agentName="Documentation Bot"
            status="busy"
            currentTask="Generating API docs"
            progress={30}
          />
          <AgentPanel
            agentName="Security Scanner"
            status="idle"
          />
        </div>

        {/* Execution Graph */}
        <div class="mb-8">
          <h2 class="text-lg font-semibold mb-4" style={{ color: colors.text }}>
            Current Execution Flow
          </h2>
          <ExecutionGraph
            nodes={executionNodes}
            edges={executionEdges}
          />
        </div>

        {/* Recent Activity */}
        <div>
          <h2 class="text-lg font-semibold mb-4" style={{ color: colors.text }}>
            Recent Activity
          </h2>
          <AIActivityFeed activities={activities} />
        </div>
      </div>
    </WorkspaceLayout>
  );
};
