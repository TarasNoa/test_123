import { Component } from "solid-js";
import { colors, layout } from "../ui/tokens";

interface TopbarProps {
  title?: string;
  user?: {
    name: string;
    avatar?: string;
  };
  actions?: any;
}

/**
 * Top Navigation Bar
 * 
 * Fixed top navigation with:
 * - Height: 64px
 * - Dark background #0F131A
 * - Border-bottom: 1px solid #1D2430
 * - Logo, title, user info, actions
 */
export const Topbar: Component<TopbarProps> = (props) => {
  return (
    <div
      class="flex items-center justify-between px-6"
      style={{
        height: layout.headerHeight,
        "background-color": colors.surface,
        "border-bottom": `1px solid ${colors.border}`,
      }}
    >
      {/* Left: Logo and Title */}
      <div class="flex items-center gap-4">
        <div class="w-8 h-8 rounded-lg flex items-center justify-center" style={{ "background-color": colors.turquoiseLight }}>
          <span class="text-lg font-bold" style={{ color: colors.turquoise }}>L</span>
        </div>
        <h1 class="text-lg font-semibold" style={{ color: colors.text }}>
          {props.title || "Libr4"}
        </h1>
      </div>

      {/* Center: Search or Navigation */}
      <div class="flex-1 flex justify-center">
        {/* Placeholder for search or navigation */}
      </div>

      {/* Right: User Info and Actions */}
      <div class="flex items-center gap-4">
        {props.actions}
        <div class="flex items-center gap-3 px-3 py-2 rounded-lg" style={{ "background-color": colors.surface2 }}>
          <div class="w-8 h-8 rounded-full flex items-center justify-center" style={{ "background-color": colors.turquoiseLight }}>
            <span class="text-sm font-medium" style={{ color: colors.turquoise }}>
              {props.user?.name?.charAt(0) || "U"}
            </span>
          </div>
          <span class="text-sm font-medium" style={{ color: colors.text }}>
            {props.user?.name || "User"}
          </span>
        </div>
      </div>
    </div>
  );
};
