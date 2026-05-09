import { Component, createSignal, onMount, onCleanup } from "solid-js";
import { colors, radius, spacing } from "../shared/ui/tokens";
import { createDefaultExecutionGraph, type ExecutionGraph, type GraphNode, type GraphEdge } from "../Services/execution/ExecutionGraph";

interface ExecutionGraphVisualizationProps {
  projectId: string;
}

/**
 * Execution Graph Visualization
 * 
 * Living orchestration graph with:
 * - Animated nodes and edges
 * - Realtime updates via event stream
 * - Status-based styling
 * - Agent assignments
 * - Risk indicators
 * 
 * This is the killer feature - visual execution orchestration
 */
export const ExecutionGraphVisualization: Component<ExecutionGraphVisualizationProps> = (props) => {
  const [graph, setGraph] = createSignal<ExecutionGraph | null>(null);
  const [selectedNode, setSelectedNode] = createSignal<string | null>(null);
  const [hoveredNode, setHoveredNode] = createSignal<string | null>(null);

  const getNodeColor = (node: GraphNode): string => {
    switch (node.status) {
      case "completed":
        return colors.success;
      case "in_progress":
        return colors.turquoise;
      case "blocked":
        return colors.error;
      case "error":
        return colors.error;
      case "pending":
      default:
        return colors.textMuted;
    }
  };

  const getNodeIcon = (node: GraphNode): string => {
    switch (node.type) {
      case "goal":
        return "🎯";
      case "phase":
        return "📦";
      case "task":
        return "⚡";
      case "milestone":
        return "🏁";
      default:
        return "○";
    }
  };

  const getEdgeColor = (edge: GraphEdge): string => {
    switch (edge.type) {
      case "dependency":
        return colors.border;
      case "flow":
        return colors.turquoise;
      case "blocker":
        return colors.error;
      default:
        return colors.border;
    }
  };

  const getNodePosition = (node: GraphNode, index: number, total: number): { x: number; y: number } => {
    const width = 600;
    const height = 400;
    const padding = 60;

    // Simple tree layout
    if (node.type === "goal") {
      return { x: width / 2, y: padding };
    }

    const level = Math.floor((index - 1) / 3) + 1;
    const positionInLevel = ((index - 1) % 3);

    const y = padding + level * 100;
    const x = padding + positionInLevel * (width - 2 * padding) / 2;

    return { x, y };
  };

  const handleNodeClick = (nodeId: string) => {
    setSelectedNode(nodeId === selectedNode() ? null : nodeId);
  };

  const handleNodeHover = (nodeId: string | null) => {
    setHoveredNode(nodeId);
  };

  onMount(() => {
    // Load initial graph
    const initialGraph = createDefaultExecutionGraph(props.projectId, "Project");
    setGraph(initialGraph);
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
      {graph() ? (
        <>
          {/* SVG Container */}
          <svg
            class="w-full h-full"
            viewBox="0 0 600 400"
            style={{ overflow: "visible" }}
          >
            {/* Edges */}
            {graph()!.edges.map((edge, index) => {
              const fromNode = graph()!.nodes.find(n => n.id === edge.from);
              const toNode = graph()!.nodes.find(n => n.id === edge.to);
              if (!fromNode || !toNode) return null;

              const fromPos = getNodePosition(fromNode, graph()!.nodes.indexOf(fromNode), graph()!.nodes.length);
              const toPos = getNodePosition(toNode, graph()!.nodes.indexOf(toNode), graph()!.nodes.length);

              return (
                <line
                  x1={fromPos.x}
                  y1={fromPos.y}
                  x2={toPos.x}
                  y2={toPos.y}
                  stroke={getEdgeColor(edge)}
                  stroke-width={edge.type === "blocker" ? 3 : edge.type === "flow" ? 2 : 1}
                  stroke-dasharray={edge.type === "dependency" ? "4,4" : "none"}
                  opacity={selectedNode() && selectedNode() !== edge.from && selectedNode() !== edge.to ? 0.3 : 1}
                  style={{
                    transition: "all 0.3s ease",
                  }}
                />
              );
            })}

            {/* Nodes */}
            {graph()!.nodes.map((node, index) => {
              const position = getNodePosition(node, index, graph()!.nodes.length);
              const isSelected = selectedNode() === node.id;
              const isHovered = hoveredNode() === node.id;

              return (
                <g
                  onClick={() => handleNodeClick(node.id)}
                  onMouseEnter={() => handleNodeHover(node.id)}
                  onMouseLeave={() => handleNodeHover(null)}
                  style={{ cursor: "pointer" }}
                >
                  {/* Node Circle */}
                  <circle
                    cx={position.x}
                    cy={position.y}
                    r={isSelected ? 24 : isHovered ? 22 : 20}
                    fill={colors.surface2}
                    stroke={getNodeColor(node)}
                    stroke-width={isSelected ? 3 : 2}
                    opacity={selectedNode() && selectedNode() !== node.id ? 0.3 : 1}
                    style={{
                      transition: "all 0.2s ease",
                    }}
                  />

                  {/* Node Icon */}
                  <text
                    x={position.x}
                    y={position.y}
                    text-anchor="middle"
                    dominant-baseline="middle"
                    font-size="16"
                  >
                    {getNodeIcon(node)}
                  </text>

                  {/* Node Label */}
                  <text
                    x={position.x}
                    y={position.y + 35}
                    text-anchor="middle"
                    font-size="11"
                    fill={colors.text}
                    style={{
                      "font-weight": isSelected ? "600" : "400",
                    }}
                  >
                    {node.label}
                  </text>

                  {/* Semantic Explanation */}
                  {node.metadata.semanticExplanation && (
                    <text
                      x={position.x}
                      y={position.y + 50}
                      text-anchor="middle"
                      font-size="9"
                      fill={colors.textMuted}
                      style={{
                        "max-width": "120px",
                      }}
                    >
                    {node.metadata.semanticExplanation}
                    </text>
                  )}

                  {/* Status Indicator */}
                  {node.status === "in_progress" && (
                    <circle
                      cx={position.x + 20}
                      cy={position.y - 20}
                      r={4}
                      fill={colors.turquoise}
                      style={{
                        animation: "pulse 1.5s ease-in-out infinite",
                      }}
                    />
                  )}

                  {/* Risk Indicator */}
                  {node.metadata.riskLevel === "high" && (
                    <circle
                      cx={position.x - 20}
                      cy={position.y - 20}
                      r={4}
                      fill={colors.warning}
                    />
                  )}
                </g>
              );
            })}
          </svg>

          {/* Selected Node Details */}
          {selectedNode() && (
            <div
              class="absolute bottom-4 left-4 p-4 rounded-lg"
              style={{
                "background-color": colors.surface2,
                border: `1px solid ${colors.border}`,
                "max-width": "250px",
              }}
            >
              {(() => {
                const node = graph()!.nodes.find(n => n.id === selectedNode());
                if (!node) return null;

                return (
                  <div class="space-y-2">
                    <div class="flex items-center gap-2">
                      <span class="text-lg">{getNodeIcon(node)}</span>
                      <span class="text-sm font-semibold" style={{ color: colors.text }}>
                        {node.label}
                      </span>
                    </div>
                    <div class="flex items-center gap-2">
                      <span
                        class="text-xs px-2 py-1 rounded"
                        style={{
                          "background-color": `rgba(${parseInt(getNodeColor(node).slice(1, 3), 16)}, ${parseInt(getNodeColor(node).slice(3, 5), 16)}, ${parseInt(getNodeColor(node).slice(5, 7), 16)}, 0.12)`,
                          color: getNodeColor(node),
                        }}
                      >
                        {node.status}
                      </span>
                      <span class="text-xs" style={{ color: colors.textMuted }}>
                        {node.type}
                      </span>
                    </div>
                    {node.metadata.assignedAgent && (
                      <div class="text-xs" style={{ color: colors.text }}>
                        Agent: {node.metadata.assignedAgent}
                      </div>
                    )}
                    {node.metadata.estimatedDuration && (
                      <div class="text-xs" style={{ color: colors.textMuted }}>
                        Est: {node.metadata.estimatedDuration} days
                      </div>
                    )}
                    {node.metadata.riskLevel === "high" && (
                      <div class="text-xs" style={{ color: colors.warning }}>
                        ⚠️ High risk
                      </div>
                    )}
                  </div>
                );
              })()}
            </div>
          )}
        </>
      ) : (
        <div class="flex items-center justify-center h-full">
          <p class="text-sm" style={{ color: colors.textMuted }}>
            Loading execution graph...
          </p>
        </div>
      )}

      {/* Pulse Animation */}
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
      `}</style>
    </div>
  );
};
