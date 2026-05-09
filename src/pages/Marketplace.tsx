import { Component } from "solid-js";
import { WorkspaceLayout } from "../shared/layouts/WorkspaceLayout";
import { Sidebar } from "../shared/layouts/Sidebar";
import { Topbar } from "../shared/layouts/Topbar";
import { AIPanel } from "../shared/layouts/AIPanel";
import { colors } from "../shared/ui/tokens";
import { MarketplaceFeed } from "../widgets/MarketplaceFeed";

/**
 * Marketplace Page
 * 
 * AI opportunity marketplace with:
 * - Opportunity feed
 * - Type filtering
 * - Difficulty indicators
 * - Reward display
 */
export const Marketplace: Component = () => {
  const sidebarItems = [
    { id: "dashboard", label: "Dashboard", icon: "📊" },
    { id: "projects", label: "Projects", icon: "📁" },
    { id: "ai-agents", label: "AI Agents", icon: "🤖" },
    { id: "marketplace", label: "Marketplace", icon: "🛒", active: true },
    { id: "ide", label: "IDE", icon: "💻" },
    { id: "messages", label: "Messages", icon: "💬" },
    { id: "teams", label: "Teams", icon: "👥" },
    { id: "settings", label: "Settings", icon: "⚙️" },
  ];

  const opportunities = [
    {
      id: "1",
      title: "Build AI Chatbot",
      description: "Create an intelligent chatbot for customer support",
      type: "ai" as const,
      reward: "$500",
      difficulty: "medium" as const,
    },
    {
      id: "2",
      title: "Automate Data Pipeline",
      description: "Set up automated ETL process for data analytics",
      type: "automation" as const,
      reward: "$750",
      difficulty: "hard" as const,
    },
    {
      id: "3",
      title: "Integrate Payment Gateway",
      description: "Add Stripe integration to e-commerce platform",
      type: "integration" as const,
      reward: "$300",
      difficulty: "easy" as const,
    },
    {
      id: "4",
      title: "AI Image Generator",
      description: "Build AI-powered image generation service",
      type: "ai" as const,
      reward: "$1000",
      difficulty: "hard" as const,
    },
  ];

  return (
    <WorkspaceLayout
      topbar={
        <Topbar
          title="Marketplace"
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
              type: "suggestion",
              content: "Try the 'Build AI Chatbot' opportunity - it matches your skills",
              timestamp: new Date(),
            },
          ]}
          onMessageSend={(msg) => console.log("Message:", msg)}
        />
      }
    >
      <div class="p-6">
        <div class="flex items-center justify-between mb-6">
          <h1 class="text-2xl font-semibold" style={{ color: colors.text }}>
            AI Opportunity Marketplace
          </h1>
          <div class="flex gap-2">
            <button
              class="px-4 py-2 rounded-lg text-sm font-medium"
              style={{
                "background-color": colors.surface2,
                color: colors.text,
                border: `1px solid ${colors.border}`,
              }}
            >
              Filter: All
            </button>
            <button
              class="px-4 py-2 rounded-lg text-sm font-medium"
              style={{
                "background-color": colors.turquoise,
                color: colors.bg,
                border: "none",
              }}
            >
              Sort by Reward
            </button>
          </div>
        </div>

        <div class="grid grid-cols-2 gap-4">
          <MarketplaceFeed
            opportunities={opportunities.slice(0, 2)}
            onOpportunityClick={(id) => console.log("Selected:", id)}
          />
          <MarketplaceFeed
            opportunities={opportunities.slice(2)}
            onOpportunityClick={(id) => console.log("Selected:", id)}
          />
        </div>
      </div>
    </WorkspaceLayout>
  );
};
