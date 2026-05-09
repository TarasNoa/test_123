/**
 * Predictive UX
 * 
 * Interface anticipates user intent and takes proactive actions:
 * - Auto-open AI panel on build failure
 * - Show relevant workspace after task acceptance
 * - Suggest structure after project creation
 * - Auto-switch to appropriate focus mode
 * 
 * This makes the interface feel intelligent and responsive
 */

import { globalEventStream } from "../activity/WorkspaceEvent";

export type FocusMode = "planning" | "coding" | "collaboration" | "intelligence";

export interface PredictiveAction {
  type: "open_panel" | "switch_tab" | "show_suggestion" | "switch_mode" | "highlight";
  context: string;
  data: unknown;
}

/**
 * Predictive UX Engine
 */
class PredictiveUXEngine {
  private actions: PredictiveAction[] = [];
  private currentMode: FocusMode = "planning";

  constructor() {
    this.subscribeToEvents();
  }

  /**
   * Subscribe to workspace events to trigger predictive actions
   */
  private subscribeToEvents(): void {
    // Build failure - auto-open AI panel
    globalEventStream.subscribe("BuildFailed", (event) => {
      if (event.type === "BuildFailed") {
        this.addAction({
          type: "open_panel",
          context: "build_failure",
          data: {
            panel: "ai",
            section: "recommendations",
            message: `Build failed in ${event.step}: ${event.error}`,
          },
        });
        this.switchMode("intelligence");
      }
    });

    // Agent error - switch to intelligence mode
    globalEventStream.subscribe("AgentError", (event) => {
      if (event.type === "AgentError") {
        this.addAction({
          type: "switch_mode",
          context: "agent_error",
          data: { mode: "intelligence" },
        });
      }
    });

    // Task accepted - switch to coding mode
    globalEventStream.subscribe("TaskAssigned", (event) => {
      if (event.type === "TaskAssigned" && event.assignee === "current_user") {
        this.addAction({
          type: "switch_mode",
          context: "task_assigned",
          data: { mode: "coding" },
        });
        this.addAction({
          type: "open_panel",
          context: "task_assigned",
          data: {
            panel: "ai",
            section: "actions",
            message: "Task assigned - AI assistance available",
          },
        });
      }
    });

    // User joined - switch to collaboration mode
    globalEventStream.subscribe("UserJoined", (event) => {
      if (event.type === "UserJoined") {
        this.addAction({
          type: "switch_mode",
          context: "user_joined",
          data: { mode: "collaboration" },
        });
      }
    });

    // Deployment started - switch to intelligence mode
    globalEventStream.subscribe("DeploymentStarted", (event) => {
      if (event.type === "DeploymentStarted") {
        this.switchMode("intelligence");
      }
    });
  }

  /**
   * Add predictive action
   */
  private addAction(action: PredictiveAction): void {
    this.actions.push(action);
  }

  /**
   * Get pending actions
   */
  getActions(): PredictiveAction[] {
    return this.actions;
  }

  /**
   * Clear actions
   */
  clearActions(): void {
    this.actions = [];
  }

  /**
   * Switch focus mode
   */
  switchMode(mode: FocusMode): void {
    this.currentMode = mode;
  }

  /**
   * Get current mode
   */
  getCurrentMode(): FocusMode {
    return this.currentMode;
  }

  /**
   * Get panel configuration for focus mode
   */
  getPanelConfiguration(mode: FocusMode): {
    leftPanelVisible: boolean;
    rightPanelVisible: boolean;
    bottomPanelVisible: boolean;
    aiPanelSection: string;
  } {
    switch (mode) {
      case "planning":
        return {
          leftPanelVisible: true,
          rightPanelVisible: true,
          bottomPanelVisible: true,
          aiPanelSection: "reasoning",
        };
      case "coding":
        return {
          leftPanelVisible: false,
          rightPanelVisible: true,
          bottomPanelVisible: false,
          aiPanelSection: "actions",
        };
      case "collaboration":
        return {
          leftPanelVisible: true,
          rightPanelVisible: false,
          bottomPanelVisible: true,
          aiPanelSection: "context",
        };
      case "intelligence":
        return {
          leftPanelVisible: true,
          rightPanelVisible: true,
          bottomPanelVisible: true,
          aiPanelSection: "recommendations",
        };
      default:
        return {
          leftPanelVisible: true,
          rightPanelVisible: true,
          bottomPanelVisible: false,
          aiPanelSection: "recommendations",
        };
    }
  }
}

// Global predictive UX engine instance
export const predictiveUX = new PredictiveUXEngine();

/**
 * Predictive UX helpers
 */
export function switchFocusMode(mode: FocusMode): void {
  predictiveUX.switchMode(mode);
}

export function getCurrentFocusMode(): FocusMode {
  return predictiveUX.getCurrentMode();
}

export function getPanelConfiguration() {
  return predictiveUX.getPanelConfiguration(predictiveUX.getCurrentMode());
}

export function getPredictiveActions(): PredictiveAction[] {
  const actions = predictiveUX.getActions();
  predictiveUX.clearActions();
  return actions;
}
