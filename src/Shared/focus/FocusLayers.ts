/**
 * Focus Layers
 * 
 * Implements focus layer system to reduce cognitive overload:
 * - Layer 1: Current Task (always dominant)
 * - Layer 2: Relevant AI (only contextual)
 * - Layer 3: Ambient Intelligence (minimally visible)
 * - Layer 4: Historical Activity (hidden by default)
 * 
 * Good AI workspaces are quiet, focused, contextual, intelligent, calm
 */

export type FocusLayer = "current-task" | "relevant-ai" | "ambient" | "historical";

export interface FocusLayerConfig {
  id: FocusLayer;
  name: string;
  priority: number;
  visible: boolean;
  opacity: number;
  zIndex: number;
}

export const FOCUS_LAYERS: Record<FocusLayer, FocusLayerConfig> = {
  "current-task": {
    id: "current-task",
    name: "Current Task",
    priority: 1,
    visible: true,
    opacity: 1,
    zIndex: 50,
  },
  "relevant-ai": {
    id: "relevant-ai",
    name: "Relevant AI",
    priority: 2,
    visible: true,
    opacity: 0.9,
    zIndex: 40,
  },
  "ambient": {
    id: "ambient",
    name: "Ambient Intelligence",
    priority: 3,
    visible: true,
    opacity: 0.6,
    zIndex: 30,
  },
  "historical": {
    id: "historical",
    name: "Historical Activity",
    priority: 4,
    visible: false,
    opacity: 0.4,
    zIndex: 20,
  },
};

/**
 * Focus Layer Manager
 */
class FocusLayerManager {
  private activeLayers: Set<FocusLayer> = new Set();
  private currentTask: string | null = null;

  constructor() {
    // Default: show current task and relevant AI
    this.activeLayers.add("current-task");
    this.activeLayers.add("relevant-ai");
    this.activeLayers.add("ambient");
  }

  /**
   * Set current task (Layer 1 - always dominant)
   */
  setCurrentTask(taskId: string | null): void {
    this.currentTask = taskId;
    if (taskId) {
      this.activeLayers.add("current-task");
    }
    this.emit("focus-layer:current-task", taskId);
  }

  /**
   * Get current task
   */
  getCurrentTask(): string | null {
    return this.currentTask;
  }

  /**
   * Show layer
   */
  showLayer(layer: FocusLayer): void {
    this.activeLayers.add(layer);
    this.emit("focus-layer:show", layer);
  }

  /**
   * Hide layer
   */
  hideLayer(layer: FocusLayer): void {
    // Never hide current task layer if there's a current task
    if (layer === "current-task" && this.currentTask) {
      return;
    }
    this.activeLayers.delete(layer);
    this.emit("focus-layer:hide", layer);
  }

  /**
   * Toggle layer
   */
  toggleLayer(layer: FocusLayer): void {
    if (this.activeLayers.has(layer)) {
      this.hideLayer(layer);
    } else {
      this.showLayer(layer);
    }
  }

  /**
   * Check if layer is visible
   */
  isLayerVisible(layer: FocusLayer): boolean {
    return this.activeLayers.has(layer);
  }

  /**
   * Get visible layers sorted by priority
   */
  getVisibleLayers(): FocusLayer[] {
    return Array.from(this.activeLayers).sort(
      (a, b) => FOCUS_LAYERS[a].priority - FOCUS_LAYERS[b].priority
    );
  }

  /**
   * Get layer configuration
   */
  getLayerConfig(layer: FocusLayer): FocusLayerConfig {
    return FOCUS_LAYERS[layer];
  }

  /**
   * Set focus mode (predefined layer combinations)
   */
  setFocusMode(mode: "focused" | "balanced" | "comprehensive"): void {
    this.activeLayers.clear();

    switch (mode) {
      case "focused":
        // Only current task
        this.activeLayers.add("current-task");
        break;
      case "balanced":
        // Current task + relevant AI + ambient
        this.activeLayers.add("current-task");
        this.activeLayers.add("relevant-ai");
        this.activeLayers.add("ambient");
        break;
      case "comprehensive":
        // All layers
        this.activeLayers.add("current-task");
        this.activeLayers.add("relevant-ai");
        this.activeLayers.add("ambient");
        this.activeLayers.add("historical");
        break;
    }

    this.emit("focus-layer:mode", mode);
  }

  /**
   * Get current focus mode
   */
  getFocusMode(): "focused" | "balanced" | "comprehensive" {
    const layers = this.activeLayers;

    if (layers.size === 1 && layers.has("current-task")) {
      return "focused";
    }

    if (layers.has("historical")) {
      return "comprehensive";
    }

    return "balanced";
  }

  /**
   * Emit custom event
   */
  private emit(event: string, ...args: unknown[]): void {
    if (typeof window === "undefined") return;
    window.dispatchEvent(new CustomEvent(event, { detail: args }));
  }
}

// Global focus layer manager instance
export const focusLayerManager = new FocusLayerManager();

/**
 * Focus layer helpers
 */
export function setCurrentTask(taskId: string | null): void {
  focusLayerManager.setCurrentTask(taskId);
}

export function getCurrentTask(): string | null {
  return focusLayerManager.getCurrentTask();
}

export function showFocusLayer(layer: FocusLayer): void {
  focusLayerManager.showLayer(layer);
}

export function hideFocusLayer(layer: FocusLayer): void {
  focusLayerManager.hideLayer(layer);
}

export function toggleFocusLayer(layer: FocusLayer): void {
  focusLayerManager.toggleLayer(layer);
}

export function isFocusLayerVisible(layer: FocusLayer): boolean {
  return focusLayerManager.isLayerVisible(layer);
}

export function getVisibleFocusLayers(): FocusLayer[] {
  return focusLayerManager.getVisibleLayers();
}

export function setFocusMode(mode: "focused" | "balanced" | "comprehensive"): void {
  focusLayerManager.setFocusMode(mode);
}

export function getFocusMode(): "focused" | "balanced" | "comprehensive" {
  return focusLayerManager.getFocusMode();
}
