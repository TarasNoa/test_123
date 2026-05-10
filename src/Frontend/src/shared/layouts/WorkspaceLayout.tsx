import { Component, JSX } from "solid-js";
import { layout } from "../ui/tokens";

interface WorkspaceLayoutProps {
  topbar?: JSX.Element;
  sidebar?: JSX.Element;
  aiPanel?: JSX.Element;
  children?: JSX.Element;
}

/**
 * Workspace Layout
 * 
 * Desktop-first workspace layout with:
 * - Top navigation (64px height)
 * - Left sidebar (72px collapsed / 240px expanded)
 * - Main workspace area
 * - AI panel (280px min / 400px max)
 */
export const WorkspaceLayout: Component<WorkspaceLayoutProps> = (props) => {
  return (
    <div
      class="flex flex-col h-screen"
      style={{
        "background-color": "#07090D",
        "color": "#F5F7FA",
        "font-family": "Inter, -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif",
      }}
    >
      {/* Top Navigation */}
      <div
        class="flex-shrink-0 border-b"
        style={{
          height: layout.headerHeight,
          "border-color": "#1D2430",
          "background-color": "#0F131A",
        }}
      >
        {props.topbar}
      </div>

      {/* Main Content Area */}
      <div class="flex flex-1 overflow-hidden">
        {/* Left Sidebar */}
        <div
          class="flex-shrink-0 border-r"
          style={{
            width: layout.sidebarExpanded,
            "border-color": "#1D2430",
            "background-color": "#0F131A",
            transition: layout.sidebarTransition,
          }}
        >
          {props.sidebar}
        </div>

        {/* Main Workspace */}
        <div class="flex-1 overflow-auto">
          {props.children}
        </div>

        {/* AI Panel */}
        <div
          class="flex-shrink-0 border-l"
          style={{
            width: layout.panelMin,
            "border-color": "#1D2430",
            "background-color": "#0F131A",
            transition: layout.panelTransition,
          }}
        >
          {props.aiPanel}
        </div>
      </div>
    </div>
  );
};
