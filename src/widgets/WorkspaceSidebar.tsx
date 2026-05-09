import { Component } from "solid-js";
import { colors, spacing } from "../shared/ui/tokens";

interface SidebarSection {
  title: string;
  items: {
    id: string;
    label: string;
    icon: string;
    count?: number;
  }[];
}

interface WorkspaceSidebarProps {
  sections: SidebarSection[];
  onItemSelect?: (sectionId: string, itemId: string) => void;
}

/**
 * Workspace Sidebar Widget
 * 
 * Project structure sidebar with:
 * - Sections (roadmap, files, tasks, collaborators)
 * - Collapsible sections
 * - Item counts
 * - Active state styling
 */
export const WorkspaceSidebar: Component<WorkspaceSidebarProps> = (props) => {
  return (
    <div class="flex flex-col h-full py-4" style={{ "background-color": colors.surface }}>
      {props.sections.map((section) => (
        <div class="mb-4">
          <div class="px-4 mb-2">
            <h3 class="text-xs font-semibold uppercase" style={{ color: colors.textMuted }}>
              {section.title}
            </h3>
          </div>
          <div class="px-2 space-y-1">
            {section.items.map((item) => (
              <button
                onClick={() => props.onItemSelect?.(section.title, item.id)}
                class="flex items-center justify-between w-full px-3 py-2 rounded-lg transition-all"
                style={{
                  "background-color": "transparent",
                  color: colors.textMuted,
                }}
                classList={{
                  "hover:bg-opacity-10": true,
                }}
              >
                <div class="flex items-center gap-2">
                  <span class="text-sm">{item.icon}</span>
                  <span class="text-sm">{item.label}</span>
                </div>
                {item.count !== undefined && (
                  <span class="text-xs" style={{ color: colors.textSecondary }}>
                    {item.count}
                  </span>
                )}
              </button>
            ))}
          </div>
        </div>
      ))}
    </div>
  );
};
