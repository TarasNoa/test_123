import { Component, createSignal } from "solid-js";
import { colors, radius, spacing, zIndex } from "./tokens";

interface CommandItem {
  id: string;
  label: string;
  shortcut?: string;
  action: () => void;
}

interface CommandPaletteProps {
  isOpen?: boolean;
  onClose?: () => void;
  commands?: CommandItem[];
}

/**
 * Command Palette Component
 * 
 * Keyboard-driven command palette with:
 * - Dark overlay background
 * - Search input
 * - Filterable commands
 * - Keyboard navigation
 * - z-index: 50
 */
export const CommandPalette: Component<CommandPaletteProps> = (props) => {
  const [searchQuery, setSearchQuery] = createSignal("");
  const [selectedIndex, setSelectedIndex] = createSignal(0);

  const filteredCommands = () => {
    if (!props.commands) return [];
    const query = searchQuery().toLowerCase();
    return props.commands.filter(cmd => 
      cmd.label.toLowerCase().includes(query)
    );
  };

  const handleKeyDown = (e: KeyboardEvent) => {
    const commands = filteredCommands();
    if (e.key === "ArrowDown") {
      setSelectedIndex((prev) => (prev + 1) % commands.length);
    } else if (e.key === "ArrowUp") {
      setSelectedIndex((prev) => (prev - 1 + commands.length) % commands.length);
    } else if (e.key === "Enter") {
      const selected = commands[selectedIndex()];
      if (selected) {
        selected.action();
        props.onClose?.();
      }
    } else if (e.key === "Escape") {
      props.onClose?.();
    }
  };

  if (!props.isOpen) return null;

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
          "background-color": colors.surface,
          border: `1px solid ${colors.border}`,
          "border-radius": radius.lg,
          padding: spacing.lg,
        }}
        onClick={(e) => e.stopPropagation()}
        onKeyDown={handleKeyDown}
        tabIndex={0}
      >
        {/* Search Input */}
        <input
          type="text"
          value={searchQuery()}
          onInput={(e) => setSearchQuery(e.currentTarget.value)}
          placeholder="Search commands..."
          class="w-full px-4 py-3 rounded-lg mb-4 outline-none"
          style={{
            "background-color": colors.surface2,
            border: `1px solid ${colors.border}`,
            color: colors.text,
            "font-size": "16px",
          }}
          autoFocus
        />

        {/* Commands List */}
        <div class="space-y-1 max-h-80 overflow-auto">
          {filteredCommands().map((cmd, index) => (
            <button
              onClick={() => {
                cmd.action();
                props.onClose?.();
              }}
              class="w-full flex items-center justify-between px-4 py-3 rounded-lg transition-all"
              style={{
                "background-color": index === selectedIndex() ? colors.hover : "transparent",
                border: index === selectedIndex() ? `1px solid ${colors.focus}` : "1px solid transparent",
              }}
            >
              <span class="text-sm" style={{ color: colors.text }}>
                {cmd.label}
              </span>
              {cmd.shortcut && (
                <span
                  class="text-xs px-2 py-1 rounded"
                  style={{
                    "background-color": colors.surface2,
                    color: colors.textMuted,
                  }}
                >
                  {cmd.shortcut}
                </span>
              )}
            </button>
          ))}
        </div>

        {/* Footer */}
        <div class="mt-4 pt-4 border-t flex items-center gap-4 text-xs" style={{ "border-color": colors.border, color: colors.textMuted }}>
          <span>↑↓ Navigate</span>
          <span>↵ Select</span>
          <span>Esc Close</span>
        </div>
      </div>
    </div>
  );
};
