/**
 * Focus Modes
 * 
 * Reduces cognitive overload by showing only relevant UI for each mode:
 * - Planning Mode: graph, roadmap, tasks
 * - Coding Mode: editor, AI actions, terminal
 * - Collaboration Mode: team, comments, live activity
 * - Intelligence Mode: risks, AI reasoning, orchestration
 * 
 * Professional tools win by reduction, not addition.
 */

import { switchFocusMode, getCurrentFocusMode, getPanelConfiguration } from "../predictive/PredictiveUX";

export type FocusModeType = "planning" | "coding" | "collaboration" | "intelligence";

export interface FocusModeConfig {
  id: FocusModeType;
  label: string;
  icon: string;
  description: string;
  panels: {
    leftPanelVisible: boolean;
    rightPanelVisible: boolean;
    bottomPanelVisible: boolean;
  };
  aiPanelSection: string;
  keyboardShortcut: string;
}

export const FOCUS_MODES: Record<FocusModeType, FocusModeConfig> = {
  planning: {
    id: "planning",
    label: "Planning",
    icon: "📋",
    description: "Graph, roadmap, tasks",
    panels: {
      leftPanelVisible: true,
      rightPanelVisible: true,
      bottomPanelVisible: true,
    },
    aiPanelSection: "reasoning",
    keyboardShortcut: "Cmd+Shift+P",
  },
  coding: {
    id: "coding",
    label: "Coding",
    icon: "💻",
    description: "Editor, AI actions, terminal",
    panels: {
      leftPanelVisible: false,
      rightPanelVisible: true,
      bottomPanelVisible: false,
    },
    aiPanelSection: "actions",
    keyboardShortcut: "Cmd+Shift+C",
  },
  collaboration: {
    id: "collaboration",
    label: "Collaboration",
    icon: "👥",
    description: "Team, comments, live activity",
    panels: {
      leftPanelVisible: true,
      rightPanelVisible: false,
      bottomPanelVisible: true,
    },
    aiPanelSection: "context",
    keyboardShortcut: "Cmd+Shift+O",
  },
  intelligence: {
    id: "intelligence",
    label: "Intelligence",
    icon: "🧠",
    description: "Risks, AI reasoning, orchestration",
    panels: {
      leftPanelVisible: true,
      rightPanelVisible: true,
      bottomPanelVisible: true,
    },
    aiPanelSection: "recommendations",
    keyboardShortcut: "Cmd+Shift+I",
  },
};

/**
 * Get current focus mode configuration
 */
export function getCurrentFocusModeConfig(): FocusModeConfig {
  const mode = getCurrentFocusMode();
  return FOCUS_MODES[mode];
}

/**
 * Switch focus mode and return configuration
 */
export function switchToFocusMode(mode: FocusModeType): FocusModeConfig {
  switchFocusMode(mode);
  return FOCUS_MODES[mode];
}

/**
 * Get visible panels for current mode
 */
export function getVisiblePanels() {
  const config = getPanelConfiguration();
  return config;
}
