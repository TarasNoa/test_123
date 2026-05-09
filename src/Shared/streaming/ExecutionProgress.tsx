import { Component } from "solid-js";
import { colors, radius, spacing } from "../ui/tokens";

interface ExecutionStep {
  id: string;
  label: string;
  status: "pending" | "running" | "completed" | "error";
  duration?: string;
}

interface ExecutionProgressProps {
  steps: ExecutionStep[];
}

/**
 * Execution Progress Component
 * 
 * Visualizes AI execution progress with:
 * - Step-by-step progress
 * - Status indicators
 * - Duration display
 * - Smooth transitions
 */
export const ExecutionProgress: Component<ExecutionProgressProps> = (props) => {
  const getStatusColor = () => {
    return {
      pending: colors.textMuted,
      running: colors.turquoise,
      completed: colors.success,
      error: colors.error,
    };
  };

  const getStatusIcon = () => {
    return {
      pending: "○",
      running: "◌",
      completed: "✓",
      error: "✕",
    };
  };

  return (
    <div class="flex flex-col gap-3">
      {props.steps.map((step, index) => (
        <div class="flex items-center gap-3">
          <div
            class="w-6 h-6 rounded-full flex items-center justify-center text-xs"
            style={{
              "background-color": "rgba(53, 224, 208, 0.12)",
              color: getStatusColor()[step.status],
              animation: step.status === "running" ? "pulse 1.5s ease-in-out infinite" : "none",
            }}
          >
            {getStatusIcon()[step.status]}
          </div>
          <div class="flex-1">
            <p class="text-sm" style={{ color: colors.text }}>
              {step.label}
            </p>
            {step.duration && (
              <p class="text-xs" style={{ color: colors.textMuted }}>
                {step.duration}
              </p>
            )}
          </div>
          {index < props.steps.length - 1 && (
            <div
              class="w-0.5 h-4"
              style={{
                "background-color": colors.border,
              }}
            />
          )}
        </div>
      ))}
      <style>{`
        @keyframes pulse {
          0%, 100% { opacity: 1; }
          50% { opacity: 0.5; }
        }
      `}</style>
    </div>
  );
};
