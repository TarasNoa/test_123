import { Component } from "solid-js";
import { colors } from "../ui/tokens";

interface ThinkingIndicatorProps {
  text?: string;
  size?: "sm" | "md" | "lg";
}

/**
 * Thinking Indicator Component
 * 
 * Animated indicator showing AI is thinking with:
 * - Pulsing dots animation
 * - Optional text label
 * - Configurable size
 * - Smooth transitions
 */
export const ThinkingIndicator: Component<ThinkingIndicatorProps> = (props) => {
  const getSize = () => {
    switch (props.size) {
      case "sm":
        return { dot: "w-1.5 h-1.5", gap: "gap-1", text: "text-xs" };
      case "lg":
        return { dot: "w-3 h-3", gap: "gap-2", text: "text-base" };
      default:
        return { dot: "w-2 h-2", gap: "gap-1.5", text: "text-sm" };
    }
  };

  const size = getSize();

  return (
    <div class="flex items-center gap-2">
      <div class={`flex items-center ${size.gap}`}>
        {[0, 1, 2].map((i) => (
          <div
            class={`${size.dot} rounded-full`}
            style={{
              "background-color": colors.turquoise,
              animation: `pulse 1.5s ease-in-out ${i * 0.2}s infinite`,
            }}
          />
        ))}
      </div>
      {props.text && (
        <span class={size.text} style={{ color: colors.textMuted }}>
          {props.text}
        </span>
      )}
      <style>{`
        @keyframes pulse {
          0%, 100% { opacity: 0.3; transform: scale(0.8); }
          50% { opacity: 1; transform: scale(1); }
        }
      `}</style>
    </div>
  );
};
