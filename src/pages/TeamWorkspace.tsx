import { Component } from "solid-js";
import { WorkspaceLayout } from "../shared/layouts/WorkspaceLayout";
import { Sidebar } from "../shared/layouts/Sidebar";
import { Topbar } from "../shared/layouts/Topbar";
import { AIPanel } from "../shared/layouts/AIPanel";
import { colors } from "../shared/ui/tokens";
import { ProjectCard } from "../widgets/ProjectCard";
import { AgentPanel } from "../widgets/AgentPanel";

/**
 * Team Workspace Page
 * 
 * Team collaboration workspace with:
 * - Team projects
 * - Team members
 * - Shared AI agents
 * - Activity feed
 */
export const TeamWorkspace: Component = () => {
  const sidebarItems = [
    { id: "dashboard", label: "Dashboard", icon: "📊" },
    { id: "projects", label: "Projects", icon: "📁" },
    { id: "ai-agents", label: "AI Agents", icon: "🤖" },
    { id: "marketplace", label: "Marketplace", icon: "🛒" },
    { id: "ide", label: "IDE", icon: "💻" },
    { id: "messages", label: "Messages", icon: "💬" },
    { id: "teams", label: "Teams", icon: "👥", active: true },
    { id: "settings", label: "Settings", icon: "⚙️" },
  ];

  return (
    <WorkspaceLayout
      topbar={
        <Topbar
          title="Team Workspace"
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
            agentName="Team Orchestrator"
            status="active"
            currentTask="Coordinating team tasks"
            progress={65}
          />
        </div>
      }
    >
      <div class="p-6">
        <h1 class="text-2xl font-semibold mb-6" style={{ color: colors.text }}>
          Team Workspace
        </h1>

        {/* Team Overview */}
        <div class="mb-8">
          <div class="flex items-center justify-between mb-4">
            <h2 class="text-lg font-semibold" style={{ color: colors.text }}>
              Team Projects
            </h2>
            <button
              class="px-4 py-2 rounded-lg text-sm font-medium"
              style={{
                "background-color": colors.turquoise,
                color: colors.bg,
                border: "none",
              }}
            >
              + New Project
            </button>
          </div>
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

        {/* Team Members */}
        <div class="mb-8">
          <h2 class="text-lg font-semibold mb-4" style={{ color: colors.text }}>
            Team Members
          </h2>
          <div class="grid grid-cols-4 gap-4">
            {[
              { name: "John Doe", role: "Lead Developer", avatar: "JD" },
              { name: "Jane Smith", role: "AI Engineer", avatar: "JS" },
              { name: "Bob Wilson", role: "Designer", avatar: "BW" },
              { name: "AI Agent", role: "Automation", avatar: "🤖" },
            ].map((member) => (
              <div
                class="p-4 rounded-lg text-center"
                style={{
                  "background-color": colors.surface2,
                  border: `1px solid ${colors.border}`,
                }}
              >
                <div
                  class="w-12 h-12 rounded-full mx-auto mb-2 flex items-center justify-center text-lg font-semibold"
                  style={{
                    "background-color": colors.turquoiseLight,
                    color: colors.turquoise,
                  }}
                >
                  {member.avatar}
                </div>
                <p class="text-sm font-medium" style={{ color: colors.text }}>
                  {member.name}
                </p>
                <p class="text-xs" style={{ color: colors.textMuted }}>
                  {member.role}
                </p>
              </div>
            ))}
          </div>
        </div>

        {/* Shared AI Agents */}
        <div>
          <h2 class="text-lg font-semibold mb-4" style={{ color: colors.text }}>
            Shared AI Agents
          </h2>
          <div class="grid grid-cols-2 gap-4">
            <AgentPanel
              agentName="Code Reviewer"
              status="idle"
            />
            <AgentPanel
              agentName="Task Manager"
              status="active"
              currentTask="Managing sprint tasks"
              progress={40}
            />
          </div>
        </div>
      </div>
    </WorkspaceLayout>
  );
};
