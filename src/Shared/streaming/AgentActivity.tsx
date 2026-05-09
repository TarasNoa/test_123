import { Component } from "solid-js";
import { colors, spacing } from "../ui/tokens";
import { ThinkingIndicator } from "./ThinkingIndicator";
import { StreamingText } from "./StreamingText";

interface ActivityItem {
  id: string;
  type: "thought" | "task" | "analysis" | "suggestion" | "execution" | "error";
  content: string;
  timestamp: Date;
  isStreaming?: boolean;
}

interface AgentActivityProps {
  activities: ActivityItem[];
  maxItems?: number;
}

/**
 * Agent Activity Component
 * 
 * Compact activity feed with:
 * - Type badges
 * - Streaming text support
 * - Thinking indicators
 * - Timestamps
 * - Smooth animations
 */
export const AgentActivity: Component<AgentActivityProps> = (props) => {
  const displayedActivities = () => {
    const max = props.maxItems || 10;
    return props.activities.slice(-max);
  };

  const getTypeColor = () => {
    return {
      thought: colors.turquoise,
      task: colors.purple,
      analysis: colors.info,
      suggestion: colors.warning,
      execution: colors.success,
      error: colors.error,
    };
  };

  const getTypeLabel = () => {
    return {
      thought: "💭",
      task: "📋",
      analysis: "🔍",
      suggestion: "💡",
      execution: "⚡",
      error: "❌",
    };
  };

  return (
    <div class="space-y-2">
      {displayedActivities().map((activity) => (
        <div
          class="p-3 rounded-lg"
          style={{
            "background-color": colors.surface2,
            border: `1px solid ${colors.border}`,
          }}
        >
          <div class="flex items-center gap-2 mb-2">
            <span class="text-sm">{getTypeLabel()[activity.type]}</span>
            <span
              class="text-xs font-medium px-2 py-1 rounded"
              style={{
                "background-color": "rgba(53, 224, 208, 0.12)",
                color: getTypeColor()[activity.type],
              }}
            >
              {activity.type}
            </span>
            <span class="text-xs" style={{ color: colors.textMuted }}>
              {activity.timestamp.toLocaleTimeString()}
            </span>
          </div>
          <div class="text-sm" style={{ color: colors.text }}>
            {activity.isStreaming ? (
              <StreamingText text={activity.content} speed={20} />
            ) : (
              <p>{activity.content}</p>
            )}
          </div>
        </div>
      ))}
      
      {props.activities.length === 0 && (
        <div class="text-center py-4">
          <p class="text-sm" style={{ color: colors.textMuted }}>
            No activity yet
          </p>
        </div>
      )}
    </div>
  );
};
