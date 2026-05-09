import { Component } from "solid-js";
import { colors, radius, spacing, shadows } from "./tokens";

interface CardProps {
  title?: string;
  children?: any;
  hover?: boolean;
  padding?: string;
}

/**
 * Card Component
 * 
 * Surface card with:
 * - Dark background #0F131A
 * - Border 1px solid #1D2430
 * - Subtle shadow
 * - Optional hover effect
 */
export const Card: Component<CardProps> = (props) => {
  return (
    <div
      class="rounded-lg"
      style={{
        "background-color": colors.surface,
        border: `1px solid ${colors.border}`,
        "border-radius": radius.lg,
        "box-shadow": shadows.sm,
        padding: props.padding || spacing.lg,
        transition: props.hover ? "0.2s ease" : "none",
      }}
      classList={{
        "hover:shadow-md": props.hover,
        "cursor-pointer": props.hover,
      }}
    >
      {props.title && (
        <h3
          class="text-lg font-semibold mb-3"
          style={{ color: colors.text }}
        >
          {props.title}
        </h3>
      )}
      {props.children}
    </div>
  );
};
