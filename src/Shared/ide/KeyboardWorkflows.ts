/**
 * Keyboard Workflows
 * 
 * Implements keyboard-first IDE UX:
 * - Command palette (Cmd+K)
 * - Quick navigation (Cmd+P)
 * - Panel toggles (Cmd+B, Cmd+J, Cmd+K, Cmd+L)
 * - Tab navigation (Cmd+1-9, Cmd+Tab, Cmd+Shift+Tab)
 * - Split view (Cmd+\)
 * - Focus modes (Cmd+Shift+P/C/O/I)
 * 
 * Professional tools require minimal mouse dependency
 */

export interface KeyboardShortcut {
  key: string;
  modifiers: ("Cmd" | "Ctrl" | "Shift" | "Alt" | "Meta")[];
  description: string;
  action: () => void;
}

export class KeyboardWorkflowEngine {
  private shortcuts: Map<string, KeyboardShortcut> = new Map();
  private enabled = true;

  constructor() {
    this.registerDefaultShortcuts();
    this.setupGlobalListener();
  }

  /**
   * Register default keyboard shortcuts
   */
  private registerDefaultShortcuts(): void {
    // Command Palette
    this.register({
      key: "k",
      modifiers: ["Cmd"],
      description: "Open command palette",
      action: () => {
        this.emit("command-palette:toggle");
      },
    });

    // Quick Navigation
    this.register({
      key: "p",
      modifiers: ["Cmd"],
      description: "Quick file navigation",
      action: () => {
        this.emit("quick-nav:open");
      },
    });

    // Panel Toggles
    this.register({
      key: "b",
      modifiers: ["Cmd"],
      description: "Toggle sidebar",
      action: () => {
        this.emit("panel:toggle", "left");
      },
    });

    this.register({
      key: "j",
      modifiers: ["Cmd"],
      description: "Toggle bottom panel",
      action: () => {
        this.emit("panel:toggle", "bottom");
      },
    });

    this.register({
      key: "k",
      modifiers: ["Cmd", "Shift"],
      description: "Toggle AI panel",
      action: () => {
        this.emit("panel:toggle", "right");
      },
    });

    this.register({
      key: "l",
      modifiers: ["Cmd"],
      description: "Toggle terminal",
      action: () => {
        this.emit("panel:toggle", "bottom");
      },
    });

    // Tab Navigation
    this.register({
      key: "1",
      modifiers: ["Cmd"],
      description: "Switch to tab 1",
      action: () => {
        this.emit("tab:switch", 0);
      },
    });

    this.register({
      key: "2",
      modifiers: ["Cmd"],
      description: "Switch to tab 2",
      action: () => {
        this.emit("tab:switch", 1);
      },
    });

    this.register({
      key: "3",
      modifiers: ["Cmd"],
      description: "Switch to tab 3",
      action: () => {
        this.emit("tab:switch", 2);
      },
    });

    this.register({
      key: "4",
      modifiers: ["Cmd"],
      description: "Switch to tab 4",
      action: () => {
        this.emit("tab:switch", 3);
      },
    });

    this.register({
      key: "5",
      modifiers: ["Cmd"],
      description: "Switch to tab 5",
      action: () => {
        this.emit("tab:switch", 4);
      },
    });

    this.register({
      key: "9",
      modifiers: ["Cmd"],
      description: "Switch to last tab",
      action: () => {
        this.emit("tab:switch", -1);
      },
    });

    // Tab Cycling
    this.register({
      key: "Tab",
      modifiers: ["Cmd"],
      description: "Next tab",
      action: () => {
        this.emit("tab:next");
      },
    });

    this.register({
      key: "Tab",
      modifiers: ["Cmd", "Shift"],
      description: "Previous tab",
      action: () => {
        this.emit("tab:previous");
      },
    });

    // Close Tab
    this.register({
      key: "w",
      modifiers: ["Cmd"],
      description: "Close current tab",
      action: () => {
        this.emit("tab:close");
      },
    });

    // Split View
    this.register({
      key: "\\",
      modifiers: ["Cmd"],
      description: "Toggle split view",
      action: () => {
        this.emit("split:toggle");
      },
    });

    // Focus Modes
    this.register({
      key: "p",
      modifiers: ["Cmd", "Shift"],
      description: "Switch to planning mode",
      action: () => {
        this.emit("focus:mode", "planning");
      },
    });

    this.register({
      key: "c",
      modifiers: ["Cmd", "Shift"],
      description: "Switch to coding mode",
      action: () => {
        this.emit("focus:mode", "coding");
      },
    });

    this.register({
      key: "o",
      modifiers: ["Cmd", "Shift"],
      description: "Switch to collaboration mode",
      action: () => {
        this.emit("focus:mode", "collaboration");
      },
    });

    this.register({
      key: "i",
      modifiers: ["Cmd", "Shift"],
      description: "Switch to intelligence mode",
      action: () => {
        this.emit("focus:mode", "intelligence");
      },
    });

    // Save
    this.register({
      key: "s",
      modifiers: ["Cmd"],
      description: "Save current file",
      action: () => {
        this.emit("file:save");
      },
    });

    // Find
    this.register({
      key: "f",
      modifiers: ["Cmd"],
      description: "Find in file",
      action: () => {
        this.emit("find:open");
      },
    });

    // Replace
    this.register({
      key: "h",
      modifiers: ["Cmd"],
      description: "Find and replace",
      action: () => {
        this.emit("replace:open");
      },
    });

    // Go to Line
    this.register({
      key: "g",
      modifiers: ["Cmd"],
      description: "Go to line",
      action: () => {
        this.emit("goto:line");
      },
    });

    // Command History
    this.register({
      key: "z",
      modifiers: ["Cmd"],
      description: "Undo",
      action: () => {
        this.emit("edit:undo");
      },
    });

    this.register({
      key: "z",
      modifiers: ["Cmd", "Shift"],
      description: "Redo",
      action: () => {
        this.emit("edit:redo");
      },
    });
  }

