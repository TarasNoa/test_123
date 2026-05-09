import { Component } from "solid-js";
import { colors, spacing } from "../shared/ui/tokens";

interface ExecutionNode {
  id: string;
  label: string;
  status: "pending" | "running" | "completed" | "error";
  duration?: string;
}

interface ExecutionEdge {
  from: string;
  to: string;
}

interface ExecutionGraphProps {
  nodes: ExecutionNode[];
  edges: ExecutionEdge[];
}

/**
 * Execution Graph Widget
 * 
 * Visualizes AI agent execution flow with:
 * - Node visualization
 * - Status indicators (pending, running, completed, error)
 * - Edge connections
 * - Duration display
 */
export const ExecutionGraph: Component<ExecutionGraphProps> = (props) => {
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
    <div class="flex flex-col gap-4">
      {props.nodes.map((node) => (
        <div class="flex items-center gap-3">
          <div
            class="w-8 h-8 rounded-full flex items-center justify-center text-sm"
            style={{
              "background-color": "rgba(53, 224, 208, 0.12)",
              color: getStatusColor()[node.status],
              animation: node.status === "running" ? "pulse 1.5s ease-in-out infinite" : "none",
            }}
          >
            {getStatusIcon()[node.status]}
          </div>
          <div class="flex-1">
            <p class="text-sm font-medium" style={{ color: colors.text }}>
              {node.label}
            </p>
            {node.duration && (
              <p class="text-xs" style={{ color: colors.textMuted }}>
                {node.duration}
              </p>
            )}
          </div>
        </div>
      ))}
      
      {/* Visual edges would be rendered here in a real implementation */}
      {props.edges.length > 0 && (
        <div class="pt-4 border-t" style={{ "border-color": colors.border }}>
          <p class="text-xs" style={{ color: colors.textMuted }}>
            {props.edges.length} connection(s)
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
