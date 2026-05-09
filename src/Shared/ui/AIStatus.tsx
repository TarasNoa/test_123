import { Component } from "solid-js";
import { colors, spacing } from "./tokens";

interface AIStatusProps {
  status: "idle" | "thinking" | "processing" | "error" | "success";
  message?: string;
}

/**
 * AI Status Component
 * 
 * Shows AI agent status with:
 * - Color-coded status indicator
 * - Optional message
 * - Animated pulse for active states
 */
export const AIStatus: Component<AIStatusProps> = (props) => {
  const getStatusColor = () => {
    switch (props.status) {
      case "idle":
        return colors.textMuted;
      case "thinking":
        return colors.turquoise;
      case "processing":
        return colors.purple;
      case "error":
        return colors.error;
      case "success":
        return colors.success;
      default:
        return colors.textMuted;
    }
  };

  const getStatusLabel = () => {
    switch (props.status) {
      case "idle":
        return "Idle";
      case "thinking":
        return "Thinking...";
      case "processing":
        return "Processing";
      case "error":
        return "Error";
      case "success":
        return "Complete";
      default:
        return "Unknown";
    }
  };

  const isActive = () => props.status === "thinking" || props.status === "processing";

  return (
    <div class="flex items-center gap-2">
      <div
        class="w-2 h-2 rounded-full"
        style={{
          "background-color": getStatusColor(),
          animation: isActive() ? "pulse 1.5s ease-in-out infinite" : "none",
        }}
      />
      <span class="text-sm" style={{ color: colors.text }}>
        {props.message || getStatusLabel()}
      </span>
      <style>{`
        @keyframes pulse {
          0%, 100% { opacity: 1; }
          50% { opacity: 0.5; }
        }
      `}</style>
    </div>
  );
};
