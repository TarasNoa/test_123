import { Component } from "solid-js";
import { colors, radius, spacing } from "../shared/ui/tokens";

interface ProjectCardProps {
  name: string;
  description?: string;
  status?: "active" | "archived" | "draft";
  lastModified?: string;
  onClick?: () => void;
}

/**
 * Project Card Widget
 * 
 * Card displaying project information with:
 * - Project name and description
 * - Status badge
 * - Last modified date
 * - Hover effect
 */
export const ProjectCard: Component<ProjectCardProps> = (props) => {
  const getStatusColor = () => {
    switch (props.status) {
      case "active":
        return colors.success;
      case "archived":
        return colors.textMuted;
      case "draft":
        return colors.warning;
      default:
        return colors.turquoise;
    }
  };

  return (
    <div
      class="rounded-lg p-4 cursor-pointer transition-all hover:shadow-md"
      style={{
        "background-color": colors.surface,
        border: `1px solid ${colors.border}`,
        "border-radius": radius.lg,
        transition: "0.2s ease",
      }}
      onClick={props.onClick}
    >
      <div class="flex items-start justify-between mb-3">
        <h3 class="text-base font-semibold" style={{ color: colors.text }}>
          {props.name}
        </h3>
        <span
          class="text-xs px-2 py-1 rounded"
          style={{
            "background-color": "rgba(53, 224, 208, 0.12)",
            color: getStatusColor(),
          }}
        >
          {props.status || "active"}
        </span>
      </div>
      
      {props.description && (
        <p class="text-sm mb-3" style={{ color: colors.textMuted }}>
          {props.description}
        </p>
      )}
      
      {props.lastModified && (
        <p class="text-xs" style={{ color: colors.textSecondary }}>
          Last modified: {props.lastModified}
        </p>
      )}
    </div>
  );
};
