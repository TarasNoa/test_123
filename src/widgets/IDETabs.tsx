import { Component } from "solid-js";
import { colors, spacing } from "../shared/ui/tokens";

interface TabItem {
  id: string;
  label: string;
  icon?: string;
  modified?: boolean;
  closable?: boolean;
}

interface IDETabsProps {
  tabs: TabItem[];
  activeTab: string;
  onTabChange: (id: string) => void;
  onTabClose?: (id: string) => void;
}

/**
 * IDE Tabs Widget
 * 
 * IDE-style tab bar with:
 * - Tab labels with icons
 * - Modified indicator (dot)
 * - Close button
 * - Active tab highlighting
 */
export const IDETabs: Component<IDETabsProps> = (props) => {
  return (
    <div class="flex items-center gap-1 border-b" style={{ "border-color": colors.border }}>
      {props.tabs.map((tab) => (
        <div
          class="flex items-center gap-2 px-4 py-2 cursor-pointer transition-all"
          style={{
            "background-color": tab.id === props.activeTab ? colors.surface : "transparent",
            "border-bottom": tab.id === props.activeTab ? `2px solid ${colors.turquoise}` : "2px solid transparent",
          }}
          onClick={() => props.onTabChange(tab.id)}
        >
          {tab.icon && <span class="text-sm">{tab.icon}</span>}
          <span class="text-sm" style={{ color: colors.text }}>
            {tab.label}
          </span>
          {tab.modified && (
            <div class="w-2 h-2 rounded-full" style={{ "background-color": colors.turquoise }} />
          )}
          {tab.closable && (
            <button
              onClick={(e) => {
                e.stopPropagation();
                props.onTabClose?.(tab.id);
              }}
              class="text-xs hover:opacity-70"
              style={{ color: colors.textMuted }}
            >
              ✕
            </button>
          )}
        </div>
      ))}
    </div>
  );
};
