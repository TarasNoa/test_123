import { Component, createSignal, onMount, onCleanup } from "solid-js";
import { colors, radius, spacing } from "../shared/ui/tokens";
import { globalEventStream } from "../shared/activity/WorkspaceEvent";

interface AgentNode {
  id: string;
  name: string;
  type: string;
  status: "idle" | "active" | "busy" | "error";
  currentTask?: string;
  position: { x: number; y: number };
}

interface AgentInteraction {
  from: string;
  to: string;
  type: "handoff" | "coordination" | "conflict" | "execution";
  status: "pending" | "active" | "completed" | "failed";
  timestamp: Date;
}

interface MultiAgentVisualizationProps {
  projectId: string;
}

/**
 * Multi-Agent Visualization
 * 
 * Shows agent interactions with:
 * - Agent nodes with status
 * - Interaction edges (handoffs, coordination, conflicts, execution)
 * - Realtime updates via event stream
 * - Animated transitions
 * - Task handoff visualization
 * 
 * This is killer UX - shows AI agents working together
 */
export const MultiAgentVisualization: Component<MultiAgentVisualizationProps> = (props) => {
  const [agents, setAgents] = createSignal<AgentNode[]>([]);
  const [interactions, setInteractions] = createSignal<AgentInteraction[]>([]);
  const [selectedAgent, setSelectedAgent] = createSignal<string | null>(null);
  const [hoveredAgent, setHoveredAgent] = createSignal<string | null>(null);

  const getAgentColor = (status: AgentNode["status"]): string => {
    switch (status) {
      case "idle":
        return colors.textMuted;
      case "active":
        return colors.turquoise;
      case "busy":
        return colors.info;
      case "error":
        return colors.error;
      default:
        return colors.textMuted;
    }
  };

  const getInteractionColor = (type: AgentInteraction["type"], status: AgentInteraction["status"]): string => {
    if (status === "failed") return colors.error;
    if (status === "completed") return colors.success;
    
    switch (type) {
      case "handoff":
        return colors.turquoise;
      case "coordination":
        return colors.info;
      case "conflict":
        return colors.warning;
      case "execution":
        return colors.purple;
      default:
        return colors.border;
    }
  };

  const getInteractionStyle = (interaction: AgentInteraction): string => {
    switch (interaction.type) {
      case "handoff":
        return "5,5";
      case "coordination":
        return "10,5";
      case "conflict":
        return "2,2";
      case "execution":
        return "none";
      default:
        return "5,5";
    }
  };

  onMount(() => {
    // Initialize with demo agents
    const demoAgents: AgentNode[] = [
      {
        id: "agent-1",
        name: "Architecture Agent",
        type: "planning",
        status: "idle",
        position: { x: 300, y: 80 },
      },
      {
        id: "agent-2",
        name: "Backend Agent",
        type: "development",
        status: "active",
        currentTask: "API authentication",
        position: { x: 150, y: 200 },
      },
      {
        id: "agent-3",
        name: "Frontend Agent",
        type: "development",
        status: "active",
        currentTask: "React components",
        position: { x: 450, y: 200 },
      },
      {
        id: "agent-4",
        name: "Deployment Agent",
        type: "ops",
        status: "idle",
        position: { x: 300, y: 320 },
      },
    ];

    const demoInteractions: AgentInteraction[] = [
      {
        from: "agent-1",
        to: "agent-2",
        type: "handoff",
        status: "completed",
        timestamp: new Date(Date.now() - 300000),
      },
      {
        from: "agent-1",
        to: "agent-3",
        type: "handoff",
        status: "completed",
        timestamp: new Date(Date.now() - 300000),
      },
      {
        from: "agent-2",
        to: "agent-4",
        type: "execution",
        status: "active",
        timestamp: new Date(Date.now() - 60000),
      },
      {
        from: "agent-3",
        to: "agent-4",
        type: "execution",
        status: "pending",
        timestamp: new Date(Date.now() - 30000),
      },
    ];

    setAgents(demoAgents);
    setInteractions(demoInteractions);

    // Subscribe to agent events
    const unsubscribe = globalEventStream.subscribe("*", (event) => {
      if (event.type === "AgentStarted") {
        setAgents(prev => {
          const existing = prev.find(a => a.id === event.agentId);
          if (existing) {
            return prev.map(a =>
              a.id === event.agentId
                ? { ...a, status: "active", currentTask: event.taskId }
                : a
            );
          }
          return [
            ...prev,
            {
              id: event.agentId,
              name: event.agentName,
              type: "development",
              status: "active",
              currentTask: event.taskId,
              position: { x: 150 + Math.random() * 300, y: 150 + Math.random() * 150 },
            },
          ];
        });
      } else if (event.type === "AgentCompleted") {
        setAgents(prev =>
          prev.map(a =>
            a.id === event.agentId
              ? { ...a, status: "idle", currentTask: undefined }
              : a
          )
        );
      }
    });

    onCleanup(unsubscribe);
  });

  return (
    <div
      class="relative w-full h-full"
      style={{
        "background-color": colors.surface,
        "border-radius": radius.lg,
        border: `1px solid ${colors.border}`,
      }}
    >
      <svg
        class="w-full h-full"
        viewBox="0 0 600 400"
        style={{ overflow: "visible" }}
      >
        {/* Interaction Edges */}
        {interactions().map((interaction) => {
          const fromAgent = agents().find(a => a.id === interaction.from);
          const toAgent = agents().find(a => a.id === interaction.to);
          if (!fromAgent || !toAgent) return null;

          return (
            <g>
              <line
                x1={fromAgent.position.x}
                y1={fromAgent.position.y}
                x2={toAgent.position.x}
                y2={toAgent.position.y}
                stroke={getInteractionColor(interaction.type, interaction.status)}
                stroke-width={interaction.status === "active" ? 3 : 2}
                stroke-dasharray={getInteractionStyle(interaction)}
                opacity={selectedAgent() && selectedAgent() !== interaction.from && selectedAgent() !== interaction.to ? 0.3 : 1}
                style={{
                  transition: "all 0.3s ease",
                }}
              />
              {/* Animated dot for active interactions */}
              {interaction.status === "active" && (
                <circle
                  r="4"
                  fill={getInteractionColor(interaction.type, interaction.status)}
                  style={{
                    animation: "flow 2s linear infinite",
                  }}
                >
                  <animateMotion
                    dur="2s"
                    repeatCount="indefinite"
                    path={`M${fromAgent.position.x},${fromAgent.position.y} L${toAgent.position.x},${toAgent.position.y}`}
                  />
                </circle>
              )}
            </g>
          );
        })}

        {/* Agent Nodes */}
        {agents().map((agent) => (
          <g
            onClick={() => setSelectedAgent(agent.id === selectedAgent() ? null : agent.id)}
            onMouseEnter={() => setHoveredAgent(agent.id)}
            onMouseLeave={() => setHoveredAgent(null)}
            style={{ cursor: "pointer" }}
          >
            {/* Agent Circle */}
            <circle
              cx={agent.position.x}
              cy={agent.position.y}
              r={selectedAgent() === agent.id ? 28 : hoveredAgent() === agent.id ? 26 : 24}
              fill={colors.surface2}
              stroke={getAgentColor(agent.status)}
              stroke-width={selectedAgent() === agent.id ? 3 : 2}
              opacity={selectedAgent() && selectedAgent() !== agent.id ? 0.3 : 1}
              style={{
                transition: "all 0.2s ease",
              }}
            />

            {/* Agent Icon/Initial */}
            <text
              x={agent.position.x}
              y={agent.position.y}
              text-anchor="middle"
              dominant-baseline="middle"
              font-size="14"
              fill={getAgentColor(agent.status)}
              style={{ "font-weight": "600" }}
            >
              {agent.name.charAt(0)}
            </text>

            {/* Agent Label */}
            <text
              x={agent.position.x}
              y={agent.position.y + 40}
              text-anchor="middle"
              font-size="11"
              fill={colors.text}
              style={{
                "font-weight": selectedAgent() === agent.id ? "600" : "400",
              }}
            >
              {agent.name}
            </text>

            {/* Current Task */}
            {agent.currentTask && (
              <text
                x={agent.position.x}
                y={agent.position.y + 55}
                text-anchor="middle"
                font-size="10"
                fill={colors.textMuted}
              >
                {agent.currentTask}
              </text>
            )}

            {/* Status Indicator */}
            {agent.status === "active" && (
              <circle
                cx={agent.position.x + 24}
                cy={agent.position.y - 24}
                r="4"
                fill={colors.turquoise}
                style={{
                  animation: "pulse 1.5s ease-in-out infinite",
                }}
              />
            )}
          </g>
        ))}
      </svg>

      {/* Selected Agent Details */}
      {selectedAgent() && (
        <div
          class="absolute bottom-4 left-4 p-4 rounded-lg"
          style={{
            "background-color": colors.surface2,
            border: `1px solid ${colors.border}`,
            "max-width": "250px",
          }}
        >
          {(() => {
            const agent = agents().find(a => a.id === selectedAgent());
            if (!agent) return null;

            return (
              <div class="space-y-2">
                <div class="flex items-center gap-2">
                  <div
                    class="w-8 h-8 rounded-full flex items-center justify-center text-sm font-semibold"
                    style={{
                      "background-color": `rgba(${parseInt(getAgentColor(agent.status).slice(1, 3), 16)}, ${parseInt(getAgentColor(agent.status).slice(3, 5), 16)}, ${parseInt(getAgentColor(agent.status).slice(5, 7), 16)}, 0.12)`,
                      color: getAgentColor(agent.status),
                    }}
                  >
                    {agent.name.charAt(0)}
                  </div>
                  <span class="text-sm font-semibold" style={{ color: colors.text }}>
                    {agent.name}
                  </span>
                </div>
                <div class="flex items-center gap-2">
                  <span
                    class="text-xs px-2 py-1 rounded"
                    style={{
                      "background-color": `rgba(${parseInt(getAgentColor(agent.status).slice(1, 3), 16)}, ${parseInt(getAgentColor(agent.status).slice(3, 5), 16)}, ${parseInt(getAgentColor(agent.status).slice(5, 7), 16)}, 0.12)`,
                      color: getAgentColor(agent.status),
                    }}
                  >
                    {agent.status}
                  </span>
                  <span class="text-xs" style={{ color: colors.textMuted }}>
                    {agent.type}
                  </span>
                </div>
                {agent.currentTask && (
                  <div class="text-xs" style={{ color: colors.text }}>
                    Task: {agent.currentTask}
                  </div>
                )}
                <div class="text-xs" style={{ color: colors.textMuted }}>
                  Interactions: {interactions().filter(i => i.from === agent.id || i.to === agent.id).length}
                </div>
              </div>
            );
          })()}
        </div>
      )}

      {/* Legend */}
      <div
        class="absolute top-4 right-4 p-3 rounded-lg"
        style={{
          "background-color": colors.surface2,
          border: `1px solid ${colors.border}`,
        }}
      >
        <div class="text-xs font-semibold mb-2" style={{ color: colors.text }}>
          Interactions
        </div>
        <div class="space-y-1">
          <div class="flex items-center gap-2">
            <div class="w-3 h-0.5" style={{ "background-color": colors.turquoise }} />
            <span class="text-xs" style={{ color: colors.textMuted }}>Handoff</span>
          </div>
          <div class="flex items-center gap-2">
            <div class="w-3 h-0.5" style={{ "background-color": colors.info }} />
            <span class="text-xs" style={{ color: colors.textMuted }}>Coordination</span>
          </div>
          <div class="flex items-center gap-2">
            <div class="w-3 h-0.5" style={{ "background-color": colors.warning }} />
            <span class="text-xs" style={{ color: colors.textMuted }}>Conflict</span>
          </div>
          <div class="flex items-center gap-2">
            <div class="w-3 h-0.5" style={{ "background-color": colors.purple }} />
            <span class="text-xs" style={{ color: colors.textMuted }}>Execution</span>
          </div>
        </div>
      </div>

      {/* Animations */}
      <style>{`
        @keyframes pulse {
          0%, 100% {
            opacity: 1;
            transform: scale(1);
          }
          50% {
            opacity: 0.5;
            transform: scale(1.2);
          }
        }
        @keyframes flow {
          0% {
            offset-distance: 0%;
          }
          100% {
            offset-distance: 100%;
          }
        }
      `}</style>
    </div>
  );
};
