import { Component, createSignal, onMount, onCleanup } from "solid-js";
import { colors, radius, spacing, zIndex } from "../ui/tokens";

interface Command {
  id: string;
  label: string;
  shortcut?: string;
  icon?: string;
  category: "navigation" | "ai" | "project" | "settings" | "workflow";
  action: () => void;
}

interface GlobalCommandPaletteProps {
  isOpen: boolean;
  onClose: () => void;
  commands: Command[];
}

/**
 * Global Command Palette
 * 
 * Keyboard-driven command palette (Cmd/Ctrl + K) with:
 * - Search functionality
 * - Category grouping
 * - Keyboard navigation (↑↓, Enter, Esc)
 * - Shortcut display
 * - Similar to Linear, Cursor, Raycast
 */
export const GlobalCommandPalette: Component<GlobalCommandPaletteProps> = (props) => {
  const [searchQuery, setSearchQuery] = createSignal("");
  const [selectedIndex, setSelectedIndex] = createSignal(0);

  const filteredCommands = () => {
    const query = searchQuery().toLowerCase();
    return props.commands.filter(cmd => 
      cmd.label.toLowerCase().includes(query) ||
      cmd.category.toLowerCase().includes(query)
    );
  };

  const groupedCommands = () => {
    const commands = filteredCommands();
    const groups: Record<string, Command[]> = {};
    
    commands.forEach(cmd => {
      if (!groups[cmd.category]) {
        groups[cmd.category] = [];
      }
      groups[cmd.category].push(cmd);
    });
    
    return groups;
  };

  const handleKeyDown = (e: KeyboardEvent) => {
    const commands = filteredCommands();
    
    if (e.key === "ArrowDown") {
      e.preventDefault();
      setSelectedIndex((prev) => (prev + 1) % commands.length);
    } else if (e.key === "ArrowUp") {
      e.preventDefault();
      setSelectedIndex((prev) => (prev - 1 + commands.length) % commands.length);
    } else if (e.key === "Enter") {
      e.preventDefault();
      const selected = commands[selectedIndex()];
      if (selected) {
        selected.action();
        props.onClose();
      }
    } else if (e.key === "Escape") {
      e.preventDefault();
      props.onClose();
    }
  };

  const getCategoryLabel = (category: string) => {
    const labels: Record<string, string> = {
      navigation: "Navigation",
      ai: "AI Actions",
      project: "Project",
      settings: "Settings",
      workflow: "Workflow",
    };
    return labels[category] || category;
  };

  const getCategoryIcon = (category: string) => {
    const icons: Record<string, string> = {
      navigation: "🧭",
      ai: "🤖",
      project: "📁",
      settings: "⚙️",
      workflow: "⚡",
    };
    return icons[category] || "•";
  };

  onMount(() => {
    window.addEventListener("keydown", handleKeyDown);
  });

  onCleanup(() => {
    window.removeEventListener("keydown", handleKeyDown);
  });

  if (!props.isOpen) return null;

  const groups = groupedCommands();
  const flatCommands = filteredCommands();

  return (
    <div
      class="fixed inset-0 flex items-start justify-center pt-[20vh]"
      style={{
        "background-color": "rgba(0, 0, 0, 0.7)",
        "z-index": zIndex.tooltip,
      }}
      onClick={props.onClose}
    >
      <div
        class="rounded-lg w-full max-w-2xl"
        style={{
          "background-color": "#0F131A",
          border: `1px solid ${colors.border}`,
          "border-radius": radius.lg,
          padding: spacing.lg,
          "box-shadow": "0 25px 50px -12px rgba(0, 0, 0, 0.5)",
        }}
        onClick={(e) => e.stopPropagation()}
        onKeyDown={handleKeyDown}
        tabIndex={0}
      >
        {/* Search Input */}
        <div class="flex items-center gap-3 mb-4">
          <span class="text-lg" style={{ color: colors.textMuted }}>🔍</span>
          <input
            type="text"
            value={searchQuery()}
            onInput={(e) => {
              setSearchQuery(e.currentTarget.value);
              setSelectedIndex(0);
            }}
            placeholder="Type a command or search..."
            class="flex-1 bg-transparent outline-none text-base"
            style={{
              color: colors.text,
              "font-size": "16px",
            }}
            autoFocus
          />
          <div class="flex gap-2">
            <span
              class="text-xs px-2 py-1 rounded"
              style={{
                "background-color": colors.surface2,
                color: colors.textMuted,
              }}
            >
              ↑↓ Navigate
            </span>
            <span
              class="text-xs px-2 py-1 rounded"
              style={{
                "background-color": colors.surface2,
                color: colors.textMuted,
              }}
            >
              ↵ Select
            </span>
            <span
              class="text-xs px-2 py-1 rounded"
              style={{
                "background-color": colors.surface2,
                color: colors.textMuted,
              }}
            >
              Esc Close
            </span>
          </div>
        </div>

        {/* Command List */}
        <div class="max-h-80 overflow-auto">
          {Object.entries(groups).map(([category, commands]) => (
            <div class="mb-4">
              <div class="flex items-center gap-2 px-3 py-2 mb-2">
                <span class="text-sm">{getCategoryIcon(category)}</span>
                <span class="text-xs font-semibold uppercase" style={{ color: colors.textMuted }}>
                  {getCategoryLabel(category)}
                </span>
              </div>
              <div>
                {commands.map((cmd, idx) => {
                  const globalIndex = flatCommands.indexOf(cmd);
                  const isSelected = globalIndex === selectedIndex();
                  
                  return (
                    <button
                      onClick={() => {
                        cmd.action();
                        props.onClose();
                      }}
                      class="w-full flex items-center justify-between px-3 py-2 rounded-lg transition-all"
                      style={{
                        "background-color": isSelected ? colors.hover : "transparent",
                        border: isSelected ? `1px solid ${colors.focus}` : "1px solid transparent",
                      }}
                    >
                      <div class="flex items-center gap-3">
                        {cmd.icon && <span class="text-sm">{cmd.icon}</span>}
                        <span class="text-sm" style={{ color: colors.text }}>
                          {cmd.label}
                        </span>
                      </div>
                      {cmd.shortcut && (
                        <span
                          class="text-xs px-2 py-1 rounded font-mono"
                          style={{
                            "background-color": colors.surface2,
                            color: colors.textMuted,
                          }}
                        >
                          {cmd.shortcut}
                        </span>
                      )}
                    </button>
                  );
                })}
              </div>
            </div>
          ))}
          
          {flatCommands.length === 0 && (
            <div class="text-center py-8">
              <p class="text-sm" style={{ color: colors.textMuted }}>
                No commands found
              </p>
            </div>
          )}
        </div>
      </div>
    </div>
  );
};