  /**
   * Register keyboard shortcut
   */
  register(shortcut: KeyboardShortcut): void {
    const key = this.getShortcutKey(shortcut);
    this.shortcuts.set(key, shortcut);
  }

  /**
   * Unregister keyboard shortcut
   */
  unregister(key: string, modifiers: KeyboardShortcut["modifiers"]): void {
    const shortcutKey = this.getShortcutKey({ key, modifiers, description: "", action: () => {} });
    this.shortcuts.delete(shortcutKey);
  }

  /**
   * Get shortcut key for lookup
   */
  private getShortcutKey(shortcut: KeyboardShortcut): string {
    return [...shortcut.modifiers, shortcut.key].join("+");
  }

  /**
   * Setup global keyboard listener
   */
  private setupGlobalListener(): void {
    if (typeof window === "undefined") return;

    window.addEventListener("keydown", (event) => {
      if (!this.enabled) return;

      const modifiers: KeyboardShortcut["modifiers"] = [];
      if (event.metaKey || event.ctrlKey) modifiers.push("Cmd");
      if (event.shiftKey) modifiers.push("Shift");
      if (event.altKey) modifiers.push("Alt");

      const key = this.getShortcutKey({ key: event.key, modifiers, description: "", action: () => {} });
      const shortcut = this.shortcuts.get(key);

      if (shortcut) {
        event.preventDefault();
        shortcut.action();
      }
    });
  }

  /**
   * Emit custom event
   */
  private emit(event: string, ...args: unknown[]): void {
    if (typeof window === "undefined") return;
    window.dispatchEvent(new CustomEvent(event, { detail: args }));
  }

  /**
   * Enable keyboard workflows
   */
  enable(): void {
    this.enabled = true;
  }

  /**
   * Disable keyboard workflows
   */
  disable(): void {
    this.enabled = false;
  }

  /**
   * Get all registered shortcuts
   */
  getShortcuts(): KeyboardShortcut[] {
    return Array.from(this.shortcuts.values());
  }
}

// Global keyboard workflow engine instance
export const keyboardWorkflows = new KeyboardWorkflowEngine();

/**
 * Keyboard workflow helpers
 */
export function registerShortcut(shortcut: KeyboardShortcut): void {
  keyboardWorkflows.register(shortcut);
}

export function unregisterShortcut(key: string, modifiers: KeyboardShortcut["modifiers"]): void {
  keyboardWorkflows.unregister(key, modifiers);
}

export function enableKeyboardWorkflows(): void {
  keyboardWorkflows.enable();
}

export function disableKeyboardWorkflows(): void {
  keyboardWorkflows.disable();
}

export function getAllShortcuts(): KeyboardShortcut[] {
  return keyboardWorkflows.getShortcuts();
}
