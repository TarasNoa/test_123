import { Component } from "solid-js";
import { colors, spacing } from "../shared/ui/tokens";

interface AgentPanelProps {
  agentName: string;
  status: "idle" | "active" | "busy" | "error";
  currentTask?: string;
  progress?: number;
}

/**
 * Agent Panel Widget
 * 
 * Displays AI agent status with:
 * - Agent name
 * - Status indicator
 * - Current task
 * - Progress bar
 * - Color-coded status
 */
export const AgentPanel: Component<AgentPanelProps> = (props) => {
  const getStatusColor = () => {
    switch (props.status) {
      case "idle":
        return colors.textMuted;
      case "active":
        return colors.turquoise;
      case "busy":
        return colors.warning;
      case "error":
        return colors.error;
      default:
        return colors.textMuted;
    }
  };

  const getStatusLabel = () => {
    switch (props.status) {
      case "idle":
        return "Idle";
      case "active":
        return "Active";
      case "busy":
        return "Busy";
      case "error":
        return "Error";
      default:
        return "Unknown";
    }
  };

  return (
    <div
      class="p-4 rounded-lg"
      style={{
        "background-color": colors.surface2,
        border: `1px solid ${colors.border}`,
      }}
    >
      <div class="flex items-center justify-between mb-3">
        <div class="flex items-center gap-2">
          <div
            class="w-2 h-2 rounded-full"
            style={{
              "background-color": getStatusColor(),
              animation: props.status === "active" || props.status === "busy" ? "pulse 1.5s ease-in-out infinite" : "none",
            }}
          />
          <h3 class="text-sm font-semibold" style={{ color: colors.text }}>
            {props.agentName}
          </h3>
        </div>
        <span
          class="text-xs px-2 py-1 rounded"
          style={{
            "background-color": "rgba(53, 224, 208, 0.12)",
            color: getStatusColor(),
          }}
        >
          {getStatusLabel()}
        </span>
      </div>

      {props.currentTask && (
        <p class="text-sm mb-3" style={{ color: colors.textMuted }}>
          {props.currentTask}
        </p>
      )}

      {props.progress !== undefined && (
        <div class="w-full">
          <div
            class="h-2 rounded-full"
            style={{
              "background-color": colors.surface3,
              "border-radius": "9999px",
            }}
          >
            <div
              class="h-2 rounded-full transition-all"
              style={{
                width: `${props.progress}%`,
                "background-color": colors.turquoise,
                "border-radius": "9999px",
              }}
            />
          </div>
          <p class="text-xs mt-1" style={{ color: colors.textSecondary }}>
            {props.progress}%
          </p>
        </div>
      )}

      <style>{`
        @keyframes pulse {
          0%, 100% { opacity: 1; }
          50% { opacity: 0.5; }
        }
      `}</style>
    </div>
  );
};
