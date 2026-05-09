import { Component } from "solid-js";
import { colors, spacing } from "../shared/ui/tokens";

interface ActivityItem {
  id: string;
  type: "thought" | "task" | "analysis" | "suggestion" | "execution";
  content: string;
  timestamp: Date;
}

interface AIActivityFeedProps {
  activities: ActivityItem[];
}

/**
 * AI Activity Feed Widget
 * 
 * Displays AI agent activity stream with:
 * - Type badges (thought, task, analysis, suggestion, execution)
 * - Timestamp
 * - Content
 * - Color-coded by type
 */
export const AIActivityFeed: Component<AIActivityFeedProps> = (props) => {
  const getTypeColor = () => {
    return {
      thought: colors.turquoise,
      task: colors.purple,
      analysis: colors.info,
      suggestion: colors.warning,
      execution: colors.success,
    };
  };

  const getTypeLabel = () => {
    return {
      thought: "💭",
      task: "📋",
      analysis: "🔍",
      suggestion: "💡",
      execution: "⚡",
    };
  };

  return (
    <div class="space-y-2">
      {props.activities.map((activity) => (
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
          <p class="text-sm" style={{ color: colors.text }}>
            {activity.content}
          </p>
        </div>
      ))}
    </div>
  );
};
