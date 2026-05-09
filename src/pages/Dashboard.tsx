import { Component } from "solid-js";
import { WorkspaceLayout } from "../shared/layouts/WorkspaceLayout";
import { Sidebar } from "../shared/layouts/Sidebar";
import { Topbar } from "../shared/layouts/Topbar";
import { AIPanel } from "../shared/layouts/AIPanel";
import { colors } from "../shared/ui/tokens";
import { ProjectCard } from "../widgets/ProjectCard";
import { AIActivityFeed } from "../widgets/AIActivityFeed";
import { AgentPanel } from "../widgets/AgentPanel";

/**
 * Dashboard Page
 * 
 * Main dashboard with:
 * - Project overview
 * - Recent activity
 * - AI agent status
 * - Quick actions
 */
export const Dashboard: Component = () => {
  const sidebarItems = [
    { id: "dashboard", label: "Dashboard", icon: "📊", active: true },
    { id: "projects", label: "Projects", icon: "📁" },
    { id: "ai-agents", label: "AI Agents", icon: "🤖" },
    { id: "marketplace", label: "Marketplace", icon: "🛒" },
    { id: "ide", label: "IDE", icon: "💻" },
    { id: "messages", label: "Messages", icon: "💬" },
    { id: "teams", label: "Teams", icon: "👥" },
    { id: "settings", label: "Settings", icon: "⚙️" },
  ];

  const activities = [
    {
      id: "1",
      type: "thought" as const,
      content: "Analyzing project structure...",
      timestamp: new Date(),
    },
    {
      id: "2",
      type: "task" as const,
      content: "Generating component boilerplate",
      timestamp: new Date(Date.now() - 60000),
    },
    {
      id: "3",
      type: "execution" as const,
      content: "Running tests - all passed",
      timestamp: new Date(Date.now() - 120000),
    },
  ];

  return (
    <WorkspaceLayout
      topbar={
        <Topbar
          title="Dashboard"
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
          messages={activities}
          onMessageSend={(msg) => console.log("Message:", msg)}
        />
      }
    >
      <div class="p-6">
        <h1 class="text-2xl font-semibold mb-6" style={{ color: colors.text }}>
          Welcome back
        </h1>

        {/* Recent Projects */}
        <div class="mb-8">
          <h2 class="text-lg font-semibold mb-4" style={{ color: colors.text }}>
            Recent Projects
          </h2>
          <div class="grid grid-cols-3 gap-4">
            <ProjectCard
              name="Libr4 IDE"
              description="AI-powered development environment"
              status="active"
              lastModified="2 hours ago"
            />
            <ProjectCard
              name="E-commerce Platform"
              description="Full-stack shopping platform"
              status="active"
              lastModified="1 day ago"
            />
            <ProjectCard
              name="Mobile App"
              description="React Native application"
              status="draft"
              lastModified="3 days ago"
            />
          </div>
        </div>

        {/* AI Agent Status */}
        <div class="mb-8">
          <h2 class="text-lg font-semibold mb-4" style={{ color: colors.text }}>
            AI Agent Status
          </h2>
          <div class="grid grid-cols-2 gap-4">
            <AgentPanel
              agentName="Code Generator"
              status="active"
              currentTask="Generating API endpoints"
              progress={75}
            />
            <AgentPanel
              agentName="Test Runner"
              status="idle"
            />
          </div>
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
