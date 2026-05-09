import { Component } from "solid-js";
import { colors, spacing } from "./tokens";

interface TabItem {
  id: string;
  label: string;
  active?: boolean;
}

interface TabsProps {
  items: TabItem[];
  onTabChange?: (id: string) => void;
}

/**
 * Tabs Component
 * 
 * Tab navigation with:
 * - Dark background #0F131A
 * - Active tab: turquoise border bottom
 * - Smooth transition
 */
export const Tabs: Component<TabsProps> = (props) => {
  return (
    <div class="flex gap-1 border-b" style={{ "border-color": colors.border }}>
      {props.items.map((item) => (
        <button
          onClick={() => props.onTabChange?.(item.id)}
          class="px-4 py-2 text-sm font-medium transition-all"
          style={{
            color: item.active ? colors.turquoise : colors.textMuted,
            "border-bottom": item.active ? `2px solid ${colors.turquoise}` : "2px solid transparent",
          }}
        >
          {item.label}
        </button>
      ))}
    </div>
  );
};
