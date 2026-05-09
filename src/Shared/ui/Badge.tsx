import { Component } from "solid-js";
import { colors, radius, spacing } from "./tokens";

interface BadgeProps {
  variant?: "default" | "success" | "warning" | "error" | "turquoise" | "purple";
  children: any;
}

/**
 * Badge Component
 * 
 * Small status badge with:
 * - Variants: default, success, warning, error, turquoise, purple
 * - Rounded corners
 * - Subtle background with colored text
 */
export const Badge: Component<BadgeProps> = (props) => {
  const getVariantStyles = () => {
    switch (props.variant) {
      case "success":
        return {
          "background-color": "rgba(34, 197, 94, 0.12)",
          color: colors.success,
        };
      case "warning":
        return {
          "background-color": "rgba(245, 158, 11, 0.12)",
          color: colors.warning,
        };
      case "error":
        return {
          "background-color": "rgba(239, 68, 68, 0.12)",
          color: colors.error,
        };
      case "turquoise":
        return {
          "background-color": colors.turquoiseLight,
          color: colors.turquoise,
        };
      case "purple":
        return {
          "background-color": colors.purpleLight,
          color: colors.purple,
        };
      default:
        return {
          "background-color": colors.surface2,
          color: colors.textMuted,
        };
    }
  };

  return (
    <span
      class="inline-flex items-center px-2 py-1 rounded-md text-xs font-medium"
      style={{
        ...getVariantStyles(),
        "border-radius": radius.sm,
        padding: `${spacing.xs} ${spacing.sm}`,
        "font-size": "12px",
      }}
    >
      {props.children}
    </span>
  );
};
