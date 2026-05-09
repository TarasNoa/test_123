import { Component, createSignal } from "solid-js";
import { colors, layout } from "../ui/tokens";

interface SidebarItem {
  id: string;
  label: string;
  icon: string;
  active?: boolean;
}

interface SidebarProps {
  items: SidebarItem[];
  onItemSelect?: (id: string) => void;
}

/**
 * Workspace Sidebar
 * 
 * Fixed left sidebar with:
 * - Width: 72px collapsed / 240px expanded
 * - Dark background #0F131A
 * - Border-right: 1px solid #1D2430
 * - Smooth transition 0.2s ease
 * - Icons centered
 * - Active item: background rgba(53,224,208,0.12), border 1px solid rgba(53,224,208,0.25), icon color #35E0D0
 */
export const Sidebar: Component<SidebarProps> = (props) => {
  const [collapsed, setCollapsed] = createSignal(false);

  return (
    <div
      class="flex flex-col h-full py-4"
      style={{
        width: collapsed() ? layout.sidebarCollapsed : layout.sidebarExpanded,
        "background-color": colors.surface,
        "border-right": `1px solid ${colors.border}`,
        transition: layout.sidebarTransition,
      }}
    >
      {/* Logo/Brand */}
      <div class="px-4 mb-6">
        <div
          class="flex items-center gap-3"
          style={{ color: colors.turquoise }}
        >
          <div class="w-8 h-8 rounded-lg flex items-center justify-center" style={{ "background-color": colors.turquoiseLight }}>
            <span class="text-lg font-bold">L</span>
          </div>
          {!collapsed() && <span class="font-semibold text-lg">Libr4</span>}
        </div>
      </div>

      {/* Navigation Items */}
      <div class="flex-1 px-2 space-y-1">
        {props.items.map((item) => (
          <button
            onClick={() => props.onItemSelect?.(item.id)}
            class="flex items-center gap-3 w-full px-3 py-2 rounded-lg transition-all"
            style={{
              "background-color": item.active ? colors.turquoiseLight : "transparent",
              border: item.active ? `1px solid ${colors.focus}` : "1px solid transparent",
              color: item.active ? colors.turquoise : colors.textMuted,
            }}
          >
            <div class="flex-shrink-0 w-5 h-5 flex items-center justify-center">
              {/* Icon placeholder - use lucide-solid icons */}
              <span class="text-sm">{item.icon}</span>
            </div>
            {!collapsed() && <span class="text-sm font-medium">{item.label}</span>}
          </button>
        ))}
      </div>

      {/* Collapse Toggle */}
      <div class="px-2 pt-4 border-t" style={{ "border-color": colors.border }}>
        <button
          onClick={() => setCollapsed(!collapsed())}
          class="flex items-center justify-center w-full px-3 py-2 rounded-lg transition-all"
          style={{
            "background-color": colors.hover,
            color: colors.textMuted,
          }}
        >
          <span class="text-sm">{collapsed() ? "→" : "←"}</span>
        </button>
      </div>
    </div>
  );
};
