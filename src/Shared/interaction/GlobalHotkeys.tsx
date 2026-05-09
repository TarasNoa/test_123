import { Component, onMount, onCleanup } from "solid-js";

interface Hotkey {
  key: string;
  ctrl?: boolean;
  meta?: boolean;
  shift?: boolean;
  alt?: boolean;
  action: () => void;
  description: string;
}

interface GlobalHotkeysProps {
  hotkeys: Hotkey[];
}

/**
 * Global Hotkeys System
 * 
 * Keyboard-first UX foundation with:
 * - Global key listeners
 * - Modifier key support (Ctrl, Meta, Shift, Alt)
 * - Hotkey descriptions
 * - Prevent default behavior when needed
 */
export const GlobalHotkeys: Component<GlobalHotkeysProps> = (props) => {
  const handleKeyDown = (e: KeyboardEvent) => {
    for (const hotkey of props.hotkeys) {
      const keyMatch = e.key.toLowerCase() === hotkey.key.toLowerCase();
      const ctrlMatch = hotkey.ctrl === undefined || hotkey.ctrl === e.ctrlKey;
      const metaMatch = hotkey.meta === undefined || hotkey.meta === e.metaKey;
      const shiftMatch = hotkey.shift === undefined || hotkey.shift === e.shiftKey;
      const altMatch = hotkey.alt === undefined || hotkey.alt === e.altKey;

      if (keyMatch && ctrlMatch && metaMatch && shiftMatch && altMatch) {
        e.preventDefault();
        hotkey.action();
        return;
      }
    }
  };

  onMount(() => {
    window.addEventListener("keydown", handleKeyDown);
  });

  onCleanup(() => {
    window.removeEventListener("keydown", handleKeyDown);
  });

  return null;
};

/**
 * Hotkey Registry
 * 
 * Central registry for all application hotkeys
 */
export const hotkeyRegistry = {
  // Navigation
  goToDashboard: {
    key: "g",
    shift: true,
    action: () => console.log("Go to Dashboard"),
    description: "Go to Dashboard",
  },
  goToProjects: {
    key: "p",
    shift: true,
    action: () => console.log("Go to Projects"),
    description: "Go to Projects",
  },
  goToIDE: {
    key: "i",
    shift: true,
    action: () => console.log("Go to IDE"),
    description: "Go to IDE",
  },
  
  // Command Palette
  openCommandPalette: {
    key: "k",
    ctrl: true,
    meta: true,
    action: () => console.log("Open Command Palette"),
    description: "Open Command Palette",
  },
  
  // AI Actions
  triggerAI: {
    key: "a",
    shift: true,
    action: () => console.log("Trigger AI"),
    description: "Trigger AI Action",
  },
  analyzeCode: {
    key: "a",
    ctrl: true,
    meta: true,
    action: () => console.log("Analyze Code"),
    description: "Analyze Code",
  },
  
  // Editor
  saveFile: {
    key: "s",
    ctrl: true,
    meta: true,
    action: () => console.log("Save File"),
    description: "Save File",
  },
  newFile: {
    key: "n",
    ctrl: true,
    meta: true,
    action: () => console.log("New File"),
    description: "New File",
  },
  
  // Workspace
  toggleSidebar: {
    key: "b",
    ctrl: true,
    meta: true,
    action: () => console.log("Toggle Sidebar"),
    description: "Toggle Sidebar",
  },
  toggleAIPanel: {
    key: "p",
    ctrl: true,
    meta: true,
    action: () => console.log("Toggle AI Panel"),
    description: "Toggle AI Panel",
  },
  
  // Search
  searchFiles: {
    key: "p",
    ctrl: true,
    meta: true,
    shift: true,
    action: () => console.log("Search Files"),
    description: "Search Files",
  },
  searchSymbols: {
    key: "s",
    shift: true,
    action: () => console.log("Search Symbols"),
    description: "Search Symbols",
  },
  
  // Execution
  runCode: {
    key: "Enter",
    shift: true,
    action: () => console.log("Run Code"),
    description: "Run Code",
  },
  stopExecution: {
    key: ".",
    ctrl: true,
    meta: true,
    action: () => console.log("Stop Execution"),
    description: "Stop Execution",
  },
};
